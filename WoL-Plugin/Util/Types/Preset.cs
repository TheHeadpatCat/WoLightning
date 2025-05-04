using Dalamud.Game.Text;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using WoLightning.WoL_Plugin.Game.Rules;
using WoLightning.WoL_Plugin.Game.Rules.Misc;
using WoLightning.WoL_Plugin.Game.Rules.PVE;
using WoLightning.WoL_Plugin.Game.Rules.Social;

namespace WoLightning.Util.Types
{
    [Serializable]
    public class Preset(string Name, string CreatorFullName) : IDisposable
    {

        public bool isInitialized { get; set; } = false;
        public string Name { get; set; } = Name;
        public string CreatorFullName { get; set; } = CreatorFullName;

        public bool AllowPVERulesInPVP { get; set; } = false;

        public bool showTriggerNotifs { get; set; } = false;
        public bool showCooldownNotifs { get; set; } = false;

        public bool isWhitelistEnabled { get; set; } = false;
        public List<Player> Whitelist { get; set; } = new();

        public bool isBlacklistEnabled { get; set; } = false;
        public List<Player> Blacklist { get; set; } = new();

        public bool LimitChats { get; set; } = false;
        public List<XivChatType> Chats { get; set; } = new();
        [JsonIgnore] public List<RuleBase> Rules { get; set; } = new List<RuleBase>();
        [JsonIgnore] private Plugin Plugin;


        // The Position of your Rule here actually also changes the Position of which spot they show up in the Config Window!

        // Social Triggers
        public DoEmote DoEmote { get; set; } = new();
        public DoEmoteTo DoEmoteTo { get; set; } = new();
        public GetEmotedAt GetEmotedAt { get; set; } = new();
        public SayWord SayWord { get; set; } = new();
        public DontSayWord DontSayWord { get; set; } = new();
        public LoseDeathroll LoseDeathroll { get; set; } = new();

        // PVE Triggers
        public Die Die { get; set; } = new();
        public FailMechanic FailMechanic { get; set; } = new();
        //public HealPlayer HealPlayer { get; set; }
        public PartyMemberDies PartyMemberDies { get; set; } = new();
        public PartyWipes PartyWipes { get; set; } = new();
        public TakeDamage TakeDamage { get; set; } = new();
        //public UseSkill UseSkill { get; set; }

        // PVP Triggers


        // Misc Triggers
        public SitOnFurniture SitOnFurniture { get; set; } = new();
        public FailCraft FailCraft { get; set; } = new();
        public FishEscaped FishEscaped { get; set; } = new();
            
        public void Initialize(Plugin Plugin)
        {
            this.Plugin = Plugin;
            Plugin.Log(3,"Initializing Preset - " + Name);

            foreach (PropertyInfo property in this.GetType().GetProperties())
            {
                if (property.PropertyType.BaseType == typeof(RuleBase))
                {
                    try
                    {
                        if (Rules.Contains((RuleBase)property.GetValue(this, null)!)) continue;
                        RuleBase r = (RuleBase)property.GetValue(this, null)!;
                        r.setPlugin(Plugin);
                        Rules.Add(r);
                        r.ShockOptions.Validate();
                        r.Triggered += Plugin.ClientPishock.SendRequest;
                        r.Triggered += Plugin.ClientOpenShock.SendRequest;
                    }
                    catch (Exception ex)
                    {
                        Plugin.Error(ex.StackTrace);
                        Plugin.Error("Failed to Load Rule");
                    }
                }
            }
        }

        public void ValidateShockers()
        {
            if (Plugin == null || Plugin.Authentification == null || Plugin.ClientPishock.Status != Clients.Pishock.ClientPishock.ConnectionStatusPishock.Connected) return;
            
            foreach (PropertyInfo property in this.GetType().GetProperties())
            {
                if (property.PropertyType.BaseType == typeof(RuleBase))
                {
                    try
                    {
                        RuleBase r = (RuleBase)property.GetValue(this, null)!;
                        int i = r.ShockOptions.ShockersPishock.RemoveAll((shocker) => !Plugin.Authentification.PishockShockers.Contains(shocker));
                        if(i > 0) Plugin.Log(2,"Removed " + i + " Invalid Shockers from " + r.Name);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Error(ex.StackTrace);
                        Plugin.Error("Failed to Load Rule");
                    }
                }
            }
            Plugin.Configuration.saveCurrentPreset();
        }

        public void Dispose()
        {
            try
            {
                foreach (RuleBase rule in Rules)
                {
                    rule.Stop();
                    rule.Triggered -= Plugin.ClientPishock.SendRequest;
                    rule.Triggered -= Plugin.ClientOpenShock.SendRequest;
                }
            }
            catch (Exception ex)
            {
                if(Plugin != null) Plugin.Error(ex.StackTrace);
            }
        }

        public void StartRules()
        {
            try
            {
                foreach (var Rule in Rules)
                {
                    if (Rule.IsEnabled)
                    {
                        Plugin.Log(3,"Starting " + Rule.Name);
                        Rule.Start();
                    }
                }
            }
            catch (Exception ex) { }
        }
        public void StopRules()
        {
            try
            {
                foreach (var Rule in Rules)
                {
                    Plugin.Log(3,"Stopping " + Rule.Name);
                    Rule.Stop();
                }
            }
            catch (Exception ex) { }
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
