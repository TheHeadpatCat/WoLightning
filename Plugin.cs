using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using WoLightning.Clients.Pishock;
using WoLightning.Clients.Webserver;
using WoLightning.Configurations;
using WoLightning.Game;
using WoLightning.Util;
using WoLightning.Util.Types;
using WoLightning.Windows;

namespace WoLightning;


public sealed class Plugin : IDalamudPlugin
{

    // General stuff
    private const string CommandName = "/wolightning";
    private const string CommandNameAlias = "/wol";
    private const string Failsafe = "/red";
    private const string OpenConfigFolder = "/wolfolder";

    public const int currentVersion = 1000;
    public const int configurationVersion = 1000;
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
    public TextLog TextLog { get; set; }

    // Gui Interfaces
    public readonly WindowSystem WindowSystem = new("WoLightning");
    private readonly BufferWindow BufferWindow = new BufferWindow();
    public MainWindow? MainWindow { get; set; }
    public ConfigWindow? ConfigWindow { get; set; }
    public MasterWindow? MasterWindow { get; set; }


    // Handler Classes
    public EmoteReaderHooks? EmoteReaderHooks { get; set; }
    public ClientPishock? ClientPishock { get; set; }
    public ClientWebserver? ClientWebserver { get; set; }
    public Authentification? Authentification { get; set; }
    public Configuration? Configuration { get; set; }



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
        ITargetManager targetManager
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


        MainWindow = new MainWindow(this);
        ConfigWindow = new ConfigWindow(this);
        

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
        Log("Running onLogin()");
        try
        {
            LocalPlayerCharacter = ClientState.LocalPlayer;
            LocalPlayer = new Player(LocalPlayerCharacter.Name.ToString(), (int)LocalPlayerCharacter.HomeWorld.Value.RowId);

            ConfigurationDirectoryPath = PluginInterface.GetPluginConfigDirectory() + "\\" + ClientState.LocalPlayer.Name;
            if (!Directory.Exists(ConfigurationDirectoryPath)) Directory.CreateDirectory(ConfigurationDirectoryPath);
            if (!Directory.Exists(ConfigurationDirectoryPath + "\\Presets")) Directory.CreateDirectory(ConfigurationDirectoryPath + "\\Presets");
            if (!Directory.Exists(ConfigurationDirectoryPath + "\\MasterPresets")) Directory.CreateDirectory(ConfigurationDirectoryPath + "\\MasterPresets");

            ConfigurationDirectoryPath += "\\";

            TextLog = new TextLog(this, ConfigurationDirectoryPath);

            Configuration = new Configuration();
            try
            {
                Configuration.Initialize(this, ConfigurationDirectoryPath);
            }
            catch (Exception e)
            {
                Configuration = new Configuration();
                Configuration.Save();
                sendNotif("Your Configuration has been reset due to an error!");
                Log(e);
            }

            try
            {
                Authentification = new Authentification(ConfigurationDirectoryPath);
                if (Authentification.Version < new Authentification().Version)
                {
                    Authentification = new Authentification(ConfigurationDirectoryPath, true);
                    sendNotif("Your Authentification has been reset due to a version upgrade!");
                }
            }
            catch (Exception e)
            {
                Authentification = new Authentification(ConfigurationDirectoryPath, true);
                sendNotif("Your Authentification has been reset due to an error!");
                Log(e);
            }

            LocalPlayer.Key = Authentification.ServerKey;
            

            EmoteReaderHooks = new EmoteReaderHooks(this);

            ClientWebserver = new ClientWebserver(this);
            ClientWebserver.Connect();

            ClientPishock = new ClientPishock(this);
            ClientPishock.createHttpClient();

            ConfigWindow.SetConfiguration(Configuration);
            MainWindow.Initialize();
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex.ToString());
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
        if (MasterWindow != null) WindowSystem.RemoveWindow(MasterWindow);
        WindowSystem?.RemoveWindow(BufferWindow);

        MainWindow?.Dispose();
        ConfigWindow?.Dispose();
        BufferWindow?.Dispose();

