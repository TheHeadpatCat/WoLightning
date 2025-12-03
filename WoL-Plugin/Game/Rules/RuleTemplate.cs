using Dalamud.Game.ClientState.Objects.SubKinds;
using System;
using System.Text.Json.Serialization;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.Misc
{
    [Serializable]
    public class RuleTemplate : RuleBase
    {

        /*

        This is a Template to create new Rules for the Plugin.


        Step 1: Copy this Rule into the correct Category folder and rename the file & class to what you have to do to trigger it.
        You can take a look at the other rules for examples on how to name it.
        Do's:   DutyPops, UseSkill, DieToPlayer
        Dont's: WhenDutyPops, IUseASkill, PlayerKillsYou

        Step 2: Fill out all the Properties below. Some of the properties aren't fully used yet, but its still important to set them.
        Some Properties like "Hint" can also be removed. In those cases, they will either be replaced by a default from RuleBase.cs or just not show up.

        Step 3: Subscribe to any needed services in the Start() function and unsubscribe from them in the Stop() function.
        You can access different events from the "Service" class. If it does not have a Event you need for your Rule to function,
        simply add another PluginService in it.

        Step 4: Now, you can write the Check() function. This function hosts all the logic of the Rule.
        Basically, you write a bunch of things to check for one after another, and if the Rule gets triggered, you call the Trigger() function.
        Trigger() always wants a string passed to it, which will show up as the Notification in the bottom right, if Notifications are enabled.

        If you want to take a look at how a Check() could look like - please check the Game/Rules/Social/SayWord.cs
        It's fully commented on what each step does in there.

        If you need any extra variables, you can either add them as public with { get; set; } to make the get saved,
        or have them private with [JsonIgnore] to make sure they do not save.

        Step 5: 
        Once all of this is done, you only need to add your Rule to the Preset.cs file.
        
        Head to Util/Types/Preset and add a property with your new Rule. The format should be obvious from the other ones.

        Once you have done all of this, your Rule should show up in that Category of the Configuration Window ingame.
        Enable it and give it a try if it properly works!
        If you encounter issues, you can always try and use Logger.Log(3, "Text") to debug any issues you might encounter.

        */

        override public string Name { get; } = "Do something Wrong"; // Needs to be set.
        override public string Description { get; } = "Triggers whenever you do something that was defined."; // Needs to be set.
        override public string Hint { get; } = "If this is set, a small (?) will appear and show this Text on hover."; // May be removed.
        override public RuleCategory Category { get; } = RuleCategory.Misc; // Needs to be set. This will dictate on which Tab of the ConfigWindow your Rule shows up. (General, PvP and Master do not work currently.)
        override public string CreatorName { get; } = "Your Name Here"; // Please put some kind of identifier into this Spot. It will show up in the Rule to show that you did in fact make this, and i'd like you to get the credit you deserve.

        [JsonIgnore] IPlayerCharacter Player; // A Property that should not be saved. In this case, a reference to the Local Player Character, retrieved from Plugin.ClientState.
        public string SomeDataThatNeedsToBeSaved { get; set; } = "Default Data"; // A Property that will be saved. It also has got some default data. Once this data gets changed in anything, the default data is ignored.


        [JsonConstructor]
        public RuleTemplate() { }
        public RuleTemplate(Plugin plugin) : base(plugin)
        {
        }

        // Use this to setup your Rule. It will get called once the Rule is "enabled" (Checkmark is set) and the Plugin has been enabled.
        // You can also be certain that the rest of the Plugin is fully setup by this point and everything is available.
        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;

            // Service.ToastGui.QuestToast += Check;   // Subscribing to some kind of Event to run our Check() on. If you do not subscribe to anything, your Check() will never get called.
            // Your Check() function also needs to match the fields of the Event you subscribed to - in this example your Check would be "Check(ref SeString messageE, ref QuestToastOptions options, ref bool isHandled)"
            Player = Service.ObjectTable.LocalPlayer; // Setting up our Player so we can use them on the Check() function.
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            // Service.ToastGui.QuestToast -= Check;  // Make sure to unsubscribe from the Event. If you do not do this, your Check() will still get called even when the Rule or even the Plugin is disabled!
        }

        private void Check(string? someDataFromTheEvent)
        {
            // Every Check should be within a Try{} Catch{}, as they are getting called outside the normal loop.
            // If you do not do this, and your Logic throws a error, the Plugin will crash.
            try
            {
                if (Player == null) { Player = Service.ObjectTable.LocalPlayer; return; } // Double Checking that our Player Reference exists.

                // Do some Logic in here to figure out if we should Trigger from this Event call or not.
                if (someDataFromTheEvent != null && someDataFromTheEvent.Equals("A String that would trigger this Rule"))
                {
                    Trigger("You did something Wrong!");
                }
                else
                {
                    // Either Do nothing, or setup some other data.
                }

            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); if(e.StackTrace != null) Logger.Error(e.StackTrace); }

        }
    }
}
