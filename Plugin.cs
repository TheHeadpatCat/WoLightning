using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using WoLightning.Clients.Intiface;
using WoLightning.Clients.OpenShock;
using WoLightning.Clients.Pishock;
using WoLightning.Configurations;
using WoLightning.Game;
using WoLightning.Util.Types;
using WoLightning.Windows;
using WoLightning.WoL_Plugin.Clients.Webserver;
using WoLightning.WoL_Plugin.Configurations;
using WoLightning.WoL_Plugin.Util;
using WoLightning.WoL_Plugin.Windows;
using Version = WoLightning.WoL_Plugin.Util.Types.Version;

namespace WoLightning;


public sealed class Plugin : IDalamudPlugin
{
    public IDalamudPluginInterface PluginInterface { get; init; }


    // General stuff
    private const string CommandName = "/wolightning";
    private const string CommandNameAlias = "/wol";
    private const string CommandOpenConfig = "/wolc";
    private const string Failsafe = "/red";
    private const string OpenConfigFolder = "/wolfolder";
    private const string OpenShockRemote = "/wolremote";
    private const string SwapPreset = "/wolpreset";

    public static readonly Version CurrentVersion = new(7, 0, 2, 'b');

    public const string randomKey = "Currently Unused";

    public bool IsEnabled = false;
    public bool IsFailsafeActive = false;
    public string? ConfigurationDirectoryPath { get; set; }
    public IPlayerCharacter LocalPlayerCharacter { get; set; }
    public Player LocalPlayer { get; set; }


    // Gui Interfaces
    public readonly WindowSystem WindowSystem = new("WoLightning");
    private readonly BufferWindow BufferWindow = new();
    public MainWindow? MainWindow { get; set; }
    public ConfigWindow? ConfigWindow { get; set; }
    public ShockRemoteWindow? ShockRemoteWindow { get; set; }
    public ControlWindow? ControlWindow { get; set; }
    public DebugWindow? DebugWindow { get; set; }


    // Handler Classes

    public ClientIntiface? ClientIntiface { get; set; }
    public EmoteReaderHooks? EmoteReaderHooks { get; set; }
    public ClientPishock? ClientPishock { get; set; }
    public ClientOpenShock? ClientOpenShock { get; set; }
    public ClientWebserver? ClientWebserver { get; set; }
    public Authentification? Authentification { get; set; }
    public Configuration? Configuration { get; set; }
    public ControlSettings? ControlSettings { get; set; }
    public GameEmotes? GameEmotes { get; set; }
    public NotificationHandler? NotificationHandler { get; set; }

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        PluginInterface = pluginInterface;

        NotificationHandler = new(this);

        // Brio @Brio/Resources/GameDataProvider.cs#L27
        GameEmotes = new(this, Service.DataManager.GetExcelSheet<Emote>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly());

        MainWindow = new(this);
        ConfigWindow = new(this);
        ShockRemoteWindow = new(this);
        ControlWindow = new(this);

        WindowSystem.AddWindow(BufferWindow);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(ShockRemoteWindow);
        WindowSystem.AddWindow(ControlWindow);

