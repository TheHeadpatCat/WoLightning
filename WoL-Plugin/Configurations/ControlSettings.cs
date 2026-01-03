using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;
using System.Timers;
using WoLightning.Util;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util;
using Version = WoLightning.WoL_Plugin.Util.Types.Version;

namespace WoLightning.WoL_Plugin.Configurations
{
    [Serializable]
    public class ControlSettings : Saveable, IDisposable // this is probably the worst class i ever made. it breaks more conventions than i commited war crimes
    {

        [JsonIgnore] private Plugin Plugin;
        protected override string FileName { get; } = "ControlSettings.json";
        protected override Version CurrentVersion { get; init; } = new Version(1, 1, 0, 'a');

        public Player Controller { get; set; }


        public bool SwappingAllowed { get; set; }
        public string SwappingCommand { get; set; } = "";

        public bool LockingAllowed { get; set; } = false;
        public ushort LockingEmote { get; set; }
        public ushort UnlockingEmote { get; set; }

        // Todo: Move this into its own class or something, this is terrible
        public bool LeashAllowed { get; set; } = false;
        public float LeashDistance { get; set; } = 8.5f;
        public float LeashGraceTime { get; set; } = 5;
        public float LeashGraceAreaTime { get; set; } = 30;
        public ushort LeashEmote { get; set; }
        public ushort UnleashEmote { get; set; }
        public ushort LeashDistanceEmote { get; set; }
        public ShockOptions LeashShockOptions { get; set; } = new();
        public float LeashTriggerInterval { get; set; } = 3;
        public int LeashWarningScalingAmount { get; set; } = 2;
        public int LeashShockScalingAmount { get; set; } = 5;
        public bool LeashShowDistanceWarning { get; set; } = true;
        public bool LeashShowGraceWarning { get; set; } = true;

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
        [JsonIgnore] private TimerPlus CheckFriendListTimer { get; set; } = new();
        [JsonIgnore] private int LastFriendAmount { get; set; } = 0;

        public ControlSettings(string saveLocation, bool reset = false) : base(saveLocation, reset)
        {

        }

        public void Initialize(Plugin plugin)
        {
            Plugin = plugin;
            if (Controller == null) Plugin.Configuration.IsLockedByController = false;
            Plugin.EmoteReaderHooks.OnEmoteIncoming += OnEmoteIncoming;
            Service.ChatGui.ChatMessage += OnChatMessage;
            Service.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "FriendList", OnFriendListOpened);
            Service.AddonLifecycle.RegisterListener(AddonEvent.PreClose, "FriendList", OnFriendListClosed);
            CheckFriendListTimer.Elapsed += CheckFriendList;
            CheckFriendListTimer.Interval = 3000;
        }



