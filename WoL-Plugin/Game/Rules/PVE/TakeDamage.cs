using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using System;
using System.Text.Json.Serialization;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    [Serializable]
    public class TakeDamage : RuleBase
    {
        override public string Name { get; } = "Take Damage";
        override public string Description { get; } = "Triggers whenever you Take Damage for any reason.";
        override public string Hint { get; } = "This will go off ALOT.\nLiterally any damage counts.\nFrom mechanics to auto attacks to dots or even fall damage!";
        override public RuleCategory Category { get; } = RuleCategory.PVE;
        override public bool hasExtraButton { get; } = true;
        public bool isProportional { get; set; } = false;
        public int minimumDamagePercent { get; set; } = 0;

        [JsonIgnore] IPlayerCharacter Player;
        [JsonIgnore] uint lastHP = 1, lastMaxHP = 1;
        [JsonIgnore] int bufferFrames = 0;

        [JsonConstructor]
        public TakeDamage() { }
        public TakeDamage(Plugin plugin) : base(plugin)
        {
        }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Service.Framework.Update += Check;
            Player = Service.ObjectTable.LocalPlayer;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.Framework.Update -= Check;
        }

        private void Check(IFramework framework)
        {
            try
            {
                Player = Service.ObjectTable.LocalPlayer;
                if (Player == null) { return; }

                if (bufferFrames > 0)
                {
                    bufferFrames--;
                    return;
                }

                if (lastMaxHP != Player.MaxHp && !Player.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat)) // out of combat maxhp increase
                {
                    Logger.Log(3, $"{Name} | MaxHP changed out of Combat.");
                    lastMaxHP = Player.MaxHp;
                    lastHP = lastMaxHP; // avoid false positives from synch and stuff
                    bufferFrames = 600; // give 10 seconds of buffering, for regens and stuff
                    return;
                }
                else if (lastMaxHP != Player.MaxHp) // in combat maxhp increase
                {
                    Logger.Log(3, $"{Name} | MaxHP changed inside Combat.");
                    lastMaxHP = Player.MaxHp;
                    lastHP = lastMaxHP; // avoid false positives from synch and stuff
                    bufferFrames = 180; // give 3 seconds of buffering, for regens and stuff
                    return;
                }

                if (lastHP > Player.CurrentHp)
                {

                    int damageTaken = (int)(lastHP - Player.CurrentHp);
                    double difference = (double)damageTaken / lastMaxHP;

                    Logger.Log(3, $"{Name} | Damage taken: " + damageTaken + " Dif: " + (int)(difference * 100));
                    if (minimumDamagePercent > difference * 100)
                    {
                        lastHP = Player.CurrentHp;
                        return;
                    }

                    if (!isProportional) Trigger("You took damage!");
                    else
                    {

                        int[] opts = { (int)(ShockOptions.Intensity * difference), (int)(ShockOptions.Duration * difference) };

                        if (opts[0] <= 0) opts[0] = 1;
                        if (opts[1] < 100 && opts[1] > 10) opts[1] = 100;
                        Logger.Log(3, $"{Name} | Proportional is enabled." +
                            "\n int: " + opts[0] + " dur: " + opts[1]);
                        Trigger("You took damage!", null, opts);
                    }
                }
                lastHP = Player.CurrentHp;
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace); }
        }

        public override void DrawExtraButton()
        {
            ImGui.SameLine();
            if (isProportional)
            {
                if (ImGui.Button("Disable Proportional##TakeDamageProportionalDisable"))
                {
                    isProportional = false;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Disabling this will cause the Options below to trigger everytime you take damage.");
                }
            }
            else
            {
                if (ImGui.Button("Enable Proportional##TakeDamageProportionalEnable"))
                {
                    isProportional = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Enabling this will cause the Options below to be proportional to the damage you take." +
                        "\nIf you take 50 percent of your MaxHP as damage, then 50 percent of the options below will be used." +
                        "\nBasically, the harder you get hit, the closer it gets to the settings below.");
                }
            }
            ImGui.SameLine();
            int minimumDamagePercentInput = minimumDamagePercent;
            ImGui.SetNextItemWidth(110);
            if (ImGui.SliderInt("Minimum Damage% Taken##TakeDamageMinimumDamageSlider", ref minimumDamagePercentInput, 0, 100))
            {
                minimumDamagePercent = minimumDamagePercentInput;
                Plugin.Configuration.saveCurrentPreset();
            }
        }

    }
}
