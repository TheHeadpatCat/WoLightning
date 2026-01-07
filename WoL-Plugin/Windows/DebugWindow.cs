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
            ImGui.TextColored(new Vector4(255, 0, 0, 255), "THESE WINDOWS OPEN BECAUSE\nYOU HAVE THE \"Dev\" DEBUGLEVEL SET.");
            if (ImGui.Button("Oh okay! Turn it off!"))
            {
                Plugin.Configuration.DebugLevel = DebugLevel.Verbose;
                Plugin.Configuration.Save();
                this.Toggle();
            }


            ImGui.Text("Grace: " + Plugin.ControlSettings.LeashGraceTimer.TimeLeft.ToString());
            ImGui.Text("Area: " + Plugin.ControlSettings.LeashGraceAreaTimer.TimeLeft.ToString());
            ImGui.Text("Shock: " + Plugin.ControlSettings.LeashShockTimer.TimeLeft.ToString());
            ImGui.Text("damage frames " + Plugin.Configuration.ActivePreset.TakeDamage.bufferFrames);
            ImGui.Text("scheduled save " + Plugin.Configuration.PresetSavingBuffer);

            ImGui.Text("Auth:");
            foreach (var device in Plugin.Authentification.DevicesIntiface)
            {
                ImGui.Text(device.Index + " Name " + device.Name + " Display " + device.DisplayName);
            }

            ImGui.Text("Rule:");
            foreach (var device in Plugin.Configuration.ActivePreset.DoEmote.ShockOptions.DevicesIntiface)
            {
                ImGui.Text(device.Index + " Name " + device.Name + " Display " + device.DisplayName);
            }


            bool pishocklogin = Plugin.Configuration.LoginOnStartPishock;
            if (ImGui.Checkbox("pishock auto login", ref pishocklogin))
            {
                Plugin.Configuration.LoginOnStartPishock = pishocklogin;
                if (pishocklogin) Plugin.ClientPishock.Setup();
            }

        }


        public void RunCode()
        {

        }
    }
}
