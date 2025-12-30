using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using WoLightning.Configurations;
using WoLightning.Util.Types;
using Emote = Lumina.Excel.Sheets.Emote;

namespace WoLightning.WoL_Plugin.Windows
{
    public class ControlWindow : Window, IDisposable
    {

        public readonly Plugin Plugin;

        private bool hasAcceptedRisks = false;
        private Player? SelectedPlayer;
        private String SelectedPlayerName = "None";

        Emote? lastEmote = null;

        private string lockingEmoteName = "";
        private string unlockingEmoteName = "";
        private string leashDistanceEmoteName = "";
        private string leashEmoteName = "";
        private string unleashEmoteName = "";

        private Vector4 Red = new(1, 0.2f, 0.2f, 1);

        bool isModalShockerSelectorOpen = false;

        Vector4 ColorNameEnabled = new Vector4(0.5f, 1, 0.3f, 0.9f);
        Vector4 ColorNameBlocked = new Vector4(1.0f, 0f, 0f, 0.9f);
        Vector4 ColorNameDisabled = new Vector4(1, 1, 1, 0.9f);
        Vector4 ColorDescription = new Vector4(0.7f, 0.7f, 0.7f, 0.8f);

        private bool isOptionsOpen = false;
        private List<int> durationArray = [100, 300, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        public ControlWindow(Plugin plugin) : base("Warrior of Lightning - Control Settings")
        {
            Flags = ImGuiWindowFlags.AlwaysUseWindowPadding;


            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(500, 800),
                MaximumSize = new Vector2(650, 950)
            };
            Plugin = plugin;
        }

        public void Dispose()
        {
            if (this.IsOpen) this.Toggle();
        }

        public override void Draw()
        {
            if (!hasAcceptedRisks)
            {
                if (Plugin.ControlSettings.Controller != null) hasAcceptedRisks = true;
                drawRisks();
                return;
            }

            if (Plugin.ControlSettings.Controller == null)
            {
                drawSelectController();
                return;
            }

            if (Plugin.ControlSettings.FullControl)
            {
                ImGui.BeginDisabled();
                ImGui.Text("Your Controller for all of eternity:");
            }
            else
            {
                ImGui.Text("Your current Controller:");
            }
            string controllerName = Plugin.ControlSettings.Controller.getFullName();
            ImGui.SetNextItemWidth(250);
            ImGui.BeginDisabled();
            ImGui.InputText("##SelectedPlayerController", ref controllerName, 64, ImGuiInputTextFlags.ReadOnly);
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button("Remove Controller"))
            {
                Plugin.ControlSettings.Reset();
                return;
            }

            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Text("When allowing Permissions, you'll need to put in all of the Settings first.");


            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            string swapCommand = Plugin.ControlSettings.SwappingCommand;
            if (swapCommand == null || swapCommand.Length < 3) ImGui.BeginDisabled();

            bool swapAllowed = Plugin.ControlSettings.SwappingAllowed;
            if (ImGui.Checkbox("Allow Preset Swapping", ref swapAllowed))
            {
                Plugin.ControlSettings.SwappingAllowed = swapAllowed;
                Plugin.ControlSettings.SwappingCommand = swapCommand;
                Plugin.ControlSettings.Save();
            }

            if (swapCommand == null || swapCommand.Length < 3) ImGui.EndDisabled();

            ImGui.SameLine();
            ImGui.TextDisabled(" (?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("The Controller has to say the command word + a presets exact name." +
                    "\nExample, if the command is \"swap to\" they can write \"you should swap to Default\"" +
                    "\nBe mindful that they cannot write anything behind \"Default\" as well needing the correct case." +
                    "\nSomething like \"you should swap to Default or i will spank you.\" will NOT work." +
                    "\nThe Plugin would try to look for a Preset called \"Default or i will spank you.\"");
            }


            ImGui.SetNextItemWidth(300);
            if (swapAllowed) ImGui.BeginDisabled();
            if (ImGui.InputTextWithHint("##swapCommandInput", "Swap Command Word/Phrase", ref swapCommand, 64))
            {
                Plugin.ControlSettings.SwappingCommand = swapCommand;
            }
            if (swapAllowed) ImGui.EndDisabled();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextColored(ColorDescription, "All Emotes need to be used while targeting you.");


