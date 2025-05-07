using FFXIVClientStructs.FFXIV.Common.Math;
using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Timers;
using WoLightning.Util;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    [Serializable]
    public class SitOnFurniture : RuleBase
    {
        override public string Name { get; } = "Sit on Furniture";
        override public string Description { get; } = "Triggers whenever you try to do /sit on a chair or similiar.";
        override public string Hint { get; }
        override public RuleCategory Category { get; } = RuleCategory.Misc;

        private bool sittingOnChair = false;
        private Vector3 sittingOnChairPos = new();
        readonly private TimerPlus sittingOnChairTimer = new();
        private int safetyStop = 0;

        [JsonConstructor]
        public SitOnFurniture() { }
        public SitOnFurniture(Plugin plugin) : base(plugin) { }

        override public void Start()
        {
            Plugin.EmoteReaderHooks.OnSitEmote += Check;
            sittingOnChairTimer.Elapsed += checkSittingOnChair;
            IsRunning = true;
        }

        override public void Stop()
        {
            Plugin.EmoteReaderHooks.OnSitEmote -= Check;
            sittingOnChairTimer.Elapsed -= checkSittingOnChair;
            sittingOnChairTimer.Stop();
            IsRunning = false;
        }

        public void Check(ushort emoteId)
        {
            try
            {
                if (emoteId == 50) // /sit on Chair done
                {
                    sittingOnChair = true;
                    Trigger("You are sitting on Furniture!");
                    int calc = 5000;
                    if (ShockOptions.Duration <= 10) calc += ShockOptions.Duration * 1000;
                    sittingOnChairTimer.Interval = calc;
                    sittingOnChairTimer.Start();
                    Task.Delay(30).Wait(); // wait for a miniscule amount to make 100% sure the position is correct.
                    sittingOnChairPos = Plugin.LocalPlayerCharacter.Position;
                }

                if (emoteId == 52) // /sit with no furniture or /groundsit on furniture - check nearby chairs
                {
                    // Todo - Implement
                }

                if (emoteId == 51)
                {
                    sittingOnChair = false;
                    sittingOnChairTimer.Stop();
                }
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); }
        }

        private void checkSittingOnChair(object? sender, ElapsedEventArgs? e)
        {
            try
            {
                if(Plugin.LocalPlayerCharacter == null || !Plugin.LocalPlayerCharacter.IsValid())
                {
                    Logger.Log(3, "No Player Character.");
                    sittingOnChair = false;
                    sittingOnChairTimer.Stop();
                    safetyStop = 0;
                    return;
                }

                safetyStop++;
                if (safetyStop > 10)
                {
                    Logger.Log(3, "Timer has exceeded safety limit - aborting Chair Check.");
                    sittingOnChair = false;
                    sittingOnChairTimer.Stop();
                    safetyStop = 0;
                    return;
                }


                if (sittingOnChair && Plugin.LocalPlayerCharacter.Position.Equals(sittingOnChairPos))
                {
                    Trigger("You are still sitting on Furniture!");
                    sittingOnChairTimer.Refresh();
                    safetyStop++;
                }
                else
                {
                    Logger.Log(4, "Moved from Position.");
                    sittingOnChair = false;
                    sittingOnChairTimer.Stop();
                    safetyStop = 0;
                }
            }
            catch (Exception ex) { 
                Logger.Error(Name + " Check() failed."); 
                Logger.Error(ex.Message);
                sittingOnChairTimer.Stop();
                safetyStop++;
            }
        }
    }
}
