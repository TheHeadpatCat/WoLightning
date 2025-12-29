using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using WoLightning.Clients.Webserver.Operations.Account;
using WoLightning.Util.Types;

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

        private Vector4 Red = new(1, 0.2f, 0.2f, 1);

        public ControlWindow(Plugin plugin) : base("Warrior of Lightning - Control Settings")
        {
            Flags = ImGuiWindowFlags.AlwaysUseWindowPadding;


            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(500, 650),
                MaximumSize = new Vector2(600, 800)
            };
            Plugin = plugin;
        }

        public void Dispose()
        {
            if(this.IsOpen) this.Toggle();
        }

        public override void Draw()
        {
            if (!hasAcceptedRisks)
            {
                if (Plugin.ControlSettings.Controller != null) hasAcceptedRisks = true;
                drawRisks();
                return;
            }

            if(Plugin.ControlSettings.Controller == null)
            {
                drawSelectController();
                return;
            }

            ImGui.Text("Your current Controller:");
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
            ImGui.Separator();
            ImGui.Spacing();

            string swapCommand = Plugin.ControlSettings.SwappingCommand;
            if (swapCommand == null || swapCommand.Length < 3) ImGui.BeginDisabled();

            bool swapAllowed = Plugin.ControlSettings.SwappingAllowed;
            if(ImGui.Checkbox("Allow Preset Swapping", ref swapAllowed))
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

            bool lockAllowed = Plugin.ControlSettings.LockingAllowed;
            ushort lockEmote = Plugin.ControlSettings.LockingEmote;
            ushort unlockEmote = Plugin.ControlSettings.UnlockingEmote;
            
            if(lockEmote == 0 || unlockEmote == 0) ImGui.BeginDisabled();

            if (ImGui.Checkbox("Allow Preset Locking", ref lockAllowed))
            {
                Plugin.ControlSettings.LockingAllowed = lockAllowed;
                Plugin.ControlSettings.LockingEmote = lockEmote;
                Plugin.ControlSettings.UnlockingEmote = unlockEmote;
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
            if (ImGui.Button("Set to Controllers last used Emote"))
            {
                //lastEmote = Plugin.GameEmotes.getEmote(Plugin.ControlSettings.LastEmoteFromController);
                if (Plugin.ControlSettings.LastEmoteFromController == 0) return;
                lockEmote = Plugin.ControlSettings.LastEmoteFromController;
                lockingEmoteName = Plugin.ControlSettings.LastEmoteFromControllerName;
            }

            ImGui.Text("Emote to Unlock Presets:");
            ImGui.SetNextItemWidth(250);
            ImGui.BeginDisabled();
            ImGui.InputTextWithHint("##UnlockingEmoteName", Plugin.ControlSettings.LastEmoteFromControllerName, ref unlockingEmoteName, 64, ImGuiInputTextFlags.ReadOnly);
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button("Set to Controllers last used Emote"))
            {
                //lastEmote = Plugin.GameEmotes.getEmote(Plugin.ControlSettings.LastEmoteFromController);
                if (Plugin.ControlSettings.LastEmoteFromController == 0) return;
                unlockEmote = Plugin.ControlSettings.LastEmoteFromController;
                unlockingEmoteName = Plugin.ControlSettings.LastEmoteFromControllerName;
            }

            if (lockAllowed) ImGui.EndDisabled();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            bool leashAllowed = Plugin.ControlSettings.LeashAllowed;
            int leashDistance = Plugin.ControlSettings.LeashDistance;
            float leashGraceTime = Plugin.ControlSettings.LeashGraceTime;
            ushort leashEmote = Plugin.ControlSettings.LeashEmote;
            ushort leashDistanceEmote = Plugin.ControlSettings.LeashDistanceEmote;

            if (leashEmote == 0 || leashDistanceEmote == 0) ImGui.BeginDisabled();

            if (ImGui.Checkbox("Allow Leashing", ref lockAllowed))
            {
                Plugin.ControlSettings.LeashAllowed = leashAllowed;
                Plugin.ControlSettings.LeashDistance = leashDistance;
                Plugin.ControlSettings.LeashGraceTime = leashGraceTime;
                Plugin.ControlSettings.LeashEmote = leashEmote;
                Plugin.ControlSettings.LeashDistanceEmote = leashDistanceEmote;
                Plugin.ControlSettings.Save();
            }

            if (leashEmote == 0 || leashDistanceEmote == 0) ImGui.EndDisabled();

            ImGui.SameLine();
            ImGui.TextDisabled(" (?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Allows the Controller to leash you by using a Emote on you." +
                    "\nYou will have to stay within a certain radius of them, or get shocked after some time." +
                    "\nA second emote is used to adjust the radius to whatever current distance you have." +
                    "\nAfter leaving the radius and grace period, you will first receive 2 warning vibrations, then increasing shocks." +
                    "\nIt is currently not possible to adjust the shock settings for this... sorry!" +
                    "\nAlso, never forget that you can use /red to stop receiving any more shocks!");
            }

            ImGui.Text("Maximum Distance from Controller: (yalms)");
            ImGui.SetNextItemWidth(75);
            if (ImGui.InputInt("##leashDistanceInput", ref leashDistance))
            {
                Plugin.ControlSettings.LeashDistance = leashDistance;
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(140);
            if (ImGui.Button("Set to current Distance"))
            {
                var controller = Plugin.ControlSettings.Controller.FindInObjectTable();
                leashDistance = controller.YalmDistanceX + controller.YalmDistanceZ;
                Plugin.ControlSettings.LeashDistance = leashDistance;
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
        }
    }
}
