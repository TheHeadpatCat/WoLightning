using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System.Text.Json.Serialization;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    public class UseSkill : RuleBase
    {
        public override string Name { get; } = "Use a Skill";

        public override string Description { get; } = "Triggers whenever you use one of the specified Skills";

        public override RuleCategory Category { get; } = RuleCategory.PVE;

        [JsonIgnore] IPlayerCharacter Player;

        public UseSkill(Plugin plugin):base(plugin) {
        
        }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            //Service.Framework.Update += Check;
            Player = Service.ObjectTable.LocalPlayer;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            //Service.Framework.Update -= Check;
        }
    }
}
