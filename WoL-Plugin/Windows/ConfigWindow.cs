using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using WoLightning.Configurations;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Game.Rules;
using WoLightning.WoL_Plugin.Util;




namespace WoLightning.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration? Configuration;
    private Plugin Plugin;

    private Preset ActivePreset;
    private int ActivePresetIndex = -1;

    private Vector2 center = ImGui.GetMainViewport().GetCenter();

    private bool isModalAddPresetOpen = false;
    private bool isModalDeletePresetOpen = false;

    private string ModalAddPresetInputName = string.Empty;

    private Player? SelectedPlayer;
    private String SelectedPlayerName = "None";

    private List<RuleBase> RulesGeneral = new();
    private List<RuleBase> RulesMaster = new();
    private List<RuleBase> RulesMisc = new();
    private List<RuleBase> RulesPVE = new();
    private List<RuleBase> RulesPVP = new();
    private List<RuleBase> RulesSocial = new();

    public ConfigWindow(Plugin plugin) : base($"Warrior of Lightning Configuration - v{Plugin.CurrentVersion}##configmain")
    {
        Flags = ImGuiWindowFlags.AlwaysUseWindowPadding;


        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(580, 650),
            MaximumSize = new Vector2(2000, 2000)
        };
        Plugin = plugin;

    }

    private void onPresetChanged(Preset preset, int index)
    {
        Logger.Log(2, "onPresetChaged() - " + preset.Name + " " + index);
        ActivePreset = preset;
        ActivePresetIndex = index;

        RulesGeneral.Clear();
        RulesMaster.Clear();
        RulesMisc.Clear();
        RulesPVE.Clear();
        RulesPVP.Clear();
        RulesSocial.Clear();
        foreach (RuleBase Rule in ActivePreset.Rules)
        {
            switch (Rule.Category)
            {
                case RuleCategory.General: RulesGeneral.Add(Rule); break;
                case RuleCategory.Master: RulesMaster.Add(Rule); break;
                case RuleCategory.Misc: RulesMisc.Add(Rule); break;
                case RuleCategory.PVE: RulesPVE.Add(Rule); break;
                case RuleCategory.PVP: RulesPVP.Add(Rule); break;
                case RuleCategory.Social: RulesSocial.Add(Rule); break;
            }
        }
    }

    public void Dispose()
    {
        if (this.IsOpen) this.Toggle();
        if (Configuration != null)
        {
            Configuration.Save();
            Configuration.PresetChanged -= onPresetChanged;
        }
    }

    public void SetConfiguration(Configuration? conf)
    {
        Logger.Log(2, "SetConfiguration() is called");
        if (conf == null)
        {
            Configuration.PresetChanged -= onPresetChanged;
            Configuration.Save();
            Configuration = conf;
            ActivePresetIndex = -1;
            return;
        }

        if (Configuration != null) Configuration.PresetChanged -= onPresetChanged;
        Configuration = conf;
        Configuration?.Save();
        ActivePreset = Configuration.ActivePreset;
        ActivePresetIndex = Configuration.ActivePresetIndex;
        Configuration.PresetChanged += onPresetChanged;

        RulesGeneral.Clear();
        RulesMaster.Clear();
        RulesMisc.Clear();
        RulesPVE.Clear();
        RulesPVP.Clear();
        RulesSocial.Clear();
        foreach (RuleBase Rule in ActivePreset.Rules)
        {
            switch (Rule.Category)
            {
                case RuleCategory.General: RulesGeneral.Add(Rule); break;
                case RuleCategory.Master: RulesMaster.Add(Rule); break;
                case RuleCategory.Misc: RulesMisc.Add(Rule); break;
                case RuleCategory.PVE: RulesPVE.Add(Rule); break;
                case RuleCategory.PVP: RulesPVP.Add(Rule); break;
                case RuleCategory.Social: RulesSocial.Add(Rule); break;
            }
        }

    }

    public override void PreDraw()
    {

    }

    public override void Draw()
    {
        if (Plugin == null || Plugin.Authentification == null || Configuration == null || ActivePresetIndex == -1)
        {
            ImGui.Text("Configuration is not loaded.\nPlease login with a Character.");
            return;
        }

        if (Plugin.Configuration.IsLockedByController)
        {
            ImGui.Text($"{Plugin.ControlSettings.Controller.Name} doesn't allow you to edit Presets.");
        }

        DrawHeader();

        if (Configuration.Version < 1) return; //safety check for old configs

        if (ImGui.BeginTabBar("Tab Bar##tabbarmain", ImGuiTabBarFlags.None))
        {
            DrawGeneralTab();
            DrawSocialTab();
            DrawPVETab();
            //DrawPVPTab();
            DrawMiscTab();
            DrawPermissionsTab();

            if (ImGui.BeginTabItem("Word Lists"))
            {
                if (ImGui.BeginTabBar("Tab Bar##tabbarwords"))
                {
                    if (ImGui.BeginTabItem("Banned Words"))
                    {
                        if (Configuration.IsLockedByController) ImGui.BeginDisabled();
                        ActivePreset.SayWord.DrawAdvancedOptions();
                        if (Configuration.IsLockedByController) ImGui.EndDisabled();
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Enforced Words"))
                    {
                        if (Configuration.IsLockedByController) ImGui.BeginDisabled();
                        ActivePreset.DontSayWord.DrawAdvancedOptions();
                        if (Configuration.IsLockedByController) ImGui.EndDisabled();
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Trigger Words"))
                    {
                        if (Configuration.IsLockedByController) ImGui.BeginDisabled();
                        ActivePreset.HearWord.DrawAdvancedOptions();
                        if (Configuration.IsLockedByController) ImGui.EndDisabled();
                        ImGui.EndTabItem();
                    }
                }
                ImGui.EndTabBar();
            }


            if (Configuration.DebugLevel == DebugLevel.Dev) DrawDebugTab();

            ImGui.EndTabBar();
        }


        ImGui.SetNextWindowPos(new Vector2(ImGui.GetWindowPos().X, ImGui.GetWindowPos().Y + ImGui.GetWindowHeight()));
        ImGui.SetNextWindowSize(new Vector2(ImGui.GetWindowWidth(), 35));
        ImGui.Begin("##DiscordFooter", ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoSavedSettings);
        ImGui.Text("Found a issue or got a feature request?");
        ImGui.SameLine();
        if (ImGui.Button("Join the Discord", new Vector2(130, 25)))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://discord.gg/hMyWcZyhRa",
                    UseShellExecute = true
                });
            }
            catch (Exception e) { Logger.Log(1, e); }
        }
        ImGui.End();

    }
    private void DrawHeader()
    {
        DrawPresetHeader();
    }

    private void DrawPresetHeader()
    {

        if (Configuration.IsLockedByController) ImGui.BeginDisabled();

        DrawModalAddPreset();
        DrawModalDeletePreset();

        ImGui.PushItemWidth(ImGui.GetWindowSize().X - 90);

        // Preset Selector
        ActivePresetIndex = Configuration.ActivePresetIndex;
        if (ImGui.Combo("", ref ActivePresetIndex, [.. Configuration.PresetNames], Configuration.Presets.Count))
        { Configuration.loadPreset(Configuration.PresetNames[ActivePresetIndex]); }
        // Preset Modal Openers - Add & Delete
        ImGui.SameLine();
        if (ImGui.SmallButton("+"))
        {
            ModalAddPresetInputName = "";
            isModalAddPresetOpen = true;
            ImGui.OpenPopup("Add Preset##addPreMod");
        }
        ImGui.SameLine();
        if (ImGui.SmallButton("X"))
        {
            isModalDeletePresetOpen = true;
            ImGui.OpenPopup("Delete Preset##delPreMod");
        }

        if (Configuration.IsLockedByController) ImGui.EndDisabled();

    }


    private void DrawGeneralTab()
    {
        if (ImGui.BeginTabItem("General"))
        {
            if (Configuration.IsLockedByController) ImGui.BeginDisabled();

            bool showTriggerNotifs = Configuration.ActivePreset.showTriggerNotifs;
            if (ImGui.Checkbox("Show Trigger Notifications", ref showTriggerNotifs))
            {
                Configuration.ActivePreset.showTriggerNotifs = showTriggerNotifs;
                Configuration.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled(" (?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Activating this will show little Notification whenever you trigger a Rule");
            }

            bool showCooldownNotifs = Configuration.ActivePreset.showCooldownNotifs;
            if (ImGui.Checkbox("Show Cooldown Notifications", ref showCooldownNotifs))
            {
                Configuration.ActivePreset.showCooldownNotifs = showCooldownNotifs;
                Configuration.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled(" (?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Activating this will show little Notification" +
                "\nthat will tell you how much time is left until that trigger can activate again.");
            }

            bool allowPVERulesInPVP = Configuration.ActivePreset.AllowRulesInPvP;
            if (ImGui.Checkbox("Have Rules active in PVP?", ref allowPVERulesInPVP))
            {
                Configuration.ActivePreset.AllowRulesInPvP = allowPVERulesInPVP;
                Configuration.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled(" (?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("This does not include PVP specific Rules. Those will always be active.");
            }

            ImGui.SetNextItemWidth(200);
            int ShownShockersIndex = (int)Configuration.ShownShockers;
            if (ImGui.Combo("Shown Shockers", ref ShownShockersIndex, ["All", "Personal Only", "Shared Only", "None...?"], 4))
            {
                Configuration.ShownShockers = (ShownShockers)ShownShockersIndex;
                Configuration.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled(" (?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Allows you to select which Shockers show up on clicking the \"Assign Shockers\" button.");
            }

            if (Configuration.IsLockedByController) ImGui.EndDisabled();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Text("These are Settings that you shouldnt change, unless specifically asked to do so.");
            ImGui.Spacing();

            ImGui.SetNextItemWidth(130);
            int debugLevelIndex = (int)Configuration.DebugLevel;
            if (ImGui.Combo("Debug Level", ref debugLevelIndex, ["None", "Info", "Debug", "Verbose", "Dev"], 5))
            {
                Configuration.DebugLevel = (DebugLevel)debugLevelIndex;
                Configuration.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled(" (?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("This sets at which level the Plugin logs things.\nIt's a good idea not to touch this.\nIf you are having performance issues, you can set it to \"None\", but your Log.txt file will be useless for finding Bugs.\nThis setting will reset after every update.\nSetting it to Dev will expose alot of personal data inside your Log.txt file.");
            }

            if (Configuration.DebugLevel == DebugLevel.Dev)
            {
                ImGui.TextColored(new Vector4(255, 0, 0, 255), "Please do not use the DEV level, unless specifically asked to do so.\nDoing so exposes personal information in your Log.txt file\nas well as enabling developer options that can break the plugin (or are just annoying).");
                if (ImGui.Button("Oh okay! Turn it off!"))
                {
                    Configuration.DebugLevel = DebugLevel.Verbose;
                    Configuration.Save();
                }
            }

            if (Configuration.DebugLevel != DebugLevel.Dev && Configuration.DebugLevel != DebugLevel.Verbose)
            {
                ImGui.Text("Setting your Log Level to anything but \"Verbose\" makes your Log.txt useless for bugfixing.");
                if (ImGui.Button("Oh okay! Set it back to Verbose!"))
                {
                    Configuration.DebugLevel = DebugLevel.Verbose;
                    Configuration.Save();
                }
            }

            ImGui.EndTabItem();
        }
    }
    private void DrawSocialTab()
    {
        if (ImGui.BeginTabItem("Social"))
        {
            foreach (var Rule in RulesSocial)
            {
                Rule.Draw();
            }
            ImGui.EndTabItem();
        }
    }

    private void DrawPVETab()
    {
        if (ImGui.BeginTabItem("Combat"))
        {
            foreach (var Rule in RulesPVE)
            {
                Rule.Draw();
            }

            ImGui.EndTabItem();
        }
    }

    private void DrawPVPTab()
    {
        if (ImGui.BeginTabItem("PVP"))
        {
            foreach (var Rule in RulesPVP)
            {
                Rule.Draw();
            }

            ImGui.EndTabItem();
        }
    }

    private void DrawMiscTab()
    {
        if (ImGui.BeginTabItem("Misc"))
        {
            foreach (var Rule in RulesMisc)
            {
                Rule.Draw();
            }

            ImGui.EndTabItem();
        }
    }

    private void DrawPermissionsTab()
    {
        if (ImGui.BeginTabItem("Permissions"))
        {
            if (Configuration.IsLockedByController) ImGui.BeginDisabled();
            bool isWhitelistEnabled = Configuration.ActivePreset.isWhitelistEnabled;
            if (ImGui.Checkbox("Activate Whitelist", ref isWhitelistEnabled))
            {
                Configuration.ActivePreset.isWhitelistEnabled = isWhitelistEnabled;
                Configuration.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled(" (?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("If the Whitelist is enabled, only Whitelisted players can interact with this Plugin." +
                "\nBlacklisted Players can never interact with it, regardless of this Setting.");
            }

            ImGui.Spacing();

            var Whitelist = Configuration.ActivePreset.Whitelist;
            var Blacklist = Configuration.ActivePreset.Blacklist;

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

            // Todo: Allow manual input.

            ImGui.Text("Select a Player ingame, to add them to a List.\nClicking on a Player in the list removes them.");
            ImGui.BeginDisabled();
            ImGui.InputText("##SelectedPlayer", ref SelectedPlayerName, 64, ImGuiInputTextFlags.ReadOnly);
            ImGui.EndDisabled();

            if (ImGui.Button("Whitelist Player##AddWhitelistButton", new Vector2(120, 25)))
            {
                if (SelectedPlayerName == "None") return;
                if (SelectedPlayerName == null) return;
                if (SelectedPlayer == null) SelectedPlayer = new Player(SelectedPlayerName);
                if (Whitelist.Contains(SelectedPlayer!)) Whitelist.Remove(SelectedPlayer!);
                if (Blacklist.Contains(SelectedPlayer!)) Blacklist.Remove(SelectedPlayer!);
                Whitelist.Add(SelectedPlayer!);
                Configuration.ActivePreset.Whitelist = Whitelist;
                Configuration.Save();
                SelectedPlayerName = new String("None");
            }
            ImGui.SameLine();
            if (ImGui.Button("Blacklist Player##AddBlacklistButton", new Vector2(120, 25)))
            {
                if (SelectedPlayerName == "None") return;
                if (SelectedPlayerName == null) return;
                if (SelectedPlayer == null) SelectedPlayer = new Player(SelectedPlayerName);
                if (Blacklist.Contains(SelectedPlayer!)) Blacklist.Remove(SelectedPlayer!);
                if (Whitelist.Contains(SelectedPlayer!)) Whitelist.Remove(SelectedPlayer!);
                Blacklist.Add(SelectedPlayer!);
                Configuration.ActivePreset.Blacklist = Blacklist;
                Configuration.Save();
                SelectedPlayerName = new String("None");
            }

            int removeIndex = -1;
            if (ImGui.BeginListBox("Whitelisted\nPlayers##WhitelistBox"))
            {
                int index = 0;
                foreach (var Player in Whitelist)
                {
                    if (ImGui.Selectable($"{Player.Name}@{Player.getWorldName()}"))
                    {
                        SelectedPlayerName = $"{Player.Name}@{Player.getWorldName()}";
                        removeIndex = index;
                    }
                    index++;
                }
                ImGui.EndListBox();
            }
            if (removeIndex >= 0)
            {
                Whitelist.RemoveAt(removeIndex);
                Configuration.ActivePreset.Whitelist = Whitelist;
                Configuration.Save();
                removeIndex = -1;
            }

            if (ImGui.BeginListBox("Blacklisted\nPlayers##BlacklistBox"))
            {
                int index = 0;
                foreach (var Player in Blacklist)
                {
                    if (ImGui.Selectable($"{Player.Name}@{Player.getWorldName()}"))
                    {
                        SelectedPlayerName = $"{Player.Name}@{Player.getWorldName()}";
                        removeIndex = index;
                    }
                    index++;
                }
                ImGui.EndListBox();
            }
            if (removeIndex >= 0)
            {
                Blacklist.RemoveAt(removeIndex);
                Configuration.ActivePreset.Blacklist = Blacklist;
                Configuration.Save();
            }

            ImGui.TextWrapped("These settings are Preset-Dependant. Swapping Presets will also swap these lists.");
            if (Configuration.IsLockedByController) ImGui.EndDisabled();

            ImGui.EndTabItem();
        }
    }

    #region Modals

    private void DrawModalAddPreset()
    {
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(340, 125));

        if (ImGui.BeginPopupModal("Add Preset##addPreMod", ref isModalAddPresetOpen, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.Popup | ImGuiWindowFlags.NoTitleBar))
        {
            ImGui.Text("Please name your new preset.");
            ImGui.PushItemWidth(ImGui.GetWindowSize().X - 10);
            ImGui.InputText("##addInput", ref ModalAddPresetInputName, 32, ImGuiInputTextFlags.CharsNoBlank);

            ImGui.PushItemWidth(ImGui.GetWindowSize().X / 2);
            if (ImGui.Button("Add##addPre", new Vector2(ImGui.GetWindowSize().X / 2, 25)))
            {
                if (ModalAddPresetInputName.Length == 0)
                {
                    ModalAddPresetInputName = Configuration.ActivePreset.Name + " Duplicate";
                }

                while (Configuration.PresetNames.Contains(ModalAddPresetInputName)) { ModalAddPresetInputName += "+"; }

                Preset tPreset = new Preset(ModalAddPresetInputName, Plugin.LocalPlayer.getFullName());
                Configuration.Presets.Add(tPreset);
                Configuration.Save();
                Configuration.loadPreset(ModalAddPresetInputName);


                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel##canPre", new Vector2(ImGui.GetWindowSize().X / 2 - 10, 25))) ImGui.CloseCurrentPopup();

            ImGui.PushItemWidth(ImGui.GetWindowSize().X / 2);
            if (ImGui.Button("Duplicate Current Preset##addPreDuplicate", new Vector2(ImGui.GetWindowSize().X / 2, 25)))
            {
                if (ModalAddPresetInputName.Length == 0)
                {
                    ModalAddPresetInputName = Configuration.ActivePreset.Name + " Duplicate";
                }

                while (Configuration.PresetNames.Contains(ModalAddPresetInputName)) { ModalAddPresetInputName += "+"; }

                String duplicate = Newtonsoft.Json.JsonConvert.SerializeObject(Configuration.ActivePreset);
                Preset tPreset = Newtonsoft.Json.JsonConvert.DeserializeObject<Preset>(duplicate)!;
                tPreset.Name = ModalAddPresetInputName;
                Configuration.Presets.Add(tPreset);
                Configuration.Save();
                Configuration.loadPreset(ModalAddPresetInputName);


                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }
    private void DrawModalDeletePreset()
    {
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(250, 85));

        if (ImGui.BeginPopupModal("Delete Preset##delPreMod", ref isModalDeletePresetOpen, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.Popup | ImGuiWindowFlags.NoTitleBar))
        {
            ImGui.TextWrapped("Are you sure you want to delete this preset?");
            ImGui.PushItemWidth(ImGui.GetWindowSize().X - 10);
            if (ImGui.Button("Confirm##conRem", new Vector2(ImGui.GetWindowSize().X / 2, 25)))
            {

                Configuration.deletePreset(Configuration.ActivePreset);
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel##canRem", new Vector2(ImGui.GetWindowSize().X / 2 - 10, 25))) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
    }

    #endregion


    private void DrawDebugTab()
    {
        if (ImGui.BeginTabItem("Debug"))
        {

            ImGui.SetWindowFontScale(2f);
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "\nThese are debug settings.\nPlease don't touch them.\n\n");
            ImGui.SetWindowFontScale(1.33f);
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "Pressing them might softlock the plugin, break your config\nBan you from the Webserver permanently, or worst of all...\nUnpat your dog/cat.");
            ImGui.SetWindowFontScale(0.77f);
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 0.8f), "(or fish)");

            ImGui.SetWindowFontScale(1f);
            ImGui.Spacing();



        }
    }


    /*
    private void DrawBadWordList()
    {
        if (ImGui.BeginTabItem("Bad Word List"))
        {
            ImGui.Text("If you say any of these words, you'll trigger its settings!" +
                "\nPunctuation doesnt matter!");
            if (Plugin.Authentification.isDisallowed) ImGui.BeginDisabled();
            var SavedWordSettings = Configuration.ActivePreset.SayBadWord.CustomData;

            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 15);
            if (ImGui.InputTextWithHint("##BadWordInput", "Click on a entry to edit it.", ref BadWordListInput, 48))
            {
                if (BadcurrentWordIndex != -1) // Get rid of the old settings, otherwise we build connections between two items
                {
                    int[] copyArray = new int[3];
                    BadWordListSetting.CopyTo(copyArray, 0);
                    BadWordListSetting = copyArray;
                }
                BadcurrentWordIndex = -1;
            }

            //clamp
            if (BadWordListSetting[0] < 0 || BadWordListSetting[0] > 3) BadWordListSetting[0] = 0;

            if (BadWordListSetting[1] <= 0) BadWordListSetting[1] = 1;
            if (BadWordListSetting[1] > 100) BadWordListSetting[1] = 100;

            if (BadWordListSetting[2] <= 0) BadWordListSetting[2] = 100;
            if (BadWordListSetting[2] > 10 && BadWordListSetting[2] != 100 && BadWordListSetting[2] != 300) BadWordListSetting[2] = 10;

            ImGui.Separator();

            ImGui.BeginGroup();
            ImGui.Spacing();
            ImGui.Text("    Mode");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 3 - 50);
            ImGui.Combo("##Word", ref BadWordListSetting[0], ["Shock", "Vibrate", "Beep"], 3);
            ImGui.EndGroup();

            ImGui.SameLine();

            ImGui.BeginGroup();
            ImGui.Spacing();
            ImGui.Text("    Duration");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 4.5f);
            int DurationIndex = durationArray.IndexOf(BadWordListSetting[2]);
            if (ImGui.Combo("##WordDur", ref DurationIndex, ["0.1s", "0.3s", "1s", "2s", "3s", "4s", "5s", "6s", "7s", "8s", "9s", "10s"], 12))
            {
                BadWordListSetting[2] = durationArray[DurationIndex];
            }
            ImGui.EndGroup();

            ImGui.SameLine();

            ImGui.BeginGroup();
            ImGui.Spacing();
            ImGui.Text("    Intensity");
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 2f);
            ImGui.SliderInt("##BadWordInt", ref BadWordListSetting[1], 1, 100);
            ImGui.EndGroup();

            ImGui.Separator();

            ImGui.Spacing();

            if (ImGui.Button("Add Word##BadWordAdd", new Vector2(ImGui.GetWindowWidth() / 2 - 8, 25)))
            {
                if (SavedWordSettings.ContainsKey(BadWordListInput)) SavedWordSettings.Remove(BadWordListInput);
                SavedWordSettings.Add(BadWordListInput, BadWordListSetting);
                Configuration.ActivePreset.SayBadWord.CustomData = SavedWordSettings;
                Configuration.Save();
                BadcurrentWordIndex = -1;
                BadWordListInput = new String("");
                BadWordListSetting = new int[3];
                BadselectedWord = new String("");
            }

            ImGui.SameLine();

            if (BadcurrentWordIndex == -1) ImGui.BeginDisabled();
            if (ImGui.Button("Remove Word##BadWordRemove", new Vector2(ImGui.GetWindowWidth() / 2 - 8, 25)))
            {
                if (SavedWordSettings.ContainsKey(BadWordListInput)) SavedWordSettings.Remove(BadWordListInput);
                Configuration.ActivePreset.SayBadWord.CustomData = SavedWordSettings;
                Configuration.Save();
                BadcurrentWordIndex = -1;
                BadWordListInput = new String("");
                BadWordListSetting = new int[3];
                BadselectedWord = new String("");
            }
            if (BadcurrentWordIndex == -1) ImGui.EndDisabled();

            ImGui.Spacing();

            if (Plugin.Authentification.isDisallowed) ImGui.EndDisabled();

            if (ImGui.BeginListBox("##BadWordListBox", new Vector2(ImGui.GetWindowWidth() - 15, 340)))
            {
                int index = 0;
                foreach (var (word, settings) in SavedWordSettings)
                {
                    string mode = new String("");
                    string durS = new String("");
                    bool is_Selected = (BadcurrentWordIndex == index);
                    switch (settings[0]) { case 0: mode = "Shock"; break; case 1: mode = "Vibrate"; break; case 2: mode = "Beep"; break; };
                    switch (settings[2]) { case 100: durS = "0.1s"; break; case 300: durS = "0.3s"; break; default: durS = $"{settings[2]}s"; break; }
                    if (ImGui.Selectable($" Word: {word}  | Mode: {mode} | Intensity: {settings[1]} | Duration: {durS}", ref is_Selected))
                    {
                        BadselectedWord = word;
                        BadcurrentWordIndex = index;
                        BadWordListInput = word;
                        BadWordListSetting = settings;
                    }
                    index++;
                }
                ImGui.EndListBox();
            }


            ImGui.EndTabItem();
        }
    }
    private void DrawEnforcedWordList()
    {
        if (ImGui.BeginTabItem("Enforced Word List"))
        {
            if (Plugin.Authentification.isDisallowed) ImGui.BeginDisabled();

            ImGui.Text("You have to say atleast one of the words from the list below, otherwise these settings will trigger." +
                "\nCorrect punctuation is needed as well.");
            createPickerBox(Configuration.ActivePreset.DontSayWord, true);

            var SavedWordSettings = Configuration.ActivePreset.DontSayWord.CustomData;

            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 15);
            if (ImGui.InputTextWithHint("##DontSayWordInput", "Click on a entry to edit it.", ref DontSayWordListInput, 48))
            {
                if (BadcurrentWordIndex != -1) // Get rid of the old settings, otherwise we build connections between two items
                {
                    int[] copyArray = new int[3];
                    BadWordListSetting.CopyTo(copyArray, 0);
                    BadWordListSetting = copyArray;
                }
                BadcurrentWordIndex = -1;
            }



            ImGui.Spacing();

            if (ImGui.Button("Add Word##DontSayWordAdd", new Vector2(ImGui.GetWindowWidth() / 2 - 8, 25)))
            {
                if (SavedWordSettings.ContainsKey(DontSayWordListInput)) SavedWordSettings.Remove(DontSayWordListInput);
                SavedWordSettings.Add(DontSayWordListInput, DontSayWordListSetting);
                Configuration.ActivePreset.DontSayWord.CustomData = SavedWordSettings;
                Configuration.Save();
                DontSaycurrentWordIndex = -1;
                DontSayWordListInput = new String("");
                DontSayWordListSetting = new int[3];
                DontSayselectedWord = new String("");
            }

            ImGui.SameLine();

            if (DontSaycurrentWordIndex == -1) ImGui.BeginDisabled();
            if (ImGui.Button("Remove Word##DontSayWordRemove", new Vector2(ImGui.GetWindowWidth() / 2 - 8, 25)))
            {
                if (SavedWordSettings.ContainsKey(DontSayWordListInput)) SavedWordSettings.Remove(DontSayWordListInput);
                Configuration.ActivePreset.DontSayWord.CustomData = SavedWordSettings;
                Configuration.Save();
                DontSaycurrentWordIndex = -1;
                DontSayWordListInput = new String("");
                DontSayWordListSetting = new int[3];
                DontSayselectedWord = new String("");
            }
            if (DontSaycurrentWordIndex == -1) ImGui.EndDisabled();

            ImGui.Spacing();

            if (Plugin.Authentification.isDisallowed) ImGui.EndDisabled();

            if (ImGui.BeginListBox("##DontSayWordListBox", new Vector2(ImGui.GetWindowWidth() - 15, 320)))
            {
                int index = 0;
                foreach (var (word, settings) in SavedWordSettings)
                {
                    var modeInt = settings[0];
                    var mode = new String("");
                    bool is_Selected = (DontSaycurrentWordIndex == index);
                    if (ImGui.Selectable($" {word} ", ref is_Selected))
                    {
                        DontSayselectedWord = word;
                        DontSaycurrentWordIndex = index;
                        DontSayWordListInput = word;
                        DontSayWordListSetting = settings;
                    }
                    index++;
                }
                ImGui.EndListBox();
            }


            ImGui.EndTabItem();
        }
    }

    private void DrawDefaultTriggerTab()
    {
        if (ImGui.BeginTabItem("Default Triggers"))
        {
            ImGui.TextWrapped("These Triggers are premade to react to certain ingame events!\nThey will always be prioritized over custom triggers, if passthrough is not enabled.");
            if (Plugin.Authentification.isDisallowed) ImGui.BeginDisabled();
            DrawSocial();
            DrawCombat();
            if (Plugin.Authentification.isDisallowed) ImGui.EndDisabled();
            ImGui.EndTabItem();
        }
    }

    private void DrawCustomTriggerTab()
    {

        if (ImGui.BeginTabItem("Custom Triggers"))
        {
            DrawCustomChats();
            DrawCustomTable();

            ImGui.EndTabItem();
        }
    }

    private void DrawPermissionsTab()
    {
        if (ImGui.BeginTabItem("Permissions"))
        {

            if (Plugin.Authentification.isDisallowed) ImGui.BeginDisabled();

            bool isWhitelistEnabled = Configuration.ActivePreset.isWhitelistEnabled;
            if (ImGui.Checkbox("Activate Whitelist", ref isWhitelistEnabled))
            {
                Configuration.ActivePreset.isWhitelistEnabled = isWhitelistEnabled;
                Configuration.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled(" (?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("If the Whitelist is enabled, only Whitelisted players can interact with this Plugin." +
                "\nBlacklisted Players can never interact with it, regardless of this Setting.");
            }

            ImGui.Spacing();

            var PermissionList = Configuration.PermissionList;

            IGameObject? st = Plugin.TargetManager.Target;
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

            ImGui.Text("Select a Player ingame, or from the List.");
            ImGui.BeginDisabled();
            ImGui.InputText("##SelectedPlayer", ref SelectedPlayerName, 512, ImGuiInputTextFlags.ReadOnly);
            ImGui.EndDisabled();


            ImGui.ListBox("##PermissionLevel", ref PermissionListSetting, ["Blacklisted", "Whitelisted"], 2);


            if (ImGui.Button("Add Player", new Vector2(75, 25)))
            {
                if (SelectedPlayerName == "None") return;
                if (PermissionList.ContainsKey(SelectedPlayerName)) PermissionList.Remove(SelectedPlayerName);
                PermissionList.Add(SelectedPlayerName, PermissionListSetting);
                Configuration.PermissionList = PermissionList;
                Configuration.Save();
                currentPermissionIndex = -1;
                SelectedPlayerName = new String("None");
            }
            ImGui.SameLine();
            if (ImGui.Button("Remove Player", new Vector2(100, 25)))
            {
                if (SelectedPlayerName == "None") return;
                if (PermissionList.ContainsKey(SelectedPlayerName)) PermissionList.Remove(SelectedPlayerName);
                Configuration.PermissionList = PermissionList;
                Configuration.Save();
                currentPermissionIndex = -1;
                SelectedPlayerName = new String("None");
            }


            if (Plugin.Authentification.isDisallowed) ImGui.EndDisabled();
            if (ImGui.BeginListBox("##PlayerPermissions"))
            {
                int index = 0;
                foreach (var (name, permissionlevel) in PermissionList)
                {
                    bool is_Selected = (BadcurrentWordIndex == index);
                    var permissionleveltext = new String("");
                    switch (permissionlevel) { case 0: permissionleveltext = "Blacklisted"; break; case 1: permissionleveltext = "Whitelisted"; break; };
                    if (ImGui.Selectable($" Player: {name}   Permission: {permissionleveltext}", ref is_Selected))
                    {
                        SelectedPlayerName = name;
                        currentPermissionIndex = index;
                        PermissionListSetting = permissionlevel;
                    }
                    index++;
                }
                ImGui.EndListBox();
            }
            ImGui.EndTabItem();
        }
    }


    private void DrawDebugTab()
    {
        if (ImGui.BeginTabItem("Debug"))
        {

            ImGui.SetWindowFontScale(2f);
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "\nThese are debug settings.\nPlease don't touch them.\n\n");
            ImGui.SetWindowFontScale(1.33f);
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "Pressing them might softlock the plugin, break your config\nBan you from the Webserver permanently, or worst of all...\nUnpat your dog/cat.");
            ImGui.SetWindowFontScale(0.77f);
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 0.8f), "(or fish)");

            ImGui.SetWindowFontScale(1f);
            ImGui.Spacing();

            ImGui.InputInt("XIVChattype", ref debugFtype);
            //ImGui.InputInt("senderId", ref debugFtype);
            ImGui.InputText("Sender", ref debugFsender, 64);
            ImGui.InputText("Message", ref debugFmessage, 128);
            //ImGui.InputInt("XIVChattype", ref debugFtype);

            if (ImGui.Button("Send Fake Message", new Vector2(200, 60)))
            {
                XivChatType t = (XivChatType)debugFtype;
                SeString s = debugFsender.ToString();
                SeString m = debugFmessage.ToString();
                bool b = false;
                Logger.Log("Sending fake message:");
                Plugin.NetworkWatcher.HandleChatMessage(t, 0, ref s, ref m, ref b);
            }

            ImGui.ListBox("Operation", ref debugOpIndex, debugOpCodes, debugOpCodes.Length, 4);
            ImGui.InputText("OpData", ref debugOpData, 512);

            IGameObject st = Plugin.TargetManager.Target;
            if (st != null && st.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player)
            {
                IPlayerCharacter st1 = (IPlayerCharacter)st;
                if (debugPlayerTarget == null || debugPlayerTarget.Name != st1.Name.ToString())
                {
                    debugPlayerTarget = new Player(st1.Name.ToString(), (int)st1.HomeWorld.Value.RowId);
                }
            }

            string playerName = "None";
            if (debugPlayerTarget != null) playerName = debugPlayerTarget.Name;
            ImGui.InputText("##debugTargetPlayer", ref playerName, 512, ImGuiInputTextFlags.ReadOnly);

            ImGui.SameLine();
            if (ImGui.Button("X##debugRemovePlayer")) debugPlayerTarget = null;


            if (ImGui.Button("Test Operation", new Vector2(200, 60)))
            {
                /*Plugin.ClientWebserver.request(
                    Operation.getOperationCode(
                        debugOpCodes[debugOpIndex].Split(" - ")[1]),
                        debugOpData,
                        debugPlayerTarget);
            }

            if (ImGui.Button("Test OnRequest()"))
            {
                Plugin.Authentification.gotRequest = true;
                Plugin.ShowMasterUI();
            }

            ImGui.EndTabItem();
        }




    }
    #endregion


    private void DrawSocial()
    {
        if (!ImGui.CollapsingHeader("Social Triggers"))
        {
            return;
        }

        createEntry(Configuration.ActivePreset.GetPat, "Get /pet'd", "Triggers whenever a player does the /pet emote on you.");
        createEntry(Configuration.ActivePreset.GetSnapped, "Get /snap'd", "Triggers whenever a player does the /snap emote on you.");
        createEntry(Configuration.ActivePreset.SitOnFurniture, "Sit on Chairs", "Triggers whenever you /sit onto any kind of furniture.",
            "This Trigger will activate again after 5 seconds (after the shock is done) if you dont get off!" +
            "\nIf you do /groundsit onto it, it wont count though.", false, true);
        createEntry(Configuration.ActivePreset.LoseDeathRoll, "Lose DR", "Triggers whenever you lose a deathroll.",
            "Deathroll is when you use /random against another player to see who reaches 1 first.");


        createEntry(Configuration.ActivePreset.SayFirstPerson, "Mention yourself", "Triggers whenever you refer to yourself in the first person.",
            "This currently only works when writing in English." +
            "\nExamples: 'Me', 'I', 'Mine' and so on.");


        createEntry(Configuration.ActivePreset.SayBadWord, "Say a bad word", "Triggers whenever you say a bad word from a list.",
            "You can set these words yourself in the new Tab 'Bad Word List' once this is activated."
            , true, true);

        createEntry(Configuration.ActivePreset.DontSayWord, "Dont say a enforced word", "Triggers whenever you forget to say a enforced word from a list.",
            "You can set these words yourself in the new Tab 'Enforced Word List' once this is activated."
            , true, false);

    }
    private void DrawCombat()
    {
        if (!ImGui.CollapsingHeader("Combat Triggers"))
        {
            return;
        }

        createEntry(Configuration.ActivePreset.Wipe, "Party Wipe", "Triggers whenever all party members die.");

        createEntry(Configuration.ActivePreset.Die, "Death", "Triggers whenever you die.");

        createEntry(Configuration.ActivePreset.PartymemberDies, "Party member death", "Triggers whenever any party member dies, this includes you.",
            "This delivers proportional shocks, based on how many players are dead - up to your set limit.");

        createEntry(Configuration.ActivePreset.FailMechanic, "Fail a Mechanic", "Triggers whenever you fail a mechanic.",
            "This will trigger whenever you get a [Vulnerability Up] or [Damage Down] debuff.");

        createEntry(Configuration.ActivePreset.TakeDamage, "Take Damage", "Triggers whenever you take damage of any kind.",
            "This will go off a lot, so be warned!" +
            "\nIt does mean literally any damage, from mobs to DoTs and even fall damage!");

    }
    private void DrawCustomChats()
    {

        ImGui.TextWrapped("These options let you set letters, words or phrases that will trigger specified settings!\nIt's important to note that ANYONE can cause these to activate!\nExcept if you set it up to only react to your playername, of course.");

        if (!ImGui.CollapsingHeader("Custom Trigger Channels"))
        {
            return;
        }
        var i = 0;
        foreach (var e in ChatType.GetOrderedChannels())
        {
            // See if it is already enabled by default
            var enabled = Configuration.ActivePreset.Channels.Contains(e);
            // Create a new line after every 4 columns
            if (i != 0 && (i == 4 || i == 7 || i == 11 || i == 15 || i == 19 || i == 23))
            {
                ImGui.NewLine();
                //i = 0;
            }
            // Move to the next row if it is LS1 or CWLS1
            if (e is ChatType.ChatTypes.LS1 or ChatType.ChatTypes.CWL1)
                ImGui.Separator();

            if (ImGui.Checkbox($"{e}", ref enabled))
            {
                // See If the UIHelpers.Checkbox is clicked, If not, add to the list of enabled channels, otherwise, remove it.
                if (enabled) Configuration.ActivePreset.Channels.Add(e);
                else Configuration.ActivePreset.Channels.Remove(e);
                Configuration.Save();
            }

            ImGui.SameLine();
            i++;
        }
        ImGui.NewLine();
    }
    private void DrawCustomTable()
    {
        if (!ImGui.CollapsingHeader("Custom Triggers"))
        {
            return;
        }

        List<RegexTrigger> Triggers = Configuration.ActivePreset.SayCustomMessage;
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString(), ImGui.GetFrameHeight() * Vector2.One))
        {
            Configuration.ActivePreset.SayCustomMessage.Add(new("New Trigger", false));
            Configuration.Save();
        }
        ImGui.PopFont();

        int cnt = 7;
        if (ImGui.BeginTable("##Triggers", cnt, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable))
        {
            ImGui.TableSetupColumn(" ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.NoResize, ImGuiHelpers.GlobalScale * 75);
            ImGui.TableSetupColumn("Regex", ImGuiTableColumnFlags.NoResize, ImGuiHelpers.GlobalScale * 170);
            ImGui.TableSetupColumn("Mode", ImGuiTableColumnFlags.NoResize, ImGuiHelpers.GlobalScale * 90);
            ImGui.TableSetupColumn("Duration", ImGuiTableColumnFlags.None, ImGuiHelpers.GlobalScale * 40);
            ImGui.TableSetupColumn("Intensity", ImGuiTableColumnFlags.None, ImGuiHelpers.GlobalScale * 40);
            ImGui.TableSetupColumn(" ", ImGuiTableColumnFlags.NoResize | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoSort);
            ImGui.TableHeadersRow();

            for (int i = 0; i < Triggers.Count; i++)
            {
                var trigger = Triggers[i];

                ImGui.PushID(trigger.GUID.ToString());

                createShockerSelector(trigger);
                bool isEnabled = trigger.IsEnabled();
                ImGui.TableNextColumn();
                if (ImGui.Checkbox("##enabled", ref isEnabled))
                {
                    ImGui.OpenPopup($"Select Shockers##selectShockers{trigger.Name}");
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Select on which shockers this trigger is enabled.");
                }

                string name = trigger.Name;
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(ImGui.GetColumnWidth());
                if (ImGui.InputTextWithHint("##name", "", ref name, 100))
                {
                    trigger.Name = name;
                    Configuration.Save();
                }

                ImGui.TableNextColumn();
                if (trigger.Regex == null)
                {
                    try
                    {
                        trigger.Regex = new Regex(trigger.RegexString);
                    }
                    catch (ArgumentException)
                    {
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.TextColored(ImGuiColors.DPSRed, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                        ImGui.PopFont();
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Not a valid regular expression. Will not be parsed.");
                        }
                        ImGui.SameLine();
                    }
                }
                ImGui.SetNextItemWidth(ImGui.GetColumnWidth());
                if (ImGui.InputTextWithHint("##regex", "Regex", ref trigger.RegexString, 200))
                {
                    try
                    {
                        trigger.Regex = new Regex(trigger.RegexString);
                    }
                    catch
                    {
                        trigger.Regex = null;
                    }
                    Configuration.Save();
                }

                int opMode = (int)trigger.OpMode;
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(ImGui.GetColumnWidth());
                if (ImGui.Combo("##mode", ref opMode, ["Shock", "Vibrate", "Beep"], 3))
                {
                    trigger.OpMode = (OpMode)opMode;
                    Configuration.Save();
                }

                int duration = trigger.Duration;
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(ImGui.GetColumnWidth());
                if (ImGui.SliderInt("##duration", ref duration, 1, 10))
                {
                    trigger.Duration = duration;
                    Configuration.Save();
                }

                int intensity = trigger.Intensity;
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(ImGui.GetColumnWidth());
                if (ImGui.SliderInt("##intensity", ref intensity, 1, 100))
                {
                    trigger.Intensity = intensity;
                    Configuration.Save();
                }

                ImGui.TableNextColumn();
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(FontAwesomeIcon.Trash.ToIconString(), ImGui.GetFrameHeight() * Vector2.One))
                {
                    Configuration.ActivePreset.SayCustomMessage.Remove(trigger);
                    Configuration.Save();
                }
                ImGui.PopFont();

                ImGui.PopID();
            }
            ImGui.EndTable();
        }
    }


    */


    /*


    private void createEntry(ShockOptions TriggerObject, string Name, string Description, bool noOptions) { createEntry(TriggerObject, Name, Description, "", noOptions, false); }
    private void createEntry(ShockOptions TriggerObject, string Name, string Description, string Hint) { createEntry(TriggerObject, Name, Description, Hint, false, false); }

    private void createEntry(ShockOptions TriggerObject, string Name, string Description, string Hint, bool noOptions, bool noCooldown)
    {
        createShockerSelector(TriggerObject);
        bool enabled = TriggerObject.IsEnabled();
        ImGui.BeginGroup();
        if (noOptions || !enabled)
        {
            ImGui.Spacing();
            ImGui.Spacing();
        }
        if (ImGui.Checkbox($"##checkBox{TriggerObject.Name}", ref enabled))
            ImGui.OpenPopup($"Select Shockers##selectShockers{TriggerObject.Name}");
        bool isOptionsOpened = TriggerObject.isOptionsOpen;
        if (!noOptions && enabled)
        {
            if (isOptionsOpened && ImGui.ArrowButton("##collapse" + TriggerObject.Name, ImGuiDir.Down))
            {
                TriggerObject.isOptionsOpen = !isOptionsOpened;
            }
            if (!isOptionsOpened && ImGui.ArrowButton("##collapse" + TriggerObject.Name, ImGuiDir.Right))
            {
                TriggerObject.isOptionsOpen = !isOptionsOpened;
            }
        }
        ImGui.EndGroup();

        ImGui.SameLine();
        ImGui.BeginGroup();
        if (!noOptions && enabled) ImGui.Spacing();
        if (enabled) ImGui.TextColored(nameColorOn, "  " + Name + $"  [{TriggerObject.OpMode}]");
        else ImGui.TextColored(nameColorOff, "  " + Name);

        if (TriggerObject.hasBeenReset)
        {
            ImGui.SameLine();
            ImGui.TextColored(nameColorReset, " (has been reset)");
        }

        ImGui.TextColored(descColor, $"  {Description}");
        if (Hint.Length > 0)
        {
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip(Hint); }
        }
        ImGui.EndGroup();

        if (isOptionsOpened && enabled)
        {
            createPickerBox(TriggerObject, noCooldown);
        }
        ImGui.Spacing();
        ImGui.Separator();
    }

    private void createPickerBox(ShockOptions TriggerObject, bool noCooldown)
    {
        bool changed = false;

        ImGui.BeginDisabled();
        ImGui.Button($"{TriggerObject.Shockers.Count}##shockerButton{TriggerObject.Name}", new Vector2(35, 50));
        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) { ImGui.SetTooltip($"Enabled Shockers:\n{TriggerObject.getShockerNamesNewLine()}"); }
        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.Text("    Mode");
        ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 3 - 50);
        int OpMode = (int)TriggerObject.OpMode;
        if (ImGui.Combo("##" + TriggerObject.Name, ref OpMode, ["Shock", "Vibrate", "Beep"], 3))
        {
            TriggerObject.OpMode = (OpMode)OpMode;
            changed = true;
        }
        ImGui.EndGroup();

        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.Text("    Duration");
        ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 7);
        int DurationIndex = durationArray.IndexOf(TriggerObject.Duration);
        if (ImGui.Combo("##Duration" + TriggerObject.Name, ref DurationIndex, ["0.1s", "0.3s", "1s", "2s", "3s", "4s", "5s", "6s", "7s", "8s", "9s", "10s"], 12))
        {
            TriggerObject.Duration = durationArray[DurationIndex];
            changed = true;
        }
        ImGui.EndGroup();

        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.Text("    Intensity");
        ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 1.85f - 30);
        int Intensity = TriggerObject.Intensity;
        if (ImGui.SliderInt("##Intensity" + TriggerObject.Name, ref Intensity, 1, 100))
        {
            TriggerObject.Intensity = Intensity;
            changed = true;
        }
        ImGui.EndGroup();

        if (TriggerObject.Name == "TakeDamage") createProportional(TriggerObject, "Amount of Health% to lose to hit the Limit.", 1, 100);
        if (TriggerObject.Name == "FailMechanic") createProportional(TriggerObject, "Amount of Stacks needed to hit the Limit.", 1, 8);

        if (!noCooldown)
        {
            int Cooldown = TriggerObject.Cooldown;
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 1.25f - 100);
            if (ImGui.SliderInt("##Cooldown" + TriggerObject.Name, ref Cooldown, 0, 300))
            {
                TriggerObject.Cooldown = Cooldown;
                changed = true;
            }
            ImGui.SameLine();
            int modifierIndex = modifierArray.IndexOf(TriggerObject.modifier);
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 4f - 30);
            if (ImGui.Combo("##TimeModifier", ref modifierIndex, ["Miliseconds", "Seconds", "Minutes", "Hours"], 4,4))
            {
                TriggerObject.modifier = modifierArray[modifierIndex];
                changed = true;
            }

            ImGui.SameLine();
            ImGui.Text("Cooldown");
        }

        if (changed) Configuration.Save();
    }

    private void createShockerSelector(ShockOptions TriggerObject)
    {
        // Todo add proper formatting, this popup looks terrible
        Vector2 center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(400, 400));
        bool isModalOpen = TriggerObject.isModalOpen;
        if (ImGui.BeginPopupModal($"Select Shockers##selectShockers{TriggerObject.Name}", ref isModalOpen,
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.Popup | ImGuiWindowFlags.NoDecoration))
        {

            if (Plugin.Authentification.PishockShockers.Count == 0)
            {
                ImGui.TextWrapped("Please add Shockers first via the \"Account Settings\" in the main window.");
                if (ImGui.Button($"Okay##okayShockerSelectorAbort", new Vector2(ImGui.GetWindowSize().X / 2, 25)))
                {
                    ImGui.CloseCurrentPopup();
                }
            }
            else
            {
                ImGui.Text("Please select all shockers that should activate for this trigger:");
                foreach (var shocker in Plugin.Authentification.PishockShockers)
                {
                    bool isEnabled = TriggerObject.Shockers.Find(sh => sh.Code == shocker.Code) != null;
                    if (ImGui.Checkbox($"{shocker.Name}##shockerbox{shocker.Code}", ref isEnabled))
                    { // this could probably be solved more elegantly
                        if (isEnabled) TriggerObject.Shockers.Add(shocker);
                        else TriggerObject.Shockers.RemoveAt(TriggerObject.Shockers.FindIndex(sh => sh.Code == shocker.Code));
                    }
                }

                if (ImGui.Button($"Apply##apply{TriggerObject.Name}", new Vector2(ImGui.GetWindowSize().X / 2, 25)))
                {
                    TriggerObject.hasBeenReset = false;
                    ImGui.CloseCurrentPopup();
                }

                if (ImGui.Button($"Reset All##resetall{TriggerObject.Name}", new Vector2(ImGui.GetWindowSize().X / 2, 25)))
                {
                    TriggerObject.Shockers.Clear();
                }
            }
            ImGui.EndPopup();
        }


    }

    private void createProportional(ShockOptions TriggerObject, string Description, int minValue, int maxValue)
    {
        TriggerObject.setupCustomData();
        bool isEnabled = TriggerObject.CustomData["Proportional"][0] == 1;
        if (ImGui.Checkbox($"Enable proportional calculations.##proportionalIsEnabled{TriggerObject.Name}", ref isEnabled)) TriggerObject.CustomData["Proportional"][0] = isEnabled ? 1 : 0;
        if (isEnabled)
        {
            int setValue = TriggerObject.CustomData["Proportional"][1];
            ImGui.SetNextItemWidth(ImGui.GetWindowWidth() / 2 - 25);
            if (ImGui.SliderInt($"{Description}##proportionalSlider{TriggerObject.Name}", ref setValue, minValue, maxValue)) TriggerObject.CustomData["Proportional"][1] = setValue;
        }

    }
    */
}
