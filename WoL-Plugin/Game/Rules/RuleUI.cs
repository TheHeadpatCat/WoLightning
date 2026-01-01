using Dalamud.Bindings.ImGui;
using WoLightning.WoL_Plugin.Util.Helpers;
using WoLightning.WoL_Plugin.Util.UI_Elements;

namespace WoLightning.WoL_Plugin.Game.Rules
{
    public class RuleUI
    {
        Plugin Plugin;
        RuleBase Rule;
        // UI
        bool isOptionsOpen = false;
        bool isModalShockerSelectorOpen = false;
        ShockOptionsEditor? OptionsEditor;

        public RuleUI(Plugin Plugin, RuleBase RuleParent)
        {
            this.Plugin = Plugin;
            this.Rule = RuleParent;
            if (Rule.hasOptions) OptionsEditor = new(Rule.Name, Plugin, Rule.ShockOptions);
        }

        public void Draw()
        {
            try
            {
                if (Rule.Name == null || Rule.Name.Length == 0) return;
                DrawBase();
                if (Rule.IsEnabled && isOptionsOpen)
                {
                    if (Rule.hasExtraButton) Rule.DrawExtraButton();
                    if (OptionsEditor != null) OptionsEditor.Draw();
                }
                ImGui.Spacing();
                ImGui.Separator();
            }
            catch { }
        }

        public void DrawBase()
        {
            ImGui.BeginGroup();
            bool refEn = Rule.IsEnabled;
            if (Plugin.Configuration.IsLockedByController) ImGui.BeginDisabled();
            if (ImGui.Checkbox("##checkbox" + Rule.Name, ref refEn))
            {
                Rule.setEnabled(refEn);
                Plugin.Configuration.saveCurrentPreset();
            }
            if (Plugin.Configuration.IsLockedByController) ImGui.EndDisabled();
            if (Rule.IsEnabled)
            {

                if (Rule.hasOptions)
                {
                    if (isOptionsOpen && ImGui.ArrowButton("##collapse" + Rule.Name, ImGuiDir.Down))
                    {
                        isOptionsOpen = !isOptionsOpen;
                    }
                    if (!isOptionsOpen && ImGui.ArrowButton("##collapse" + Rule.Name, ImGuiDir.Right))
                    {
                        isOptionsOpen = !isOptionsOpen;
                    }
                }
            }
            ImGui.EndGroup();

            ImGui.SameLine();
            ImGui.BeginGroup();

            if (Plugin.IsFailsafeActive)
            {
                ImGui.TextColored(UIValues.ColorNameBlocked, "  " + Rule.Name + $" [Failsafe Active]");
            }
            else
            {
                if (Rule.IsEnabled && !Rule.hasOptions) ImGui.TextColored(UIValues.ColorNameEnabled, "  " + Rule.Name);
                else if (Rule.IsEnabled && Rule.ShockOptions.getShockerCount() > 0) ImGui.TextColored(UIValues.ColorNameEnabled, "  " + Rule.Name + $"  [{Rule.ShockOptions.OpMode}]");
                else if (Rule.IsEnabled) ImGui.TextColored(UIValues.ColorNameDisabled, "  " + Rule.Name + " [No Shockers]");
                else ImGui.TextColored(UIValues.ColorNameDisabled, "  " + Rule.Name);
                if (Rule.CreatorName != null)
                {
                    ImGui.SameLine();
                    ImGui.TextColored(UIValues.ColorDescription, $"    by {Rule.CreatorName}");
                }
            }

            ImGui.TextColored(UIValues.ColorDescription, $"  {Rule.Description}");

            if (Rule.Hint != null && Rule.Hint.Length > 0)
            {
                HoverText.ShowHint(Rule.Hint);
            }
            ImGui.EndGroup();
        }

    }
}
