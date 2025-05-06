using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace WoLightning
{
    public class Service
    {
        [PluginService] public static IFramework Framework { get; private set; }
        [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; }
        [PluginService] public static ICommandManager CommandManager { get; private set; }
        [PluginService] public static IPluginLog PluginLog { get; private set; }

        [PluginService] public static IGameNetwork GameNetwork { get; private set; }
        [PluginService] public static IChatGui ChatGui { get; private set; }
        [PluginService] public static IDutyState DutyState { get; private set; }
        [PluginService] public static IClientState ClientState { get; private set; }
        [PluginService] public static INotificationManager NotificationManager { get; private set; }
        [PluginService] public static IGameInteropProvider GameInteropProvider { get; private set; }
        [PluginService] public static IPartyList PartyList { get; private set; }
        [PluginService] public static ITargetManager TargetManager { get; private set; }
        [PluginService] public static IDataManager DataManager { get; private set; }
        [PluginService] public static IToastGui ToastGui { get; private set; }
        [PluginService] public static IGameConfig GameConfig { get; private set; }
        [PluginService] public static ICondition Condition { get; private set; }
        [PluginService] public static IObjectTable ObjectTable { get; private set; }
    }
}
