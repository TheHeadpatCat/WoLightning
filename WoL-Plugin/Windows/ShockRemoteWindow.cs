using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;
using WoLightning.Configurations;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util.Helpers;

namespace WoLightning.WoL_Plugin.Windows
{
    public class ShockRemoteWindow : Window, IDisposable
    {
        private Plugin Plugin;
        private ShockOptions Options { get; set; }

        bool isModalShockerSelectorOpen = false;

        public ShockRemoteWindow(Plugin plugin) : base("Shocker Remote")
        {
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(270, 250),
                MaximumSize = new Vector2(320, 2000)
            };

            Plugin = plugin;
            Options = new ShockOptions();
        }

        public void Dispose()
        {
            if (this.IsOpen) this.Toggle();
        }

        public override void Draw()
        {

            if (Plugin == null || Plugin.Configuration == null || Plugin.Authentification == null || Plugin.Authentification.GetDevicesCount() == 0)
            {
                ImGui.TextWrapped("This Window isn't initialized yet.\nPlease login with a character first and make sure you are connected to either the Pishock, or OpenShock API.");
                return;
            }

            if (ImGui.Button($"Assigned {Options.getShockerCount()} Shockers##assignedShockersRemote", new Vector2(150, 25)))
            {
                isModalShockerSelectorOpen = true;
                ImGui.OpenPopup("Select Shockers##ShockerSelectRemote");
            }

            DrawOptionsBase();
            DrawShockerSelector();

            if (Options.hasCooldown()) ImGui.BeginDisabled();
            ImGui.BeginGroup();
            if (ImGui.Button("Shock!", new Vector2(90, 40)))
            {
                Options.OpMode = OpMode.Shock;
                Plugin.ClientPishock.SendRequest(Options);
                Plugin.ClientOpenShock.SendRequest(Options);
                Options.startCooldown();
            }
            ImGui.SameLine();
            if (ImGui.Button("Vibrate!", new Vector2(90, 40)))
            {
                Options.OpMode = OpMode.Vibrate;
                Plugin.ClientPishock.SendRequest(Options);
                Plugin.ClientOpenShock.SendRequest(Options);
                Options.startCooldown();
            }
            ImGui.SameLine();
            if (ImGui.Button("Beep!", new Vector2(90, 40)))
            {
                Options.OpMode = OpMode.Beep;
                Plugin.ClientPishock.SendRequest(Options);
                Plugin.ClientOpenShock.SendRequest(Options);
                Options.startCooldown();
            }
            ImGui.EndGroup();
            if (Options.hasCooldown()) ImGui.EndDisabled();
        }


