using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using System;
using System.Linq;
using WoLightning.WoL_Plugin.Util;


namespace WoLightning.Game
{
    public class EmoteReaderHooks : IDisposable
    {
        private Plugin Plugin;
        public Action<IPlayerCharacter, IGameObject?, ushort> OnEmoteUnrelated;
        public Action<IPlayerCharacter, ushort> OnEmoteIncoming;
        public Action<IGameObject, ushort> OnEmoteOutgoing;
        public Action<ushort> OnEmoteSelf;
        public Action<ushort> OnSitEmote;

        public delegate void OnEmoteFuncDelegate(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2);
        private readonly Hook<OnEmoteFuncDelegate> hookEmote;

        public bool IsValid = false;

        internal bool log = true;

        public EmoteReaderHooks(Plugin plugin)
        {
            Plugin = plugin;
            try
            {
                hookEmote = Service.GameInteropProvider.HookFromSignature<OnEmoteFuncDelegate>("E8 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 4C 89 74 24", OnEmoteDetour);
                hookEmote.Enable();
                Logger.Log(3, "Started EmoteReaderHook!");
                IsValid = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex + "");
            }
        }

        public void Dispose()
        {
            hookEmote?.Dispose();
            IsValid = false;
        }

        void OnEmoteDetour(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2)
        {

            try
            {
                if (Service.ClientState.LocalPlayer != null)
                {
                    IGameObject? instigatorObject = Service.ObjectTable.FirstOrDefault(x => (ulong)x.Address == instigatorAddr);
                    IGameObject? targetObject = Service.ObjectTable.FirstOrDefault(x => x.GameObjectId == targetId);

                    bool isLocalPlayerTarget = targetId == Service.ClientState.LocalPlayer.GameObjectId;

                    Logger.Log(4, $"EmoteHook - ID: {emoteId} " +
                        $"\nInstigator - Address: {instigatorAddr} Obj: {instigatorObject}" +
                        $"\nTarget - ID: {targetId} Obj: {targetObject}");


                    // We are the Target.
                    if (isLocalPlayerTarget)
                    {
                        Logger.Log(4, $"Emote targeting us. ID: {emoteId} from {targetObject.Name}");
                        if (instigatorObject != null) OnEmoteIncoming?.Invoke((IPlayerCharacter)instigatorObject, emoteId);

                        hookEmote.Original(unk, instigatorAddr, emoteId, targetId, unk2);
                        return;
                    }

                    // Special - We are using a sit emote.
                    if (instigatorObject.GameObjectId == Service.ClientState.LocalPlayer.GameObjectId
                        && (emoteId >= 50 && emoteId <= 52))
                    {
                        OnSitEmote?.Invoke(emoteId);
                        hookEmote.Original(unk, instigatorAddr, emoteId, targetId, unk2);
                        return;
                    }


                    // We are not the target.
                    if (targetObject == null) // There is no target.
                    {
                        if (instigatorObject.GameObjectId == Service.ClientState.LocalPlayer.GameObjectId) // We sent a Emote without a target.
                            OnEmoteSelf?.Invoke(emoteId);
                        else
                            OnEmoteUnrelated?.Invoke((IPlayerCharacter)instigatorObject, targetObject, emoteId); // Someone sent a Emote without a target.

                        hookEmote.Original(unk, instigatorAddr, emoteId, targetId, unk2);
                        return;
                    }

                    if (instigatorObject.GameObjectId == Service.ClientState.LocalPlayer.GameObjectId)
                        OnEmoteOutgoing?.Invoke(targetObject, emoteId); // We sent a Emote to a target.
                    else
                        OnEmoteUnrelated?.Invoke((IPlayerCharacter)instigatorObject, targetObject, emoteId); // Someone is sending a Emote to someone else.




                }
            }
            catch (Exception ex) { Logger.Error(ex.ToString()); }

            hookEmote.Original(unk, instigatorAddr, emoteId, targetId, unk2);
        }
    }
}