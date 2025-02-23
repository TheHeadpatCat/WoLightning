﻿using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules
{
    public class RuleUI
    {
        Plugin Plugin;
        BaseRule Rule;
        // UI
        bool IsOptionsOpen = false;
        bool isModalShockerSelectorOpen = false;
        Vector4 ColorNameEnabled = new Vector4(0.5f, 1, 0.3f, 0.9f);
        Vector4 ColorNameDisabled = new Vector4(1, 1, 1, 0.9f);
        Vector4 ColorDescription = new Vector4(0.7f, 0.7f, 0.7f, 0.8f);

        List<int> durationArray = [100, 300, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        List<CooldownModifier> modifierArray = [CooldownModifier.Miliseconds, CooldownModifier.Seconds, CooldownModifier.Minutes, CooldownModifier.Hours];

        public RuleUI(Plugin Plugin, BaseRule RuleParent)
        {
            this.Plugin = Plugin;
            this.Rule = RuleParent;
        }

        public void Draw()
        {
            if(Rule.Name == null || Rule.Name.Length == 0) return;
            DrawBase();
            if (Rule.IsEnabled && IsOptionsOpen) DrawOptions();
            ImGui.Spacing();
            ImGui.Separator();
        }

        protected void DrawBase()
        {
            ImGui.BeginGroup();
            bool refEn = Rule.IsEnabled;
            if(ImGui.Checkbox("##checkbox" + Rule.Name, ref refEn))
            {
                Rule.IsEnabled = refEn;
                Plugin.Configuration.Save();
            }
            if (Rule.IsEnabled)
            {
                if (IsOptionsOpen && ImGui.ArrowButton("##collapse" + Rule.Name, ImGuiDir.Down))
                {
                    IsOptionsOpen = !IsOptionsOpen;
                }
                if (!IsOptionsOpen && ImGui.ArrowButton("##collapse" + Rule.Name, ImGuiDir.Right))
                {
                    IsOptionsOpen = !IsOptionsOpen;
                }
            }
            ImGui.EndGroup();

            ImGui.SameLine();
            ImGui.BeginGroup();
            if (Rule.IsEnabled) ImGui.TextColored(ColorNameEnabled, "  " + Rule.Name + $"  [{Rule.ShockOptions.OpMode}]");
            else ImGui.TextColored(ColorNameDisabled, "  " + Rule.Name);
            ImGui.TextColored(ColorDescription, $"  {Rule.Description}");
            if (Rule.Hint != null && Rule.Hint.Length > 0)
            {
                ImGui.SameLine();
                ImGui.TextDisabled("(?)");
                if (ImGui.IsItemHovered()) { ImGui.SetTooltip(Rule.Hint); }
            }
            ImGui.EndGroup();
        }

        protected void DrawOptions()
        {
            bool changed = false;
            DrawShockerSelector();
            DrawOptionsBase(ref changed);
            DrawOptionsCooldown(ref changed);
            if (changed) Plugin.Configuration.Save();
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
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 1.85f - 30);
            int Intensity = Rule.ShockOptions.Intensity;
            if (ImGui.SliderInt("##IntensitySelect" + Rule.Name, ref Intensity, 1, 100))
            {
                Rule.ShockOptions.Intensity = Intensity;
                changed = true;
            }
            ImGui.EndGroup();
        }

        protected void DrawOptionsCooldown(ref bool changed)
        {
            int Cooldown = Rule.ShockOptions.Cooldown;
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 1.25f - 100);
            if (ImGui.SliderInt("##CooldownSelect" + Rule.Name, ref Cooldown, 0, 300))
            {
                Rule.ShockOptions.Cooldown = Cooldown;
                changed = true;
            }
            ImGui.SameLine();
            int modifierIndex = modifierArray.IndexOf(Rule.ShockOptions.modifier);
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 4f - 30);
            if (ImGui.Combo("##TimeModifier", ref modifierIndex, ["Miliseconds", "Seconds", "Minutes", "Hours"], 4, 4))
            {
                Rule.ShockOptions.modifier = modifierArray[modifierIndex];
                changed = true;
            }

            ImGui.SameLine();
            ImGui.Text("Cooldown");
        }

        protected void DrawShockerSelector()
        {
            if (ImGui.Button($"Assigned {Rule.ShockOptions.Shockers.Count} Shockers##assignedShockers" + Rule.Name, new Vector2(150,25)))
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
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.Popup))
            {
                if (Plugin.Authentification.GetShockerCount() == 0)
                {
                    ImGui.TextWrapped("It seems you havent added any Shockers yet!\n" +
                        "Please add them through the \"Account Settings\" in the main window.");
                    if (ImGui.Button($"Okay##okayShockerSelectorAbort", new Vector2(ImGui.GetWindowSize().X / 2, 25)))
                    {
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndPopup();
                    return;
                }

                //todo: make shocker selector



                ImGui.EndPopup();
            }
            

        }
    }
}
