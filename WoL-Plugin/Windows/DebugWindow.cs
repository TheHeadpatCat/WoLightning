using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WoLightning.WoL_Plugin.Windows
{
    public class DebugWindow : Window, IDisposable
    {
        private Plugin Plugin;
        public DebugWindow(Plugin plugin)
            : base("WoLightning - DebugWindow")
        {
            Plugin = plugin;

            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(200, 200),
                MaximumSize = new Vector2(2000, 2000)
            };
        }

        public void Dispose() { }
        public override async void Draw()
        {
            ImGui.TextColored(new Vector4(255, 0, 0, 255), "THESE WINDOWS OPEN BECAUSE YOU HAVE THE \"Dev\" DEBUGLEVEL SET.\nYou can change this in the General Settings.");


            ImGui.Text("ActivePreset: " + Plugin.Configuration.ActivePreset.Name);
            ImGui.Text("ActivePresetIndex: " + Plugin.Configuration.ActivePresetIndex);
            ImGui.Text("Intensity Value: " + Plugin.Configuration.ActivePreset.DoEmote.ShockOptions.Intensity);

        }
    }
}
