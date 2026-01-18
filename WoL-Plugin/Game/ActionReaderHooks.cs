using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Timers;
using WoLightning.Util;
using WoLightning.WoL_Plugin.Util;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.ActionEffectHandler;

namespace WoLightning.WoL_Plugin.Game
{
    public unsafe class ActionReaderHooks : IDisposable
    {
        Plugin Plugin;
        public Lumina.Excel.Sheets.Action LastActionUsed;

        public Action<uint, float> CastStartedID;
        public Action<Lumina.Excel.Sheets.Action, float> CastStarted;

        public Action<uint> ActionUsedID;
        public Action<Lumina.Excel.Sheets.Action> ActionUsed;

        private delegate void OnReceiveDelegate(uint casterEntityId, Character* casterPtr, Vector3* targetPos, Header* header, TargetEffects* effects, GameObjectId* targetEntityIds);
        private readonly Hook<OnReceiveDelegate> hookReceive;

        public ActionReaderHooks(Plugin plugin)
        {
            Plugin = plugin;

            hookReceive = Service.GameInteropProvider.HookFromAddress<OnReceiveDelegate>(ActionEffectHandler.Addresses.Receive.Value, OnReceiveDetour);
            hookReceive.Enable();
        }

        public void Dispose()
        {
            hookReceive.Dispose();
        }

        private void OnReceiveDetour(uint casterEntityId, Character* casterPtr, Vector3* targetPos, Header* header, TargetEffects* effects, GameObjectId* targetEntityIds)
        {
            try
            {
                if (Service.ObjectTable.LocalPlayer == null || casterEntityId != Service.ObjectTable.LocalPlayer.EntityId)
                {
                    hookReceive.Original(casterEntityId, casterPtr, targetPos, header, effects, targetEntityIds);
                    return;
                }

                uint actionId = header->ActionId;
                var ac = Plugin.GameActions.getAction(actionId);

                if (actionId == 7 || actionId == 8) // Auto Attack
                { 
                    hookReceive.Original(casterEntityId, casterPtr, targetPos, header, effects, targetEntityIds);
                    return;
                }

                ActionUsedID?.Invoke(actionId);
                if (ac == null)
                {
                    Logger.Log(4, "Player used Unknown Action: " + actionId);
                }
                else
                {
                    Logger.Log(4, "Player used Action: " + ac.Value.Name.ExtractText() + " Id: " + ac.Value.RowId);
                    ActionUsed?.Invoke(ac.Value);
                    LastActionUsed = ac.Value;
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error when trying to extract ActionID!");
                Logger.Error(e.ToString());
            }

            hookReceive.Original(casterEntityId, casterPtr, targetPos, header, effects, targetEntityIds);
        }
    }
}
