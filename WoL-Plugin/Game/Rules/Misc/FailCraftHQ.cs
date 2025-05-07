using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Inventory;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Common.Component.Excel;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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
            Player = Service.ClientState.LocalPlayer;

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
                Logger.Log(4, $"{type} {data} - isCrafing {isCrafting}");
                if (type != GameInventoryEvent.Added) return;
                if (!isCrafting) return;
                Item? itemData = Service.DataManager.GetExcelSheet<Item>().GetRowOrDefault(data.Item.BaseItemId);
                if (itemData == null) return;

                Logger.Log(3, $"MaxQuality: {MaxQuality} Reached Quality: {CurrentQuality} which is {(double)CurrentQuality / MaxQuality * 100}%");

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
            catch (Exception ex) { }
        }

        private void UpdateState(ConditionFlag flag, bool value)
        {
            try
            {
                Logger.Log(4, "Flag " + flag + " changed to " + value);

                if (Player == null || Player.MaxCp == 0)
                {
                    Player = Service.ClientState.LocalPlayer;
                    isCrafting = false;
                    return;
                }

                if (flag == ConditionFlag.Crafting)
                {
                    isCrafting = value;

                    /*
                    var agent = AgentRecipeNote.Instance();
                    CurrentRecipe = Service.DataManager.GetExcelSheet<Recipe>().GetRowOrDefault(agent->ActiveCraftRecipeId);
                    if (CurrentRecipe == null) return;

                    var table = Service.DataManager.GetExcelSheet<RecipeLevelTable>();
                    //table.GetRow(CurrentRecipe.Value.RecipeLevelTable.RowId).Quality
                    var adjustTable = Service.DataManager.GetExcelSheet<GathererCrafterLvAdjustTable>();
                    var resolvedLevelTableRow = CurrentRecipe.Value.RecipeLevelTable.RowId;

                    var t = adjustTable.GetRow(CurrentRecipe.Value.MaxAdjustableJobLevel).RecipeLevel;




                    var MaxQuality = (CurrentRecipe.Value.CanHq || CurrentRecipe.Value.IsExpert) ? (int)table.GetRow(t).Quality * CurrentRecipe.Value.QualityFactor / 100 : 0;
                    if (CurrentRecipe != null) Logger.Log(4, "Max Quality: " + MaxQuality);
                    */
                }
            }
            catch (Exception ex) { }
        }

        private unsafe async void UpdateQuality(IFramework framework)
        {
            if (!isCrafting) return;
            try
            {
                nint pointerToSynthesis;
                AddonSynthesis addonSynthesis;
                pointerToSynthesis = Service.GameGui.GetAddonByName("Synthesis");
                addonSynthesis = Dalamud.Memory.MemoryHelper.Cast<AddonSynthesis>(pointerToSynthesis);

                var currentQuality = addonSynthesis.GetNodeById(62)->GetAsAtkTextNode()->NodeText;
                var maxQuality = addonSynthesis.GetNodeById(63)->GetAsAtkTextNode()->NodeText;

                var currentProgress = addonSynthesis.GetNodeById(56)->GetAsAtkTextNode()->NodeText;
                var maxProgress = addonSynthesis.GetNodeById(57)->GetAsAtkTextNode()->NodeText;

                if (currentQuality.Length > 0)
                    CurrentQuality = int.Parse(currentQuality);

                if (maxQuality.Length > 0)
                    MaxQuality = int.Parse(maxQuality);

                //Logger.Log(4, $"{int.Parse(currentProgress)}/{int.Parse(maxProgress)} and isTriggered {!FallbackTimer.Enabled}");

                if (int.Parse(currentProgress) == int.Parse(maxProgress) && !FallbackTimer.Enabled)
                {
                    FallbackTimer.Start();
                    Logger.Log(4, "Started Timer.");
                }

                if (int.Parse(currentProgress) == 0)
                {
                    IsTriggered = false;
                    FallbackTimer.Stop();
                }


            }
            catch (Exception e) { }
        }

        private void RunFallback(object? sender, ElapsedEventArgs e)
        {
            Logger.Log(4, $"Fallback called. will run? {!IsTriggered}");
            if(!IsTriggered)
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
            ImGui.SameLine();
            int minimumCollectabilitySlide = (int)MinimumQualityPercent;
            ImGui.SetNextItemWidth(250);
            if (ImGui.SliderInt("Minimum Quality in %", ref minimumCollectabilitySlide, 0, 100))
            {
                MinimumQualityPercent = (uint)minimumCollectabilitySlide;
                Plugin.Configuration.saveCurrentPreset();
            }
        }

    }
}
