using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using FFXIVClientStructs;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;
using System.Timers;
using System.Xml.Linq;
using WoLightning.Configurations;
using WoLightning.Util;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util;
using WoLightning.WoL_Plugin.Util.Types;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.CharaView.Delegates;
using Version = WoLightning.WoL_Plugin.Util.Types.Version;

namespace WoLightning.WoL_Plugin.Configurations
{
    [Serializable]
    public class ControlSettings : Saveable, IDisposable
    {

        [JsonIgnore] private Plugin Plugin;
        protected override string FileName { get; } = "ControlSettings.json";
        protected override Version CurrentVersion { get; init; } = new Version(1,0,0,'a');

        public Player Controller { get; set; }


        public bool SwappingAllowed { get; set; }
        public string SwappingCommand { get; set; } = "";

        public bool LockingAllowed { get; set; } = false;
        public ushort LockingEmote { get; set; }
        public ushort UnlockingEmote { get; set; }

        public bool LeashAllowed { get; set; } = false;
        public float LeashDistance { get; set; } = 8.5f;
        public float LeashGraceTime { get; set; } = 5;
        public float LeashGraceAreaTime { get; set; } = 30;
        public ushort LeashEmote { get; set; }
        public ushort UnleashEmote { get; set; }
        public ushort LeashDistanceEmote { get; set; }
        public ShockOptions LeashShockOptions { get; set; } = new();

        public bool FullControl { get; set; } = false;
        public bool SafewordDisabled { get; set; } = false;



        [JsonIgnore] public bool LeashActive { get; set; } = false;
        [JsonIgnore] private IPlayerCharacter? ControllerReference { get; set; } = null;
        [JsonIgnore] private bool HasBeenWarned { get; set; } = false;
        [JsonIgnore] private bool HasBeenToldToFollow { get; set; } = false;
        [JsonIgnore] public TimerPlus LeashGraceTimer { get; set; } = new TimerPlus();
        [JsonIgnore] public TimerPlus LeashGraceAreaTimer { get; set; } = new TimerPlus();
        [JsonIgnore] public TimerPlus LeashShockTimer { get; set; } = new TimerPlus();
        [JsonIgnore] public int LeashShockAmount { get; set; } = 0;
        [JsonIgnore] private int CheckInterval = 0;

        
        [JsonIgnore] public ushort LastEmoteFromController { get; set; } = 0;
        [JsonIgnore] public string LastEmoteFromControllerName { get; set; } = "None";

        public ControlSettings(string saveLocation, bool reset = false) : base(saveLocation, reset)
        {

        }

        public void Initialize(Plugin plugin)
        {
            Plugin = plugin;
            if (Controller == null) Plugin.Configuration.IsLockedByController = false;
            Plugin.EmoteReaderHooks.OnEmoteIncoming += OnEmoteIncoming;
            Service.ChatGui.ChatMessage += OnChatMessage;
        }

        private void OnChatMessage(XivChatType type, int timestamp, ref SeString senderE, ref SeString messageE, ref bool isHandled)
        {

            try
            {

                if (!SwappingAllowed || SwappingCommand.Length < 3) return;
                if (senderE.TextValue == null || senderE.TextValue == "") return;
                if (type == XivChatType.StandardEmote || type == XivChatType.CustomEmote) return;

                Player? sender = null;
                foreach (var payload in senderE.Payloads)
                {
                    if (payload.Type == PayloadType.Player) sender = new(payload);
                }

                if (sender == null) return;

                Logger.Log(4, sender);

                if (!sender.Equals(Controller)) return;

                if ((int)type <= 107 && type != XivChatType.TellOutgoing) // Allow all possible social channels, EXCEPT Tell_Outgoing
                {
                    string message = StringSanitizer.LetterOrDigit(messageE.ToString());

                    if (!message.Contains(SwappingCommand)) return;

                    var array = message.Split(SwappingCommand);
                    if (array.Length != 2)
                    {
                        Logger.Log(3, "Command to Swap was heard, but Message was malformed.");
                        Plugin.NotificationHandler.send($"Cannot swap Preset, as Message was malformed.", "Swap Failed", Dalamud.Interface.ImGuiNotification.NotificationType.Warning, new TimeSpan(0, 0, 5));
                        return;
                    }

                    /*
                    if (Plugin.Configuration.PresetNames.Contains(array[1]))
                    {
                        Logger.Log(3, $"Command to Swap was heard, but not Preset named \"{array[1]}\" exists.");
                        Plugin.NotificationHandler.send($"No Preset named \"{array[1]}\" exists.", "Swap Failed", Dalamud.Interface.ImGuiNotification.NotificationType.Warning, new TimeSpan(0, 0, 10));
                        return;
                    }*/

                    array[1] = array[1].Trim();
                    Logger.Log(3, $"Swapping to \"{array[1]}\" via Command.");

                    if (Plugin.Configuration.loadPreset(array[1]))
                    {
                        Plugin.NotificationHandler.send($"Swapping to Preset \"{array[1]}\"!", "Swap Success!", Dalamud.Interface.ImGuiNotification.NotificationType.Info, new TimeSpan(0, 0, 10));
                        return;
                    }
                    Plugin.NotificationHandler.send($"No Preset named \"{array[1]}\" exists.", "Swap Failed", Dalamud.Interface.ImGuiNotification.NotificationType.Warning, new TimeSpan(0, 0, 10));
                }
            }
            catch (Exception e)
            {
                Logger.Error("ControlSettings | Something went wrong while handling message.");
                Logger.Error(e);
                Logger.Error(e.StackTrace);
            }
         }