        Service.CommandManager.AddHandler(Failsafe, new CommandInfo(OnFailsafe)
        { HelpMessage = "Stops the plugin." });
        Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        { HelpMessage = "Opens the main window." });
        Service.CommandManager.AddHandler(CommandNameAlias, new CommandInfo(OnCommandAlias)
        { HelpMessage = "Alias for /wolighting." });
        Service.CommandManager.AddHandler(CommandOpenConfig, new CommandInfo(OnCommandConfiguration)
        { HelpMessage = "Opens the Configuration window." });
        Service.CommandManager.AddHandler(OpenConfigFolder, new CommandInfo(OnOpenConfigFolder)
        { HelpMessage = "Opens the Configuration folder." });
        Service.CommandManager.AddHandler(OpenShockRemote, new CommandInfo(OnOpenShockRemote)
        { HelpMessage = "Opens the Shock Remote Window." });
        Service.CommandManager.AddHandler(SwapPreset, new CommandInfo(OnSwapPreset)
        { HelpMessage = "Swaps to the named Preset." });

        Service.PluginInterface.UiBuilder.Draw += DrawUI;

        Service.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        Service.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        Service.Framework.Update += OnUpdate;

        Service.ClientState.Login += OnLogin;
        Service.ClientState.Logout += OnLogout;
        Service.PluginLog.Verbose("Finished initializing Plugin.");
    }

    ~Plugin()
    {
        Dispose();
    }


    private void OnUpdate(IFramework framework)
    {
        if (LocalPlayerCharacter == null && Service.ObjectTable.LocalPlayer != null) OnLogin();
        if (LocalPlayerCharacter == null || !LocalPlayerCharacter.IsValid()) LocalPlayerCharacter = Service.ObjectTable.LocalPlayer;
    }

    private void OnLogin()
    {
        try
        {

            LocalPlayerCharacter = Service.ObjectTable.LocalPlayer;
            LocalPlayer = new Player(LocalPlayerCharacter.Name.ToString(), (int)LocalPlayerCharacter.HomeWorld.Value.RowId);

            if (!File.Exists(Service.PluginInterface.GetPluginConfigDirectory() + "\\version")) // Either new installation or old data - either way, purge.
            {
                foreach (var dir in Directory.EnumerateDirectories(Service.PluginInterface.GetPluginConfigDirectory()))
                {
                    Directory.Delete(dir, true);
                }
                File.WriteAllText(Service.PluginInterface.GetPluginConfigDirectory() + "\\version", CurrentVersion + "");
            }

            ConfigurationDirectoryPath = Service.PluginInterface.GetPluginConfigDirectory() + "\\" + Service.ObjectTable.LocalPlayer.Name;
            if (!Directory.Exists(ConfigurationDirectoryPath)) Directory.CreateDirectory(ConfigurationDirectoryPath);
            if (!Directory.Exists(ConfigurationDirectoryPath + "\\Presets")) Directory.CreateDirectory(ConfigurationDirectoryPath + "\\Presets");
            if (!Directory.Exists(ConfigurationDirectoryPath + "\\MasterPresets")) Directory.CreateDirectory(ConfigurationDirectoryPath + "\\MasterPresets");

            ConfigurationDirectoryPath += "\\";

            Logger.SetupFile();

            ClientWebserver = new ClientWebserver(this);
            ClientPishock = new ClientPishock(this);
            ClientOpenShock = new ClientOpenShock(this);
            ClientIntiface = new ClientIntiface(this);

            Configuration = new Configuration();
            try
            {
                Configuration.Initialize(this, ConfigurationDirectoryPath);
            }
            catch (Exception e)
            {
                Configuration = new Configuration();
                Configuration.Save();
                NotificationHandler.send("Your Configuration has been reset due to an error!");
                Logger.Error(e);
            }

            try
            {
                // todo: update to new versioning
                Authentification = new Authentification(ConfigurationDirectoryPath);
                if (Authentification.Version < new Authentification().Version)
                {
                    Authentification = new Authentification(ConfigurationDirectoryPath, true);
                    NotificationHandler.send("Your Authentification has been reset due to a version upgrade!");
                }
            }
            catch (Exception e)
            {
                Authentification = new Authentification(ConfigurationDirectoryPath, true);
                NotificationHandler.send("Your Authentification has been reset due to an error!");
                Logger.Error(e);
            }

            LocalPlayer.Key = Authentification.ServerKey;

            try
            {
                ControlSettings = new ControlSettings(ConfigurationDirectoryPath);
                ControlSettings.Load();
            }
            catch (Exception e)
            {
                ControlSettings = new ControlSettings(ConfigurationDirectoryPath, true);
                NotificationHandler.send("Your Control Settings have been reset due to an error!");
                Logger.Error(e.Message);
            }

            ControlSettings.Save();

            //ClientWebserver.Connect();
            if (Authentification.PishockEnabled) ClientPishock.Setup();
            if (Authentification.OpenShockEnabled) ClientOpenShock.Setup();
            if (Authentification.IntifaceEnabled) ClientIntiface.Setup();

            EmoteReaderHooks = new EmoteReaderHooks(this);

            ConfigWindow.SetConfiguration(Configuration);
            ControlSettings.Initialize(this);
            MainWindow.Initialize();

            Logger.Log(3, "The Game is running " + (ClientLanguage)Service.GameConfig.System.GetUInt("Language") + " Language");

            string fileVersionString = File.ReadAllText(Service.PluginInterface.GetPluginConfigDirectory() + "\\version");

            Version savedVersion;
            bool versionFail = false;
            try
            {
                savedVersion = new Version(fileVersionString);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                versionFail = true;
                savedVersion = new Version(0, 0, 0);
            }

            if (versionFail || CurrentVersion.NeedsUpdate(savedVersion) > Version.NeedUpdateState.Keep)
            {
                File.Delete(Service.PluginInterface.GetPluginConfigDirectory() + "\\version");
                File.WriteAllText(Service.PluginInterface.GetPluginConfigDirectory() + "\\version", CurrentVersion.ToString());

                if (Configuration.DebugLevel < DebugLevel.Verbose) Configuration.DebugLevel = DebugLevel.Verbose;
            }

            if (Configuration.IsDeveloper)
            {
                MainWindow.Toggle();
                ConfigWindow.Toggle();
                DebugWindow = new(this);
                WindowSystem.AddWindow(DebugWindow);
                DebugWindow.Toggle();
            }

        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex.StackTrace!);
            Service.PluginLog.Error("Something went terribly wrong!!!");
        }
    }

    public void OnLogout(int type, int code)
    {
        EmoteReaderHooks.Dispose();
        ClientWebserver.Dispose();

        Configuration.Dispose();
        Authentification.Dispose();
        ConfigWindow.SetConfiguration(null);
    }

    public void Dispose()
    {
        /* Old Way
        if (MainWindow != null) WindowSystem.RemoveWindow(MainWindow);
        if (ConfigWindow != null) WindowSystem.RemoveWindow(ConfigWindow);
        if (DebugWindow != null) WindowSystem.RemoveWindow(DebugWindow);
        if (ControlWindow != null) WindowSystem.RemoveWindow(ControlWindow);
        if (BufferWindow != null && WindowSystem.Windows.Contains(BufferWindow)) WindowSystem?.RemoveWindow(BufferWindow);
        */

        WindowSystem.RemoveAllWindows();

        MainWindow?.Dispose();
        ConfigWindow?.Dispose();
        BufferWindow?.Dispose();
        ShockRemoteWindow?.Dispose();
        ControlWindow?.Dispose();
        DebugWindow?.Dispose();

        EmoteReaderHooks?.Dispose();
        ClientWebserver?.Dispose();
        ClientPishock?.Dispose();
        ClientOpenShock?.Dispose();
        ClientIntiface?.Dispose();

        Configuration?.Dispose();
        Authentification?.Dispose();



        Service.CommandManager.RemoveHandler(CommandName);
        Service.CommandManager.RemoveHandler(CommandNameAlias);
        Service.CommandManager.RemoveHandler(Failsafe);
        Service.CommandManager.RemoveHandler(OpenConfigFolder);
        Service.CommandManager.RemoveHandler(OpenShockRemote);
        Service.CommandManager.RemoveHandler(SwapPreset);
    }

    private void OnCommand(string command, string args)
    {
        ToggleMainUI();
    }
    private void OnCommandAlias(string command, string args)
    {
        OnCommand(command, args);
    }
    private void OnCommandConfiguration(string command, string arguments)
    {
        ToggleConfigUI();
    }
    private void OnFailsafe(string command, string args)
    {

        if (ControlSettings.SafewordDisabled)
        {
            Service.ChatGui.PrintError("I know you disabled the safeword, but i just cant get myself to actually allow that... sorry!");
        }

        IsFailsafeActive = !IsFailsafeActive;
        if (IsFailsafeActive) Service.ChatGui.Print("Failsafe is active!\nStopping all requests...");
        else Service.ChatGui.Print("Failsafe deactivated.");
    }


    private void OnOpenConfigFolder(string command, string args)
    {
        Process.Start(new ProcessStartInfo { Arguments = Service.PluginInterface.GetPluginConfigDirectory(), FileName = "explorer.exe" });
    }

    private void OnOpenShockRemote(string command, string arguments)
    {
        ShockRemoteWindow.Toggle();
    }
    private void OnSwapPreset(string command, string arguments)
    {

        if (Configuration.IsLockedByController)
        {
            Service.ChatGui.PrintError($"{ControlSettings.Controller.Name} doesn't allow you to swap Presets!");
            return;
        }

        arguments = arguments.Trim();
        if (Configuration.loadPreset(arguments)) Service.ChatGui.Print($"Loaded Preset {Configuration.ActivePreset.Name}!");
        else Service.ChatGui.PrintError($"Cannot find a Preset named \"{arguments}\"!\n(This Command is case-sensitive!)");
    }
    private void DrawUI() => WindowSystem.Draw();
    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}



