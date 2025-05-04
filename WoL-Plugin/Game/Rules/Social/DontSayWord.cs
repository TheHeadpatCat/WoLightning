using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using WoLightning.Util;
using WoLightning.WoL_Plugin.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    public class DontSayWord : RuleBase
    {
        public override string Name { get; } = "Forget to say a Enforced Word";

        public override string Description { get; } = "Triggers whenever you do not say a word from a list.";
        override public string Hint { get; } = "Once enabled, you can set the Words in the \"Word List\" Tab!";

        public override RuleCategory Category { get; } = RuleCategory.Social;

        public List<SpecificWord> EnforcedWords { get; set; } = new();

        override public bool hasAdvancedOptions { get; } = true;

        [JsonIgnore] string Input = string.Empty;
        [JsonIgnore] int Index = -1;
        [JsonIgnore] String SelectedWord = string.Empty;
        [JsonIgnore] int[] Settings = new int[3];

        [JsonIgnore] bool Punctuation = false;
        [JsonIgnore] bool ProperCase = false;


        [JsonConstructor]
        public DontSayWord() { }
        public DontSayWord(Plugin plugin) : base(plugin)
        {

        }
        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Plugin.ChatGui.ChatMessage += Check;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Plugin.ChatGui.ChatMessage -= Check;
        }
        private void Check(XivChatType type, int timestamp, ref SeString senderE, ref SeString messageE, ref bool isHandled)
        {
            try
            {
                if (Plugin.ClientState.LocalPlayer == null) return;
                //check for chat type limitation
                if (Plugin.Configuration.ActivePreset.LimitChats && !Plugin.Configuration.ActivePreset.Chats.Contains(type)) return;

                string sender = StringSanitizer.LetterOrDigit(senderE.ToString()).ToLower();
                if ((int)type <= 107 && sender.Equals(Plugin.ClientState.LocalPlayer.Name.ToString().ToLower()))
                {
                    string message = StringSanitizer.LetterOrDigit(messageE.ToString());
                    bool found = false;
                    foreach (var EnforcedWord in EnforcedWords)
                    {
                        foreach (var word in message.Split(" "))
                        {
                            if (EnforcedWord.Compare(word))
                            {
                                found = true;
                            }
                        }
                    }
                    if (!found) Trigger($"You forgot to say a Enforced Word!");
                }
            }
            catch (Exception e) { Plugin.Error(Name + " Check() failed."); Plugin.Error(e.Message); }
        }

        public override void DrawAdvancedOptions()
        {
            ImGui.Text("Not saying atleast one Word from this list, will trigger the \"Forget to say Enforced Word\" Rule!");

            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 230);
            if (ImGui.InputTextWithHint("##EnforcedWordInput", "Click on a entry to edit it.", ref Input, 48))
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
                foreach (SpecificWord EnforcedWord in EnforcedWords)
                {
                    if (EnforcedWord.Word.ToLower() == Input.ToLower())
                    {
                        target = EnforcedWord;
                        break;
                    }
                }
                if (target != null)
                {
                    EnforcedWords.Remove(target);
                }

                target = new SpecificWord(Input);
                target.NeedsPunctuation = Punctuation;
                target.NeedsProperCase = ProperCase;
                EnforcedWords.Add(target);

                Index = -1;
                Input = new String("");
                Settings = new int[3];
                SelectedWord = new String("");
            }

            ImGui.SameLine();

            if (Index == -1) ImGui.BeginDisabled();
            if (ImGui.Button("Remove Word##EnforcedWordRemove", new Vector2(ImGui.GetWindowWidth() / 2 - 8, 25)))
            {
                SpecificWord? target = null;
                foreach (SpecificWord EnforcedWord in EnforcedWords)
                {
                    if (EnforcedWord.Word.ToLower() == Input.ToLower())
                    {
                        target = EnforcedWord;
                        break;
                    }
                }
                if (target != null)
                {
                    EnforcedWords.Remove(target);
                }
                Index = -1;
                Input = new String("");
                Settings = new int[3];
                SelectedWord = new String("");
            }
            if (Index == -1) ImGui.EndDisabled();

            ImGui.Spacing();

            if (ImGui.BeginListBox("##EnforcedWordListBox", new Vector2(ImGui.GetWindowWidth() - 15, 340)))
            {
                int index = 0;
                foreach (SpecificWord EnforcedWord in EnforcedWords)
                {
                    string mode = new String("");
                    string durS = new String("");
                    bool is_Selected = (Index == index);
                    if (ImGui.Selectable($"{EnforcedWord.Word} - Punctuation: {EnforcedWord.NeedsPunctuation} - Proper Case: {EnforcedWord.NeedsProperCase}", ref is_Selected))
                    {
                        SelectedWord = EnforcedWord.Word;
                        Index = index;
                        Input = EnforcedWord.Word;
                    }
                    index++;
                }
                ImGui.EndListBox();
            }
        }

    }
}