        private void DrawOptionsBase()
        {

            ImGui.BeginGroup();
            ImGui.Text("    Duration");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 4);
            int DurationIndex = UIValues.DurationArray.IndexOf(Options.Duration);
            if (ImGui.Combo("##DurationSelectRemote", ref DurationIndex, ["0.1s", "0.3s", "1s", "2s", "3s", "4s", "5s", "6s", "7s", "8s", "9s", "10s"], 12))
            {
                Options.Duration = UIValues.DurationArray[DurationIndex];
            }
            ImGui.EndGroup();

            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.Text("    Warning Mode");
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Sends out a 1 second Vibration before the actual command." +
                "\nHigher settings have a longer waiting time between the Warning and Command." +
                "\nShort is between 1-3 seconds." +
                "\nMedium is between 5-10 seconds." +
                "\nLong is between 10-25 seconds.");
            }
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 3);
            int warning = (int)Options.WarningMode;
            if (ImGui.Combo("##WarningModeRemote", ref warning, ["None", "Short", "Medium", "Long"], 4))
            {
                Options.WarningMode = (WarningMode)warning;
            }
            ImGui.EndGroup();

            ImGui.BeginGroup();
            ImGui.Text("    Intensity");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 1.33f);
            int Intensity = Options.Intensity;
            if (ImGui.SliderInt("##IntensitySelectRemote", ref Intensity, 1, 100))
            {
                Options.Intensity = Intensity;
            }
            ImGui.EndGroup();



        }

        private void DrawShockerSelector()
        {

            if (isModalShockerSelectorOpen) //setup modal
            {
                Vector2 center = ImGui.GetMainViewport().GetCenter();
                ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
                ImGui.SetNextWindowSize(new Vector2(400, 400));
            }

            if (ImGui.BeginPopupModal("Select Shockers##ShockerSelectRemote", ref isModalShockerSelectorOpen,
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.Popup))
            {
                if (Plugin.Authentification.GetDevicesCount() == 0)
                {
                    ImGui.TextWrapped("The Shockers are still being loaded!" +
                        "\nIf this doesn't change, please make sure that your" +
                        "\nAccount Settings are properly set up!");
                    if (ImGui.Button($"Okay##okayShockerSelectorAbort", new Vector2(ImGui.GetWindowSize().X / 2, 25)))
                    {
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndPopup();
                    return;
                }



                ImGui.Text("Please select all shockers that should activate for this trigger:");
                ImGui.BeginGroup();
                ImGui.Text("Available Pishock Devices:           ");
                ImGui.BeginChild("PishockShockerList", new Vector2(180, 260));
                foreach (var shocker in Plugin.Authentification.PishockShockers)
                {
                    if (Plugin.Configuration.ShownShockers == ShownShockers.None) continue;
                    if (Plugin.Configuration.ShownShockers == ShownShockers.Personal && !shocker.isPersonal) continue;
                    if (Plugin.Configuration.ShownShockers == ShownShockers.Shared && shocker.isPersonal) continue;


                    bool isEnabled = Options.ShockersPishock.Find(sh => sh.getInternalId() == shocker.getInternalId()) != null;

                    if (ImGui.Checkbox($"##shockerbox{shocker.getInternalId()}", ref isEnabled))
                    { // this could probably be solved more elegantly
                        if (isEnabled) Options.ShockersPishock.Add(shocker);
                        else Options.ShockersPishock.RemoveAt(Options.ShockersPishock.FindIndex(sh => sh.getInternalId() == shocker.getInternalId()));
                    }
                    ImGui.SameLine();
                    if (!shocker.isPersonal)
                    {
                        ImGui.BeginGroup();
                        ImGui.Text(shocker.username);
                        if (!shocker.isPaused) ImGui.TextColored(UIValues.ColorNameEnabled, shocker.name);
                        else ImGui.TextColored(UIValues.ColorNameDisabled, "[Paused] " + shocker.name);
                        ImGui.EndGroup();
                        continue;
                    }
                    if (!shocker.isPaused) ImGui.TextColored(UIValues.ColorNameEnabled, shocker.name);
                    else ImGui.TextColored(UIValues.ColorNameDisabled, "[Paused] " + shocker.name);
                }

                ImGui.EndChild();
                ImGui.EndGroup();

                ImGui.SameLine();

                ImGui.BeginGroup();
                ImGui.Text("Available OpenShock Devices:");
                ImGui.BeginChild("OpenShockShockerList", new Vector2(180, 260));
                foreach (var shocker in Plugin.Authentification.OpenShockShockers)
                {
                    bool isEnabled = Options.ShockersOpenShock.Find(sh => sh.getInternalId() == shocker.getInternalId()) != null;

                    if (ImGui.Checkbox($"##shockerbox{shocker.getInternalId()}", ref isEnabled))
                    { // this could probably be solved more elegantly
                        if (isEnabled) Options.ShockersOpenShock.Add(shocker);
                        else Options.ShockersOpenShock.RemoveAt(Options.ShockersOpenShock.FindIndex(sh => sh.getInternalId() == shocker.getInternalId()));
                    }
                    ImGui.SameLine();
                    if (!shocker.isPaused) ImGui.TextColored(UIValues.ColorNameEnabled, shocker.name);
                    else ImGui.TextColored(UIValues.ColorNameDisabled, "[Paused] " + shocker.name);
                }
                ImGui.EndChild();
                ImGui.EndGroup();

                ImGui.SetCursorPos(new Vector2(ImGui.GetWindowSize().X / 2 - 170, ImGui.GetWindowSize().Y - 65));
                ImGui.SetNextItemWidth(200);
                int ShownShockersIndex = (int)Plugin.Configuration.ShownShockers;
                if (ImGui.Combo("Shown Shockers", ref ShownShockersIndex, ["All", "Personal Only", "Shared Only", "None...?"], 4))
                {
                    Plugin.Configuration.ShownShockers = (ShownShockers)ShownShockersIndex;
                    Plugin.Configuration.Save();
                }
                ImGui.SameLine();
                ImGui.TextDisabled(" (?)");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Allows you to select which Shockers show up on clicking the \"Assign Shockers\" button.");
                }

                ImGui.SetCursorPos(new Vector2(ImGui.GetWindowSize().X / 2 - 170, ImGui.GetWindowSize().Y - 35));
                ImGui.BeginGroup();
                if (ImGui.Button($"Apply##applyRemote", new Vector2(ImGui.GetWindowSize().X - 120, 25)))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button($"Reset##resetallRemote", new Vector2(ImGui.GetWindowSize().X / 8, 25)))
                {
                    Options.ShockersPishock.Clear();
                }
                ImGui.EndGroup();


                ImGui.EndPopup();
            }


        }


    }
}

