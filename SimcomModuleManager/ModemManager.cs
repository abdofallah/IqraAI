using HardwareHelperLib;
using RJCP.IO.DeviceMgr;

namespace SimcomModuleManager
{
    public class ModemManager
    {
        private HH_Lib _hwh;

        private DeviceInstance modemCompositeInstance;
        private DeviceInstance modemModemInstance;
        private DeviceInstance modemATPortInstance;
        private DeviceInstance modemAudioInstance;

        public Modem sim7600Modem;
        public ModemAudio sim7600ModemAudio;

        public EventHandler<bool> OnRingingCommandReceived;
        public EventHandler<bool> OnCallBeginCommandReceived;
        public EventHandler<bool> OnCallEndCommandReceived;

        public ModemManager()
        {
            _hwh = new HH_Lib();
        }

        public async Task<bool> Initialize()
        {
            if (!(await FindAndSetModemSerialPorts()))
            {
                return false;
            }

            sim7600Modem = new Modem(modemATPortInstance);
            sim7600ModemAudio = new ModemAudio(modemAudioInstance);

            if (!sim7600Modem.OpenModemSerialConnection())
            {
                return false;
            }

            if (!sim7600ModemAudio.OpenModemSerialConnection())
            {
                return false;
            }

            await RunInitialModemCommands();

            return true;
        }

        public async Task StartCheckingRingCommandLoop(CancellationToken cancellationToken, int loopDelay = 50)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                string currentResult = await sim7600Modem.ReadBuffer();

                if (!string.IsNullOrWhiteSpace(currentResult))
                {
                    if (currentResult.Contains("RING"))
                    {
                        OnRingingCommandReceived.Invoke(this, true);
                    }
                }

                await Task.Delay(loopDelay);
            }
        }

        public async Task StartCheckingForCallBeginLoop(CancellationToken cancellationToken, int loopDelay = 50)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                string currentResult = await sim7600Modem.ReadBuffer();

                if (!string.IsNullOrWhiteSpace(currentResult))
                {
                    if (currentResult.Contains("VOICE CALL: BEGIN"))
                    {
                        OnCallBeginCommandReceived.Invoke(this, true);
                    }
                }

                await Task.Delay(loopDelay);
            }
        }

        public async Task StartCheckingForCallEndLoop(CancellationToken cancellationToken, int loopDelay = 50)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                string currentResult = await sim7600Modem.ReadBuffer();

                if (!string.IsNullOrWhiteSpace(currentResult))
                {
                    if (currentResult.Contains("VOICE CALL: END"))
                    {
                        OnCallEndCommandReceived.Invoke(this, true);
                    }
                }

                await Task.Delay(loopDelay);
            }
        }

        public async Task PickupIncomingCall()
        {
            string result = await sim7600Modem.WriteCommandGetResult("ATA");
        }

        public async Task EnableModemAudioTransfer()
        {
            string result = await sim7600Modem.WriteCommandGetResult("AT+CPCMREG=1");
        }

        public async Task DropOngoingCall()
        {
            string result = await sim7600Modem.WriteCommandGetResult("ATH");
        }

        public async Task DisableModemAudioTransfer()
        {
            string result = await sim7600Modem.WriteCommandGetResult("AT+CPCMREG=0");
        }

        public async Task<bool> DisableEnableSerialPort()
        {
            if (!(await _hwh.DisableThenEnableDevice(modemCompositeInstance.HardwareIds[0], 10)))
            {
                return false;
            }

            return true;
        }

        private async Task<bool> FindAndSetModemSerialPorts()
        {
            IList<DeviceInstance> deviceInstances = DeviceInstance.GetList();

            foreach (DeviceInstance deviceInstance in deviceInstances)
            {
                if (deviceInstance.Manufacturer == "Qualcomm Incorporated")
                {
                    if (deviceInstance.FriendlyName.Contains("Simcom USB Composite Device 9001"))
                    {
                        modemCompositeInstance = deviceInstance;
                    }
                    else if (deviceInstance.FriendlyName.Contains("Simcom HS-USB Modem 9001"))
                    {
                        modemModemInstance = deviceInstance;
                    }
                    else if (deviceInstance.FriendlyName.Contains("Simcom HS-USB AT PORT 9001"))
                    {
                        modemATPortInstance = deviceInstance;
                    }
                    else if (deviceInstance.FriendlyName.Contains("Simcom HS-USB Audio 9001"))
                    {
                        modemAudioInstance = deviceInstance;
                    }
                }
            }

            if (
                modemCompositeInstance == null
                || modemModemInstance == null
                || modemATPortInstance == null
                || modemAudioInstance == null)
            {
                Console.WriteLine("Could not find modem ports or audio ports");
                return false;
            }

            if (!(await DisableEnableSerialPort()))
            {
                Console.WriteLine("Failed to reset the modem instance during initial test.");
                return false;
            }

            return true;
        }

        private async Task RunInitialModemCommands()
        {
            Console.WriteLine("Echo ON:\n" + await sim7600Modem.WriteCommandGetResult("ATE1"));
            Console.WriteLine("AT Command:\n" + await sim7600Modem.WriteCommandGetResult("AT"));
            Console.WriteLine("ATI Command:\n" + await sim7600Modem.WriteCommandGetResult("ATI"));
            Console.WriteLine("Firmware Version:\n" + await sim7600Modem.WriteCommandGetResult("AT+GMR"));
            Console.WriteLine("AP Version:\n" + await sim7600Modem.WriteCommandGetResult("AT+CSUB"));
            Console.WriteLine("Sim Status:\n" + await sim7600Modem.WriteCommandGetResult("AT+CPIN?"));
            Console.WriteLine("Network Status:\n" + await sim7600Modem.WriteCommandGetResult("AT+CGREG?"));
            Console.WriteLine("Registered Network:\n" + await sim7600Modem.WriteCommandGetResult("AT+COPS?"));
            Console.WriteLine("Number:\n" + await sim7600Modem.WriteCommandGetResult("AT+CNUM"));
            Console.WriteLine("Signal Quality:\n" + await sim7600Modem.WriteCommandGetResult("AT+CSQ"));
            Console.WriteLine("Set 16K bitrate Audio:\n" + await sim7600Modem.WriteCommandGetResult("AT+CPCMFRM=1"));
            Console.WriteLine("Set speaker volume:\n" + await sim7600Modem.WriteCommandGetResult("AT+CLVL=5"));
            Console.WriteLine("Set mic gain:\n" + await sim7600Modem.WriteCommandGetResult("AT+CMICGAIN=5"));
            Console.WriteLine("Audio mode:\n" + await sim7600Modem.WriteCommandGetResult("AT+CPCMREG?"));
        }
    }
}
