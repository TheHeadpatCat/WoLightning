using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;
using WoLightning.Util;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util;
using WoLightning.WoL_Plugin.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    public class HearWord : RuleBase
    {
        public override string Name { get; } = "Hear a Trigger Word";

        public override string Description { get; } = "Triggers whenever you hear a specific Word from a list.";
        override public string Hint { get; } = "Once enabled, you can set the Words in the \"Word List\" Tab!";

        public override RuleCategory Category { get; } = RuleCategory.Social;

        public List<SpecificWord> TriggerWords { get; set; } = new();

        override public bool hasAdvancedOptions { get; } = true;

        [JsonIgnore] string Input = string.Empty;
        [JsonIgnore] int Index = -1;
        [JsonIgnore] String SelectedWord = string.Empty;
        [JsonIgnore] int[] Settings = new int[3];

        [JsonIgnore] bool Punctuation = false;
        [JsonIgnore] bool ProperCase = false;


        [JsonConstructor]
        public HearWord() { }
        public HearWord(Plugin plugin) : base(plugin)
        {

        }
        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Service.ChatGui.ChatMessage += Check;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.ChatGui.ChatMessage -= Check;
        }
        private void Check(XivChatType type, int timestamp, ref SeString senderE, ref SeString messageE, ref bool isHandled)
        {
            try
            {
                if (Service.ClientState.LocalPlayer == null) return;
                if (Plugin.Configuration.ActivePreset.LimitChats && !Plugin.Configuration.ActivePreset.Chats.Contains(type)) return;

                Player? sender = null;
                foreach (var payload in senderE.Payloads)
                {
                    if (payload.Type == PayloadType.Player) sender = new(payload);
                }

                if (sender == null) return;

                Logger.Log(4, sender);

                if ((int)type <= 107 && (int)type != 12) // Allow all possible social channels, EXCEPT Tell_Outgoing
                {
                    string message = StringSanitizer.LetterOrDigit(messageE.ToString());
                    foreach (var TriggerWord in TriggerWords)
                    {
                        foreach (var word in message.Split(" "))
                        {
                            if (TriggerWord.Compare(word))
                            {
                                Trigger($"You heard {TriggerWord.Word}!", sender);
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); }
        }

        public override void DrawAdvancedOptions()
        {
            ImGui.TextWrapped("If someone says one of these words, it will trigger the \"Hear a Trigger Word\" Rule!" +
                "\nYou can change who can say these words in the \"Permissions\" Tab.");

            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 230);
            if (ImGui.InputTextWithHint("##TriggerWordInput", "Click on a entry to edit it.", ref Input, 48))
            {
                if (Index != -1) // Get rid of the old settings, otherwise we build connections between two items
                {
                    int[] copyArray = new int[3];
                    Settings.CopyTo(copyArray, 0);
                    Settings = copyArray;
                }
                Index = -1;
            }
            ImGui.Checkbox("Punctuation", ref Punctuation);
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("If this is enabled, the Word has to be properly written out on its own." +
                "\nIf the word is \"Master\" then writing \"i like my master\" is accepted, while \"ilikemymaster\" isn't.");
            }
            ImGui.SameLine();
            ImGui.Checkbox("Proper Case", ref ProperCase);
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("If this is enabled, the Word has to properly match the Case." +
                "\nIf the word is \"Collar\" then writing \"i have a Collar\" is accepted, while \"i have a collar\" isn't.");
            }

            ImGui.Separator();

            ImGui.Spacing();

            if (ImGui.Button("Add/Edit Word##EnforcedWordAdd", new Vector2(ImGui.GetWindowWidth() / 2 - 8, 25)))
            {

                SpecificWord? target = null;
                foreach (SpecificWord TriggerWord in TriggerWords)
                {
                    if (TriggerWord.Word.ToLower() == Input.ToLower())
                    {
                        target = TriggerWord;
                        break;
                    }
                }
                if (target != null)
                {
                    TriggerWords.Remove(target);
                }

                target = new SpecificWord(Input);
                target.NeedsPunctuation = Punctuation;
                target.NeedsProperCase = ProperCase;
                TriggerWords.Add(target);

                Index = -1;
                Input = new String("");
                Settings = new int[3];
                SelectedWord = new String("");
            }

            ImGui.SameLine();

            if (Index == -1) ImGui.BeginDisabled();
            if (ImGui.Button("Remove Word##TriggerWordRemove", new Vector2(ImGui.GetWindowWidth() / 2 - 8, 25)))
            {
                SpecificWord? target = null;
                foreach (SpecificWord TriggerWord in TriggerWords)
                {
                    if (TriggerWord.Word.ToLower() == Input.ToLower())
                    {
                        target = TriggerWord;
                        break;
                    }
                }
                if (target != null)
                {
                    TriggerWords.Remove(target);
                }
                Index = -1;
                Input = new String("");
                Settings = new int[3];
                SelectedWord = new String("");
            }
            if (Index == -1) ImGui.EndDisabled();

            ImGui.Spacing();

            if (ImGui.BeginListBox("##TriggerWordListBox", new Vector2(ImGui.GetWindowWidth() - 15, 340)))
            {
                int index = 0;
                foreach (SpecificWord TriggerWord in TriggerWords)
                {
                    string mode = new String("");
                    string durS = new String("");
                    bool is_Selected = (Index == index);
                    if (ImGui.Selectable($"{TriggerWord.Word} - Punctuation: {TriggerWord.NeedsPunctuation} - Proper Case: {TriggerWord.NeedsProperCase}", ref is_Selected))
                    {
                        SelectedWord = TriggerWord.Word;
                        Index = index;
                        Input = TriggerWord.Word;
                    }
                    index++;
                }
                ImGui.EndListBox();
            }
        }

    }
}
