using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using WoLightning.Clients.OpenShock;
using WoLightning.Clients.Pishock;
using WoLightning.Clients.Webserver;
using WoLightning.Configurations;
using WoLightning.Game;
using WoLightning.Util;
using WoLightning.Util.Types;
using WoLightning.Windows;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning;


public sealed class Plugin : IDalamudPlugin
{

    // General stuff
    private const string CommandName = "/wolightning";
    private const string CommandNameAlias = "/wol";
    private const string CommandOpenConfig = "/wolc";
    private const string Failsafe = "/red";
    private const string OpenConfigFolder = "/wolfolder";

    public const int currentVersion = 520;
    public const String currentVersionString = "0.5.2.0";
    public const int configurationVersion = 501;
    public const string randomKey = "Currently Unused";

    public bool isFailsafeActive = false;
    public string? ConfigurationDirectoryPath { get; set; }
    public IPlayerCharacter LocalPlayerCharacter { get; set; }
    public Player LocalPlayer { get; set; }

    // Services
    public IDalamudPluginInterface PluginInterface { get; init; }
    private ICommandManager CommandManager { get; init; }
    public IPluginLog PluginLog { get; init; }
    public IFramework Framework { get; init; }
    public IGameNetwork GameNetwork { get; init; }
    public IChatGui ChatGui { get; init; }
    public IDutyState DutyState { get; init; }
    public IClientState ClientState { get; init; }
    public INotificationManager NotificationManager { get; init; }
    public IObjectTable ObjectTable { get; init; }
    public IGameInteropProvider GameInteropProvider { get; init; }
    public IPartyList PartyList { get; init; }
    public ITargetManager TargetManager { get; init; }
    public IDataManager DataManager { get; init; }
    public IToastGui ToastGui { get; init; }
    public IGameConfig GameConfig { get; init; }
    public TextLog TextLog { get; set; }

    // Gui Interfaces
    public readonly WindowSystem WindowSystem = new("WoLightning");
    private readonly BufferWindow BufferWindow = new BufferWindow();
    public MainWindow? MainWindow { get; set; }
    public ConfigWindow? ConfigWindow { get; set; }


