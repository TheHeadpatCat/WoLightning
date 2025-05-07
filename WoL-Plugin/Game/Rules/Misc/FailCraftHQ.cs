using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Inventory;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System;
using System.Text.Json.Serialization;
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
        public uint MinimumCollectability { get; set; } = 0;
        [JsonIgnore] bool isCrafting { get; set; }

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
            Service.Condition.ConditionChange += UpdateCondition;
            Service.GameInventory.ItemAdded += Check;
            Player = Service.ClientState.LocalPlayer;
        }



        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.GameInventory.ItemAdded -= Check;
            Service.Condition.ConditionChange -= UpdateCondition;
        }

        private void Check(GameInventoryEvent type, InventoryEventArgs data)
        {
            Logger.Log(4, $"{type} {data} - isCrafing {isCrafting}");
            if (type != GameInventoryEvent.Added) return;
            if (!isCrafting) return;
            Item? itemData = Service.DataManager.GetExcelSheet<Item>().GetRowOrDefault(data.Item.BaseItemId);
            if (itemData == null) return;

            Logger.Log(4, itemData.Value);

            if (itemData.Value.CanBeHq && !data.Item.IsHq)
            {
                Trigger("You failed to craft a HQ Item!");
                return;
            }

            if(itemData.Value.IsCollectable && data.Item.SpiritbondOrCollectability < MinimumCollectability)
            {
                Trigger($"You failed to reach {MinimumCollectability} Collectability!");
                return;
            }
        }

        private unsafe void UpdateCondition(ConditionFlag flag, bool value)
        {
            Logger.Log(4, "Flag " + flag + " changed to " + value);

            if (Player == null || Player.MaxCp == 0)
            {
                Player = Service.ClientState.LocalPlayer;
                isCrafting = false;
                return;
            }

            if (flag == ConditionFlag.Crafting) isCrafting = value;
        }

        public override void DrawExtraButton()
        {
            ImGui.SameLine();
            int minimumCollectabilitySlide = (int)MinimumCollectability;
            ImGui.SetNextItemWidth(250);
            if (ImGui.SliderInt("Minimum Collectability", ref minimumCollectabilitySlide, 0, 1000))
            {
                MinimumCollectability = (uint)minimumCollectabilitySlide;
                Plugin.Configuration.saveCurrentPreset();
            }
        }

    }
}
