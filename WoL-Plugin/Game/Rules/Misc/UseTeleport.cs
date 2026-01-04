using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Inventory;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using WoLightning.WoL_Plugin.Util;
using WoLightning.WoL_Plugin.Util.UI_Elements;

namespace WoLightning.WoL_Plugin.Game.Rules.Misc
{
    public class UseTeleport : RuleBase
    {
        override public string Name { get; } = "Use Teleportation";
        override public string Description { get; } = "Triggers whenever you change Areas using Teleportation.";
        override public RuleCategory Category { get; } = RuleCategory.Misc;
        override public bool hasExtraButton { get; } = true;

        public bool UseCosts { get; set; } = false;
        public int MinimumGil { get; set; } = 0;
        public int MaximumGil { get; set; } = 2000;

        [JsonIgnore] int LastKnownGil = -1;
        [JsonIgnore] bool DidUseCast = false;

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
            Service.GameInventory.ItemChanged += HandleItemUpdate;
            Service.Condition.ConditionChange += HandleFlagUpdate;
            LastKnownGil = GetCurrentGil();
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.ClientState.TerritoryChanged -= Check;
            Service.GameInventory.ItemChanged -= HandleItemUpdate;
            Service.Condition.ConditionChange -= HandleFlagUpdate;
        }

        private void Check(ushort obj)
        {
            try
            {
                int DifferenceGil = LastKnownGil - GetCurrentGil();
                LastKnownGil = GetCurrentGil();

                if (!DidUseCast) return; // We didnt cast anything - so its impossible that we teleported.
                DidUseCast = false;

                if (DifferenceGil == 0) return; // We didnt use any money - same thing here.

                if (!UseCosts) { Trigger("You used Teleportation!"); return; }

                if (LastKnownGil == -1) return;

                if (DifferenceGil > MaximumGil) { Trigger("You exceeded the Teleportation cost!"); return; }
                if (DifferenceGil < MinimumGil) { Trigger("You didnt hit the Teleportation cost!"); return; }
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace); }
        }

        private void HandleFlagUpdate(ConditionFlag flag, bool value)
        {
            if (flag == ConditionFlag.Casting)
            {
                DidUseCast = true;
                return;
            }

            if (flag != ConditionFlag.BetweenAreas && flag != ConditionFlag.BetweenAreas51)
            {
                LastKnownGil = GetCurrentGil();
            }
        }

        private async void HandleItemUpdate(GameInventoryEvent type, InventoryEventArgs data)
        {
            if (data.Item.ItemId == 1)
            {
                Task.Run(() => { Task.Delay(2000).Wait(); LastKnownGil = data.Item.Quantity; Logger.Log(4, "Changed gil to " + LastKnownGil); }).WaitAsync(CancellationToken.None);
            }
        }

        private int GetCurrentGil()
        {
            int output = -1;
            try
            {
                foreach (var item in Service.GameInventory.GetInventoryItems(GameInventoryType.Currency))
                {
                    if (item.ItemId == 1)
                    {
                        output = item.Quantity;
                        break;
                    }
                }
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace); }
            return output;
        }

        public override void DrawExtraButton()
        {

            bool useCosts = UseCosts;
            if (ImGui.Checkbox("Use Costs", ref useCosts))
            {
                UseCosts = useCosts;
                Plugin.Configuration.SaveCurrentPresetScheduled();
            }
            HoverText.ShowHint("Allows you to set a minimum and maximum Cost for teleportation." +
                "\nMinimum as in \"the teleport has to cost atleast this much gil\"." +
                "\nMaximum as in \"the teleport is not allowed to cost more than this\".");

            if (!UseCosts) return;
            ImGui.BeginGroup();

            int minimumGil = MinimumGil;
            if (minimumGil > MaximumGil) MaximumGil = minimumGil;
            ImGui.SetNextItemWidth(250);
            if (ImGui.SliderInt("Minimum Cost", ref minimumGil, 0, 2000))
            {
                MinimumGil = minimumGil;
                Plugin.Configuration.SaveCurrentPresetScheduled();
            }
            int maximumGil = MaximumGil;

            ImGui.SetNextItemWidth(250);
            if (ImGui.SliderInt("Maximum Cost", ref maximumGil, 0, 2000))
            {
                if (maximumGil < MinimumGil) MinimumGil = maximumGil;
                MaximumGil = maximumGil;
                Plugin.Configuration.SaveCurrentPresetScheduled();
            }
            ImGui.EndGroup();
        }

    }
}
