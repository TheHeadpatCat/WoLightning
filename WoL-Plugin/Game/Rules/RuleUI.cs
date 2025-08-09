﻿using Dalamud.Bindings.ImGui;
using System.Collections.Generic;
using System.Numerics;
using WoLightning.Configurations;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules
{
    public class RuleUI
    {
        Plugin Plugin;
        RuleBase Rule;
        // UI
        bool isOptionsOpen = false;
        bool isModalShockerSelectorOpen = false;

        Vector4 ColorNameEnabled = new Vector4(0.5f, 1, 0.3f, 0.9f);
        Vector4 ColorNameBlocked = new Vector4(1.0f, 0f, 0f, 0.9f);
        Vector4 ColorNameDisabled = new Vector4(1, 1, 1, 0.9f);
        Vector4 ColorDescription = new Vector4(0.7f, 0.7f, 0.7f, 0.8f);

        List<int> durationArray = [100, 300, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        List<CooldownModifier> modifierArray = [CooldownModifier.Miliseconds, CooldownModifier.Seconds, CooldownModifier.Minutes, CooldownModifier.Hours];

        public RuleUI(Plugin Plugin, RuleBase RuleParent)
        {
            this.Plugin = Plugin;
            this.Rule = RuleParent;
        }

        public void Draw()
        {
            try
            {
                if (Rule.Name == null || Rule.Name.Length == 0) return;
                DrawBase();
                if (Rule.IsEnabled && isOptionsOpen) { DrawOptions(); }
                ImGui.Spacing();
                ImGui.Separator();
            }
            catch { }
        }

        public void DrawBase()
        {
            ImGui.BeginGroup();
            bool refEn = Rule.IsEnabled;
            if (ImGui.Checkbox("##checkbox" + Rule.Name, ref refEn))
            {
                Rule.setEnabled(refEn);
                Plugin.Configuration.saveCurrentPreset();
            }
            if (Rule.IsEnabled)
            {
				/* TODO: Add back
                if (isOptionsOpen && ImGui.ArrowButton("##collapse" + Rule.Name, ImGuiDir.Down))
                {
                    isOptionsOpen = !isOptionsOpen;
                }
                if (!isOptionsOpen && ImGui.ArrowButton("##collapse" + Rule.Name, ImGuiDir.Right))
                {
                    isOptionsOpen = !isOptionsOpen;
                }*/

				if (isOptionsOpen && ImGui.Button("\\I/ ##collapse" + Rule.Name))
				{
					isOptionsOpen = !isOptionsOpen;
				}
				if (!isOptionsOpen && ImGui.Button("-> ##collapse" + Rule.Name))
				{
					isOptionsOpen = !isOptionsOpen;
				}
			}
            ImGui.EndGroup();

            ImGui.SameLine();
            ImGui.BeginGroup();

            if (Plugin.isFailsafeActive)
            {
                ImGui.TextColored(ColorNameBlocked, "  " + Rule.Name + $" [Failsafe Active]");
            }
            else
            {
                if (Rule.IsEnabled && Rule.ShockOptions.getShockerCount() > 0) ImGui.TextColored(ColorNameEnabled, "  " + Rule.Name + $"  [{Rule.ShockOptions.OpMode}]");
                else if (Rule.IsEnabled) ImGui.TextColored(ColorNameDisabled, "  " + Rule.Name + " [No Shockers]");
                else ImGui.TextColored(ColorNameDisabled, "  " + Rule.Name);
                if (Rule.CreatorName != null)
                {
                    ImGui.SameLine();
                    ImGui.TextColored(ColorDescription, $"    by {Rule.CreatorName}");
                }
            }

            ImGui.TextColored(ColorDescription, $"  {Rule.Description}");

            if (Rule.Hint != null && Rule.Hint.Length > 0)
            {
                ImGui.SameLine();
                ImGui.TextDisabled("(?)");
                if (ImGui.IsItemHovered()) { ImGui.SetTooltip(Rule.Hint); }
            }
            ImGui.EndGroup();
        }

        public void DrawOptions()
        {
            bool changed = false;
            DrawShockerSelector();
            if (Rule.hasExtraButton) Rule.DrawExtraButton();
            DrawOptionsBase(ref changed);
            DrawOptionsCooldown(ref changed);
            if (changed) Plugin.Configuration.saveCurrentPreset();
        }
        protected void DrawOptionsBase(ref bool changed)
        {
            ImGui.BeginGroup();
            ImGui.Text("    Mode");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 3 - 50);
            int OpMode = (int)Rule.ShockOptions.OpMode;
            if (ImGui.Combo("##OpModeSelect" + Rule.Name, ref OpMode, ["Shock", "Vibrate", "Beep"], 3))
            {
                Rule.ShockOptions.OpMode = (OpMode)OpMode;
                changed = true;
            }
            ImGui.EndGroup();

            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.Text("    Duration");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 7);
            int DurationIndex = durationArray.IndexOf(Rule.ShockOptions.Duration);
            if (ImGui.Combo("##DurationSelect" + Rule.Name, ref DurationIndex, ["0.1s", "0.3s", "1s", "2s", "3s", "4s", "5s", "6s", "7s", "8s", "9s", "10s"], 12))
            {
                Rule.ShockOptions.Duration = durationArray[DurationIndex];
                changed = true;
            }
            ImGui.EndGroup();

            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.Text("    Intensity");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 2.50f - 30);
            int Intensity = Rule.ShockOptions.Intensity;
            if (ImGui.SliderInt("##IntensitySelect" + Rule.Name, ref Intensity, 1, 100))
            {
                Rule.ShockOptions.Intensity = Intensity;
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
            int warning = (int)Rule.ShockOptions.WarningMode;
            if (ImGui.Combo("##WarningMode" + Rule.Name, ref warning, ["None", "Short", "Medium", "Long"], 4))
            {
                Rule.ShockOptions.WarningMode = (WarningMode)warning;
                changed = true;
            }
            ImGui.EndGroup();

        }

        protected void DrawOptionsCooldown(ref bool changed)
        {
            int Cooldown = (int)Rule.ShockOptions.Cooldown;
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 1.25f - 100);
            if (ImGui.SliderInt("##CooldownSelect" + Rule.Name, ref Cooldown, 0, 300))
            {
                Rule.ShockOptions.Cooldown = Cooldown;
                changed = true;
            }
            ImGui.SameLine();
            int modifierIndex = modifierArray.IndexOf(Rule.ShockOptions.modifier);
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 4f - 30);
            if (ImGui.Combo("##TimeModifier" + Rule.Name, ref modifierIndex, ["Miliseconds", "Seconds", "Minutes", "Hours"], 4))
            {
                Rule.ShockOptions.modifier = modifierArray[modifierIndex];
                changed = true;
            }

            ImGui.SameLine();
            ImGui.Text("Cooldown");
        }

        protected void DrawShockerSelector()
        {
            if (ImGui.Button($"Assigned {Rule.ShockOptions.getShockerCount()} Shockers##assignedShockers" + Rule.Name, new Vector2(150, 25)))
            {
                isModalShockerSelectorOpen = true;
                ImGui.OpenPopup("Select Shockers##ShockerSelect" + Rule.Name);
            }

            if (isModalShockerSelectorOpen) //setup modal
            {
                Vector2 center = ImGui.GetMainViewport().GetCenter();
                ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
                ImGui.SetNextWindowSize(new Vector2(400, 400));
            }

            if (ImGui.BeginPopupModal("Select Shockers##ShockerSelect" + Rule.Name, ref isModalShockerSelectorOpen,
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
                    if (Plugin.Configuration.ShownShockers == Configurations.ShownShockers.None) continue;
                    if (Plugin.Configuration.ShownShockers == Configurations.ShownShockers.Personal && !shocker.isPersonal) continue;
                    if (Plugin.Configuration.ShownShockers == Configurations.ShownShockers.Shared && shocker.isPersonal) continue;


                    bool isEnabled = Rule.ShockOptions.ShockersPishock.Find(sh => sh.getInternalId() == shocker.getInternalId()) != null;

                    if (ImGui.Checkbox($"##shockerbox{shocker.getInternalId()}", ref isEnabled))
                    { // this could probably be solved more elegantly
                        if (isEnabled) Rule.ShockOptions.ShockersPishock.Add(shocker);
                        else Rule.ShockOptions.ShockersPishock.RemoveAt(Rule.ShockOptions.ShockersPishock.FindIndex(sh => sh.getInternalId() == shocker.getInternalId()));
                    }
                    ImGui.SameLine();
                    if (!shocker.isPersonal)
                    {
                        ImGui.BeginGroup();
                        ImGui.Text(shocker.username);
                        if (!shocker.isPaused) ImGui.TextColored(ColorNameEnabled, shocker.name);
                        else ImGui.TextColored(ColorNameDisabled, "[Paused] " + shocker.name);
                        ImGui.EndGroup();
                        continue;
                    }
                    if (!shocker.isPaused) ImGui.TextColored(ColorNameEnabled, shocker.name);
                    else ImGui.TextColored(ColorNameDisabled, "[Paused] " + shocker.name);
                }

                ImGui.EndChild();
                ImGui.EndGroup();

                ImGui.SameLine();

                ImGui.BeginGroup();
                ImGui.Text("Available OpenShock Devices:");
                ImGui.BeginChild("OpenShockShockerList", new Vector2(180, 260));
                foreach (var shocker in Plugin.Authentification.OpenShockShockers)
                {
                    bool isEnabled = Rule.ShockOptions.ShockersOpenShock.Find(sh => sh.getInternalId() == shocker.getInternalId()) != null;

                    if (ImGui.Checkbox($"##shockerbox{shocker.getInternalId()}", ref isEnabled))
                    { // this could probably be solved more elegantly
                        if (isEnabled) Rule.ShockOptions.ShockersOpenShock.Add(shocker);
                        else Rule.ShockOptions.ShockersOpenShock.RemoveAt(Rule.ShockOptions.ShockersOpenShock.FindIndex(sh => sh.getInternalId() == shocker.getInternalId()));
                    }
                    ImGui.SameLine();
                    if (!shocker.isPaused) ImGui.TextColored(ColorNameEnabled, shocker.name);
                    else ImGui.TextColored(ColorNameDisabled, "[Paused] " + shocker.name);
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
                if (ImGui.Button($"Apply##apply{Rule.Name}", new Vector2(ImGui.GetWindowSize().X - 120, 25)))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button($"Reset##resetall{Rule.Name}", new Vector2(ImGui.GetWindowSize().X / 8, 25)))
                {
                    Rule.ShockOptions.ShockersPishock.Clear();
                }
                ImGui.EndGroup();


                ImGui.EndPopup();
            }


        }

    }
}
