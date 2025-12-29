using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;
using WoLightning.Configurations;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util;
using WoLightning.WoL_Plugin.Util.Types;
using Version = WoLightning.WoL_Plugin.Util.Types.Version;

namespace WoLightning.WoL_Plugin.Configurations
{
    [Serializable]
    public class ControlSettings : Saveable, IDisposable
    {

        [JsonIgnore] private Plugin Plugin;
        protected override string FileName { get; } = "ControlSettings.json";
        protected override Version CurrentVersion { get; init; } = new Version(0,3,0,'a');

        public Player Controller { get; set; }


        public bool SwappingAllowed { get; set; }
        public string SwappingCommand { get; set; } = "";

        public bool LockingAllowed { get; set; } = false;
        public ushort LockingEmote { get; set; }
        public ushort UnlockingEmote { get; set; }

        public bool LeashAllowed { get; set; } = false;
        public int LeashDistance { get; set; } = 0;
        public float LeashGraceTime { get; set; } = 0;
        public ushort LeashEmote { get; set; }
        public ushort UnleashEmote { get; set; }
        public ushort LeashDistanceEmote { get; set; }

        public bool FullControl { get; set; } = false;


        [JsonIgnore] public ushort LastEmoteFromController { get; set; } = 0;
        [JsonIgnore] public string LastEmoteFromControllerName { get; set; } = "None";

        public ControlSettings(string saveLocation, bool reset = false) : base(saveLocation, reset)
        {

        }

        public void Initialize(Plugin plugin)
        {
            Plugin = plugin;
            Plugin.EmoteReaderHooks.OnEmoteIncoming += OnEmoteIncoming;
        }

        private void OnEmoteIncoming(IPlayerCharacter character, ushort arg2)
        {
            try
            {
                if (Controller == null) return;
                Player against = new(character);
                if (against == null) return;

                if (against.Equals(Controller))
                {
                    LastEmoteFromController = arg2;
                    Emote? emote = Plugin.GameEmotes.getEmote(arg2);
                    LastEmoteFromControllerName = ((Emote)emote!).Name.ToString();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                Logger.Error(e.StackTrace);
            }
        }

        public void Reset()
        {
            Controller = null;

            LockingAllowed = false;
            LockingEmote = 0;
            UnlockingEmote = 0;

            SwappingAllowed = false;
            SwappingCommand = "";

            LeashAllowed = false;
            LeashEmote = 0;
            UnleashEmote = 0;

            FullControl = false;

            Save();
        }



        public void Dispose()
        {
            Plugin.EmoteReaderHooks.OnEmoteIncoming -= OnEmoteIncoming;
            Save();
        }


        #region File Management
        private ControlSettings deserialize(string dataString)
        {
            Logger.Log(3, $"Deserializing ControlSettings...");
            if (dataString == "")
            {
                Logger.Log(3, $"No Data read, creating new settings...");
                return new ControlSettings(SaveLocation);
            }
            return JsonConvert.DeserializeObject<ControlSettings>(dataString)!;
        }

        internal void injectProperties(ControlSettings loadedSettings)
        {
            Logger.Log(3, $"Injecting Properties into ControlSettings...");
            foreach (PropertyInfo property in typeof(ControlSettings).GetProperties().Where(p => p.CanWrite)) property.SetValue(this, property.GetValue(loadedSettings, null), null);
        }

        public override void Load()
        {
            injectProperties(deserialize(loadFile()));
            updateFile();
        }

        internal override void updateFile()
        {
            if (CurrentVersion <= SavedVersion) return;

            Logger.Log(3, $"Updating ControlSettings from {SavedVersion} to {CurrentVersion}");
            if (CurrentVersion.NeedsUpdate(SavedVersion) >= Version.NeedUpdateState.Remake)
            {
                ControlSettings resetSettings = new ControlSettings(SaveLocation);
                foreach (PropertyInfo property in typeof(ControlSettings).GetProperties().Where(p => p.CanWrite)) property.SetValue(this, property.GetValue(resetSettings, null), null);
            }
        }
        #endregion
    }
}
