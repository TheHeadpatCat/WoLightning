using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    public class GetEmotedAt : RuleBase
    {
        override public string Name { get; } = "Get Emoted at";
        override public string Description { get; } = "Triggers whenever a player sends a specific emote to you.";
        override public string Hint { get; } = "The sending player must not be Blacklisted.\nIf the Whitelist is active, the sending player has to be Whitelisted.";
        override public RuleCategory Category { get; } = RuleCategory.Social;
        override public bool hasExtraButton { get; } = true;
        [JsonIgnore] private bool isEmoteSelectorOpen = false;
        [JsonIgnore] private String Input = "";
        [JsonIgnore] private List<Emote> currentEmotes = new List<Emote>();
        [JsonIgnore] int Index = -1;

        public List<ushort> TriggeringEmotes { get; set; } = new();

        [JsonConstructor]
        public GetEmotedAt() { }
        public GetEmotedAt(Plugin plugin) : base(plugin) { }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Plugin.EmoteReaderHooks.OnEmoteIncoming += Check;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Plugin.EmoteReaderHooks.OnEmoteIncoming -= Check;
        }

        public override void Draw()
        {
            RuleUI.Draw();
        }

        public void Check(IPlayerCharacter player, ushort emoteId)
        {
            try
            {
                if (Service.ClientState.LocalPlayer == null) return;
                if (!TriggeringEmotes.Contains(emoteId)) return; // Emote isnt listed
                if (player.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player) return; // The Sender wasnt a player
                Player sendingPlayer = new Player(player.Name.ToString(), (int)player.HomeWorld.RowId);
                Trigger(sendingPlayer + " has used Emote " + emoteId + " on you!", sendingPlayer);
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); }
        }

        public override void DrawExtraButton()
        {
            ImGui.SameLine();
            if (ImGui.Button("Open Emote Selector##GetEmotedAtOpenButton"))
            {
                isEmoteSelectorOpen = true;
                Input = "";
                currentEmotes = Plugin.GameEmotes.findEmotes(TriggeringEmotes);
                ImGui.OpenPopup("Emote Selector##GetEmotedAtSelector");
            }

            Vector2 center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new Vector2(440, 445));

            if (ImGui.BeginPopupModal("Emote Selector##GetEmotedAtSelector", ref isEmoteSelectorOpen,
                ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.Popup))
            {
                ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 15);
                if (ImGui.InputTextWithHint("##GetEmotedAtInput", "Type a Emote Name or click on a Emote to disable it.", ref Input, 48))
                {
                    if (Input.Length == 0)
                    {
                        currentEmotes = Plugin.GameEmotes.findEmotes(TriggeringEmotes);
                    }
                    else currentEmotes = Plugin.GameEmotes.findEmotesByName(Input);
                }

                if (ImGui.BeginListBox("##GetEmotedAtListBox", new Vector2(ImGui.GetWindowWidth() - 15, 340)))
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

                if (ImGui.Button("Apply##GetEmotedAtApply", new Vector2(ImGui.GetWindowSize().X / 2 - 10, 25)))
                {
                    isEmoteSelectorOpen = false;
                    Plugin.Configuration.Save();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                ImGui.PushItemWidth(ImGui.GetWindowSize().X / 2);
                if (ImGui.Button("Reset All##GetEmotedAtReset", new Vector2(ImGui.GetWindowSize().X / 2 - 10, 25)))
                {
                    TriggeringEmotes.Clear();
                }
                ImGui.EndPopup();
            }
        }

    }
}
