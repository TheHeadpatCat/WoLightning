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


        public bool IsPassthroughAllowed { get; set; } = false;
        public int globalTriggerCooldown { get; set; } = 10;
        public float globalTriggerCooldownGate { get; set; } = 0.75f;
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


        // Social Triggers
        public DoEmote DoEmote { get; set; }
        public DoEmoteTo DoEmoteTo { get; set; }
        public GetEmotedAt GetEmotedAt { get; set; }
        public SayWord SayWord { get; set; }
        public DontSayWord DontSayWord { get; set; }
        public LoseDeathroll LoseDeathroll { get; set; }

        // PVE Triggers
        public Die Die { get; set; }
        public FailMechanic FailMechanic { get; set; }
        //public HealPlayer HealPlayer { get; set; }
        public PartyMemberDies PartyMemberDies { get; set; }
        public PartyWipes PartyWipes { get; set; }
        public TakeDamage TakeDamage { get; set; }
        //public UseSkill UseSkill { get; set; }

        // PVP Triggers


        // Misc Triggers
        public SitOnFurniture SitOnFurniture { get; set; }
        public FailCraft FailCraft { get; set; }
        public FishEscaped FishEscaped { get; set; }

        public void Initialize(Plugin Plugin)
        {
            this.Plugin = Plugin;
            Plugin.Log("Initializing Preset - " + Name);

            // I have not found a better way to do this. I know this is terrible and probably a design issue.
            // Reflection has its own issues

            // Social
            DoEmote ??= new(Plugin);
            DoEmote.setPlugin(Plugin);

            DoEmoteTo ??= new(Plugin);
            DoEmoteTo.setPlugin(Plugin);

            GetEmotedAt ??= new(Plugin);
            GetEmotedAt.setPlugin(Plugin);

            SayWord ??= new(Plugin);
            SayWord.setPlugin(Plugin);

            DontSayWord ??= new(Plugin);
            DontSayWord.setPlugin(Plugin);

            LoseDeathroll ??= new(Plugin);
            LoseDeathroll.setPlugin(Plugin);

            // PVE

            Die ??= new(Plugin);
            Die.setPlugin(Plugin);

            FailMechanic ??= new(Plugin);
            FailMechanic.setPlugin(Plugin);

            //HealPlayer ??= new(Plugin);
            //HealPlayer.setPlugin(Plugin);

            PartyMemberDies ??= new(Plugin);
            PartyMemberDies.setPlugin(Plugin);

            PartyWipes ??= new(Plugin);
            PartyWipes.setPlugin(Plugin);

            TakeDamage ??= new(Plugin);
            TakeDamage.setPlugin(Plugin);

            //UseSkill ??= new(Plugin);
            //UseSkill.setPlugin(Plugin);

            // PVP


            // Misc

            SitOnFurniture ??= new(Plugin);
            SitOnFurniture.setPlugin(Plugin);

            FailCraft ??= new(Plugin);
            FailCraft.setPlugin(Plugin);

            FishEscaped ??= new(Plugin);
            FishEscaped.setPlugin(Plugin);


            foreach (PropertyInfo property in this.GetType().GetProperties())
            {
                if (property.PropertyType.BaseType == typeof(RuleBase))
                {
                    try
                    {
                        if (Rules.Contains((RuleBase)property.GetValue(this, null)!)) continue;
                        RuleBase r = (RuleBase)property.GetValue(this, null)!;
                        Rules.Add(r);
                        r.Triggered += Plugin.ClientPishock.SendRequest;
                        //r.Triggered += Plugin.ClientOpenShock.sendRequest; Todo: implement
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
                        
                        //if (Rules.Contains((RuleBase)property.GetValue(this, null)!)) continue;
                        RuleBase r = (RuleBase)property.GetValue(this, null)!;
                        int i = r.ShockOptions.ShockersPishock.RemoveAll((shocker) => !Plugin.Authentification.PishockShockers.Contains(shocker));
                        Plugin.Log("Removed " + i + " Invalid Shockers from " + r.Name);
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
                    //rule.Triggered -= Plugin.ClientOpenShock.sendRequest; Todo: implement
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
                        Plugin.Log("Starting " + Rule.Name);
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
                    Plugin.Log("Stopping " + Rule.Name);
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
