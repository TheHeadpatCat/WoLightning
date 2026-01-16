using Dalamud.Bindings.ImGui;

namespace WoLightning.WoL_Plugin.Util.UI_Elements
{
    public static class HoverText
    {

        public static void Show(string hoverText)
        {
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip(hoverText); }
        }

        public static void Show(string text, string hoverText)
        {
            ImGui.TextDisabled(text);
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip(hoverText); }
        }

        public static void ShowSameLine(string hoverText)
        {
            ImGui.SameLine();
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip(hoverText); }
        }

        public static void ShowSameLine(string text, string hoverText)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(text);
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip(hoverText); }
        }

        public static void ShowHint(string hoverText)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(" (?) ");
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip(hoverText); }
        }

    }
}
