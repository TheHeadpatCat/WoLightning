using Newtonsoft.Json;
using System;
using WoLightning.Util.Types;
using ImGuiNET;
using System.Collections.Generic;

namespace WoLightning.WoL_Plugin.Game.Rules
{

    public enum RuleCategory
    {
        Unknown = 0,
        General = 1,
        Social = 2,
        Combat = 3,
        PVP = 4,
        Misc = 5,
        Master = 6,
    }

    [Serializable]
    abstract public class BaseRule : IDisposable
    {
        [JsonIgnore] abstract public string Name { get; }
        [JsonIgnore] abstract public string Description { get; }
        [JsonIgnore] virtual public string Hint { get; }
        [JsonIgnore] abstract public RuleCategory Category { get; }
        [JsonIgnore] virtual public bool isUsingCustomData { get; } = false;

        virtual public ShockOptions ShockOptions { get; set; }

        // TODO: Implement Point system
        virtual public float Points { get; set; }
        virtual public float PointsToTrigger { get; set; }

        virtual public bool IsEnabled { get; set; } // True when the User has it checked as "On"
        [NonSerialized] public bool IsRunning;      // True when we are actually running the "Check" function

        virtual public bool IsLocked { get; set; }

        [NonSerialized] protected Plugin Plugin;
        [NonSerialized] public Action<BaseRule> Triggered;
        [NonSerialized] protected RuleUI RuleUI;
        [NonSerialized] public bool hasRuleWindow;

        [NonSerialized] protected List<int> DurationArray = [100, 300, 500, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        [NonSerialized] protected string[] DurationArrayString = ["0.1s", "0.3s", "0.5s", "1s", "2s", "3s", "4s", "5s", "6s", "7s", "8s", "9s", "10s"];


        protected BaseRule(Plugin plugin)
        {
            setPlugin(plugin);
            ShockOptions = new ShockOptions();
        }

        virtual public void setPlugin(Plugin plugin)
        {
            this.Plugin = plugin;
            RuleUI = new RuleUI(plugin, this, isUsingCustomData);
        }

        virtual public void Start()
        {
            throw new NotImplementedException();
        }

        virtual public void Stop()
        {
            throw new NotImplementedException();
        }

        virtual public void Trigger(string Text)
        {
            if (ShockOptions.hasCooldown() || !IsRunning) return;
            Triggered?.Invoke(this);
            Plugin.sendNotif(Text);
            ShockOptions.startCooldown();
        }

        virtual public void Trigger(string Text, Player source)
        {
            if (ShockOptions.hasCooldown() || !IsRunning) return;
            if (!Plugin.Configuration.ActivePreset.isPlayerAllowedToTrigger(source)) return;
            Triggered?.Invoke(this);
            Plugin.sendNotif(Text);
            ShockOptions.startCooldown();
        }

        virtual public void Draw()
        {
            RuleUI.Draw();
        }

        virtual public void DrawRuleWindow()
        {
            ImGui.Text("This Rule doesnt have any special settings.");
            return;
        }

        public void Dispose()
        {
            Stop();
        }

    }
}
