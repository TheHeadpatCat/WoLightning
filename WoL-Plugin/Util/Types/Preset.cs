using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using WoLightning.WoL_Plugin.Game.Rules;
using WoLightning.WoL_Plugin.Game.Rules.Misc;
using WoLightning.WoL_Plugin.Game.Rules.PVE;
using WoLightning.WoL_Plugin.Game.Rules.Social;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.Util.Types
{
    [Serializable]
    public class Preset(string Name, string CreatorFullName) : IDisposable
    {
        public int Version { get; set; } = 100;

        public bool isInitialized { get; set; } = false;
        public string Name { get; set; } = Name;


        public string CreatorFullName { get; set; } = CreatorFullName;
        public bool IsPresetLocked { get; set; } = false;



        public bool AllowRulesInPvP { get; set; } = false;

        public bool showTriggerNotifs { get; set; } = false;
        public bool showCooldownNotifs { get; set; } = false;

        public bool isWhitelistEnabled { get; set; } = false;
        public List<Player> Whitelist { get; set; } = new();
        public List<Player> Blacklist { get; set; } = new();

        [JsonIgnore] public List<RuleBase> Rules { get; set; } = new List<RuleBase>();
        [JsonIgnore] private Plugin Plugin;

        // The Position of your Rule here actually also changes the Position of which spot they show up in the Config Window!

        // TODO: Make these dynamic. Surely i can somehow iterate through all rules or smth. -- Is that even a good idea though?

        // Social Triggers
        public DoEmote DoEmote { get; set; } = new();
        public DoEmoteTo DoEmoteTo { get; set; } = new();
        public GetEmotedAt GetEmotedAt { get; set; } = new();
        public SayWord SayWord { get; set; } = new();
        public DontSayWord DontSayWord { get; set; } = new();
        public HearWord HearWord { get; set; } = new();
        public LoseDeathroll LoseDeathroll { get; set; } = new();

        // PVE Triggers
        public Die Die { get; set; } = new();
        public FailMechanic FailMechanic { get; set; } = new();
        //public HealPlayer HealPlayer { get; set; }
        public PartyMemberDies PartyMemberDies { get; set; } = new();
        public PartyWipes PartyWipes { get; set; } = new();
        public TakeDamage TakeDamage { get; set; } = new();
        //public UseSkill UseSkill { get; set; }
        public ForgetSync ForgetSync { get; set; } = new();

        // PVP Triggers


        // Misc Triggers
        public SitOnFurniture SitOnFurniture { get; set; } = new();
        public UseMount UseMount { get; set; } = new();
        public FailMeld FailMeld { get; set; } = new();
        public FailCraft FailCraft { get; set; } = new();
        public FailCraftHQ FailCraftHQ { get; set; } = new();
        public FishEscaped FishEscaped { get; set; } = new();
        public UseTeleport UseTeleport { get; set; } = new();

        public void Initialize(Plugin Plugin)
        {
            this.Plugin = Plugin;
            Logger.Log(3, "Initializing Preset - " + Name);

            if (CreatorFullName == null || CreatorFullName.Equals("Unknown") || CreatorFullName.Length == 0)
            {
                CreatorFullName = Plugin.LocalPlayer.getFullName();
            }

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
                        r.Triggered += Plugin.ClientIntiface.SendRequest;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex.Message);
                        Logger.Error("Failed to Load Rule");
                    }
                }
            }
            Logger.Log(3, Name + " has been initialized.");
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
                        if (i > 0) Logger.Log(2, "Removed " + i + " Invalid Shockers from " + r.Name);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex.Message);
                        Logger.Error("Failed to Load Rule");
                    }
                }
            }
            Plugin.Configuration.SaveCurrentPreset();
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
                if (Plugin != null) Logger.Error(ex.Message);
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
                        Logger.Log(3, "Starting " + Rule.Name);
                        Rule.Start();
                    }
                }
            }
            catch (Exception ex) { Logger.Error("Failed to start Rule " + Name); Logger.Error(ex.Message); }
        }
        public void StopRules()
        {
            try
            {
                foreach (var Rule in Rules)
                {
                    if (Rule.IsEnabled || Rule.IsRunning)
                    {
                        Logger.Log(3, "Stopping " + Rule.Name);
                        Rule.Stop();
                    }
                }
            }
            catch (Exception ex) { Logger.Error("Failed to start Rule " + Name); Logger.Error(ex.Message); }
        }
        public bool isPlayerAllowedToTrigger(Player player)
        {
            if (player == null) return false;
            if (player == Plugin.LocalPlayer) return true;
            bool isAllowed = true;

            foreach (var playerS in Blacklist)
            {
                Logger.Log(4, "comparing " + player + " and " + playerS + Blacklist.Contains(player));

            }

            if (Blacklist.Contains(player)) isAllowed = false;
            if (isWhitelistEnabled && !Whitelist.Contains(player)) isAllowed = false;
            return isAllowed;
        }

        public void resetInvalidTriggers()
        {
            Preset cleanPreset = new Preset("Clean", "None");

            foreach (var property in typeof(Preset).GetProperties())
            {
                //Logger.Log($"{property.Name} - {property.PropertyType}");
                if (property.PropertyType == typeof(DeviceOptions))
                {
                    object? obj = property.GetValue(this);
                    if (obj == null) continue;
                    DeviceOptions t = (DeviceOptions)obj;

                    if (!t.Validate())
                    {
                        property.SetValue(this, property.GetValue(cleanPreset));
                        ((DeviceOptions)property.GetValue(this)!).hasBeenReset = true;
                    }
                }
            }
        }
    }


}
