using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Features
{
    public class RuleReaction
    {

        // Todo: Allow for different reactions to play out when a Rule gets triggered.
        // Possibly give different Options for stuff like "Shock above 40%" or similiar
        // Also allow for randomization


        public RuleReaction()
        {
            var t = new Dalamud.Game.Text.XivChatEntry();
            Service.ChatGui.Print(t);
        }

        public void Trigger(ShockOptions options)
        {

        }
    }
}