        private void OnEmoteIncoming(IPlayerCharacter character, ushort emoteId)
        {
            try
            {
                if (Controller == null) return;
                Player against = new(character);
                if (against == null) return;
                if (!against.Equals(Controller)) return;

                LastEmoteFromController = emoteId;
                Emote? emote = Plugin.GameEmotes.getEmote(emoteId);
                LastEmoteFromControllerName = ((Emote)emote!).Name.ToString();

                if (LockingAllowed) // this is terrible, please someone revoke my coding license
                {
                    if (emoteId == LockingEmote)
                    {
                        if(LockingEmote == UnlockingEmote)
                        {
                            Plugin.Configuration.IsLockedByController = !Plugin.Configuration.IsLockedByController;
                            if(Plugin.Configuration.IsLockedByController) Plugin.NotificationHandler.send("Locked Presets!");
                            else Plugin.NotificationHandler.send("Unlocked Presets!");
                            return;
                        }

                        Plugin.Configuration.IsLockedByController = true;
                        Plugin.NotificationHandler.send("Locked Presets!");
                        return;
                    }

                    if (emoteId == UnlockingEmote)
                    {
                        Plugin.Configuration.IsLockedByController = false;
                        Plugin.NotificationHandler.send("Unlocked Presets!");
                        return;
                    }
                }

                if (LeashAllowed)
                {
                    if(emoteId == LeashEmote)
                    {
                        if(LeashEmote == UnleashEmote)
                        {
                            if(LeashActive) RemoveLeash();
                            else ApplyLeash();
                            return;
                        }

                        if(!LeashActive) ApplyLeash();
                        return;
                    }

                    if (emoteId == UnleashEmote)
                    {
                        if(LeashActive) RemoveLeash();
                        return;
                    }

                    if(emoteId == LeashDistanceEmote)
                    {
                        LeashDistance = DistanceFromController() + 0.05f;
                        Service.ToastGui.ShowQuest($"Leash Distance is now {LeashDistance.ToString("0.0")} Yalms");
                        return;
                    }
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

            LeashShockOptions = new();

            if (LeashActive) RemoveLeash();

            Save();
        }


        private void ApplyLeash()
        {
            LeashActive = true;
            LeashShockOptions.Validate();

            Service.Framework.Update += CheckLeash;
            Plugin.NotificationHandler.send("You have been Leashed!",$"{Controller.Name} applied a Leash to you");
            Service.ToastGui.ShowQuest($"Follow {Controller.Name}!");

            LeashGraceTimer.Interval = LeashGraceTime * 1000;
            LeashGraceTimer.AutoReset = false;
            LeashGraceTimer.Elapsed += OnGraceElapsed;

            LeashGraceAreaTimer.Interval = LeashGraceAreaTime * 1000;
            LeashGraceAreaTimer.AutoReset = false;
            LeashGraceAreaTimer.Elapsed += OnGraceAreaElapsed;

            LeashShockTimer.Interval = 3000;
            LeashShockTimer.AutoReset = true;
            LeashShockTimer.Elapsed += OnShockElapsed;

            HasBeenToldToFollow = true;

        }

        private void RemoveLeash()
        {
            Service.Framework.Update -= CheckLeash;
            Plugin.NotificationHandler.send("The Leash has been removed!", $"{Controller.Name} removed the Leash");

            LeashGraceTimer.Elapsed -= OnGraceElapsed;
            LeashGraceAreaTimer.Elapsed -= OnGraceAreaElapsed;
            LeashShockTimer.Elapsed -= OnShockElapsed;
            LeashActive=false;
        }

        private void CheckLeash(IFramework framework)
        {
            if(CheckInterval > 0)
            {
                CheckInterval--;
                return;
            }
            CheckInterval = 30;

            if(ControllerReference == null)
            {
                if (!HasBeenToldToFollow)
                {
                    Logger.Log(4, "Lost Controller Signature - searching...");
                    Service.ToastGui.ShowError($"{Controller.Name} has left the Area - Follow them!");
                    ControllerReference = null;
                    LeashGraceAreaTimer.Refresh();
                    LeashGraceAreaTimer.Start();

                    LeashGraceTimer.Stop();
                    LeashShockAmount = 0;
                    HasBeenToldToFollow = true;
                }
                ControllerReference = Controller.FindInObjectTable();
                return;
            }

            HasBeenToldToFollow=false;

            float Distance = DistanceFromController();
            
            if(Distance > LeashDistance)
            {
                if (!HasBeenWarned)
                {
                    Service.ToastGui.ShowError($"You are too far from {Controller.Name}!");
                    HasBeenWarned = true;
                    LeashGraceTimer.Refresh();
                    LeashGraceTimer.Start();
                }
                return;
            }
            else
            {
                HasBeenWarned = false;
                LeashGraceTimer.Stop();
                LeashGraceAreaTimer.Stop();
                LeashShockTimer.Stop();
                LeashShockAmount = 0;
            }

        }

        private void OnGraceElapsed(object? sender, ElapsedEventArgs e)
        {
            Service.ToastGui.ShowError($"Get closer to {Controller.Name} or you will get shocked!");
            LeashShockTimer.Refresh();
            LeashShockTimer.Start();
        }

        private void OnGraceAreaElapsed(object? sender, ElapsedEventArgs e)
        {
            Service.ToastGui.ShowError($"Follow {Controller.Name} or you will get shocked!");
            LeashShockTimer.Refresh();
            LeashShockTimer.Start();
        }

        private void OnShockElapsed(object? sender, ElapsedEventArgs e)
        {
            ShockOptions newOpt;
            switch (LeashShockAmount) // i want to vomit
            {
                case 0: newOpt = new ShockOptions(OpMode.Vibrate, 30, 2); break;
                case 1: newOpt = new ShockOptions(OpMode.Vibrate, 60, 2); break;
                case 2: newOpt = new ShockOptions(OpMode.Shock, 10, 1); break;
                case 3: newOpt = new ShockOptions(OpMode.Shock, 30, 1); break;
                case 4: newOpt = new ShockOptions(OpMode.Shock, 60, 2); break;
                case 5: newOpt = new ShockOptions(OpMode.Shock, 80, 2); break;
                case 6: newOpt = new ShockOptions(OpMode.Shock, 90, 2); break;
                default: newOpt = new ShockOptions(OpMode.Shock, 100, 3); break;
            }

            newOpt.ShockersPishock = LeashShockOptions.ShockersPishock;
            newOpt.ShockersOpenShock = LeashShockOptions.ShockersOpenShock;
            newOpt.Validate();
            Plugin.ClientPishock.SendRequest(newOpt);
            Plugin.ClientOpenShock.SendRequest(newOpt);
            Plugin.NotificationHandler.send($"You are too far from {Controller.Name}", "Leash is broken!");
            LeashShockAmount++;
        }

        public float DistanceFromController()
        {
            if (Controller == null) return 0;
            if (Plugin.LocalPlayer == null) return 0;

            ControllerReference = Controller.FindInObjectTable();

            if(ControllerReference == null ) return 0;

            var local = Service.ObjectTable.LocalPlayer;

            float x = Math.Abs(ControllerReference.Position.X - local.Position.X);
            float y = Math.Abs(ControllerReference.Position.Y - local.Position.Y);
            float z = Math.Abs(ControllerReference.Position.Z - local.Position.Z);
            return x + y + z;
        }

        public void Dispose()
        {
            Plugin.EmoteReaderHooks.OnEmoteIncoming -= OnEmoteIncoming;
            Service.ChatGui.ChatMessage -= OnChatMessage;
            if (LeashActive) RemoveLeash();
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
