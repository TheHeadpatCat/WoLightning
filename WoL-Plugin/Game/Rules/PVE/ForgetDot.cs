using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using System;
using System.Text.Json.Serialization;
using System.Timers;
using WoLightning.Util;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util;
using WoLightning.WoL_Plugin.Util.UI_Elements;

namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    public class ForgetDot : RuleBase
    {
        public override string Name { get; } = "Forget your Dot";

        public override string Description { get; } = "Triggers whenever you don't reapply your Dot within a certain timeframe.";
        public override string Hint { get; } = "Multi-Target Dots do not count, only Single-Target dots.\nIf the boss becomes untargetable, the timeframe starts once they become targetable again.\nCurrently Supported Jobs are: WHM, SCH, AST, SGE, DRG, SAM, RPR, BLM(only single target dots), BRD(only checks for 1 dot)";
        public override RuleCategory Category { get; } = RuleCategory.PVE;
        public override bool hasExtraButton { get; } = true;

        public int GraceTime { get; set; } = 5;
        public bool IsRepeating { get; set; } = false;

        [JsonIgnore] bool InCombat = false;
        [JsonIgnore] IPlayerCharacter? Player;

        [JsonIgnore] IBattleChara? LastTarget;
        [JsonIgnore] bool IsUntargetable = false;

        [JsonIgnore] TimerPlus DotTimer = new();
        [JsonIgnore] TimerPlus GraceTimer = new();

        [JsonIgnore] static readonly double UpdateInterval = 150;
        [JsonIgnore] public double UpdateDelta = 150;
        public ForgetDot() { }
        public ForgetDot(Plugin plugin) : base(plugin) { }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Service.Condition.ConditionChange += OnConditionChange;
            DotTimer.AutoReset = false;
            DotTimer.Elapsed += DotRanOut;
            GraceTimer.AutoReset = false;
            GraceTimer.Elapsed += GraceRanOut;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.Condition.ConditionChange -= OnConditionChange;
            Service.Framework.Update -= OnUpdate;
            DotTimer.Elapsed -= DotRanOut;
            GraceTimer.Elapsed -= GraceRanOut;
        }

        

        private void OnConditionChange(ConditionFlag flag, bool value)
        {
            if (flag != ConditionFlag.InCombat) return;
            InCombat = value;

            if (InCombat)
            {
                //if (!Service.Condition.Any(ConditionFlag.BoundByDuty, ConditionFlag.BoundByDuty56, ConditionFlag.BoundByDuty95)) return; // we arent actually in any content right now
                UpdateDelta = UpdateInterval;
                Service.Framework.Update += OnUpdate;
                Logger.Log(4, "Entered Combat.");
            }
            else
            {
                Service.Framework.Update -= OnUpdate;
                GraceTimer.Stop();
                DotTimer.Stop();
                LastTarget = null;
                IsUntargetable = false;
                Logger.Log(4, "Exited Combat.");
            }
        }

        private void OnUpdate(IFramework framework)
        {
            if (DotTimer.Enabled || GraceTimer.Enabled) return; // would love to just unsubscribe, but that leads to possible edge cases...

            if (UpdateDelta > 0)
            {
                UpdateDelta -= framework.UpdateDelta.TotalMilliseconds;
                return;
            }
            UpdateDelta = UpdateInterval;

            if (IsUntargetable)
            {
                if (LastTarget == null) return;
                if (!LastTarget.IsValid()) { LastTarget = null; return; }
                if (!LastTarget.IsTargetable) return;

                Logger.Log(4, "Boss is targetable again, starting grace...");
                IsUntargetable = false;
                if(FindDot(LastTarget) == null) GraceTimer.Start();
                return;
            }

            Player = Service.ObjectTable.LocalPlayer;
            if (Player == null) return;

            if (Player.TargetObject == null) return;

            if (Player.TargetObject is IBattleNpc)
            {
                //Logger.Log(4, $"{Player.TargetObject.Name} is Battle NPC");
                IBattleNpc npc = (IBattleNpc)Player.TargetObject;

                IStatus? dot = FindDot(npc);
                if (dot != null)
                {
                    LastTarget = npc;
                    Logger.Log(4, $"{Player.TargetObject.Name} is Dot Target.");
                    DotTimer.Interval = dot.RemainingTime * 1000 + 300;
                    DotTimer.Start();
                    Logger.Log(4, $"Starting Timer for {dot.GameData.Value.Name} with {dot.RemainingTime}s");
                }
            }
        }

        private void DotRanOut(object? sender, ElapsedEventArgs e)
        {
            Logger.Log(4, "Dot Ran out, validating...");
            if (LastTarget == null) return;
            if (!LastTarget.IsValid()) { LastTarget = null; return; }
            if (!LastTarget.IsTargetable) { IsUntargetable = true; Logger.Log(4, "Boss is Untargetable, waiting..."); return; }

            IStatus? dot = FindDot(LastTarget);
            if (dot != null)
            {
                Logger.Log(4, $"Dot was reapplied in time.");
                DotTimer.Interval = dot.RemainingTime * 1000 + 300;
                DotTimer.Start();
                Logger.Log(4, $"Starting Timer for {dot.GameData.Value.Name} with {dot.RemainingTime}s");
                return;
            }

            Logger.Log(4, "Validated, starting grace...");
            GraceTimer.Interval = GraceTime * 1000 + 300;
            GraceTimer.Start();
        }

        private void GraceRanOut(object? sender, ElapsedEventArgs e)
        {
            Logger.Log(4, "Grace ran out, validating...");
            if (LastTarget == null) return;
            if (!LastTarget.IsValid()) { LastTarget = null; return; }
            if (!LastTarget.IsTargetable) { IsUntargetable = true; Logger.Log(4, "Boss is Untargetable, resetting..."); return; }

            IStatus? dot = FindDot(LastTarget);
            if (dot != null)
            {
                Logger.Log(4, $"Dot was reapplied in time.");
                DotTimer.Interval = dot.RemainingTime * 1000 + 300;
                DotTimer.Start();
                Logger.Log(4, $"Starting Timer for {dot.GameData.Value.Name} with {dot.RemainingTime}s");
                return;
            }

            Logger.Log(4, "Validated, sending request!");
            Trigger("You forgot your Dot!");
            if (IsRepeating)
            {
                DotTimer.Interval = ShockOptions.getDurationOpenShock() + 1500;
                DotTimer.Start();
            }
        }

        private IStatus? FindDot(IBattleChara npc)
        {
            foreach (var status in npc.StatusList)
            {
                if (Player.GameObjectId != status.SourceId) continue;

                //Logger.Log(4, $"Player inflicted Status: {status.StatusId}");

                if (IsDot(status.StatusId))
                {
                    Logger.Log(4, $"Found Dot: {status.StatusId}");
                    return status;
                }
            }
            return null;
        }

        // yeah this looks terrible, but it kinda needs to be performant, so a list isnt a good idea.

        private bool IsDot(uint id)
        { 
            return  id == 143 || id == 144 || id == 1871               // WHM: Aero, Aero II, Dia
                 || id == 179 || id == 189 || id == 1895               // SCH: Bio, Bio II, Biolysis
                 || id == 838 || id == 843 || id == 1881               // AST: Combust, Combust II, Combust III
                 || id == 2614|| id == 2615|| id == 2616               // SGE: Eukrasian Dosis, Eukrasian Dosis II, Eukrasian Dosis III
                 || id == 118 || id == 2719                            // DRG: Chaos Thrust, Chaotic Spring
                 || id == 1228                                         // SAM: Higanbana
                 || id == 2586                                         // RPR: Death's Design
                 || id == 124 || id == 128 || id == 1200 || id == 1201 // BRD: Venomous Bite, Windbite, Caustic Bite, Stormbite
                 || id == 161 || id == 3871;                           // BLM: Thunder, High Thunder
        }


        public override void DrawExtraButton()
        {
            ImGui.Separator();
            int grace = GraceTime;
            ImGui.SetNextItemWidth(250);
            if(ImGui.SliderInt("Grace period (s)##graceForgetDot",ref grace,2,30))
            {
                GraceTime = grace;
                Plugin.Configuration.SaveCurrentPresetScheduled();
            }
            bool repeat = IsRepeating;
            if (ImGui.Checkbox("Keep Triggering until reapplied?##repeatForgetDot", ref repeat))
            {
                IsRepeating = repeat;
                Plugin.Configuration.SaveCurrentPresetScheduled();
            }
            ImGui.Separator();
        }
    }
}
