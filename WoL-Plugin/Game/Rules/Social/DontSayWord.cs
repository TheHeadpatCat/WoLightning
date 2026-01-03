using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using WoLightning.Util;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util;
using WoLightning.WoL_Plugin.Util.Helpers;
using WoLightning.WoL_Plugin.Util.Types;
using WoLightning.WoL_Plugin.Util.UI_Elements;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    public class DontSayWord : RuleBase
    {
        public override string Name { get; } = "Forget to say a Enforced Word";

        public override string Description { get; } = "Triggers whenever you do not say a word from this list.";
        override public string Hint { get; } = "Once enabled, you can set the Words in the \"Word List\" Tab!";
        public override bool hasOptions { get; } = false;

        public override RuleCategory Category { get; } = RuleCategory.Social;

        public List<SpecificWord> EnforcedWords { get; set; } = new();
        public List<XivChatType> Chats { get; set; } = new();
        [JsonIgnore] private bool isChatLimiterOpen = false;
        [JsonIgnore] private WordListBox wordListBox;

        override public bool hasAdvancedOptions { get; } = true;
        public override bool hasExtraButton { get; } = true;

        [JsonIgnore] ShockOptionsEditor shockOptionsEditor;
        [JsonIgnore] private string TestInput = "";
        [JsonIgnore] private SpecificWord? TestWord;


        [JsonConstructor]
        public DontSayWord() { }
        public DontSayWord(Plugin plugin) : base(plugin)
        {

        }

        public override void Draw()
        {
            // removes it from the "social" tab. We are putting it into its own space.
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
                if (EnforcedWords.Count == 0) return;

                //check for chat type limitation
                if (Chats.Count >= 1 && !Chats.Contains(type)) return;

                // Check if its actually a emote. We don't want to trigger on emotes.
                if (type == XivChatType.StandardEmote || type == XivChatType.CustomEmote) return;

                Player? sender = null;
                foreach (var payload in senderE.Payloads)
                {
                    if (payload.Type == PayloadType.Player) sender = new(payload);
                }

                Logger.Log(4, $"{Name} | Message from: " + senderE.TextValue + " comparing against " + Plugin.LocalPlayer.Name + " type: " + type.ToString());

                string senderClean = StringSanitizer.PlayerName(senderE.TextValue);
                if (sender == null && senderClean == Plugin.LocalPlayer.Name) sender = Plugin.LocalPlayer; // If there is no player payload, check if names match atleast.

                if (sender != Plugin.LocalPlayer && type == XivChatType.TellOutgoing) sender = Plugin.LocalPlayer;

                if (sender == null || sender != Plugin.LocalPlayer) return;

                Logger.Log(4, $"{Name} | Comparing sender " + sender + " against " + Plugin.LocalPlayer + " is same player?: " + sender.Equals(Plugin.LocalPlayer));

                if ((int)type <= 107 &&
                    (sender.Equals(Plugin.LocalPlayer)
                    || type == XivChatType.TellOutgoing)) // Tell is a very weird channel. It considers only the target player and gives us no reference of the actual sender.
                {
                    string message = StringSanitizer.LetterOrDigit(messageE.ToString());
                    SpecificWord? result = CheckMessage(message);
                    if (result != null) return;
                    else Trigger($"You forgot to say a Enforced Word!", sender);
                }
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace); }
        }

        public SpecificWord? CheckMessage(string message)
        {
            Logger.Log(4, "Message: " + message);
            foreach (var enforcedWord in EnforcedWords) // Go through every banned word the user put in.
            {
                int spaceAmount = enforcedWord.Word.CountSpaces();
                Logger.Log(4, $"{Name} | Found " + spaceAmount + " spaces.");

                string[] words = message.Split(' ');
                for (int i = 0; i < words.Length; i++)
                {
                    string wordsToCompare = words[i];
                    if (spaceAmount > 0)
                    {
                        for (int j = i + 1; j < words.Length; j++)
                        {
                            wordsToCompare += " " + words[j];
                            Logger.Log(4, $"{Name} | Added " + words[j] + " to the compound.");
                        }
                    }

                    Logger.Log(3, $"{Name} | Comparing [" + wordsToCompare + "] against [" + enforcedWord.Word + "] which is " + enforcedWord.Compare(wordsToCompare));

                    if (enforcedWord.Compare(wordsToCompare)) // Now, with both parts. Check each said word, against all banned words. If any of them match, Trigger the Rule and end the Logic.
                    {
                        Logger.Log(3, $"{Name} | Found [" + wordsToCompare + "] - sending request...");
                        return enforcedWord;
                    }
                }
            }
            return null;
        }

        public override void DrawExtraButton()
        {

            if (ImGui.Button("Open Chat Limiter##DontSayWordOpenButton"))
            {
                isChatLimiterOpen = true;
                ImGui.OpenPopup("Chat Limiter##DontSayWordChatLimiter");
            }

            Vector2 center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new Vector2(640, 555));

            if (ImGui.BeginPopupModal("Chat Limiter##DontSayWordChatLimiter", ref isChatLimiterOpen,
                ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.Popup))
            {

                ImGui.Text("This Rule will only work on the selected Channels,\nunless no Channel is selected.");
                ImGui.Separator();

                var i = 0;
                foreach (var e in ChatType.GetOrderedChannels()) // Old Code from @lexiconmage
                {
                    // See if it is already enabled by default
                    var enabled = Chats.Contains((XivChatType)ChatType.GetXivChatTypeFromChatType(e)!);
                    // Create a new line after every 4 columns
                    if (i != 0 && (i == 4 || i == 7 || i == 11 || i == 15 || i == 19 || i == 23))
                    {
                        ImGui.NewLine();
                        //i = 0;
                    }
                    // Move to the next row if it is LS1 or CWLS1
                    if (e is ChatType.ChatTypes.LS1 or ChatType.ChatTypes.CWL1)
                        ImGui.Separator();

                    if (ImGui.Checkbox($"{e}", ref enabled))
                    {
                        XivChatType type = (XivChatType)ChatType.GetXivChatTypeFromChatType(e)!;

                        // See If the UIHelpers.Checkbox is clicked, If not, add to the list of enabled channels, otherwise, remove it.
                        if (enabled) Chats.Add(type);
                        else Chats.Remove(type);

                        if (type == XivChatType.TellIncoming) // Tell is split into 2 parts for some reason, so add the second part as well.
                        {
                            if (enabled) Chats.Add(XivChatType.TellOutgoing);
                            else Chats.Remove(XivChatType.TellOutgoing);
                        }

                    }

                    ImGui.SameLine();
                    i++;
                }
                ImGui.NewLine();

                if (ImGui.Button("Apply##DontSayWordApply", new Vector2(ImGui.GetWindowSize().X / 2 - 10, 25)))
                {
                    isChatLimiterOpen = false;
                    Plugin.Configuration.Save();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                ImGui.PushItemWidth(ImGui.GetWindowSize().X / 2);
                if (ImGui.Button("Reset All##DontSayWordReset", new Vector2(ImGui.GetWindowSize().X / 2 - 10, 25)))
                {
                    Chats.Clear();
                }

                ImGui.EndPopup();
            }


        }
        public override void DrawAdvancedOptions()
        {

            bool enabled = IsEnabled;
            if (ImGui.Checkbox("##EnforcedWordsEnabled", ref enabled))
            {
                IsEnabled = enabled;
                Plugin.Configuration.saveCurrentPreset();
            }

            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.Text(Name);
            ImGui.TextColored(UIValues.ColorDescription, Description);
            ImGui.EndGroup();

            if (shockOptionsEditor == null)
            {
                shockOptionsEditor = new(Name, Plugin, ShockOptions);
            }

            if (IsEnabled)
            {
                shockOptionsEditor.Draw();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();


            if (wordListBox == null)
            {
                wordListBox = new(Name, Plugin, EnforcedWords);
                wordListBox.HasPerWordOptions = false;
            }
            bool changed = false;
            wordListBox.Draw(ref changed);
            if (changed) Plugin.Configuration.saveCurrentPreset();

            ImGui.TextColored(new Vector4(0.66f, 0.66f, 0.66f, 0.80f), "Note: Despite saying \"Word\", sentences are also supported.");

            DrawWordTesting();
        }

        private void DrawWordTesting()
        {
            ImGui.Separator();
            ImGui.Spacing();
            if (TestInput.Length == 0)
            {
                ImGui.Text("Test out if the List works: ");
            }
            else
            {
                if (TestWord == null)
                {
                    ImGui.TextColored(UIValues.ColorNameBlocked, "You haven't said a Enforced word!");
                }
                else
                {
                    ImGui.TextColored(UIValues.ColorNameEnabled, $"Found: {TestWord.Word}");
                }
            }
            if (ImGui.InputTextWithHint($"##{Name}TestInput", "Input something you would say!", ref TestInput))
            {
                TestWord = CheckMessage(TestInput);
            }
        }

    }
}
