using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Lumina.Excel.Sheets;
using System;
using System.Text.Json.Serialization;

namespace WoLightning.WoL_Plugin.Game.Rules.Misc
{
    [Serializable]
    public class FailCraftHQ : RuleBase
    {
        override public string Name { get; } = "Fail a HQ Craft";
        override public string Description { get; } = "Triggers whenever you fail to craft a HQ Item when it would have been possible.";
        override public RuleCategory Category { get; } = RuleCategory.Misc;
        [JsonIgnore] Recipe? CurrentCraft { get; set; }
        [JsonIgnore] IPlayerCharacter Player;


        [JsonConstructor]
        public FailCraftHQ() { }
        public FailCraftHQ(Plugin plugin) : base(plugin)
        {
        }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Plugin.ToastGui.QuestToast += Check;
            Player = Plugin.ClientState.LocalPlayer;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Plugin.ToastGui.QuestToast -= Check;
        }

        private unsafe void Check(ref SeString messageE, ref QuestToastOptions options, ref bool isHandled)
        {
            if (Player == null) { Player = Plugin.ClientState.LocalPlayer; return; }
            if (Player.MaxCp == 0) return; // We are not a Crafter.

            /*
            var agent = AgentRecipeNote.Instance();
            CurrentCraft = Plugin.DataManager.GetExcelSheet<Recipe>().GetRowOrDefault(agent->ActiveCraftRecipeId);
            if(CurrentCraft == null) return;
            */


        }
    }
}
