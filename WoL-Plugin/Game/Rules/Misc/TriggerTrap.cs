using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.Misc
{
    [Serializable]
    public class TriggerTrap : RuleBase
    {
        override public string Name { get; } = "Trigger a Deep Dungeon Trap";
        override public string Description { get; } = "Triggers whenever step onto a Deep Dungeon Trap.\n  Optionally includes Mimics and Bomb Coffers.";
        override public RuleCategory Category { get; } = RuleCategory.Misc;
        public override bool hasExtraButton { get; } = true;

        public bool TriggeredMimic { get; set; } = false;
        public bool TriggeredBomb { get; set; } = false;


        [JsonConstructor]
        public TriggerTrap() { }
        public TriggerTrap(Plugin plugin) : base(plugin)
        {
        }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Service.ToastGui.Toast += Check;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.ToastGui.Toast -= Check;
        }

        private void Check(ref SeString messageE, ref ToastOptions options, ref bool isHandled)
        {
            try
            {
                if (messageE == null || messageE.ToString() == null) { return; }
                String message = messageE.ToString();

                if (message.Contains(LanguageStrings.DeepDungeonTrap())) 
                    Trigger("You triggered a Trap!");

                if(TriggeredMimic && message.Contains(LanguageStrings.DeepDungeonCofferMimic()))
                    Trigger("You triggered a Mimic!");

                if (TriggeredBomb && message.Contains(LanguageStrings.DeepDungeonCofferBomb()))
                    Trigger("You triggered a Bomb Coffer!");

            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace); }
        }

        public override void DrawExtraButton()
        {

            bool triggerMimic = TriggeredMimic;
            if(ImGui.Checkbox("Trigger from Mimics?##TriggerTrapMimics", ref triggerMimic))
            {
                TriggeredMimic = triggerMimic;
                Plugin.Configuration.SaveCurrentPresetScheduled();
            }

            bool triggerBomb = TriggeredBomb;
            if (ImGui.Checkbox("Trigger from Bomb coffers?##TriggerBomb", ref triggerBomb))
            {
                TriggeredBomb = triggerBomb;
                Plugin.Configuration.SaveCurrentPresetScheduled();
            }
        }
    }
}
