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
        public List<XivChatType> Chats { get; set; } = new();
        [JsonIgnore] private bool isChatLimiterOpen = false;
        public override bool hasExtraButton { get; } = true;

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
            Service.ChatGui.ChatMessage += Check;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.ChatGui.ChatMessage -= Check;
        }

        // All of the passed variables have to match with Plugin.ChatGui.ChatMessage - you can let this be generated for you if you are using Visual Studio or similiar
        private void Check(XivChatType type, int timestamp, ref SeString senderE, ref SeString messageE, ref bool isHandled)
        {
            try
            {
                if (Service.ClientState.LocalPlayer == null) return; // If the LocalPlayer is null, we might be transitioning between areas or similiar. Abort the check in those cases.


                // Check if the player has enabled any of the "Limit Chat" options, and if so check if the message is in one of those channels.
                if (Chats.Count >= 1 && !Chats.Contains(type)) return;

                Player? sender = null;
                foreach (var payload in senderE.Payloads)
                {
                    if (payload.Type == PayloadType.Player) sender = new(payload); // FF messages are split into Payloads, so that some of the stuff is clickable. You can pass a Player payload to the "Player" class and it will filter out everything that you dont need.
                }

                if (sender == null) sender = Plugin.LocalPlayer; // If there is no player payload, we have to have been the sender.

                Logger.Log(4, "Comparing sender " + sender + " against " + Plugin.LocalPlayer + " is same player?: " + sender.Equals(Plugin.LocalPlayer));

                if (sender != Plugin.LocalPlayer && type != XivChatType.TellOutgoing) return; // We check if we either are the Person that sent the message, or if the message is a outgoing /tell message - those have always have the "target" as their sender (for some reason)

                // Check if the type of Chat we received is below a specific number. Noteably 107 is the last Social Chat that players can technically send stuff to.
                if ((int)type <= 107)
                {
                    // Get the message into a cleaned String. Messages can have symbols and stuff in them and they might mess up our logic.
                    string message = StringSanitizer.LetterOrDigit(messageE.ToString());
                    foreach (var bannedWord in BannedWords) // Go through every banned word the user put in.
                    {
                        foreach (var word in message.Split(" ")) // Split the message we sent into seperate words and go through every said word.
                        {
                            Logger.Log(4, "Comparing " + word + " against " + bannedWord.Word + " which is " + bannedWord.Compare(word));
                            if (bannedWord.Compare(word)) // Now, with both parts. Check each said word, against all banned words. If any of them match, Trigger the Rule and end the Logic.
                            {
                                Trigger($"You have said {bannedWord}!", sender);
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); }
        }


        // This is optional UI Code for the extra "Open Chat Limiter" button that you can see next to the "Assign Shockers" button.
        // If you have the bool "hasExtraButton" set to true and also implement this function, you are able to add more UI Code right next to the "Assign Shockers" button.
        // In this case, we add a button that opens a big modal where the user can select a number of Chats to limit the code to.

        // If your Rule doesn't need this extra button, simply remove this function as well as the "public override bool hasExtraButton { get; } = true;" property at line 27.
        public override void DrawExtraButton()
        {
            ImGui.SameLine();
            if (ImGui.Button("Open Chat Limiter##SayWordOpenButton"))
            {
                isChatLimiterOpen = true;
                ImGui.OpenPopup("Chat Limiter##SayWordChatLimiter");
            }

            Vector2 center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new Vector2(640, 555));

            if (ImGui.BeginPopupModal("Chat Limiter##SayWordChatLimiter", ref isChatLimiterOpen,
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

                if (ImGui.Button("Apply##SayWordApply", new Vector2(ImGui.GetWindowSize().X / 2 - 10, 25)))
                {
                    isChatLimiterOpen = false;
                    Plugin.Configuration.Save();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                ImGui.PushItemWidth(ImGui.GetWindowSize().X / 2);
                if (ImGui.Button("Reset All##SayWordReset", new Vector2(ImGui.GetWindowSize().X / 2 - 10, 25)))
                {
                    Chats.Clear();
                }

                ImGui.EndPopup();
            }


        }

        // This is UI Code for the Configuration Window.
        // It gets called in Windows/ConfigWindow under "Word Lists"
        // Basically, its offloaded here to let the User type in words that will get saved in the BannedWords property.
        // If you are creating a basic Rule that doesn't need any Input Userdata, then you can simply remove this function as well as the "override public bool hasAdvancedOptions { get; } = true;" in line 29.

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