            bool lockAllowed = Plugin.ControlSettings.LockingAllowed;
            ushort lockEmote = Plugin.ControlSettings.LockingEmote;
            ushort unlockEmote = Plugin.ControlSettings.UnlockingEmote;

            if (lockEmote == 0 || unlockEmote == 0) ImGui.BeginDisabled();

            if (ImGui.Checkbox("Allow Preset Locking", ref lockAllowed))
            {
                Plugin.ControlSettings.LockingAllowed = lockAllowed;
                Plugin.ControlSettings.LockingEmote = lockEmote;
                Plugin.ControlSettings.UnlockingEmote = unlockEmote;
                if (!lockAllowed) Plugin.Configuration.IsLockedByController = false;
                Plugin.ControlSettings.Save();
            }

            if (lockEmote == 0 || unlockEmote == 0) ImGui.EndDisabled();

            ImGui.SameLine();
            ImGui.TextDisabled(" (?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Allows the Controller to Lock your ability of swapping Presets." +
                    "\nThey themselves are still able to swap them if \"Preset Swapping\" is allowed above." +
                    "\nIf both the Lock and Unlock emote are the same, using it will toggle between locking and unlocking.");
            }

            if (lockAllowed) ImGui.BeginDisabled();
            ImGui.Text("Emote to Lock Presets:");
            ImGui.SetNextItemWidth(250);
            ImGui.BeginDisabled();
            ImGui.InputTextWithHint("##LockingEmoteName", Plugin.ControlSettings.LastEmoteFromControllerName, ref lockingEmoteName, 64, ImGuiInputTextFlags.ReadOnly);
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button("Set to Controllers last used Emote##lockEmoteButton"))
            {
                if (Plugin.ControlSettings.LastEmoteFromController == 0) return;
                lockEmote = Plugin.ControlSettings.LastEmoteFromController;
                Plugin.ControlSettings.LockingEmote = lockEmote;
                lockingEmoteName = Plugin.ControlSettings.LastEmoteFromControllerName;
            }

            ImGui.Text("Emote to Unlock Presets:");
            ImGui.SetNextItemWidth(250);
            ImGui.BeginDisabled();
            ImGui.InputTextWithHint("##UnlockingEmoteName", Plugin.ControlSettings.LastEmoteFromControllerName, ref unlockingEmoteName, 64, ImGuiInputTextFlags.ReadOnly);
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button("Set to Controllers last used Emote##unlockEmoteButton"))
            {
                if (Plugin.ControlSettings.LastEmoteFromController == 0) return;
                unlockEmote = Plugin.ControlSettings.LastEmoteFromController;
                Plugin.ControlSettings.UnlockingEmote = unlockEmote;
                unlockingEmoteName = Plugin.ControlSettings.LastEmoteFromControllerName;
            }

            if (lockAllowed) ImGui.EndDisabled();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            bool leashAllowed = Plugin.ControlSettings.LeashAllowed;
            float leashDistance = Plugin.ControlSettings.LeashDistance;
            float leashGraceTime = Plugin.ControlSettings.LeashGraceTime;
            float leashGraceAreaTime = Plugin.ControlSettings.LeashGraceAreaTime;
            ushort leashEmote = Plugin.ControlSettings.LeashEmote;
            ushort unleashEmote = Plugin.ControlSettings.UnleashEmote;
            ushort leashDistanceEmote = Plugin.ControlSettings.LeashDistanceEmote;

            if (leashEmote == 0 || unleashEmote == 0 || leashDistanceEmote == 0) ImGui.BeginDisabled();

            if (ImGui.Checkbox("Allow Leashing", ref leashAllowed))
            {
                Plugin.ControlSettings.LeashAllowed = leashAllowed;
                Plugin.ControlSettings.LeashDistance = leashDistance;
                Plugin.ControlSettings.LeashGraceTime = leashGraceTime;
                Plugin.ControlSettings.LeashGraceAreaTime = leashGraceAreaTime;
                Plugin.ControlSettings.LeashEmote = leashEmote;
                Plugin.ControlSettings.UnleashEmote = unleashEmote;
                Plugin.ControlSettings.LeashDistanceEmote = leashDistanceEmote;
                Plugin.ControlSettings.Save();
            }

            if (leashEmote == 0 || unleashEmote == 0 || leashDistanceEmote == 0) ImGui.EndDisabled();

            ImGui.SameLine();
            ImGui.TextDisabled(" (?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Allows the Controller to leash you by using a Emote on you." +
                    "\nIf the Emote for leashing and unleashin is the same, using it will toggle the Leash." +
                    "\nYou will have to stay within a certain radius of them, or get shocked after some time." +
                    "\nAnother emote is used to adjust the radius to whatever current distance you have." +
                    "\nAfter leaving the radius and a grace period, you will first receive 2 warning vibrations, then increasing shocks." +
                    "\nIf the Controller leaves the Area, you get a special Area Grace Period, which is alot longer." +
                    "\nIn the case that the Controller logs off, you can open the Friendlist and it will remove the leash." +
                    "\nAlso, never forget that you can use /red to stop receiving any more shocks!");
            }

            if (leashAllowed) ImGui.BeginDisabled();

            DrawLeashBase();
            if (!Plugin.ControlSettings.LeashAllowed)
            {
                ImGui.SameLine();
                ImGui.Text("Options");
            }
            if (isOptionsOpen)
            {
                DrawLeashOptions();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
            }

            ImGui.Text("Emote to attach Leash:");
            ImGui.SetNextItemWidth(250);
            ImGui.BeginDisabled();
            ImGui.InputTextWithHint("##LeashEmoteName", Plugin.ControlSettings.LastEmoteFromControllerName, ref leashEmoteName, 64, ImGuiInputTextFlags.ReadOnly);
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button("Set to Controllers last used Emote##leashEmoteButton"))
            {
                if (Plugin.ControlSettings.LastEmoteFromController == 0) return;
                leashEmote = Plugin.ControlSettings.LastEmoteFromController;
                Plugin.ControlSettings.LeashEmote = leashEmote;
                leashEmoteName = Plugin.ControlSettings.LastEmoteFromControllerName;
            }

            ImGui.Text("Emote to remove Leash:");
            ImGui.SetNextItemWidth(250);
            ImGui.BeginDisabled();
            ImGui.InputTextWithHint("##UnleashEmoteName", Plugin.ControlSettings.LastEmoteFromControllerName, ref unleashEmoteName, 64, ImGuiInputTextFlags.ReadOnly);
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button("Set to Controllers last used Emote##unleashEmoteButton"))
            {
                if (Plugin.ControlSettings.LastEmoteFromController == 0) return;
                unleashEmote = Plugin.ControlSettings.LastEmoteFromController;
                Plugin.ControlSettings.UnleashEmote = unleashEmote;
                unleashEmoteName = Plugin.ControlSettings.LastEmoteFromControllerName;
            }

            ImGui.Text("Maximum Distance from Controller: (yalms)");
            ImGui.SetNextItemWidth(75);
            if (ImGui.InputFloat("##leashDistanceInput", ref leashDistance))
            {
                Plugin.ControlSettings.LeashDistance = leashDistance;
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(140);
            if (ImGui.Button("Set to current Distance"))
            {
                leashDistance = Plugin.ControlSettings.DistanceFromController();
                Plugin.ControlSettings.LeashDistance = leashDistance;

            }

            ImGui.Text("Emote to change to current Distance: (can't be same as above)");
            ImGui.SetNextItemWidth(250);
            ImGui.BeginDisabled();
            ImGui.InputTextWithHint("##DistanceEmoteName", Plugin.ControlSettings.LastEmoteFromControllerName, ref leashDistanceEmoteName, 64, ImGuiInputTextFlags.ReadOnly);
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button("Set to Controllers last used Emote##distanceEmoteButton"))
            {
                if (Plugin.ControlSettings.LastEmoteFromController == 0) return;
                leashDistanceEmote = Plugin.ControlSettings.LastEmoteFromController;
                Plugin.ControlSettings.LeashDistanceEmote = leashDistanceEmote;
                leashDistanceEmoteName = Plugin.ControlSettings.LastEmoteFromControllerName;
            }

            ImGui.Text("Grace Time in Seconds:");
            ImGui.SetNextItemWidth(95);
            if (ImGui.InputFloat("##leashGraceInput", ref leashGraceTime))
            {
                Plugin.ControlSettings.LeashGraceTime = leashGraceTime;
            }

            ImGui.Text("Left Area Grace Time in Seconds:");
            ImGui.SetNextItemWidth(95);
            if (ImGui.InputFloat("##leashGraceAreaInput", ref leashGraceAreaTime))
            {
                Plugin.ControlSettings.LeashGraceAreaTime = leashGraceAreaTime;
            }

            if (leashGraceAreaTime < 30)
            {
                ImGui.TextColored(Red, "It is strongly recommended to not set this value too low." +
                    "\nLoading Screens still count towards this time!");
            }


            if (leashAllowed) ImGui.EndDisabled();


            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();

            bool fullControl = Plugin.ControlSettings.FullControl;

            if (!swapAllowed || !lockAllowed || !leashAllowed) ImGui.BeginDisabled();

            if (ImGui.Checkbox("Make Settings Permanent", ref fullControl))
            {
                Plugin.ControlSettings.FullControl = fullControl;
                Plugin.Configuration.ActivateOnStart = true;
                if (!Plugin.IsEnabled)
                {
                    Plugin.IsEnabled = true;
                    Plugin.Configuration.ActivePreset.StartRules();
                }
                Plugin.ControlSettings.Save();
            }

            if (!swapAllowed || !lockAllowed || !leashAllowed) ImGui.EndDisabled();

            ImGui.SameLine();
            ImGui.TextDisabled(" (?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Enabling this makes you unable to remove the Controller or their Permissions." +
                    "\nThe only way to remove them, is by either resetting the Plugins data, or changing the Files in /wolfolder manually." +
                    "\nYou can only enable this, once you have given all permissions to the Controller.");
            }

            if (Plugin.ControlSettings.FullControl) ImGui.EndDisabled();

            if (fullControl)
            {
                if (Plugin.ControlSettings.SafewordDisabled) ImGui.BeginDisabled();
                bool safewordDisabled = Plugin.ControlSettings.SafewordDisabled;
                if (ImGui.Checkbox("Disable /red Safeword", ref safewordDisabled))
                {
                    Plugin.ControlSettings.SafewordDisabled = safewordDisabled;
                    Plugin.ControlSettings.Save();
                }
                if (Plugin.ControlSettings.SafewordDisabled) ImGui.EndDisabled();
                ImGui.SameLine();
                ImGui.TextDisabled(" (?)");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Enabling this removes the /red safeword command." +
                        "\nThis is potentially very dangerous, so please make absolutely sure that you actually want this!" +
                        "\nOnce enabled, you cannot reverse this decision without resetting the Plugins data, or changing the Files in /wolfolder manually.");
                }
            }

        }

        private void drawSelectController()
        {
            IGameObject? st = Service.ObjectTable.LocalPlayer.TargetObject;
            if (st != null && st.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player)
            {
                IPlayerCharacter st1 = (IPlayerCharacter)st;
                if (SelectedPlayer == null || SelectedPlayer.Name != st1.Name.ToString())
                {
                    SelectedPlayer = new Player(st1.Name.ToString(), (int)st1.HomeWorld.Value.RowId);
                }
            }
            else SelectedPlayer = null;

            if (SelectedPlayer != null) SelectedPlayerName = SelectedPlayer.Name + "@" + SelectedPlayer.getWorldName();

            ImGui.Text("To begin, select a Player ingame.");
            ImGui.BeginDisabled();
            ImGui.InputText("##SelectedPlayerController", ref SelectedPlayerName, 64, ImGuiInputTextFlags.ReadOnly);
            ImGui.EndDisabled();

            if (SelectedPlayerName != null && SelectedPlayerName != "None")
            {
                if (ImGui.Button("Set as Controller"))
                {
                    Plugin.ControlSettings.Controller = new(SelectedPlayerName);
                    Plugin.ControlSettings.Save();
                }
            }
        }

        private void drawRisks()
        {
            ImGui.TextWrapped("The Settings in this Window, allow you to designate a \"Controller\"." +
                "\nYou can let that person control some aspects of the Plugin." +
                "\nMost of the interactions run through the game, via Emotes or Chat.");
            ImGui.TextColoredWrapped(Red, "Because of this, its up to you to make sure you don't get into trouble with Square Enix.");
            ImGui.Spacing();
            ImGui.TextWrapped("In short: Don't use the features in obvious ways, like Limsa /say chat");
            if (ImGui.Button("I understand the Risks."))
            {
                hasAcceptedRisks = true;
            }

            Emote? emote = null;
            if (Plugin.ControlSettings.LockingEmote != 0)
            {
                emote = Plugin.GameEmotes.getEmote(Plugin.ControlSettings.LockingEmote);
                lockingEmoteName = ((Emote)emote!).Name.ToString();
            }

            if (Plugin.ControlSettings.UnlockingEmote != 0)
            {
                emote = Plugin.GameEmotes.getEmote(Plugin.ControlSettings.UnlockingEmote);
                unlockingEmoteName = ((Emote)emote!).Name.ToString();
            }

            if (Plugin.ControlSettings.LeashDistanceEmote != 0)
            {
                emote = Plugin.GameEmotes.getEmote(Plugin.ControlSettings.LeashDistanceEmote);
                leashDistanceEmoteName = ((Emote)emote!).Name.ToString();
            }

            if (Plugin.ControlSettings.LeashEmote != 0)
            {
                emote = Plugin.GameEmotes.getEmote(Plugin.ControlSettings.LeashEmote);
                leashEmoteName = ((Emote)emote!).Name.ToString();
            }

            if (Plugin.ControlSettings.UnleashEmote != 0)
            {
                emote = Plugin.GameEmotes.getEmote(Plugin.ControlSettings.UnleashEmote);
                unleashEmoteName = ((Emote)emote!).Name.ToString();
            }

        }


        protected void DrawShockerSelector()
        {
            if (ImGui.Button($"Assigned {Plugin.ControlSettings.LeashShockOptions.getShockerCount()} Shockers##assignedShockersLeash", new Vector2(150, 25)))
            {
                isModalShockerSelectorOpen = true;
                ImGui.OpenPopup("Select Shockers##ShockerSelectLeash");
            }

            if (isModalShockerSelectorOpen) //setup modal
            {
                Vector2 center = ImGui.GetMainViewport().GetCenter();
                ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
                ImGui.SetNextWindowSize(new Vector2(400, 400));
            }

            if (ImGui.BeginPopupModal("Select Shockers##ShockerSelectLeash", ref isModalShockerSelectorOpen,
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.Popup))
            {
                if (Plugin.Authentification.GetShockerCount() == 0)
                {
                    ImGui.TextWrapped("The Shockers are still being loaded!" +
                        "\nIf this doesn't change, please make sure that your" +
                        "\nAccount Settings are properly set up!");
                    if (ImGui.Button($"Okay##okayShockerSelectorAbort", new Vector2(ImGui.GetWindowSize().X / 2, 25)))
                    {
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndPopup();
                    return;
                }



                ImGui.Text("Please select all shockers that should activate for this trigger:");
                ImGui.BeginGroup();
                ImGui.Text("Available Pishock Devices:           ");
                ImGui.BeginChild("PishockShockerList", new Vector2(180, 260));
                foreach (var shocker in Plugin.Authentification.PishockShockers)
                {
                    if (Plugin.Configuration.ShownShockers == ShownShockers.None) continue;
                    if (Plugin.Configuration.ShownShockers == ShownShockers.Personal && !shocker.isPersonal) continue;
                    if (Plugin.Configuration.ShownShockers == ShownShockers.Shared && shocker.isPersonal) continue;


                    bool isEnabled = Plugin.ControlSettings.LeashShockOptions.ShockersPishock.Find(sh => sh.getInternalId() == shocker.getInternalId()) != null;

                    if (ImGui.Checkbox($"##shockerbox{shocker.getInternalId()}", ref isEnabled))
                    { // this could probably be solved more elegantly
                        if (isEnabled) Plugin.ControlSettings.LeashShockOptions.ShockersPishock.Add(shocker);
                        else Plugin.ControlSettings.LeashShockOptions.ShockersPishock.RemoveAt(Plugin.ControlSettings.LeashShockOptions.ShockersPishock.FindIndex(sh => sh.getInternalId() == shocker.getInternalId()));
                    }
                    ImGui.SameLine();
                    if (!shocker.isPersonal)
                    {
                        ImGui.BeginGroup();
                        ImGui.Text(shocker.username);
                        if (!shocker.isPaused) ImGui.TextColored(ColorNameEnabled, shocker.name);
                        else ImGui.TextColored(ColorNameDisabled, "[Paused] " + shocker.name);
                        ImGui.EndGroup();
                        continue;
                    }
                    if (!shocker.isPaused) ImGui.TextColored(ColorNameEnabled, shocker.name);
                    else ImGui.TextColored(ColorNameDisabled, "[Paused] " + shocker.name);
                }

                ImGui.EndChild();
                ImGui.EndGroup();

                ImGui.SameLine();

                ImGui.BeginGroup();
                ImGui.Text("Available OpenShock Devices:");
                ImGui.BeginChild("OpenShockShockerList", new Vector2(180, 260));
                foreach (var shocker in Plugin.Authentification.OpenShockShockers)
                {
                    bool isEnabled = Plugin.ControlSettings.LeashShockOptions.ShockersOpenShock.Find(sh => sh.getInternalId() == shocker.getInternalId()) != null;

                    if (ImGui.Checkbox($"##shockerbox{shocker.getInternalId()}", ref isEnabled))
                    { // this could probably be solved more elegantly
                        if (isEnabled) Plugin.ControlSettings.LeashShockOptions.ShockersOpenShock.Add(shocker);
                        else Plugin.ControlSettings.LeashShockOptions.ShockersOpenShock.RemoveAt(Plugin.ControlSettings.LeashShockOptions.ShockersOpenShock.FindIndex(sh => sh.getInternalId() == shocker.getInternalId()));
                    }
                    ImGui.SameLine();
                    if (!shocker.isPaused) ImGui.TextColored(ColorNameEnabled, shocker.name);
                    else ImGui.TextColored(ColorNameDisabled, "[Paused] " + shocker.name);
                }
                ImGui.EndChild();
                ImGui.EndGroup();

                ImGui.SetCursorPos(new Vector2(ImGui.GetWindowSize().X / 2 - 170, ImGui.GetWindowSize().Y - 65));
                ImGui.SetNextItemWidth(200);
                int ShownShockersIndex = (int)Plugin.Configuration.ShownShockers;
                if (ImGui.Combo("Shown Shockers", ref ShownShockersIndex, ["All", "Personal Only", "Shared Only", "None...?"], 4))
                {
                    Plugin.Configuration.ShownShockers = (ShownShockers)ShownShockersIndex;
                    Plugin.Configuration.Save();
                }
                ImGui.SameLine();
                ImGui.TextDisabled(" (?)");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Allows you to select which Shockers show up on clicking the \"Assign Shockers\" button.");
                }

                ImGui.SetCursorPos(new Vector2(ImGui.GetWindowSize().X / 2 - 170, ImGui.GetWindowSize().Y - 35));
                ImGui.BeginGroup();
                if (ImGui.Button($"Apply##applyLeash", new Vector2(ImGui.GetWindowSize().X - 120, 25)))
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button($"Reset##resetallLeash", new Vector2(ImGui.GetWindowSize().X / 8, 25)))
                {
                    Plugin.ControlSettings.LeashShockOptions.ShockersPishock.Clear();
                }
                ImGui.EndGroup();


                ImGui.EndPopup();
            }
        }


        public void DrawLeashBase()
        {
            if (!Plugin.ControlSettings.LeashAllowed)
            {

                if (isOptionsOpen && ImGui.ArrowButton("##collapseLeash", ImGuiDir.Down))
                {
                    isOptionsOpen = !isOptionsOpen;
                }
                if (!isOptionsOpen && ImGui.ArrowButton("##collapseLeash", ImGuiDir.Right))
                {
                    isOptionsOpen = !isOptionsOpen;
                }
            }
        }

        public void DrawLeashOptions()
        {
            bool changed = false;
            DrawShockerSelector();
            DrawLeashOptionsBase(ref changed);
            if (changed) Plugin.ControlSettings.Save();
        }
        protected void DrawLeashOptionsBase(ref bool changed)
        {
            ImGui.BeginGroup();
            ImGui.Text("    Mode");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 3 - 50);
            int OpMode = (int)Plugin.ControlSettings.LeashShockOptions.OpMode;
            if (ImGui.Combo("##OpModeSelectLeash", ref OpMode, ["Shock", "Vibrate", "Beep"], 3))
            {
                Plugin.ControlSettings.LeashShockOptions.OpMode = (OpMode)OpMode;
                changed = true;
            }
            ImGui.EndGroup();

            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.Text("    Duration");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 7);
            int DurationIndex = durationArray.IndexOf(Plugin.ControlSettings.LeashShockOptions.Duration);
            if (ImGui.Combo("##DurationSelectLeash", ref DurationIndex, ["0.1s", "0.3s", "1s", "2s", "3s", "4s", "5s", "6s", "7s", "8s", "9s", "10s"], 12))
            {
                Plugin.ControlSettings.LeashShockOptions.Duration = durationArray[DurationIndex];

                float intervalS = Plugin.ControlSettings.LeashTriggerInterval;
                int duration = Plugin.ControlSettings.LeashShockOptions.Duration;
                if (duration > 10) duration = 1;
                if (intervalS < duration) intervalS = duration;
                Plugin.ControlSettings.LeashTriggerInterval = intervalS;
                changed = true;
            }
            ImGui.EndGroup();

            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.Text("    Intensity");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 2.50f - 30);
            int Intensity = Plugin.ControlSettings.LeashShockOptions.Intensity;
            if (ImGui.SliderInt("##IntensitySelectLeash", ref Intensity, 1, 100))
            {
                Plugin.ControlSettings.LeashShockOptions.Intensity = Intensity;
                changed = true;
            }
            ImGui.EndGroup();

            ImGui.BeginGroup();
            ImGui.Text("Amount of Warning Vibrations");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 2.50f - 30);
            int warningAmount = Plugin.ControlSettings.LeashWarningScalingAmount;
            if (ImGui.InputInt("##WarningAmountLeash", ref warningAmount))
            {
                if (warningAmount < 0) warningAmount = 0;
                if (warningAmount > 9) warningAmount = 9;

                Plugin.ControlSettings.LeashWarningScalingAmount = warningAmount;
                changed = true;
            }
            ImGui.EndGroup();

            
            ImGui.BeginGroup();
            ImGui.Text("Amount of Triggers to reach above Settings");
            ImGui.SameLine();
            ImGui.TextDisabled(" (?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("This Setting will divide the Settings to scale them up after multiple requests." +
                    "\nIf you set this to 5, then the first request will be divided by 4, next one by 3, and so on, until it reaches 100% of your settings above.");
            }
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 2.50f - 30);
            int shockAmount = Plugin.ControlSettings.LeashShockScalingAmount;
            if (ImGui.InputInt("##ShockAmountLeash", ref shockAmount))
            {
                if (shockAmount < 0) shockAmount = 0;
                if (shockAmount > 9) shockAmount = 9;

                Plugin.ControlSettings.LeashShockScalingAmount = shockAmount;
                changed = true;
            }
            ImGui.EndGroup();

            ImGui.BeginGroup();
            ImGui.Text("Interval of Triggers (s)");
            ImGui.SameLine();
            ImGui.TextDisabled(" (?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("This sets at which interval requests will be sent out." +
                    "\n5 seconds would mean, that for every 5 seconds that you are outside the leash distance, a request will be sent." +
                    "\nPlease note that this doesnt include the Duration itself, so if you have a 5 second shock and a 5 second interval, you will get consistent shocks.");
            }
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 2.50f - 30);
            float interval = Plugin.ControlSettings.LeashTriggerInterval;
            int durationT = Plugin.ControlSettings.LeashShockOptions.Duration;
            if (durationT > 10) durationT = 1;
            if (ImGui.InputFloat("##ShockInterval", ref interval))
            {
                if (interval < durationT) interval = durationT;
                if (interval > 60) interval = 60;

                Plugin.ControlSettings.LeashTriggerInterval = interval;
                changed = true;
            }
            ImGui.EndGroup();

            if(interval == durationT)
            {
                ImGui.TextColored(ColorDescription,"You cannot set the interval lower than the maximum duration." +
                    "\n(Because setting it to the same means constant shocks already)");
            }

        }
    }
}
