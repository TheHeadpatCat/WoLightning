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
    public class DontSayWord : RuleBase
    {
        public override string Name { get; } = "Forget to say a Enforced Word";

        public override string Description { get; } = "Triggers whenever you do not say a word from a list.";
        override public string Hint { get; } = "Once enabled, you can set the Words in the \"Word List\" Tab!";

        public override RuleCategory Category { get; } = RuleCategory.Social;

        public List<SpecificWord> EnforcedWords { get; set; } = new();
        public List<XivChatType> Chats { get; set; } = new();
        [JsonIgnore] private bool isChatLimiterOpen = false;

        override public bool hasAdvancedOptions { get; } = true;
        public override bool hasExtraButton { get; } = true;

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
                if (Service.ClientState.LocalPlayer == null) return;

                //check for chat type limitation
                if (Chats.Count >= 1 && !Chats.Contains(type)) return;

                // Check if its actually a emote. We don't want to trigger on emotes.
                if (type == XivChatType.StandardEmote || type == XivChatType.CustomEmote) return;

                Player? sender = null;
                foreach (var payload in senderE.Payloads)
                {
                    if (payload.Type == PayloadType.Player) sender = new(payload);
                }

                if (sender == null && senderE.TextValue == Plugin.LocalPlayer.Name) sender = Plugin.LocalPlayer; // If there is no player payload, check if names match atleast.
                else return;

                Logger.Log(4, sender);

                if ((int)type <= 107 &&
                    (sender.Equals(Plugin.LocalPlayer)
                    || type == XivChatType.TellOutgoing)) // Tell is a very weird channel. It considers only the target player and gives us no reference of the actual sender.
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
                    if (!found) Trigger($"You forgot to say a Enforced Word!", sender);
                }
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); }
        }

        public override void DrawExtraButton()
        {
            ImGui.SameLine();
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
