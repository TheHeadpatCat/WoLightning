using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using WoLightning.WoL_Plugin.Util;
using WoLightning.WoL_Plugin.Util.Helpers;
using WoLightning.WoL_Plugin.Util.UI_Elements;
using static FFXIVClientStructs.FFXIV.Client.Graphics.Kernel.VertexShader;

namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    public enum ContentTypeWoL
    {
        None = 0,
        Any = 1,
        LightParty = 2,
        Dungeon = 3,
        Trial = 4,
        Raid = 5,
        Extreme = 6,
        Unreal = 7,
        Savage = 8,
        Ultimate = 9,
    }
    public class ForgetFood : RuleBase
    {

        public override string Name { get; } = "Forget to eat Food";
        public override string Description { get; } = "Triggers whenever you enter combat and/or start crafting without a food buff.";
        public override RuleCategory Category { get; } = RuleCategory.Misc;

        public override bool hasExtraButton { get; } = true;

        public bool IsTriggeredByCrafting { get; set; } = true;
        public Dictionary<ContentTypeWoL,bool> IsTriggeredByContent { get; set; } = new();

        [JsonIgnore] IPlayerCharacter Player;
        [JsonIgnore] bool IsCrafting = false;
        [JsonIgnore] bool IsPreparingToCraft = false;
        [JsonIgnore] bool IsContentModalOpen = false;

        public ForgetFood() {
            CreateDictionary();
        }
        public ForgetFood(Plugin plugin) : base(plugin)
        {
            CreateDictionary();
        }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Service.Condition.ConditionChange += OnConditionChange;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.Condition.ConditionChange -= OnConditionChange;
        }

        private void OnConditionChange(ConditionFlag flag, bool value)
        {
            if (flag == ConditionFlag.InCombat && value == true)
            {
                CheckCombat();
                return;
            }

            if (!IsTriggeredByCrafting) return;

            if (flag == ConditionFlag.Crafting)
            {
                IsCrafting = value;
                if (IsCrafting) CheckCrafting(); // Started Crafting now
                return;
            }

            if (flag == ConditionFlag.PreparingToCraft)
            {
                IsPreparingToCraft = value;
                if (IsCrafting && !IsPreparingToCraft) CheckCrafting(); // Crafted before and is continueing to craft.
                return;
            }
        }

        private unsafe void CheckCombat()
        {
            Player = Service.ObjectTable.LocalPlayer;
            if (Player == null) return;

            

            Logger.Log(4, "Territory: " + Service.ClientState.TerritoryType);

            if (HasFoodBuff()) return;

            if (IsTriggeredByContent[ContentTypeWoL.Any])
            {
                Trigger("You entered Combat without food!");
                return;
            }

            //if (Service.PartyList.Count < 4) return; // If party is below 4 
            if (!Service.Condition.Any(ConditionFlag.BoundByDuty, ConditionFlag.BoundByDuty56, ConditionFlag.BoundByDuty95)) return; // we arent actually in any content right now

            if (IsTriggeredByContent[ContentTypeWoL.LightParty])
            {
                Trigger("You entered Combat without food!");
                return;
            }

            // Todo: move this to helper class
            TerritoryType territory;
            string territoryName;
            uint territoryContentType;

            try
            {
                territory = Service.DataManager.GetExcelSheet<TerritoryType>().GetRow(Service.ClientState.TerritoryType);
                
                territoryName = territory.ContentFinderCondition.Value.Name.ExtractText();
                territoryContentType = territory.ContentFinderCondition.Value.ContentType.Value.RowId;
                Logger.Log(4, "Territory: " + territoryName + " ContentType: " + territoryContentType);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to extract Duty Name from territory ID " + Service.ClientState.TerritoryType);
                return;
            }

            if (   (IsTriggeredByContent[ContentTypeWoL.Trial] && territoryContentType == 4)
                || (IsTriggeredByContent[ContentTypeWoL.Raid] && territoryContentType == 5)
                || (IsTriggeredByContent[ContentTypeWoL.Extreme] && territoryContentType == 38)) // Unreal
            {
                Trigger("You entered Combat without food!");
                return;
            }

            if (Service.PartyList.IsAlliance) return; // Dont let Alliance raids through. Sometimes people place waymarks in them.

            // Todo: move this to helper class
            bool isMarkerPlaced = false;
            try
            {
                var agent = MarkingController.Instance(); 
                for (int i = 0; i < 8; i++)
                {
                    Logger.Log(4,$"Marker {i} placed: " + agent->FieldMarkers[i].Active);
                    if (agent->FieldMarkers[i].Active) isMarkerPlaced = true;
                }
            }
            catch (Exception e) {
                Logger.Error("Failed to check Markers.");
                Logger.Error(e.Message);
            }

            if (!isMarkerPlaced) return; // assume we arent doing hard content, if there isnt a single marker placed.

            if (   (IsTriggeredByContent[ContentTypeWoL.Extreme] && territoryContentType == 4)
                || (IsTriggeredByContent[ContentTypeWoL.Savage] && territoryContentType == 5)
                || (IsTriggeredByContent[ContentTypeWoL.Ultimate] && territoryContentType == 28)
                )
            {
                Trigger("You entered Combat without food!");
                return;
            }
        }

        private void CheckCrafting()
        {
            if (!HasFoodBuff()) Trigger("You forgot to eat food!");
        }

        private bool HasFoodBuff()
        {
            Player = Service.ObjectTable.LocalPlayer;
            if (Player == null) return false;
            bool found = false;
            foreach (var status in Player.StatusList)
            {
                Logger.Log(4, $"Status {status.StatusId} {status.GameData.Value.Name}");
                if (status.StatusId == 48) { found = true; break; }
            }
            return found;
        }

        private void CreateDictionary()
        {
            if (IsTriggeredByContent.Count != 0) return;

            IsTriggeredByContent.Add(ContentTypeWoL.Any, false);
            IsTriggeredByContent.Add(ContentTypeWoL.LightParty, false);
            IsTriggeredByContent.Add(ContentTypeWoL.Trial, false);
            IsTriggeredByContent.Add(ContentTypeWoL.Raid, false);
            IsTriggeredByContent.Add(ContentTypeWoL.Extreme, true);
            IsTriggeredByContent.Add(ContentTypeWoL.Savage, true);
            IsTriggeredByContent.Add(ContentTypeWoL.Ultimate, true);
        }

        public override void DrawExtraButton()
        {
            if (ImGui.Button("Open Content Selector##ForgetFoodOpenButton"))
            {
                IsContentModalOpen = true;
                ImGui.OpenPopup("Content Selector##ForgetFoodModal");
            }

            Vector2 center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new Vector2(640, 545));

            if (ImGui.BeginPopupModal("Content Selector##ForgetFoodModal", ref IsContentModalOpen,
                ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.Popup))
            {
                bool Crafting = IsTriggeredByCrafting;
                if (ImGui.Checkbox("Crafting", ref Crafting))
                {
                    IsTriggeredByCrafting = Crafting;
                    Plugin.Configuration.SaveCurrentPresetScheduled();
                }

                bool ContentAny = IsTriggeredByContent[ContentTypeWoL.Any];
                if(ImGui.Checkbox("Any kind of Combat", ref ContentAny))
                {
                    IsTriggeredByContent[ContentTypeWoL.Any] = ContentAny;
                    Plugin.Configuration.SaveCurrentPresetScheduled();
                }

                if (ContentAny) ImGui.BeginDisabled();

                bool ContentLightParty = IsTriggeredByContent[ContentTypeWoL.LightParty];
                if (ImGui.Checkbox("4 Man Content", ref ContentLightParty))
                {
                    IsTriggeredByContent[ContentTypeWoL.LightParty] = ContentLightParty;
                    Plugin.Configuration.SaveCurrentPresetScheduled();
                }
                HoverText.ShowHint("This includes Dungeons, Guildhests or doing Unsynched Content with atleast 4 players.");

                bool ContentTrial = IsTriggeredByContent[ContentTypeWoL.Trial];
                if (ImGui.Checkbox("Normal Trials", ref ContentTrial))
                {
                    IsTriggeredByContent[ContentTypeWoL.Trial] = ContentTrial;
                    if (ContentTrial) IsTriggeredByContent[ContentTypeWoL.Extreme] = true;
                    Plugin.Configuration.SaveCurrentPresetScheduled();
                }

                bool ContentRaid = IsTriggeredByContent[ContentTypeWoL.Raid];
                if (ImGui.Checkbox("Normal & Alliance Raids", ref ContentRaid))
                {
                    IsTriggeredByContent[ContentTypeWoL.Raid] = ContentRaid;
                    if (ContentRaid) IsTriggeredByContent[ContentTypeWoL.Savage] = true;
                    Plugin.Configuration.SaveCurrentPresetScheduled();
                }

                bool ContentExtreme = IsTriggeredByContent[ContentTypeWoL.Extreme];
                if (ImGui.Checkbox("Extreme & Unreal Trials", ref ContentExtreme))
                {
                    IsTriggeredByContent[ContentTypeWoL.Extreme] = ContentExtreme;
                    if (!ContentExtreme) IsTriggeredByContent[ContentTypeWoL.Trial] = false;
                    Plugin.Configuration.SaveCurrentPresetScheduled();
                }

                bool ContentSavage = IsTriggeredByContent[ContentTypeWoL.Savage];
                if (ImGui.Checkbox("Savage & Chaotic Raids", ref ContentSavage))
                {
                    IsTriggeredByContent[ContentTypeWoL.Savage] = ContentSavage;
                    if (!ContentSavage) IsTriggeredByContent[ContentTypeWoL.Raid] = false;
                    Plugin.Configuration.SaveCurrentPresetScheduled();
                }

                bool ContentUltimate = IsTriggeredByContent[ContentTypeWoL.Ultimate];
                if (ImGui.Checkbox("Ultimate Raids", ref ContentUltimate))
                {
                    IsTriggeredByContent[ContentTypeWoL.Ultimate] = ContentUltimate;
                    Plugin.Configuration.SaveCurrentPresetScheduled();
                }

                ImGui.TextColoredWrapped(UIValues.ColorDescription, "The Plugin detects Content Difficulty by checking for placed Waymarks. (Except Alliance Raids)" +
                    "\nIf you place waymarks in a normal trial or raid, it will assume you are doing extreme or savage.");

                if (ContentAny) ImGui.EndDisabled();

                if (ImGui.Button("Apply##ForgetFoodApply", new Vector2(ImGui.GetWindowSize().X -15, 0)))
                {
                    IsContentModalOpen = false;
                    ImGui.CloseCurrentPopup();
                }


                ImGui.EndPopup();
            }
        }
    }
}
