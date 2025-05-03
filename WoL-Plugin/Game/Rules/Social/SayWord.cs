using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using WoLightning.Util;
using WoLightning.WoL_Plugin.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    public class SayWord : RuleBase
    {
        public override string Name { get; } = "Say a Banned Word";

        public override string Description { get; } = "Triggers whenever you say a word from a list.";
        override public string Hint { get; } = "Once enabled, you can set the Words in the \"Word List\" Tab!";

        public override RuleCategory Category { get; } = RuleCategory.Social;

        public List<SpecificWord> BannedWords { get; set; } = new();

        override public bool hasAdvancedOptions { get; } = true;

        [JsonIgnore] string Input = string.Empty;
        [JsonIgnore] int Index = -1;
        [JsonIgnore] String SelectedWord = string.Empty;
        [JsonIgnore] int[] Settings = new int[3];

        [JsonIgnore] bool Punctuation = false;
        [JsonIgnore] bool ProperCase = false;


        [JsonConstructor]
        public SayWord() { }
        public SayWord(Plugin plugin) : base(plugin)
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

        // All of the passed variables have to match with Plugin.ChatGui.ChatMessage - you can let this be generated for you if you are using Visual Studio or similiar
        private void Check(XivChatType type, int timestamp, ref SeString senderE, ref SeString messageE, ref bool isHandled)
        {
            try
            {
                if (Plugin.ClientState.LocalPlayer == null) return; // If the LocalPlayer is null, we might be transitioning between areas or similiar. Abort the check in those cases.


                // This will Check if the Checkbox for "Limit Chats" is enabled and if so, checks if the type of chat message we received is included in that. If its not, then abort the check.
                if (Plugin.Configuration.ActivePreset.LimitChats && !Plugin.Configuration.ActivePreset.Chats.Contains(type)) return;

                string sender = StringSanitizer.LetterOrDigit(senderE.ToString()).ToLower(); // Get the Sender in a cleaned String. SeStrings can have payloads and stuff and we dont want any of those.

                // First check if the type of Chat we received is above a specific number. Noteably 107 is the last Social Chat that players can technically send stuff to.
                // Afterwards, check if the sender of the message has the same name as our Local Player Character. If so, we are the person that sent it. We can ignore the World, as if there is another Person with the same name, they will always show their World in the Sender Name, while we dont.
                if ((int)type <= 107 && sender.Equals(Plugin.ClientState.LocalPlayer.Name.ToString().ToLower()))
                {
                    // Get the message into a cleaned String. Again, messages can have symbols and stuff in them and they might mess up our logic.
                    string message = StringSanitizer.LetterOrDigit(messageE.ToString());
                    foreach (var bannedWord in BannedWords) // Go through every banned word the user put in.
                    {
                        foreach (var word in message.Split(" ")) // Split the message we sent into seperate words and go through every said word.
                        {
                            if (bannedWord.Compare(word)) // Now, with both parts. Check each said word, against all banned words. If any of them match, Trigger the Rule and end the Logic.
                            {
                                Trigger($"You have said {bannedWord}!");
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception e) { Plugin.Error(e.StackTrace); }
        }


        // This is UI Code for the Configuration Window.
        // It gets called in Windows/ConfigWindow under "Word Lists"
        // Basically, its offloaded here to let the User type in words that will get saved in the BannedWords property.
        // If you are creating a basic Rule that doesn't need any Input Userdata, then you don't need to implement this.

        // Alternatively, you can add a "public override void DrawExtraButton()" function, to draw extra UI code next to the "Assigned Shockers" button on a Rule.
        public override void DrawAdvancedOptions()
        {
            ImGui.Text("Saying any Word from this list, will trigger the \"Say Banned Word\" Rule!");

            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 230);
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

            if (ImGui.Button("Add/Edit Word##BadWordAdd", new Vector2(ImGui.GetWindowWidth() / 2 - 8, 25)))
            {

                SpecificWord? target = null;
                foreach (SpecificWord BannedWord in BannedWords)
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

                target = new SpecificWord(Input);
                target.NeedsPunctuation = Punctuation;
                target.NeedsProperCase = ProperCase;
                BannedWords.Add(target);

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
                foreach (SpecificWord BannedWord in BannedWords)
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
                foreach (SpecificWord BannedWord in BannedWords)
                {
                    string mode = new String("");
                    string durS = new String("");
                    bool is_Selected = (Index == index);
                    if (ImGui.Selectable($"{BannedWord.Word} - Punctuation: {BannedWord.NeedsPunctuation} - Proper Case: {BannedWord.NeedsProperCase}", ref is_Selected))
                    {
                        SelectedWord = BannedWord.Word;
                        Index = index;
                        Input = BannedWord.Word;
                    }
                    index++;
                }
                ImGui.EndListBox();
            }
        }

    }
}