        EmoteReaderHooks?.Dispose();
        ClientWebserver?.Dispose();

        Configuration?.Dispose();
        Authentification?.Dispose();

        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler(CommandNameAlias);
        CommandManager.RemoveHandler(Failsafe);
        CommandManager.RemoveHandler(OpenConfigFolder);
    }

    public void validateShockerAssignments() // Goes through all Triggers and finds Shockers that are no longer saved - then deletes them.
    {
        List<Shocker> shockers = Authentification.PishockShockers;

        foreach (var property in typeof(Preset).GetProperties())
        {
            //Log($"{property.Name} - {property.PropertyType}");
            if (property.PropertyType == typeof(ShockOptions))
            {
                object? obj = property.GetValue(Configuration.ActivePreset);
                if (obj == null) continue;
                ShockOptions t = (ShockOptions)obj;

                if (shockers.Count == 0)
                {
                    t.Shockers.Clear();
                    continue;
                }

                bool[] marked = new bool[t.Shockers.Count];
                int i = 0;
                foreach (Shocker sh in t.Shockers)
                {
                    Log(sh);
                    if (shockers.Find(sh2 => sh.Code == sh2.Code) == null) marked[i] = true;
                    i++;
                }
                i = 0;
                foreach (bool del in marked)
                {

                    if (del) t.Shockers.RemoveAt(i);
                    i++;
                }
            }
        }
        Configuration.Save();
    }


    private void OnCommand(string command, string args)
    {
        ToggleMainUI();
    }
    private void OnCommandAlias(string command, string args)
    {
        OnCommand(command, args);
    }
    private void OnFailsafe(string command, string args)
    {
        isFailsafeActive = !isFailsafeActive;
        ClientPishock.cancelPendingRequests();
    }

    private void OnOpenConfigFolder(string command, string args)
    {
        Process.Start(new ProcessStartInfo { Arguments = ConfigurationDirectoryPath, FileName = "explorer.exe" });
    }
    private void DrawUI() => WindowSystem.Draw();
    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
    public void ToggleMasterUI() => MasterWindow.Toggle();
    public void ToggleMasterConfigUI() => MasterWindow.CopiedConfigWindow.Toggle();
    //public void ShowMasterUI() => MasterWindow.Open();

    #region Logging
    public void Log(string message)
    {
        
        PluginLog.Verbose(message);
        
    }

    public void Log(Object obj)
    {
        
        PluginLog.Verbose(obj.ToString());
        TextLog.Log(obj);
    }

    public void Log(string message, bool noText)
    {
        
        PluginLog.Verbose(message);
    }

    public void Log(Object obj, bool noText)
    {
        
        PluginLog.Verbose(obj.ToString());
    }


    public void Error(string message)
    {
        
        PluginLog.Error(message);
        TextLog.Log("--- ERROR: \n" + message);
    }

    public void Error(string message, Object obj)
    {
       
        PluginLog.Error(message);
        PluginLog.Error(obj.ToString());
        TextLog.Log("--- ERROR: \n" + message);
        TextLog.Log(obj);
    }

    public void Error(string message, bool noText)
    {
        
        PluginLog.Error(message);
    }

    public void Error(string message, Object obj, bool noText)
    {
       
        PluginLog.Error(message);
        PluginLog.Error(obj.ToString());
    }
    #endregion


    // Todo: Move all of these into a seperate Class
    public Notification getNotifTemp()
    {
        Notification result = new Notification();
        result.InitialDuration = new TimeSpan(0, 0, 7);
        result.Title = "Warrior of Lighting";
        result.Type = NotificationType.Warning;
        return result;
    }

    public void sendNotif(string content)
    {
        Log(content);
        Notification result = new Notification();
        result.InitialDuration = new TimeSpan(0, 0, 7);
        result.Title = "Warrior of Lighting";
        result.Type = NotificationType.Warning;
        result.Content = content;
        NotificationManager.AddNotification(result);
    }




}


/*
 * Random Notes
 * Add Positionals as a trigger?
 * 
 * 
 * 
 * 
 * 
 */