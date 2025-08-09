using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;
using WoLightning.Clients.Webserver;
using WoLightning.Util;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Clients.OpenShock;
using WoLightning.WoL_Plugin.Clients.Pishock;
using WoLightning.WoL_Plugin.Util;
using static WoLightning.Clients.OpenShock.ClientOpenShock;
using static WoLightning.Clients.Pishock.ClientPishock;

namespace WoLightning.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private int presetIndex = 0;

    private static Vector4 ColorGreen = new(0, 1, 0, 1);
    private static Vector4 ColorRed = new(1, 0, 0, 1);
    private static Vector4 ColorGray = new(0.7f, 0.7f, 0.7f, 1);

    private float WindowWidth = 0;

    private bool isEulaModalActive = false;
    private TimerPlus eulaTimer = new TimerPlus();

    private bool isPishockMenuOpen = true;

    public bool IsEnabled = false;

    public MainWindow(Plugin plugin)
        : base($"Warrior of Lightning - v{Plugin.currentVersionString}##Main", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize)
    {

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(270, 250),
            MaximumSize = new Vector2(320, 2000)
        };

        Plugin = plugin;
        eulaTimer.Interval = 16000;
        eulaTimer.AutoReset = false;
    }

    public void Dispose()
    {
        if (this.IsOpen) this.Toggle();

    }

    public void Initialize()
    {
        IsEnabled = Plugin.Configuration.ActivateOnStart;
        if (IsEnabled) Plugin.Configuration.ActivePreset.StartRules();
    }

    public override async void Draw()
    {
        WindowWidth = ImGui.GetWindowWidth();
        try
        {

            if (Plugin == null || Plugin.Authentification == null || Plugin.Configuration == null)
            {
                ImGui.Text("There was an Issue loading a vital Asset of the plugin.\nPlease login with a character and reload the plugin if needed.");
                return;
            }

            DrawShockerAPI();
            ImGui.Separator();

            //DrawWebserverAPI();
            //ImGui.Separator();

            DrawControlPanel();

            DrawAccountPanel();

            DrawEulaWindow();
        }
        catch (Exception e)
        {
            Logger.Log(0, "Something went terribly wrong!");
            Logger.Error(e);
        }

    }


    private async void DrawShockerAPI()
    {
        ImGui.BeginGroup();
        ImGui.Text("Pishock API");

        ImGui.SameLine();
        ImGui.TextDisabled(" (?)       ");
        if (ImGui.IsItemHovered()) { ImGui.SetTooltip("This is where your Shocks get sent to, if you are using a Pishock Account.\nIf you are not connected to it, you cannot receive shocks."); }
        switch (Plugin.ClientPishock.Status)
        {
            case ConnectionStatusPishock.NotStarted:
                ImGui.TextColored(ColorGray, "No Userdata."); break;

            case ConnectionStatusPishock.Connecting:
                ImGui.TextColored(ColorGray, "Connecting..."); break;
            case ConnectionStatusPishock.ConnectedNoInfo:
                ImGui.TextColored(ColorGray, "Getting Information..."); break;


            case ConnectionStatusPishock.InvalidUserdata:
                ImGui.TextColored(ColorRed, "Invalid Userdata!"); break;
            case ConnectionStatusPishock.Unavailable:
                ImGui.TextColored(ColorRed, "Unable to Connect!"); break;
            case ConnectionStatusPishock.FatalError:
                ImGui.TextColored(ColorRed, "Fatal Error!"); break;
            case ConnectionStatusPishock.ExceededAttempts:
                ImGui.TextColored(ColorRed, "Cannot Connect.\nPlease Restart the Plugin."); break;

            case ConnectionStatusPishock.Connected:
                ImGui.TextColored(ColorGreen, $"Connected!"); break;
        }
        ImGui.EndGroup();

        ImGui.SameLine();

        ImGui.BeginGroup();
        ImGui.Text("OpenShock API");
        ImGui.SameLine();
        ImGui.TextDisabled(" (?)");
        if (ImGui.IsItemHovered()) { ImGui.SetTooltip("This is the Server that Shocks get sent to if you are using a OpenShock Account.\nIf you are not connected to it, you cannot receive shocks."); }

        switch (Plugin.ClientOpenShock.Status)
        {
            case ConnectionStatusOpenShock.NotStarted:
                ImGui.TextColored(ColorGray, "No Userdata."); break;

            case ConnectionStatusOpenShock.Connecting:
                ImGui.TextColored(ColorGray, "Connecting..."); break;
            case ConnectionStatusOpenShock.ConnectedNoInfo:
                ImGui.TextColored(ColorGray, "Getting Information..."); break;


            case ConnectionStatusOpenShock.InvalidUserdata:
                ImGui.TextColored(ColorRed, "Invalid Userdata!"); break;
            case ConnectionStatusOpenShock.Unavailable:
                ImGui.TextColored(ColorRed, "Unable to Connect!"); break;
            case ConnectionStatusOpenShock.FatalError:
                ImGui.TextColored(ColorRed, "Fatal Error!"); break;

            case ConnectionStatusOpenShock.Connected:
                ImGui.TextColored(ColorGreen, $"Connected!"); break;
        }

        ImGui.EndGroup();



    }
    private async void DrawWebserverAPI()
    {
        ImGui.Text("Webserver API");

        ImGui.SameLine();
        ImGui.TextDisabled(" (?)");
        if (ImGui.IsItemHovered()) { ImGui.SetTooltip("The Webserver is used for things between players, like sharing Presets or Mastermode.\nIt has no impact on the Pishock stuff!"); }

        switch (Plugin.ClientWebserver.Status)
        {
            case ConnectionStatusWebserver.NotStarted:
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), "Starting Plugin..."); break;

            case ConnectionStatusWebserver.Connecting:
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), "Connecting to web server..."); break;

            case ConnectionStatusWebserver.EulaNotAccepted:
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), "Eula isn't accepted. ");
                ImGui.SameLine();
                if (ImGui.Button("Open"))
                {
                    eulaTimer.Start();
                    isEulaModalActive = true;
                    ImGui.OpenPopup("WoL Webserver Eula##webserverEula");
                }
                break;

            case ConnectionStatusWebserver.Outdated:
                ImGui.TextColored(new Vector4(1, 0, 0, 1), "Can't Connect - Outdated Version!"); break;
            case ConnectionStatusWebserver.WontRespond:
            //ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Offline.\nRetrying in {(int)TimeSpan.FromMilliseconds(Plugin.ClientWebserver.PingTimer.TimeLeft).TotalSeconds}s..."); break;
            case ConnectionStatusWebserver.FatalError:
                ImGui.TextColored(new Vector4(1, 0, 0, 1), "Something went wrong!\nPlease check the /xllog window."); break;
            case ConnectionStatusWebserver.InvalidKey:
                ImGui.TextColored(new Vector4(1, 0, 0, 1), "The saved key does not match with the server.\nYou may only reset it by asking the dev."); break;
            case ConnectionStatusWebserver.DevMode:
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 0.7f), "Can't Connect - Webserver is in construction."); break;

            case ConnectionStatusWebserver.Connected:
                ImGui.TextColored(new Vector4(0, 1, 0, 1), $"Connected!"); break;

            default:
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 0.7f), "Unknown Response."); break;

        }
    }

    private async void DrawControlPanel()
    {
        ImGui.BeginDisabled();
        presetIndex = Plugin.Configuration.ActivePresetIndex;
        if (presetIndex == -1) Plugin.Configuration.Save();
        ImGui.SetNextItemWidth(WindowWidth - 15);
        if (ImGui.Combo("", ref presetIndex, [.. Plugin.Configuration.PresetNames], Plugin.Configuration.Presets.Count))
        {
            Plugin.Configuration.loadPreset(Plugin.Configuration.PresetNames[presetIndex]);
        }
        ImGui.EndDisabled();

        if (Plugin.isFailsafeActive)
        {
            ImGui.TextColored(ColorRed, "Failsafe is active.\nType /red to disable it.");
        }

        if (IsEnabled)
        {
            if (ImGui.Button("Stop Plugin", new Vector2(270, 40))) // Todo: Color coding
            {
                IsEnabled = false;
                Plugin.Configuration.ActivePreset.StopRules();
            }
        }

        if (!IsEnabled)
        {
            if (ImGui.Button("Start Plugin", new Vector2(270, 40)))
            {
                IsEnabled = true;
                Plugin.Configuration.ActivePreset.StartRules();
            }
        }


        var ActivateOnStart = Plugin.Configuration.ActivateOnStart;

        if (ImGui.Checkbox("Activate whenever the game starts.", ref ActivateOnStart))
        {
            Plugin.Configuration.ActivateOnStart = ActivateOnStart;
            Plugin.Configuration.Save();
        }

        if (ImGui.Button("Open Shocker Remote", new Vector2(ImGui.GetWindowSize().X - 10, 25)))
        {
            Plugin.ShockRemoteWindow.Toggle();
        }

        //if (Plugin.Authentification.isDisallowed) ImGui.EndDisabled();
        if (ImGui.Button("Open Trigger Configuration", new Vector2(ImGui.GetWindowSize().X - 10, 25)))
        {
            Plugin.ToggleConfigUI();
        }

        /*
        if (Plugin.ClientWebserver.Status != ConnectionStatusWebserver.Connected) ImGui.BeginDisabled();
        if (ImGui.Button("Master Mode", new Vector2(ImGui.GetWindowSize().X - 15, 25)))
        {
            Plugin.ToggleMasterUI();
        }
        if (Plugin.ClientWebserver.Status != ConnectionStatusWebserver.Connected) ImGui.EndDisabled();

        if (Plugin.ClientWebserver.Status != ConnectionStatusWebserver.Connected && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) { ImGui.SetTooltip($"You need to be Connected to the Webserver\nto access Mastermode!"); }
        */
    }

    private async void DrawAccountPanel()
    {
        ImGui.SetNextItemWidth(WindowWidth - 15);
        if (ImGui.CollapsingHeader("Account & Shockers", ImGuiTreeNodeFlags.CollapsingHeader))
        {
            if (ImGui.RadioButton("Pishock", isPishockMenuOpen)) isPishockMenuOpen = true;
            ImGui.SameLine();
            if (ImGui.RadioButton("OpenShock", !isPishockMenuOpen)) isPishockMenuOpen = false;

            if (isPishockMenuOpen) DrawPishockAccount();
            else DrawOpenShockAccount();

        }
    }

    private async void DrawPishockAccount()
    {
        ImGui.SetNextItemWidth(WindowWidth - 15);
        //if (Plugin.Authentification.isDisallowed) ImGui.BeginDisabled();
        var PishockNameField = Plugin.Authentification.PishockName;
        if (ImGui.InputTextWithHint("##PishockUsername", "Pishock Username", ref PishockNameField, 24))
            Plugin.Authentification.PishockName = PishockNameField;
        ImGui.SetNextItemWidth(WindowWidth - 15);
        var PishockApiField = Plugin.Authentification.PishockApiKey;
        if (ImGui.InputTextWithHint("##PishockAPIKey", "API Key from \"Account\"", ref PishockApiField, 64, ImGuiInputTextFlags.Password))
            Plugin.Authentification.PishockApiKey = PishockApiField;

        if (ImGui.Button("Save & Connect##SavePishock", new Vector2(ImGui.GetWindowSize().X - 15, 25)))
        {
            Plugin.Authentification.Save();
            Plugin.ClientPishock.Setup();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (Plugin.ClientPishock.Status == ConnectionStatusPishock.ConnectedNoInfo || Plugin.ClientPishock.Status == ConnectionStatusPishock.Connecting)
        {
            ImGui.Text("Getting Shocker Information...");
            return;
        }

        int x = 0;
        ImGui.Text("Available Shockers:");
        while (Plugin.Authentification.PishockShockers.Count > x)
        {
            ShockerPishock target = Plugin.Authentification.PishockShockers[x];
            string tName = target.name;


            if (ImGui.Button("Test##TestShocker" + target.getInternalId()))
            {
                ShockOptions temp = new ShockOptions(1, 35, 1);
                temp.ShockersPishock.Add(target);
                Plugin.ClientPishock.SendRequest(temp);
            }
            ImGui.SameLine();
            if (!target.isPersonal)
            {
                ImGui.BeginGroup();
                ImGui.Text(target.username);
                if (target.isPaused) ImGui.TextColored(ColorGray, "[Paused] " + target.name);
                else ImGui.TextColored(ColorGreen, target.name);
                ImGui.EndGroup();
                ImGui.Separator();
                x++;
                continue;
            }

            if (target.isPaused) ImGui.TextColored(ColorGray, "[Paused] " + target.name);
            else ImGui.TextColored(ColorGreen, target.name);
            ImGui.Separator();
            x++;
        }
    }
    private async void DrawOpenShockAccount()
    {
        ImGui.SetNextItemWidth(WindowWidth - 15);
        //if (Plugin.Authentification.isDisallowed) ImGui.BeginDisabled();
        var OpenShockURLField = Plugin.Authentification.OpenShockURL;
        if (ImGui.InputTextWithHint("##OpenShockUrl", "OpenShock URL", ref OpenShockURLField, 64))
            Plugin.Authentification.OpenShockURL = OpenShockURLField;
        ImGui.SetNextItemWidth(WindowWidth - 15);
        var OpenShockApiField = Plugin.Authentification.OpenShockApiKey;
        if (ImGui.InputTextWithHint("##OpenShockApiField", "API Key from \"Account\"", ref OpenShockApiField, 96, ImGuiInputTextFlags.Password))
            Plugin.Authentification.OpenShockApiKey = OpenShockApiField;

        if (ImGui.Button("Save & Connect##SaveOpenShock", new Vector2(ImGui.GetWindowSize().X - 15, 25)))
        {
            Plugin.Authentification.Save();
            Plugin.ClientOpenShock.Setup();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (Plugin.ClientOpenShock.Status == ConnectionStatusOpenShock.ConnectedNoInfo || Plugin.ClientOpenShock.Status == ConnectionStatusOpenShock.Connecting)
        {
            ImGui.Text("Getting Shocker Information...");
            return;
        }

        int x = 0;
        ImGui.Text("Available Shockers:");
        while (Plugin.Authentification.OpenShockShockers.Count > x)
        {
            ShockerOpenShock target = Plugin.Authentification.OpenShockShockers[x];
            string tName = target.name;


            if (ImGui.Button("Test##TestShocker" + target.getInternalId()))
            {
                ShockOptions temp = new ShockOptions(1, 35, 1);
                temp.ShockersOpenShock.Add(target);
                Plugin.ClientOpenShock.SendRequest(temp);
            }
            ImGui.SameLine();
            if (target.isPaused) ImGui.TextColored(ColorGray, "[Paused] " + target.name);
            else ImGui.TextColored(ColorGreen, target.name);
            ImGui.Separator();
            x++;
        }
    }

    private async void DrawEulaWindow()
    {
        Vector2 center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(450, 465));

        if (ImGui.BeginPopupModal("WoL Webserver Eula##webserverEula", ref isEulaModalActive,
            ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.Popup))
        {
            ImGui.TextWrapped("This is a simple Eula for the Webserver functionality of this Plugin.\n\nThe Webserver currently has these functions:\n");

            ImGui.BulletText("Allow storage of user configurations as backups.");
            ImGui.BulletText("Allow sharing of user made presets.");
            ImGui.BulletText("Usage of the Mastermode feature.");
            ImGui.BulletText("Usage of the Soulbound feature.");

            ImGui.TextWrapped("" +
                "\nTo use these features, a persistent user account will be created upon your first connection." +
                "\nThis account is valid for the currently played FF character." +
                "\nHowever this does also mean, your character Name and World will be saved serverside." +
                "\n\nYou will receive a unique Key used for logins, which will then be stored in the Authentification.json file." +
                "\nThere is no way to recover this key upon loss, so remember to keep this file safe or create a backup of it." +
                "\n\nUpon accepting this Eula, your account will be created.");
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "This is a non-reversible option.");


            ImGui.PushItemWidth(ImGui.GetWindowSize().X / 2);
            if (eulaTimer.TimeLeft > 0)
            {
                ImGui.BeginDisabled();
                ImGui.Button($"Wait {(int)(eulaTimer.TimeLeft / 1000)}s...##eulaAccept", new Vector2(ImGui.GetWindowSize().X / 2 - 5, 25));
                ImGui.EndDisabled();
            }
            else
            {
                if (ImGui.Button("Accept##eulaAccept", new Vector2(ImGui.GetWindowSize().X / 2 - 5, 25)))
                {
                    Plugin.Authentification.acceptedEula = true;
                    //Plugin.ClientWebserver.createHttpClient();
                    isEulaModalActive = false;
                    ImGui.CloseCurrentPopup();
                }
            }
            ImGui.SameLine();
            ImGui.PushItemWidth(ImGui.GetWindowSize().X / 2);
            if (ImGui.Button("Decline##eulaDecline", new Vector2(ImGui.GetWindowSize().X / 2 - 5, 25)))
            {
                isEulaModalActive = false;
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }
}
