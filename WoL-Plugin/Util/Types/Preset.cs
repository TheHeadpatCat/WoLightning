﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
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


        public DoEmote DoEmote { get; set; }
        public DoEmoteTo DoEmoteTo { get; set; }

        public void Initialize(Plugin Plugin)
        {
            isInitialized = true;
            DoEmote = new DoEmote(Plugin);
            DoEmoteTo = new DoEmoteTo(Plugin);
        }

        public bool isPlayerAllowedToTrigger(Player player)
        {
            if (player == null) return false;
            bool isAllowed = true;
            if (isBlacklistEnabled &&  Blacklist.Contains(player))isAllowed = false;
            if (isWhitelistEnabled && !Whitelist.Contains(player))isAllowed = false;
            return isAllowed;
        }

        /*
        // Social Triggers
        public ShockOptions GetPat { get; set; } = new ShockOptions("GetPat", "You got pat'd!", false);
        public ShockOptions GetSnapped { get; set; } = new ShockOptions("GetSnapped", "You got snap'd!", false);
        public ShockOptions LoseDeathRoll { get; set; } = new ShockOptions("LoseDeathroll", "You lost a deathroll!", false);
        public ShockOptions SitOnFurniture { get; set; } = new ShockOptions("SitOnFurniture", "You are sitting on furniture!", false);
        public ShockOptions SayFirstPerson { get; set; } = new ShockOptions("SayFirstPerson", "You refered to yourself wrongly!", false);
        public ShockOptions SayBadWord = new ShockOptions("SayBadWord", "You said a bad word!", true);
        public ShockOptions DontSayWord = new ShockOptions("DontSayWord", "You forgot to say a enforced word!", true);

        

        // Combat Triggers
        public ShockOptions TakeDamage { get; set; } = new ShockOptions("TakeDamage", "You took damage!", true);
        public ShockOptions FailMechanic { get; set; } = new ShockOptions("FailMechanic", "You failed a mechanic!", true);
        public ShockOptions Die { get; set; } = new ShockOptions("Die", "You died!", false);
        public ShockOptions PartymemberDies { get; set; } = new ShockOptions("PartymemberDies", "A partymember died!", false);
        public ShockOptions Wipe { get; set; } = new ShockOptions("Wipe", "Your party wiped!", false);
        */

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
