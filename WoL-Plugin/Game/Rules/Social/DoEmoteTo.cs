using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    [Serializable]
    public class DoEmoteTo : RuleBase
    {
        override public string Name { get; } = "Do a Emote to someone";
        override public string Description { get; } = "Triggers whenever you do a specific Emote, while having a player targeted.";
        override public string Hint { get; } = "The targeted player must not be Blacklisted.\nIf the Whitelist is active, the targeted player has to be Whitelisted.";
        override public RuleCategory Category { get; } = RuleCategory.Social;
        override public bool hasExtraButton { get; } = true;
        [JsonIgnore] private bool isEmoteSelectorOpen = false;
        [JsonIgnore] private String Input = "";
        [JsonIgnore] private List<Emote> currentEmotes = new List<Emote>();
        [JsonIgnore] int Index = -1;
        public List<ushort> TriggeringEmotes { get; set; } = new();

        [JsonConstructor]
        public DoEmoteTo() { }
        public DoEmoteTo(Plugin plugin) : base(plugin) { }

        override public void Start()
        {
            if (IsRunning) return;
            Plugin.EmoteReaderHooks.OnEmoteOutgoing += Check;
            IsRunning = true;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            Plugin.EmoteReaderHooks.OnEmoteOutgoing -= Check;
            IsRunning = false;
        }

        public void Check(IGameObject target, ushort emoteId)
        {
            try { 
            if(Plugin.ClientState.LocalPlayer == null) return;
            if (!TriggeringEmotes.Contains(emoteId)) return; // Emote isnt listed
            if (target.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player) return; // We arent targeting a player - somehow.
            IPlayerCharacter character = (IPlayerCharacter)target;
            Player targetedPlayer = new Player(character.Name.ToString(), (int)character.HomeWorld.RowId);
            Trigger("You used a forbidden emote on " + targetedPlayer, targetedPlayer);
            }
            catch (Exception e) { Plugin.Error(e.StackTrace); }
        }

        public override void DrawExtraButton()
        {
            ImGui.SameLine();
            if (ImGui.Button("Open Emote Selector##DoEmoteToOpenButton"))
            {
                isEmoteSelectorOpen = true;
                Input = "";
                currentEmotes = Plugin.GameEmotes.findEmotes(TriggeringEmotes);
                ImGui.OpenPopup("Emote Selector##DoEmoteToSelector");
            }

            Vector2 center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new Vector2(440, 445));

            if (ImGui.BeginPopupModal("Emote Selector##DoEmoteToSelector", ref isEmoteSelectorOpen,
                ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.Popup))
            {
                ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 15);
                if (ImGui.InputTextWithHint("##DoEmoteToInput", "Type a Emote Name or click on a Emote to disable it.", ref Input, 48))
                {
                    if (Input.Length == 0)
                    {
                        currentEmotes = Plugin.GameEmotes.findEmotes(TriggeringEmotes);
                    }
                    else currentEmotes = Plugin.GameEmotes.findEmotesByName(Input);
                }

                if (ImGui.BeginListBox("##DoEmoteToListBox", new Vector2(ImGui.GetWindowWidth() - 15, 340)))
                {
                    int index = 0;
                    foreach (Emote em in currentEmotes)
                    {
                        bool is_Selected = (Index == index);
                        String isEnabled = "";
                        if (TriggeringEmotes.Contains((ushort)em.RowId)) isEnabled = "X";
                        if (ImGui.Selectable($"{isEnabled} | {em.Name} ID: {em.RowId}", ref is_Selected))
                        {
                            if (TriggeringEmotes.Contains((ushort)em.RowId)) TriggeringEmotes.Remove((ushort)em.RowId);
                            else TriggeringEmotes.Add((ushort)em.RowId);
                            Index = index;
                        }
                        index++;
                    }
                    ImGui.EndListBox();
                }

                if (ImGui.Button("Apply##DoEmoteToApply", new Vector2(ImGui.GetWindowSize().X / 2 - 10, 25)))
                {
                    isEmoteSelectorOpen = false;
                    Plugin.Configuration.Save();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                ImGui.PushItemWidth(ImGui.GetWindowSize().X / 2);
                if (ImGui.Button("Reset All##DoEmoteToReset", new Vector2(ImGui.GetWindowSize().X / 2 - 10, 25)))
                {
                    TriggeringEmotes.Clear();
                }
                ImGui.EndPopup();
            }
        }
    }
}