    // Handler Classes
    public EmoteReaderHooks? EmoteReaderHooks { get; set; }
    public ClientPishock? ClientPishock { get; set; }
    public ClientOpenShock? ClientOpenShock { get; set; }
    public ClientWebserver? ClientWebserver { get; set; }
    public Authentification? Authentification { get; set; }
    public Configuration? Configuration { get; set; }
    public GameEmotes? GameEmotes { get; set; }
    public LanguageStrings? LanguageStrings { get; set; }
    public NotificationHandler? NotificationHandler { get; set; }

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        ITextureProvider textureProvider,
        IPluginLog pluginlog,
        IFramework framework,
        IGameNetwork gamenetwork,
        IChatGui chatgui,
        IDutyState dutystate,
        IClientState clientstate,
        INotificationManager notificationManager,
        IObjectTable objectTable,
        IGameInteropProvider gameInteropProvider,
        IPartyList partyList,
        ITargetManager targetManager,
        IDataManager dataManager,
        IGameConfig gameConfig
,
        IToastGui toastGui

        )
    {
        // Setup all Services
        PluginInterface = pluginInterface;
        CommandManager = commandManager;
        PluginLog = pluginlog;
        Framework = framework;
        GameNetwork = gamenetwork;
        ObjectTable = objectTable;
        ChatGui = chatgui;
        DutyState = dutystate;
        ClientState = clientstate;
        NotificationManager = notificationManager;
        GameInteropProvider = gameInteropProvider;
        PartyList = partyList;
        TargetManager = targetManager;
        DataManager = dataManager;
        ToastGui = toastGui;
        GameConfig = gameConfig;


        
        LanguageStrings = new(this);
        NotificationHandler = new(this);
        

        // Brio @Brio/Resources/GameDataProvider.cs#L27
        GameEmotes = new(this, dataManager.GetExcelSheet<Emote>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly());

        MainWindow = new(this);
        ConfigWindow = new(this);


        WindowSystem.AddWindow(BufferWindow);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(ConfigWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the main window."
        });
        CommandManager.AddHandler(CommandNameAlias, new CommandInfo(OnCommandAlias)
        {
            HelpMessage = "Alias for /wolighting."
        });
        CommandManager.AddHandler(CommandOpenConfig, new CommandInfo(OnCommandConfiguration)
        {
            HelpMessage = "Opens the Configuration window."
        });
        CommandManager.AddHandler(Failsafe, new CommandInfo(OnFailsafe)
        {
            HelpMessage = "Stops the plugin."
        });
        CommandManager.AddHandler(OpenConfigFolder, new CommandInfo(OnOpenConfigFolder)
        {
            HelpMessage = "Opens the configuration folder."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;


        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        Framework.Update += onUpdate;

        ClientState.Login += onLogin;
        ClientState.Logout += onLogout;
        PluginLog.Verbose("Finished initializing Plugin.");
    }

    

    private void onUpdate(IFramework framework)
    {
        if (LocalPlayerCharacter == null && ClientState.LocalPlayer != null) onLogin();
    }

    private void onLogin()
    {
        Log(3,"Running onLogin()");
        try
        {

            LocalPlayerCharacter = ClientState.LocalPlayer;
            LocalPlayer = new Player(LocalPlayerCharacter.Name.ToString(), (int)LocalPlayerCharacter.HomeWorld.Value.RowId);

            if (!File.Exists(PluginInterface.GetPluginConfigDirectory() + "\\version")) // Either new installation or old data - either way, purge.
            {
                Log(1,"Missing Version file - purging folder.");
                foreach(var dir in Directory.EnumerateDirectories(PluginInterface.GetPluginConfigDirectory()))
                {
                    Directory.Delete(dir, true);
                }
                File.WriteAllText(PluginInterface.GetPluginConfigDirectory() + "\\version", currentVersion + "");
            }

            int version = int.Parse(File.ReadAllText(PluginInterface.GetPluginConfigDirectory() + "\\version"));

            ConfigurationDirectoryPath = PluginInterface.GetPluginConfigDirectory() + "\\" + ClientState.LocalPlayer.Name;
            if (!Directory.Exists(ConfigurationDirectoryPath)) Directory.CreateDirectory(ConfigurationDirectoryPath);
            if (!Directory.Exists(ConfigurationDirectoryPath + "\\Presets")) Directory.CreateDirectory(ConfigurationDirectoryPath + "\\Presets");
            if (!Directory.Exists(ConfigurationDirectoryPath + "\\MasterPresets")) Directory.CreateDirectory(ConfigurationDirectoryPath + "\\MasterPresets");

            ConfigurationDirectoryPath += "\\";

            


            TextLog = new TextLog(this, ConfigurationDirectoryPath);

            ClientWebserver = new ClientWebserver(this);
            ClientPishock = new ClientPishock(this);
            ClientOpenShock = new ClientOpenShock(this);

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
                Log(1,e);
            }

            try
            {
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
                Log(1, e);
            }

            LocalPlayer.Key = Authentification.ServerKey;


            //ClientWebserver.Connect();
            ClientPishock.Setup();
            ClientOpenShock.Setup();

            EmoteReaderHooks = new EmoteReaderHooks(this);

            ConfigWindow.SetConfiguration(Configuration);
            MainWindow.Initialize();

            Log(3,"The Game is running " + (ClientLanguage)GameConfig.System.GetUInt("Language") + " Language");

            if (version < currentVersion)
            {
                File.Delete(PluginInterface.GetPluginConfigDirectory() + "\\version");
                File.WriteAllText(PluginInterface.GetPluginConfigDirectory() + "\\version", currentVersion + "");

                if(Configuration.DebugLevel < DebugLevel.Verbose) Configuration.DebugLevel = DebugLevel.Verbose;
            }

        }
        catch (Exception ex)
        {
            PluginLog.Error(ex.StackTrace!);
            PluginLog.Error("Something went terribly wrong!!!");
        }
    }

    public void onLogout(int type, int code)
    {
        EmoteReaderHooks.Dispose();
        ClientWebserver.Dispose();

        Configuration.Dispose();
        Authentification.Dispose();
        ConfigWindow.SetConfiguration(null);
    }


    public void Dispose()
    {
        if (MainWindow != null) WindowSystem.RemoveWindow(MainWindow);
        if (ConfigWindow != null) WindowSystem.RemoveWindow(ConfigWindow);
        WindowSystem?.RemoveWindow(BufferWindow);

        MainWindow?.Dispose();
        ConfigWindow?.Dispose();
        BufferWindow?.Dispose();

        EmoteReaderHooks?.Dispose();
        ClientWebserver?.Dispose();
        ClientPishock?.Dispose();
        ClientOpenShock?.Dispose();

        Configuration?.Dispose();
        Authentification?.Dispose();



        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler(CommandNameAlias);
        CommandManager.RemoveHandler(Failsafe);
        CommandManager.RemoveHandler(OpenConfigFolder);
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
        isFailsafeActive = !isFailsafeActive;
        if (isFailsafeActive) ChatGui.Print("Failsafe is active!\nStopping all requests...");
        else ChatGui.Print("Failsafe deactivated.");
    }


    private void OnOpenConfigFolder(string command, string args)
    {
        Process.Start(new ProcessStartInfo { Arguments = ConfigurationDirectoryPath, FileName = "explorer.exe" });
    }
    private void DrawUI() => WindowSystem.Draw();
    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();

    #region Logging
    public void Log(DebugLevel level, string message)
    {
        if (Configuration == null) return;
        if (level > Configuration.DebugLevel) return;
        switch (level)
        {
            case DebugLevel.Dev:
                PluginLog.Verbose(message);
                TextLog?.Log("[Dev] " + message);
                break;

            case DebugLevel.Verbose:
                PluginLog.Verbose(message);
                TextLog?.Log("[Verbose] " + message);
                break;

            case DebugLevel.Debug:
                PluginLog.Debug(message);
                TextLog?.Log("[Debug] " + message);
                break;

            case DebugLevel.Info:
                PluginLog.Info(message);
                TextLog?.Log("[Info] " + message);
                break;

            case DebugLevel.None: default: break;
        }
    }

    public void Log(DebugLevel level, Object obj)
    {
        string? message = obj.ToString();
        if(message != null) Log(level, message);
    }

    public void Log(int level, string message)
    {
        Log((DebugLevel)level, message);
    }

    public void Log(int level, Object obj)
    {
        string? message = obj.ToString();
        if (message != null) Log(level, message);
    }

    public void Error(string message)
    {
        PluginLog.Error(message);
        TextLog?.Log("====================" +
                     "\n[ERROR] " + message + "" +
                     "\n====================");
    }

    public void Error(Object obj)
    {
        string? message = obj.ToString();
        if (message != null) Error(message);
    }
    #endregion

}