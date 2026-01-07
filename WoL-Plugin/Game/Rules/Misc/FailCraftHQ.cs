using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Inventory;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Excel.Sheets;
using System;
using System.Text.Json.Serialization;
using System.Timers;
using WoLightning.Util;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.Misc
{
    [Serializable]
    public class FailCraftHQ : RuleBase
    {
        override public string Name { get; } = "Fail a HQ Craft";
        override public string Description { get; } = "Triggers whenever you fail to craft a HQ Item when it would have been possible.";
        override public RuleCategory Category { get; } = RuleCategory.Misc;
        override public bool hasExtraButton { get; } = true;
        public uint MinimumQualityPercent { get; set; } = 0;
        [JsonIgnore] bool isCrafting;
        [JsonIgnore] int CurrentProgress = 0;
        [JsonIgnore] int MaxProgress = 0;
        [JsonIgnore] int CurrentQuality = 0;
        [JsonIgnore] int MaxQuality = 0;
        [JsonIgnore] bool IsTriggered = false;
        [JsonIgnore] TimerPlus FallbackTimer = new();
        [JsonIgnore] IPlayerCharacter Player;


        [JsonConstructor]
        public FailCraftHQ() { }
        public FailCraftHQ(Plugin plugin) : base(plugin)
        {
        }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Service.Condition.ConditionChange += UpdateState;
            Service.Framework.Update += UpdateQuality;
            Service.GameInventory.ItemAdded += Check;
            Player = Service.ObjectTable.LocalPlayer;

            IsTriggered = false;

            FallbackTimer.Interval = 1500;
            FallbackTimer.AutoReset = false;
            FallbackTimer.Elapsed += RunFallback;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.GameInventory.ItemAdded -= Check;
            Service.Condition.ConditionChange -= UpdateState;
            Service.Framework.Update -= UpdateQuality;
            FallbackTimer.Stop();
            FallbackTimer.Elapsed -= RunFallback;
        }

        private void Check(GameInventoryEvent type, InventoryEventArgs data)
        {
            try
            {
                Logger.Log(4, $"{Name} | {type} {data} - isCrafing {isCrafting}");
                if (type != GameInventoryEvent.Added) return;
                if (!isCrafting) return;
                Item? itemData = Service.DataManager.GetExcelSheet<Item>().GetRowOrDefault(data.Item.BaseItemId);
                if (itemData == null) return;

                Logger.Log(3, $"{Name} | MaxQuality: {MaxQuality} Reached Quality: {CurrentQuality} which is {(double)CurrentQuality / MaxQuality * 100}%");

                if (MaxQuality > 0 && (double)CurrentQuality / MaxQuality * 100 < MinimumQualityPercent)
                {
                    Trigger($"You failed to reach {MinimumQualityPercent}% Quality!");
                    IsTriggered = true;
                    return;
                }

                if (itemData.Value.CanBeHq && !data.Item.IsHq)
                {
                    Trigger("You failed to craft a HQ Item!");
                    IsTriggered = true;
                    return;
                }


            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace); }
        }

        private void UpdateState(ConditionFlag flag, bool value)
        {
            try
            {
                Logger.Log(4, "Flag " + flag + " changed to " + value);

                if (Player == null || Player.MaxCp == 0)
                {
                    Player = Service.ObjectTable.LocalPlayer;
                    isCrafting = false;
                    return;
                }

                if (flag == ConditionFlag.Crafting)
                {
                    isCrafting = value;
                }
            }
            catch (Exception e) { Logger.Error(Name + " UpdateState() failed."); Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace); }
        }

        private unsafe async void UpdateQuality(IFramework framework)
        {
            if (!isCrafting) return;
            try
            {
                nint pointerToSynthesis;
                AddonSynthesis addonSynthesis;
                pointerToSynthesis = Service.GameGui.GetAddonByName("Synthesis");
                if (pointerToSynthesis == 0 || pointerToSynthesis == -1) return;
                addonSynthesis = Dalamud.Memory.MemoryHelper.Cast<AddonSynthesis>(pointerToSynthesis);

                var currentQuality = addonSynthesis.GetNodeById(62)->GetAsAtkTextNode()->NodeText;
                var maxQuality = addonSynthesis.GetNodeById(63)->GetAsAtkTextNode()->NodeText;

                var currentProgress = addonSynthesis.GetNodeById(56)->GetAsAtkTextNode()->NodeText;
                var maxProgress = addonSynthesis.GetNodeById(57)->GetAsAtkTextNode()->NodeText;

                if (currentQuality.Length > 0)
                    CurrentQuality = int.Parse(currentQuality);

                if (maxQuality.Length > 0)
                    MaxQuality = int.Parse(maxQuality);

                if (currentProgress.Length > 0)
                    CurrentProgress = int.Parse(currentProgress);

                if (maxProgress.Length > 0)
                    MaxProgress = int.Parse(maxProgress);

                if (MaxProgress == 0 || MaxQuality == 0)
                    return;

                //Logger.Log(4, $"{int.Parse(currentProgress)}/{int.Parse(maxProgress)} and isTriggered {!FallbackTimer.Enabled}");

                if (CurrentProgress == MaxProgress && !FallbackTimer.Enabled)
                {
                    FallbackTimer.Start();
                    Logger.Log(4, "Started Timer.");
                }

                if (CurrentProgress == 0)
                {
                    IsTriggered = false;
                    FallbackTimer.Stop();
                }


            }
            catch (Exception e) { Logger.Error(Name + " UpdateQuality() failed."); Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace); }
        }

        private void RunFallback(object? sender, ElapsedEventArgs e)
        {
            Logger.Log(4, $"Fallback called. will run? {!IsTriggered}");
            if (!IsTriggered)
            {
                Logger.Log(4, $"{MaxQuality} and {(double)CurrentQuality / MaxQuality * 100 < MinimumQualityPercent}");
                if (MaxQuality > 0 && (double)CurrentQuality / MaxQuality * 100 < MinimumQualityPercent)
                {
                    Trigger($"You failed to reach {MinimumQualityPercent}% Quality!");
                    IsTriggered = true;
                    return;
                }
            }
        }



        public override void DrawExtraButton()
        {

            int minimumCollectabilitySlide = (int)MinimumQualityPercent;
            ImGui.SetNextItemWidth(250);
            if (ImGui.SliderInt("Minimum Quality in %", ref minimumCollectabilitySlide, 0, 100))
            {
                MinimumQualityPercent = (uint)minimumCollectabilitySlide;
                Plugin.Configuration.SaveCurrentPresetScheduled();
            }
        }

    }
}