/* Unused Leash Code
        private void HandleNetworkMessage(nint dataPtr, ushort OpCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
        {
            Plugin.PluginLog.Info($"(Net) dataPtr: {dataPtr} - OpCode: {OpCode} - ActorId: {sourceActorId} - TargetId: {targetActorId} - direction: ${direction.ToString()}");

            //if (MasterCharacter != null && MasterCharacter.IsValid() && MasterCharacter.Name + "#" + MasterCharacter.HomeWorld.Id == Plugin.Configuration.MasterNameFull) return;

            var targetOb = Plugin.ObjectTable.FirstOrDefault(x => (ulong)x.GameObjectId == targetActorId);
            if (targetOb != null && targetOb.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player)
            {
                if (((IPlayerCharacter)targetOb).Name + "#" + ((IPlayerCharacter)targetOb).HomeWorld.Id == Plugin.Configuration.MasterNameFull)
                {
                    MasterCharacter = (IPlayerCharacter)targetOb;
                    Plugin.PluginLog.Info("Found Master Signature!");
                    Plugin.PluginLog.Info(MasterCharacter.ToString());
                    Plugin.GameNetwork.NetworkMessage -= HandleNetworkMessage;
                    return;
                }
                //Plugin.PluginLog.Info(targetOb.ToString());
            }
        }*/