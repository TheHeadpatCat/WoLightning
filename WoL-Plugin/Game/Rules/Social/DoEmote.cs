using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    [Serializable]
    public class DoEmote : RuleBase
    {
        override public string Name { get; } = "Do a Emote";
        override public string Description { get; } = "Triggers whenever you do a specific Emote.";
        override public RuleCategory Category { get; } = RuleCategory.Social;
        override public bool hasExtraButton { get; } = true;
        [JsonIgnore] private bool isEmoteSelectorOpen = false;
        [JsonIgnore] private String Input = "";
        [JsonIgnore] private List<Emote> currentEmotes = new List<Emote>();
        [JsonIgnore] int Index = -1;

        public List<ushort> TriggeringEmotes { get; set; } = new();

        [JsonConstructor]
        public DoEmote() { }
        public DoEmote(Plugin plugin) : base(plugin)
        {
        }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Plugin.EmoteReaderHooks.OnEmoteSelf += Check;
            Plugin.EmoteReaderHooks.OnEmoteOutgoing += Check;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Plugin.EmoteReaderHooks.OnEmoteSelf -= Check;
            Plugin.EmoteReaderHooks.OnEmoteOutgoing -= Check;
        }

        public void Check(ushort emoteId)
        {
            try
            {
                if (TriggeringEmotes.Contains(emoteId)) Trigger("You used a forbidden emote!");
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace); }
        }
        public void Check(IGameObject target, ushort emoteId)
        {
            try
            {
                if (TriggeringEmotes.Contains(emoteId)) Trigger("You used a forbidden emote!"); // Todo: Get emotename somehow
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace); }
        }
        public override void DrawExtraButton()
        {

            if (ImGui.Button("Open Emote Selector##DoEmoteOpenButton"))
            {
                isEmoteSelectorOpen = true;
                Input = "";
                currentEmotes = Plugin.GameEmotes.findEmotes(TriggeringEmotes);
                ImGui.OpenPopup("Emote Selector##DoEmoteSelector");
            }

            Vector2 center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new Vector2(440, 445));

            if (ImGui.BeginPopupModal("Emote Selector##DoEmoteSelector", ref isEmoteSelectorOpen,
                ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.Popup))
            {
                ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 15);
                if (ImGui.InputTextWithHint("##DoEmoteInput", "Type a Emote Name or click on a Emote to disable it.", ref Input, 48))
                {
                    if (Input.Length == 0)
                    {
                        currentEmotes = Plugin.GameEmotes.findEmotes(TriggeringEmotes);
                    }
                    else currentEmotes = Plugin.GameEmotes.findEmotesByName(Input);
                }

                if (ImGui.BeginListBox("##DoEmoteListBox", new Vector2(ImGui.GetWindowWidth() - 15, 340)))
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


                if (ImGui.Button("Apply##DoEmoteApply", new Vector2(ImGui.GetWindowSize().X / 2 - 10, 0)))
                {
                    isEmoteSelectorOpen = false;
                    Plugin.Configuration.Save();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                ImGui.PushItemWidth(ImGui.GetWindowSize().X / 2);
                if (ImGui.Button("Reset All##DoEmoteReset", new Vector2(ImGui.GetWindowSize().X / 2 - 10, 0)))
                {
                    TriggeringEmotes.Clear();
                }
                ImGui.EndPopup();
            }
        }
    }
}
