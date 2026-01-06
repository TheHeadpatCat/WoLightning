using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Buttplug.Client;
using System;
using System.Threading.Tasks;
using Buttplug.Core.Messages;
using WoLightning.WoL_Plugin.Util;
using System.Collections.Generic;
using System.Linq;
using WoLightning.Util.Types;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;


namespace WoLightning.Clients.Buttplugio
{
    public class ClientButtplug : IDisposable
    {
        public enum ConnectionStatusButtplug
        {
            NotStarted = 0,
            Unavailable = 1,
            FatalError = 3,


            Connecting = 99,
            ConnectedNoDevices = 101,
            Connected = 200,   
        }

        private Plugin? Plugin;
        public ConnectionStatusButtplug Status { get; set; } = ConnectionStatusButtplug.NotStarted;
        public string WebsocketAddress;
        public ClientButtplug(Plugin plugin)
        {
            Plugin = plugin;
        }
        public async void Setup()
        {
            await SetupAllData();
        }
        public ButtplugClient ButtplugSession;
        public async Task SetupAllData()
        {

            if (ButtplugSession != null) return;

            ButtplugSession = new ButtplugClient("WoLightning");

            ButtplugSession.DeviceAdded += OnDeviceAdded;
            ButtplugSession.DeviceRemoved += OnDeviceRemoved;
                
           try 
           {
                Logger.Log(4, $"Connecting to Intiface on {Plugin.Authentification.ButtplugURL}");
                await ButtplugSession.ConnectAsync(new ButtplugWebsocketConnector(new Uri(Plugin.Authentification.ButtplugURL)));
                Logger.Log(4, $"Succesfully connected!" +
                    $"\nName: {ButtplugSession.Name}");
            }
            catch (Exception e)
            {
                Logger.Log(4,"[BUTTPLUG] Couldnt connect!");
                Logger.Log(4,e?.InnerException?.Message);
            }
        }

       

        private void OnDeviceAdded(object? sender, DeviceAddedEventArgs Arguments)
        {
            Logger.Log(4,$"Device {Arguments.Device.Name} Connected!");
            Plugin.Authentification.ButtplugDevices = ButtplugSession.Devices.ToList();
        }

         private void OnDeviceRemoved(object? sender, DeviceRemovedEventArgs Arguments)
        {
           Logger.Log(4,$"Device {Arguments.Device.Name} Removed!");
           Plugin.Authentification.ButtplugDevices = ButtplugSession.Devices.ToList();
        }

        public async void SendRequest(ShockOptions options)
        {
            #region Validation

            if (Plugin.IsFailsafeActive)
            {
                Logger.Log(3, " -> Blocked request due to failsafe mode!");
                return;
            }

            if (!options.Validate())
            {
                Logger.Log(3, " -> Blocked due to invalid Buttplug Options!");
                return;
            }


            if (options.ButtplugDevices.Count == 0)
            {
                Logger.Log(3, " -> No Buttplug Devices assigned, discarding!");
                return;
            }
            #endregion

            if (ButtplugSession == null || Status != ConnectionStatusButtplug.Connected)
            {
                if (Status == ConnectionStatusButtplug.Connecting) return;
                ButtplugSession?.Dispose();
                ButtplugSession = null;
                await SetupAllData();
                return;
            }


            foreach (var Device in options.ButtplugDevices)
            {
                Task schedule = new Task(() =>
                {
                    Device.VibrateAsync(options.Intensity / 100.0);
                    Task.Delay(options.getDurationOpenShock());
                    Device.Stop();
                });

                schedule.Start();
            }
        }

        public void Dispose()
        {
            if (ButtplugSession == null) return;
            ButtplugSession.DeviceAdded -= OnDeviceAdded;
            ButtplugSession.DeviceRemoved -= OnDeviceRemoved;


            ButtplugSession?.DisconnectAsync().Wait();
            ButtplugSession?.Dispose();
        }
    }
}














/*  public ButtplugClient TestClient;
    public async Task WebSocketTest()
    {
            Logger.Log(4,"[BUTTPLUG] Starting Test");
            // Creating a Websocket Connector is as easy as using the right
            // options object.
            
            TestClient = new ButtplugClient("Example Client");

            TestClient.DeviceAdded += (aObj, aDeviceEventArgs) =>
                Logger.Log(4,$"Device {aDeviceEventArgs.Device.Name} Connected!");

            TestClient.DeviceRemoved += (aObj, aDeviceEventArgs) =>
                Logger.Log(4,$"Device {aDeviceEventArgs.Device.Name} Removed!");

            

            try 
            {
            
            await TestClient.ConnectAsync(new ButtplugWebsocketConnector(new Uri("ws://127.0.0.1:12345")));
            
            }
            catch (Exception e)
        {
            
            Logger.Log(4,"[BUTTPLUG] Couldnt connect!");
            Logger.Log(4,e?.InnerException?.Message);

        }
            
            Logger.Log(4,TestClient.ToString());

            Logger.Log(4,"[BUTTPLUG] Connected to Intiface");


            Logger.Log(4,"[BUTTPLUG] Starting Device Scan");

            await TestClient.StartScanningAsync();
            
            foreach (var device in TestClient.Devices)
            {
                Logger.Log(4,$"[BUTTPLUG] {device.Name} is connected");
            }
            await Task.Delay(5000);
            Logger.Log(4,"[BUTTPLUG] Waited 5 seconds, stopping scan");

            await TestClient.StopScanningAsync();
            

            foreach (var device in TestClient.Devices)
            {
                Logger.Log(4,$"[BUTTPLUG] {device.Name} supports vibration: {device.VibrateAttributes.Count > 0}");
                if (device.VibrateAttributes.Count > 0)
                {
                   Logger.Log(4,$"[BUTTPLUG]  - Number of Vibrators: {device.VibrateAttributes.Count}");
                }
            }

            Logger.Log(4,"[BUTTPLUG] ________FINISHED SETUP_________");

            Logger.Log(4,"[BUTTPLUG] Now trying to send commands");


            var testClientDevice = TestClient.Devices[0];
            await testClientDevice.VibrateAsync(0.3);

            await Task.Delay(2000);

            await testClientDevice.Stop();
            Logger.Log(4,"[BUTTPLUG] Stopped Command!");
            //test
        }
    }

}*/