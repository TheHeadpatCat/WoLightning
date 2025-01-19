using System;
using System.Collections.Generic;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Configurations
{
    public class ConfigurationMaster : IDisposable
    {

        private readonly Plugin Plugin;

        // Mastermode - Master Settings
        public bool IsMaster { get; set; } = false;
        public Dictionary<string, Player> OwnedSubs { get; set; } = [];

        // Mastermode - Sub Settings
        public bool HasMaster { get; set; } = false;
        public Player? Master { get; set; }
        public bool isDisallowed { get; set; } = false; //locks the interface



        public void Save()
        {

        }

        public void Dispose()
        {
            Save();
        }
    }
}
