using Dalamud.Game.Inventory;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WoLightning.Util;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.Misc
{
    public class UseTeleport : RuleBase
    {
        override public string Name { get; } = "Use Teleportation";
        override public string Description { get; } = "Triggers whenever you change Areas using Teleportation.";
        override public RuleCategory Category { get; } = RuleCategory.Misc;
        override public bool hasExtraButton { get; } = true;

        public int MaximumGil { get; set; } = 0;

        [JsonIgnore] int LastKnownGil = -1;

        [JsonConstructor]
        public UseTeleport() { }
        public UseTeleport(Plugin plugin) : base(plugin)
        {
        }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Service.ClientState.TerritoryChanged += Check;
            LastKnownGil = GetCurrentGil();
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.ClientState.TerritoryChanged -= Check;
        }

        private void Check(ushort obj)
        {
            int DifferenceGil = LastKnownGil - GetCurrentGil();
            LastKnownGil = GetCurrentGil();
            if (DifferenceGil == 0) return; // We didnt teleport.
            if (MaximumGil == 0) { Trigger("You used Teleportation!"); return; }
            if (DifferenceGil > MaximumGil) { Trigger("You exceeded the Teleportation cost!"); return; }
        }

        private int GetCurrentGil()
        {
            int output = -1;
            foreach(var item in Service.GameInventory.GetInventoryItems(GameInventoryType.Currency))
            {
                if(item.ItemId == 1)
                {
                    output = item.Quantity;
                    break;
                }
            }
            return output;
        }

        public override void DrawExtraButton()
        {
            ImGui.SameLine();
            int maximumGil = MaximumGil;
            ImGui.SetNextItemWidth(250);
            if (ImGui.SliderInt("Max Gil cost",ref maximumGil, 0, 1500))
            {
                MaximumGil = maximumGil;
                Plugin.Configuration.saveCurrentPreset();
            }
        }

    }
}