        private unsafe void CheckFriendList(object? sender, ElapsedEventArgs e)
        {
            if (Controller == null) return;
            try
            {
                AddonFriendList* addon = Service.GameGui.GetAddonByName<AddonFriendList>("FriendList");
                if (addon == null) return;
                var friendList = addon->GetNodeById(14)->GetAsAtkComponentList();
                if (friendList == null) return;

                Logger.Log(4, friendList->ListLength);

                if (LastFriendAmount == friendList->ListLength) return;
                LastFriendAmount = friendList->ListLength;

                var character = InfoProxyFriendList.Instance()->GetEntryByName(Controller.Name, (ushort)Controller.WorldId!);
                if (character == null) return;

                Logger.Log(4, character->State.ToString());
                if (character->State.ToString().Contains("Offline")) // for some reason, flags didnt work
                {
                    if (LeashActive)
                    {
                        Service.ChatGui.PrintError($"{Controller.Name} is offline, so the leash has been removed.");
                        RemoveLeash();
                    }
                }
                CheckFriendListTimer.Stop();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        private void OnFriendListOpened(AddonEvent eventType, AddonArgs addonRef)
        {
            CheckFriendListTimer.Refresh();
            CheckFriendListTimer.Start();
            LastFriendAmount = 0;
        }

        private void OnFriendListClosed(AddonEvent eventType, AddonArgs addonRef)
        {
            CheckFriendListTimer.Stop();
            LastFriendAmount = 0;
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
                Logger.Error(e.Message);
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
                        if (LockingEmote == UnlockingEmote)
                        {
                            Plugin.Configuration.IsLockedByController = !Plugin.Configuration.IsLockedByController;
                            if (Plugin.Configuration.IsLockedByController) Plugin.NotificationHandler.send("Locked Presets!");
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
                    if (emoteId == LeashEmote)
                    {
                        if (LeashEmote == UnleashEmote)
                        {
                            if (LeashActive) RemoveLeash();
                            else ApplyLeash();
                            return;
                        }

                        if (!LeashActive) ApplyLeash();
                        return;
                    }

                    if (emoteId == UnleashEmote)
                    {
                        if (LeashActive) RemoveLeash();
                        return;
                    }

                    if (emoteId == LeashDistanceEmote)
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


        public void ApplyLeash()
        {
            if (LeashActive) return;

            if (!Plugin.IsEnabled)
            {
                Service.ChatGui.PrintError($"{Controller.Name} tried to leash you, but the Plugin is stopped.");
                return;
            }

            LeashActive = true;
            LeashShockOptions.Validate();

            Service.Framework.Update += CheckLeash;
            Plugin.NotificationHandler.send("You have been Leashed!", $"{Controller.Name} applied a Leash to you");
            Service.ToastGui.ShowQuest($"Follow {Controller.Name}!");
            Service.ChatGui.PrintError($"You have been Leashed to {Controller.Name}!" +
                $"\nStay within {LeashDistance.ToString("0.0")} yalms of them.");

            LeashGraceTimer.Interval = LeashGraceTime * 1000;
            LeashGraceTimer.AutoReset = false;
            LeashGraceTimer.Elapsed += OnGraceElapsed;

            LeashGraceAreaTimer.Interval = LeashGraceAreaTime * 1000;
            LeashGraceAreaTimer.AutoReset = false;
            LeashGraceAreaTimer.Elapsed += OnGraceAreaElapsed;

            LeashShockTimer.Interval = LeashTriggerInterval * 1000;
            LeashShockTimer.AutoReset = true;
            LeashShockTimer.Elapsed += OnShockElapsed;

            HasBeenToldToFollow = true;

        }

        public void RemoveLeash()
        {
            if (!LeashActive) return;
            Service.Framework.Update -= CheckLeash;
            Plugin.NotificationHandler.send("The Leash has been removed!", $"{Controller.Name} removed the Leash");

            LeashGraceTimer.Elapsed -= OnGraceElapsed;
            LeashGraceAreaTimer.Elapsed -= OnGraceAreaElapsed;
            LeashShockTimer.Elapsed -= OnShockElapsed;
            LeashActive = false;
        }

        private void CheckLeash(IFramework framework)
        {
            if (CheckInterval > 0)
            {
                CheckInterval--;
                return;
            }
            CheckInterval = 30;

            if (!LeashAllowed || Controller == null)
            {
                RemoveLeash();
                return;
            }

            if (ControllerReference == null)
            {
                if (!HasBeenToldToFollow)
                {
                    Logger.Log(4, "Lost Controller Signature - searching...");
                    Service.ToastGui.ShowError($"{Controller.Name} has left the Area - Follow them!");
                    Service.ChatGui.PrintError($"{Controller.Name} has left the Area - Follow them!" +
                        $"\n(If they went offline, open your Friendlist to confirm)");
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

            HasBeenToldToFollow = false;

            float Distance = DistanceFromController();

            if (Distance > LeashDistance)
            {
                if (!HasBeenWarned)
                {
                    if (LeashShowDistanceWarning)
                    {
                        Service.ToastGui.ShowError($"You are too far from {Controller.Name}!");
                    }
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
            if (LeashShowGraceWarning)
            {
                Service.ToastGui.ShowError($"Get closer to {Controller.Name} or you will get shocked!");
                Service.ChatGui.PrintError($"Get closer to {Controller.Name} or you will get shocked!");
            }
            LeashShockTimer.Refresh();
            LeashShockTimer.Start();
        }

        private void OnGraceAreaElapsed(object? sender, ElapsedEventArgs e)
        {
            if (LeashShowGraceWarning)
            {
                Service.ToastGui.ShowError($"Follow {Controller.Name} or you will get shocked!");
                Service.ChatGui.PrintError($"Follow {Controller.Name} or you will get shocked!");
            }
            LeashShockTimer.Refresh();
            LeashShockTimer.Start();
        }

        private void OnShockElapsed(object? sender, ElapsedEventArgs e)
        {
            Logger.Log(4, "Shock Elapsed!");
            ShockOptions newOpt = new ShockOptions();

            try
            {
                if (LeashShockAmount < LeashWarningScalingAmount)
                {
                    float scale = (float)(LeashShockAmount + 1) / LeashWarningScalingAmount;
                    if (scale > 1) scale = 1;
                    Logger.Log(4, "Scaling " + scale);
                    newOpt.OpMode = OpMode.Vibrate;
                    newOpt.Intensity = (int)(100 * scale);
                    newOpt.Duration = (int)(LeashTriggerInterval * scale);
                    if (newOpt.Duration > 5) newOpt.Duration = 5;
                }
                else
                {
                    float scale = (float)(LeashShockAmount + 1 - LeashWarningScalingAmount) / LeashShockScalingAmount;
                    if (scale > 1) scale = 1;
                    Logger.Log(4, "Scaling " + scale);
                    newOpt.OpMode = LeashShockOptions.OpMode;
                    newOpt.Intensity = (int)(LeashShockOptions.Intensity * scale);
                    newOpt.Duration = (int)(LeashShockOptions.Duration * scale);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                Logger.Error(ex.ToString());
            }

            Logger.Log(4, "Creating Request...");
            Logger.Log(4, newOpt.ToString());

            newOpt.ShockersPishock = LeashShockOptions.ShockersPishock;
            newOpt.ShockersOpenShock = LeashShockOptions.ShockersOpenShock;
            newOpt.Validate();
            Plugin.ClientPishock.SendRequest(newOpt);
            Plugin.ClientOpenShock.SendRequest(newOpt);
            Plugin.NotificationHandler.send($"You are too far from {Controller.Name}");
            LeashShockAmount++;
        }

        public float DistanceFromController()
        {
            if (Controller == null) return 0;
            if (Plugin.LocalPlayer == null) return 0;

            ControllerReference = Controller.FindInObjectTable();

            if (ControllerReference == null) return 0;

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
            Service.AddonLifecycle.UnregisterListener(OnFriendListOpened);
            Service.AddonLifecycle.UnregisterListener(OnFriendListClosed);
            CheckFriendListTimer.Elapsed -= CheckFriendList;
            CheckFriendListTimer.Stop();
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
