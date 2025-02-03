using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using WoLightning.WoL_Plugin.Game.Rules.Social;

namespace WoLightning.Util.Types
{
    [Serializable]
    public class Preset(string Name, string CreatorFullName)
    {

        [JsonIgnore] bool isInitialized = false;
        public string Name { get; set; } = Name;
        public string CreatorFullName { get; set; } = CreatorFullName;


        public bool IsPassthroughAllowed { get; set; } = false;
        public int globalTriggerCooldown { get; set; } = 10;
        public float globalTriggerCooldownGate { get; set; } = 0.75f;
        public bool showCooldownNotifs { get; set; } = false;

        public bool isWhitelistEnabled { get; set; } = false;
        public List<Player> Whitelist { get; set; }

        public bool isBlacklistEnabled { get; set; } = false;
        public List<Player> Blacklist { get; set; }


        // Social Triggers
        public DoEmote DoEmote { get; set; }
        public DoEmoteTo DoEmoteTo { get; set; }
        public GetEmotedAt GetEmotedAt { get; set; }

        public SayWord SayWord { get; set; }
        public DontSayWord DontSayWord { get; set; }

        public LoseDeathroll LoseDeathroll { get; set; }
        

        public void Initialize(Plugin Plugin)
        {
            isInitialized = true;
            DoEmote = new DoEmote(Plugin);
            DoEmoteTo = new DoEmoteTo(Plugin);
            GetEmotedAt = new GetEmotedAt(Plugin);
            SayWord = new SayWord(Plugin);
            DontSayWord = new DontSayWord(Plugin);
            LoseDeathroll = new LoseDeathroll(Plugin);
        }

        public bool isPlayerAllowedToTrigger(Player player)
        {
            if (player == null) return false;
            bool isAllowed = true;
            if (isBlacklistEnabled && Blacklist.Contains(player)) isAllowed = false;
            if (isWhitelistEnabled && !Whitelist.Contains(player)) isAllowed = false;
            return isAllowed;
        }

        public void resetInvalidTriggers()
        {
            Preset cleanPreset = new Preset("Clean", "None");

            foreach (var property in typeof(Preset).GetProperties())
            {
                //Log($"{property.Name} - {property.PropertyType}");
                if (property.PropertyType == typeof(ShockOptions))
                {
                    object? obj = property.GetValue(this);
                    if (obj == null) continue;
                    ShockOptions t = (ShockOptions)obj;

                    if (!t.Validate())
                    {
                        property.SetValue(this, property.GetValue(cleanPreset));
                        ((ShockOptions)property.GetValue(this)!).hasBeenReset = true;
                    }
                }
            }
        }
    }


}
