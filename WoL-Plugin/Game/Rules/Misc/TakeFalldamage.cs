using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.Toast;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util;
using WoLightning.WoL_Plugin.Util.Helpers;

namespace WoLightning.WoL_Plugin.Game.Rules.Misc
{
    [Serializable]
    public class TakeFalldamage : RuleBase
    {
        public override string Name { get; } = "Take Falldamage";

        public override string Description { get; } = "Triggers whenever you take Falldamage out of combat specifically.";
        public override string Hint { get; } = "Please note that due to code limitations, any damage taken while airborne will count as falldamage.";

        public override RuleCategory Category { get; } = RuleCategory.Misc;
        override public bool hasExtraButton { get; } = true;
        public bool isProportional { get; set; } = false;
        public int minimumDamagePercent { get; set; } = 0;

        [JsonIgnore] IPlayerCharacter? Player;
        [JsonIgnore] double buffer = 0.5;
        [JsonIgnore] uint lastHP = 1, lastMaxHP = 1;
        [JsonIgnore] Vector3 lastPosition = new Vector3();
        [JsonIgnore] bool isFalling = false;

        [JsonConstructor]
        public TakeFalldamage() { }
        public TakeFalldamage(Plugin plugin) : base(plugin)
        {
        }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Service.Framework.Update += Check;
            Player = Service.ObjectTable.LocalPlayer;
            lastPosition = Player.Position;
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

                if (buffer > 0)
                {
                    buffer -= framework.UpdateDelta.TotalSeconds;
                    return;
                }

                buffer = 0.5;

                if (Player.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat))
                {
                    buffer = 5;
                    lastHP = Player.CurrentHp;
                    lastMaxHP = Player.MaxHp;
                    lastPosition = Player.Position;
                    return;
                }

                if (isFalling)
                {
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

                        if (!isProportional) Trigger("You took Falldamage!");
                        else
                        {

                            ShockOptions opts = new(ShockOptions);

                            opts.Intensity = (int)(ShockOptions.Intensity * difference);
                            opts.Duration = (int)(ShockOptions.Duration * difference);

                            if (opts.Intensity <= 0) opts.Intensity = 1;
                            if (opts.Duration < 100 && opts.Duration > 10) opts.Duration = 100;

                            Logger.Log(3, $"{Name} | Proportional is enabled." +
                                "\n int: " + opts.Intensity + " dur: " + opts.Duration);

                            Trigger("You took Falldamage!", null, opts);
                        }
                        isFalling = false;
                        return;
                    }
                }


                if (lastPosition.Y > Player.Position.Y + 0.2)
                    isFalling = true;
                else if (lastPosition.Y <= Player.Position.Y)
                    isFalling = false;

                //Logger.Log(4, $"Last: {lastPosition.Y} Current: {Player.Position.Y} Falling?: {isFalling}");

                lastPosition = Player.Position;
                lastHP = Player.CurrentHp;
                lastMaxHP = Player.MaxHp;
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace); }

        }

        public override void DrawExtraButton()
        {
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
            if (ImGui.SliderInt("Minimum Damage% Taken##TakeFalldamageMinimumDamageSlider", ref minimumDamagePercentInput, 0, 100))
            {
                minimumDamagePercent = minimumDamagePercentInput;
                Plugin.Configuration.SaveCurrentPresetScheduled();
            }
        }
    }
}
