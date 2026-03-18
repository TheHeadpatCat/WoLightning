using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;
using WoLightning.Clients.Webserver.Operations.Account;
using WoLightning.Configurations;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util.Helpers;
using WoLightning.WoL_Plugin.Util.UI_Elements;
using Emote = Lumina.Excel.Sheets.Emote;

namespace WoLightning.WoL_Plugin.Windows
{
    public class ControlWindow : Window, IDisposable
    {

        public readonly Plugin Plugin;

        private bool hasAcceptedRisks = false;
        private Player? SelectedPlayer;
        private String SelectedPlayerName = "None";

        private string lockingEmoteName = "";
        private string unlockingEmoteName = "";
        private string leashDistanceEmoteName = "";
        private string leashEmoteName = "";
        private string unleashEmoteName = "";

        private Vector4 Red = new(1, 0.2f, 0.2f, 1);

        ShockOptionsEditor leashOptions;
        bool isModalShockerSelectorOpen = false;

        private bool isOptionsOpen = false;

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

            ImGui.TextColored(UIValues.ColorDescription, "All Emotes need to be used while targeting you.");


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
            bool leashShowDistanceWarning = Plugin.ControlSettings.LeashShowDistanceWarning;
            bool leashShowGraceWarning = Plugin.ControlSettings.LeashShowGraceWarning;

            if (leashEmote == 0 || unleashEmote == 0 || leashDistanceEmote == 0)
            {
                string missingEmotes = "";
                if (leashEmote == 0) missingEmotes += "Attach Leash\n";
                if (unleashEmote == 0) missingEmotes += "Remove Leash\n";
                if (leashDistanceEmote == 0) missingEmotes += "Change Distance";
                
                ImGui.TextColored(UIValues.ColorDescription, "Please set emotes for: \n" + missingEmotes);
                ImGui.BeginDisabled();
            }

            if (ImGui.Checkbox("Allow Leashing", ref leashAllowed))
            {
                Plugin.ControlSettings.LeashAllowed = leashAllowed;
                Plugin.ControlSettings.LeashDistance = leashDistance;
                Plugin.ControlSettings.LeashGraceTime = leashGraceTime;
                Plugin.ControlSettings.LeashGraceAreaTime = leashGraceAreaTime;
                Plugin.ControlSettings.LeashEmote = leashEmote;
                Plugin.ControlSettings.UnleashEmote = unleashEmote;
                Plugin.ControlSettings.LeashDistanceEmote = leashDistanceEmote;
                Plugin.ControlSettings.LeashShowDistanceWarning = leashShowDistanceWarning;
                Plugin.ControlSettings.LeashShowGraceWarning = leashShowGraceWarning;
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

            if (ImGui.Checkbox("Show Distance Warning", ref leashShowDistanceWarning))
            {
                Plugin.ControlSettings.LeashShowDistanceWarning = leashShowDistanceWarning;
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

            if (ImGui.Checkbox("Show Grace Warning", ref leashShowGraceWarning))
            {
                Plugin.ControlSettings.LeashShowGraceWarning = leashShowGraceWarning;
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
            DrawLeashOptionsBase(ref changed);
            if (changed) Plugin.ControlSettings.Save();
        }
        protected void DrawLeashOptionsBase(ref bool changed)
        {
            if (leashOptions == null)
            {
                leashOptions = new("LeashOptions", Plugin, Plugin.ControlSettings.LeashShockOptions);
                leashOptions.HasCooldown = false;
                leashOptions.HasWarning = false;
                leashOptions.SetNames("Mode", "Max Duration", "Max Intensity");
            }
            leashOptions.Draw();

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
            if (interval < durationT) interval = durationT;
            if (ImGui.InputFloat("##ShockInterval", ref interval))
            {
                if (interval > 60) interval = 60;

                Plugin.ControlSettings.LeashTriggerInterval = interval;
                changed = true;
            }
            ImGui.EndGroup();

            if (interval == durationT)
            {
                ImGui.TextColored(UIValues.ColorDescription, "You cannot set the interval lower than the maximum duration." +
                    "\n(Because setting it to the same means constant shocks already)");
            }

        }
    }
}
