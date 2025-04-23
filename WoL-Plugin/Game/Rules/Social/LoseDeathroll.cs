using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    public class LoseDeathroll : BaseRule
    {
        override public string Name { get; } = "Lose a Deathroll";
        override public string Description { get; } = "Triggers whenever you lose a Deathroll";
        override public string Hint { get; } = "A Deathroll is when two players do /random on each others numbers until someone reaches 1 and loses.";
        override public RuleCategory Category { get; } = RuleCategory.Social;

        public List<ushort> TriggeringEmotes { get; set; } = new List<ushort>();

        [JsonConstructor]
        public LoseDeathroll() { }
        public LoseDeathroll(Plugin plugin) : base(plugin) { }

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
            if ((int)type == 2122 && messageE.Payloads.Find(pay => pay.Type == PayloadType.Icon) != null) // Deathroll channel and Icon found
            {
                string[] parts = messageE.ToString().Split(" ");
                if (parts[1].Length < 6) // check if the name is "You" or similiar, as different languages exist
                {
                    foreach (string part in parts)
                    {
                        if (char.IsDigit(part[0])) // this is a number
                        {
                            if (part.Length == 1 && part == "1")
                            {
                                Trigger("You lost a Deathroll!");
                            }
                        }
                    }
                }
            }
        }
    }
}
