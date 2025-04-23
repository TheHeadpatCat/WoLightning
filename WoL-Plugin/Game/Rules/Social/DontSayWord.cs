using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using WoLightning.Configurations;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    public class DontSayWord : BaseRule
    {
        public override string Name { get; } = "Forget to say a Enforced Word";

        public override string Description { get; } = "Triggers whenever you do not say a word from a list.";

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


        public override void DrawAdvancedOptions()
        {
            ImGui.Text("If you do not say any single word from this list, you'll trigger the Rule!");

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
            ImGui.SameLine();
            ImGui.Checkbox("Punctuation", ref Punctuation);
            ImGui.SameLine();
            ImGui.Checkbox("Proper Case", ref ProperCase);

            ImGui.Separator();

            ImGui.Spacing();

            if (ImGui.Button("Add Word##EnforcedWordAdd", new Vector2(ImGui.GetWindowWidth() / 2 - 8, 25)))
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
