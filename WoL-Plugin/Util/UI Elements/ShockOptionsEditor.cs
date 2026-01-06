using Dalamud.Bindings.ImGui;
using System.Numerics;
using WoLightning.Configurations;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util.Helpers;

namespace WoLightning.WoL_Plugin.Util.UI_Elements
{
    public class ShockOptionsEditor
    {
        string Name { get; set; }
        Plugin Plugin { get; set; }
        public ShockOptions Options { get; set; }
        public bool HasCooldown { get; set; } = true;
        public bool AutoSave { get; set; } = true;

        bool isModalShockerSelectorOpen = false;

        public ShockOptionsEditor(string name, Plugin plugin, ShockOptions options)
        {
            Name = name;
            Plugin = plugin;
            Options = options;
        }

        public void Draw()
        {
            if (Plugin == null || Options == null)
            {
                ImGui.TextColored(UIValues.ColorNameBlocked, "Failed to load Settings. Please restart the Plugin");
                return;
            }
            DrawOptions();
        }

        private void DrawOptions()
        {
            bool changed = false;
            DrawShockerSelector();
            DrawOptionsBase(ref changed);
            if (HasCooldown) DrawOptionsCooldown(ref changed);
            if (AutoSave && changed) Plugin.Configuration.SaveCurrentPresetScheduled();
        }
        private void DrawOptionsBase(ref bool changed)
        {
            ImGui.BeginGroup();
            ImGui.Text("    Mode");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 3 - 50);
            int OpMode = (int)Options.OpMode;
            if (ImGui.Combo("##OpModeSelect" + Name, ref OpMode, ["Shock", "Vibrate", "Beep"], 3))
            {
                Options.OpMode = (OpMode)OpMode;
                changed = true;
            }
            ImGui.EndGroup();

            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.Text("    Duration");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 7);
            int DurationIndex = UIValues.DurationArray.IndexOf(Options.Duration);
            if (ImGui.Combo("##DurationSelect" + Name, ref DurationIndex, ["0.1s", "0.3s", "1s", "2s", "3s", "4s", "5s", "6s", "7s", "8s", "9s", "10s"], 12))
            {
                Options.Duration = UIValues.DurationArray[DurationIndex];
                changed = true;
            }
            ImGui.EndGroup();

            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.Text("    Intensity");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 2.50f - 30);
            int Intensity = Options.Intensity;
            if (ImGui.SliderInt("##IntensitySelect" + Name, ref Intensity, 1, 100))
            {
                Options.Intensity = Intensity;
                changed = true;
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
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 5 - 30);
            int warning = (int)Options.WarningMode;
            if (ImGui.Combo("##WarningMode" + Name, ref warning, ["None", "Short", "Medium", "Long"], 4))
            {
                Options.WarningMode = (WarningMode)warning;
                changed = true;
            }
            ImGui.EndGroup();

        }

        protected void DrawOptionsCooldown(ref bool changed)
        {
            int Cooldown = (int)Options.Cooldown;
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 1.5f - 100);
            if (ImGui.SliderInt("##CooldownSelect" + Name, ref Cooldown, 0, 300))
            {
                Options.Cooldown = Cooldown;
                changed = true;
            }
            ImGui.SameLine();
            int modifierIndex = UIValues.ModifierArray.IndexOf(Options.modifier);
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 4f - 30);
            if (ImGui.Combo("##TimeModifier" + Name, ref modifierIndex, ["Miliseconds", "Seconds", "Minutes", "Hours"], 4))
            {
                Options.modifier = UIValues.ModifierArray[modifierIndex];
                changed = true;
            }

            ImGui.SameLine();
            ImGui.Text("Cooldown");
        }

        protected void DrawShockerSelector()
        {
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 4f - 30);
            if (ImGui.Button($"Assigned {Options.getShockerCount()} Shockers##assignedShockers" + Name))
            {
                isModalShockerSelectorOpen = true;
                ImGui.OpenPopup("Select Shockers##ShockerSelect" + Name);
            }

            if (isModalShockerSelectorOpen) //setup modal
            {
                Vector2 center = ImGui.GetMainViewport().GetCenter();
                ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
                ImGui.SetNextWindowSize(new Vector2(900, 900));
            }

            if (ImGui.BeginPopupModal("Select Shockers##ShockerSelect" + Name, ref isModalShockerSelectorOpen,
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.Popup))
            {
                if (Plugin.Authentification.GetShockerCount() == 0)
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
                ImGui.Text("Available OpenShock Devices:           ");
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

                ImGui.SameLine();

                ImGui.BeginGroup();
                ImGui.Text("Available Intiface Devices:");
                ImGui.BeginChild("IntifaceShockerList", new Vector2(180, 260));
                foreach (var device in Plugin.Authentification.ButtplugDevices)
                {
                    bool isEnabled = Options.ButtplugDevices.Find(sh => sh.Index == device.Index) != null;

                    if (ImGui.Checkbox($"##devicebox{device.Index}", ref isEnabled))
                    { // this could probably be solved more elegantly
                        if (isEnabled) Options.ButtplugDevices.Add(device);
                        else Options.ButtplugDevices.RemoveAt(Options.ButtplugDevices.FindIndex(sh => sh.Index == device.Index));
                    }
                    ImGui.SameLine();
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
                if (ImGui.Button($"Apply##apply{Name}", new Vector2(ImGui.GetWindowSize().X - 120, 25)))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button($"Reset##resetall{Name}", new Vector2(ImGui.GetWindowSize().X / 8, 25)))
                {
                    Options.ShockersPishock.Clear();
                }
                ImGui.EndGroup();



                ImGui.EndPopup();
            }


        }

    }
}
