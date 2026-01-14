using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;
using WoLightning.Configurations;

namespace WoLightning.WoL_Plugin.Windows
{
    public class DebugWindow : Window, IDisposable
    {
        private readonly Plugin Plugin;
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
            ImGui.TextColored(new Vector4(255, 0, 0, 255), "You should not be touching these settings, if you don't know what you are doing.");


            ImGui.Text("Interval: " + Plugin.Configuration.ActivePreset.ForgetDot.UpdateDelta);



        }


        public void RunCode()
        {

        }
    }
}
