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
    public class SayWord : BaseRule
    {
        public override string Name { get; } = "Say a Banned Word";

        public override string Description { get; } = "Triggers whenever you say a word from a list.";

        public override RuleCategory Category { get; } = RuleCategory.Social;

        public Dictionary<SpecificWord, ShockOptions> BannedWords { get; set; }

        [JsonIgnore] public new bool hasRuleWindow = true;

        [JsonIgnore] string Input = string.Empty;
        [JsonIgnore] int Index = -1;
        [JsonIgnore] String SelectedWord = string.Empty;
        [JsonIgnore] int[] Settings = new int[3];

        [JsonIgnore] bool Punctuation = false;
        [JsonIgnore] bool ProperCase = false;
        

        public SayWord(Plugin plugin) : base(plugin) {
            
        }


        public override void DrawRuleWindow()
        {
            ImGui.Text("If you say any of these words, you'll trigger its settings!");

            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 85);
            if (ImGui.InputTextWithHint("##BannedWordInput", "Click on a entry to edit it.", ref Input, 48))
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
            

            //clamp
            if (Settings[0] < 0 || Settings[0] > 3) Settings[0] = 0;

            if (Settings[1] <= 0) Settings[1] = 1;
            if (Settings[1] > 100) Settings[1] = 100;

            if (Settings[2] <= 0) Settings[2] = 100;
            if (Settings[2] > 10 && Settings[2] != 100 && Settings[2] != 300) Settings[2] = 10;

            ImGui.Separator();

            ImGui.BeginGroup();
            ImGui.Spacing();
            ImGui.Text("    Mode");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 3 - 50);
            ImGui.Combo("##Word", ref Settings[0], ["Shock", "Vibrate", "Beep"], 3);
            ImGui.EndGroup();

            ImGui.SameLine();

            ImGui.BeginGroup();
            ImGui.Spacing();
            ImGui.Text("    Duration");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 4.5f);
            int DurationIndex = DurationArray.IndexOf(Settings[2]);
            if (ImGui.Combo("##WordDur", ref DurationIndex, DurationArrayString, 12))
            {
                Settings[2] = DurationArray[DurationIndex];
            }
            ImGui.EndGroup();

            ImGui.SameLine();

            ImGui.BeginGroup();
            ImGui.Spacing();
            ImGui.Text("    Intensity");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 2f);
            ImGui.SliderInt("##BadWordInt", ref Settings[1], 1, 100);
            ImGui.EndGroup();

            ImGui.Separator();

            ImGui.Spacing();

            if (ImGui.Button("Add Word##BadWordAdd", new Vector2(ImGui.GetWindowWidth() / 2 - 8, 25)))
            {
                
                SpecificWord? target = null;
                foreach ((SpecificWord BannedWord,ShockOptions Options) in BannedWords)
                {
                    if(BannedWord.Word.ToLower() == Input.ToLower())
                    {
                        target = BannedWord;
                        break;
                    }
                }
                if (target != null)
                {
                    BannedWords.Remove(target);
                }

                target = new SpecificWord(Input);
                target.NeedsPunctuation = Punctuation;
                target.NeedsProperCase = ProperCase;
                BannedWords.Add(target, new ShockOptions(Settings));

                Index = -1;
                Input = new String("");
                Settings = new int[3];
                SelectedWord = new String("");
            }

            ImGui.SameLine();

            if (Index == -1) ImGui.BeginDisabled();
            if (ImGui.Button("Remove Word##BadWordRemove", new Vector2(ImGui.GetWindowWidth() / 2 - 8, 25)))
            {
                SpecificWord? target = null;
                foreach ((SpecificWord BannedWord, ShockOptions Options) in BannedWords)
                {
                    if (BannedWord.Word.ToLower() == Input.ToLower())
                    {
                        target = BannedWord;
                        break;
                    }
                }
                if (target != null)
                {
                    BannedWords.Remove(target);
                }
                Index = -1;
                Input = new String("");
                Settings = new int[3];
                SelectedWord = new String("");
            }
            if (Index == -1) ImGui.EndDisabled();

            ImGui.Spacing();

            if (ImGui.BeginListBox("##BadWordListBox", new Vector2(ImGui.GetWindowWidth() - 15, 340)))
            {
                int index = 0;
                foreach ((SpecificWord BannedWord, ShockOptions Options) in BannedWords)
                {
                    string mode = new String("");
                    string durS = new String("");
                    bool is_Selected = (Index == index);
                    switch ((int)Options.OpMode) { case 0: mode = "Shock"; break; case 1: mode = "Vibrate"; break; case 2: mode = "Beep"; break; };
                    switch (Options.Duration) { case 100: durS = "0.1s"; break; case 300: durS = "0.3s"; break; default: durS = $"{Options.Duration}s"; break; }
                    if (ImGui.Selectable($" Word: {BannedWord.Word}  | Mode: {mode} | Intensity: {Options.Intensity} | Duration: {durS}", ref is_Selected))
                    {
                        SelectedWord = BannedWord.Word;
                        Index = index;
                        Input = BannedWord.Word;
                        Settings = Options.toSimpleArray();
                    }
                    index++;
                }
                ImGui.EndListBox();
            }
        }

    }
}
