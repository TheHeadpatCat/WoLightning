using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules
{

    public enum RuleCategory
    {
        Unknown = 0,
        General = 1,
        Social = 2,
        PVE = 3,
        PVP = 4,
        Misc = 5,
        Master = 6,
    }

    [Serializable]
    abstract public class RuleBase : IDisposable
    {
        [JsonIgnore] abstract public string Name { get; }
        [JsonIgnore] abstract public string Description { get; }
        [JsonIgnore] virtual public string Hint { get; }
        [JsonIgnore] abstract public RuleCategory Category { get; }
        [JsonIgnore] virtual public bool hasAdvancedOptions { get; } = false;
        [JsonIgnore] virtual public bool hasExtraButton { get; } = false;
        [JsonIgnore] virtual public string CreatorName { get; }

        virtual public ShockOptions ShockOptions { get; set; }

        virtual public bool IsEnabled { get; set; } // True when the User has it checked as "On"
        [NonSerialized] public bool IsRunning;      // True when we are actually running the "Check" function

        virtual public bool IsLocked { get; set; }

        [NonSerialized] protected Plugin Plugin;
        [NonSerialized] public Action<ShockOptions> Triggered;
        [NonSerialized] protected RuleUI RuleUI;

        [NonSerialized] protected List<int> DurationArray = [100, 300, 500, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        [NonSerialized] protected string[] DurationArrayString = ["0.1s", "0.3s", "0.5s", "1s", "2s", "3s", "4s", "5s", "6s", "7s", "8s", "9s", "10s"];


        [JsonConstructor]
        protected RuleBase()
        {

        }

        protected RuleBase(Plugin plugin)
        {
            setPlugin(plugin);
        }

        virtual public void setPlugin(Plugin plugin)
        {
            this.Plugin = plugin;
            RuleUI = new RuleUI(plugin, this);
            if (ShockOptions == null) ShockOptions = new ShockOptions();
        }

        virtual public void setEnabled(bool enabled)
        {
            IsEnabled = enabled;
            if (IsEnabled && !IsRunning && Plugin.MainWindow.IsEnabled) Start();
            if (!IsEnabled && IsRunning) Stop();
        }

        virtual public void Start()
        {
            Plugin.Log(2, Name + ".Start() is not Implemented");
        }

        virtual public void Stop()
        {
            Plugin.Log(2, Name + ".Stop() is not Implemented");
        }

        virtual public void Trigger(string Text) { Trigger(Text, null, null, null); }
        virtual public void Trigger(string Text, Player? source) { Trigger(Text, source, null, null); }
        virtual public void Trigger(string Text, Player? source, int[]? overrideOptions) { Trigger(Text, source, overrideOptions, null); }

        virtual public void Trigger(string Text, Player? source, int[]? overrideOptions, bool? noNotification)
        {
            if (ShockOptions.hasCooldown() || !IsRunning || Plugin.isFailsafeActive) return;
            if (source != null && !Plugin.Configuration.ActivePreset.isPlayerAllowedToTrigger(source)) return;
            if (!Plugin.Configuration.ActivePreset.AllowRulesInPvP && Plugin.ClientState.IsPvP) return;

            if (overrideOptions == null) Triggered?.Invoke(this.ShockOptions);
            else Triggered?.Invoke(new ShockOptions(0, overrideOptions[0], overrideOptions[1]));

            if ((noNotification == null || noNotification == false) && Plugin.Configuration.ActivePreset.showTriggerNotifs) Plugin.NotificationHandler.send(Text);


            ShockOptions.startCooldown();
            if (Plugin.Configuration.ActivePreset.showCooldownNotifs && ShockOptions.Cooldown > 0)
            {
                Plugin.NotificationHandler.send($"{Name} Cooldown", null, Dalamud.Interface.ImGuiNotification.NotificationType.Info, new TimeSpan(0, 0, ShockOptions.cooldownLeft() + 1));
            }
        }

        virtual public void Draw()
        {
            RuleUI.Draw();
        }
        virtual public void DrawAdvancedOptions()
        {
            throw new NotImplementedException();
        }
        virtual public void DrawExtraButton()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            Stop();
        }

    }
}
