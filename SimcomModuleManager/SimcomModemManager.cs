using HardwareHelperLib;
using RJCP.IO.DeviceMgr;
using SimcomModuleManager.Ports;
using System.Diagnostics;

namespace SimcomModuleManager
{
    public class SimcomModemManager
    {
        private HH_Lib _hwh;

        private DeviceInstance _modemCompositeInstance;
        private DeviceInstance _modemATPortInstance;
        private DeviceInstance _modemAudioInstance;

        public ModemPort sim7600Modem;
        public ModemAudioPort sim7600ModemAudio;

        public EventHandler<string> OnRingingCommandReceived;
        public EventHandler<bool> OnCallBeginCommandReceived;
        public EventHandler<bool> OnCallEndCommandReceived;
        public EventHandler<string> OnDMTFKeyPressRecieved;

        public SimcomModemManager(DeviceInstance modemCompositeInstance, DeviceInstance modemATPortInstance, DeviceInstance modemAudioInstance)
        {
            _modemCompositeInstance = modemCompositeInstance;
            _modemAudioInstance = modemAudioInstance;
            _modemATPortInstance = modemATPortInstance;

            _hwh = new HH_Lib();
        }

        public async Task<bool> Initialize()
        {
            sim7600Modem = new ModemPort(_modemATPortInstance);
            sim7600ModemAudio = new ModemAudioPort(_modemAudioInstance);   

            await RunInitialModemCommands();

            if (await DisableEnableSerialPort() == false)
            {
                return false;
            }

            return true;
        }

        public async Task StartCheckingRingCommandLoop(CancellationToken cancellationToken, int loopDelay = 50, int invokeDelay = 100)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                string currentResult = await sim7600Modem.WriteCommandGetResult("AT");

                if (currentResult.Contains("RING"))
                {
                    string phoneActivityStatus = await sim7600Modem.WriteCommandGetResult("AT+CPAS");
                    if (phoneActivityStatus.Contains("+CPAS: 3")) // 3 = ringing (ME is ready for commands fromTA/TE, but theringer is active)
                    {
                        string currentCallInformation = await sim7600Modem.WriteCommandGetResult("AT+CLCC");
                        if (currentCallInformation.Contains("+CLCC:"))
                        {
                            currentCallInformation = currentCallInformation.Substring(currentCallInformation.IndexOf("+CLCC:"));

                            string[] currentCallDetails = currentCallInformation.Split(",");
                            if (currentCallDetails.Length >= 7)
                            {
                                if (
                                    currentCallDetails[1] == "1"  // mobile terminated (MT) call
                                    && currentCallDetails[2] == "4" // incoming (MT call)
                                    && currentCallDetails[3] == "0" // voice
                                )
                                {
                                    string phoneNumberCalling = currentCallDetails[5];

                                    Console.WriteLine("Incoming Call: " + phoneNumberCalling);
                                    OnRingingCommandReceived?.Invoke(this, phoneNumberCalling);

                                    break;
                                }
                            }
                        }
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
                        OnCallBeginCommandReceived?.Invoke(this, true);
                        break;
                    }
                }

                await Task.Delay(loopDelay);
            }
        }

        public async Task StartCheckingForCallEndLoop(CancellationToken cancellationToken, int loopDelay = 50)
        {
            string currentResult = await sim7600Modem.WriteCommandGetResult("AT", 250);

            while (true)
            {
                await Task.Delay(loopDelay);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                currentResult = await sim7600Modem.WriteCommandGetResult("AT+CLCC");
                if (string.IsNullOrWhiteSpace(currentResult)) continue;

                if (currentResult.Contains("+CLCC:"))
                {
                    string CLCCResult = currentResult.Substring(currentResult.IndexOf("+CLCC:"));

                    string[] currentCallDetails = CLCCResult.Split(",");
                    if (currentCallDetails.Length >= 7)
                    {
                        if (
                            currentCallDetails[1] == "1"  // mobile terminated (MT) call
                            && currentCallDetails[2] == "0" // active (MT call)
                            && currentCallDetails[3] == "0" // voice
                        )
                        {
                            if (currentResult.Contains("+RXDTMF:"))
                            {
                                string DTMFResult = currentResult.Substring(currentResult.IndexOf("+RXDTMF:")).Split(":")[1].Split(Environment.NewLine)[0].Trim();
                                OnDMTFKeyPressRecieved?.Invoke(this, DTMFResult);
                            }

                            continue;
                        }
                    }
                }

                OnCallEndCommandReceived?.Invoke(this, true); 
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
            string result = await sim7600Modem.WriteCommandGetResult("AT+CVHU=0");
            result = await sim7600Modem.WriteCommandGetResult("ATH");
        }

        public async Task DisableModemAudioTransfer()
        {
            string result = await sim7600Modem.WriteCommandGetResult("AT+CPCMREG=0");
        }

        public async Task EnableDTMFTones()
        {
            string result = await sim7600Modem.WriteCommandGetResult("AT+VTS=1");
        }

        public async Task<bool> SetMicrophoneGain(int gain)
        {
            if (gain < 0 || gain > 8) return false;

            string result = await sim7600Modem.WriteCommandGetResult($"AT+CMICGAIN={gain}");
            result = await sim7600Modem.WriteCommandGetResult($"AT+COUTGAIN={gain}");

            return true;
        }

        public async Task<bool> DisableEnableSerialPort()
        {
            if (!(await _hwh.DisableThenEnableDevice(_modemCompositeInstance.HardwareIds[0], 500)))
            {
                return false;
            }

            await Task.Delay(500);

            if (!(await _hwh.DisableThenEnableDevice(_modemAudioInstance.HardwareIds[0], 500)))
            {
                return false;
            }

            await Task.Delay(500);

            if (!(await _hwh.DisableThenEnableDevice(_modemATPortInstance.HardwareIds[0], 500)))
            {
                return false;
            }

            return true;
        }

        public async Task<string?> GetModulePhoneNumber()
        {
            await sim7600Modem.WriteCommandGetResult("AT+CPBS=\"ON\"");

            string result = await sim7600Modem.WriteCommandGetResult("AT+CNUM");
            if (!result.Contains("+CNUM:"))
            {
                return null;
            }

            result = result.Split(",")[1].Replace("\"", "");

            return result;
        }

        public async Task<bool> SetModulePhoneNumber(string phoneNumber)
        {
            await sim7600Modem.WriteCommandGetResult("AT+CPBS=\"ON\"");
            string result = await sim7600Modem.WriteCommandGetResult($"AT+CPBW=1,\"{phoneNumber}\",129,\"Voice\"");

            return result.EndsWith("OK" + Environment.NewLine);
        }

        private async Task RunInitialModemCommands()
        {
            await sim7600Modem.WriteCommandGetResult("ATE1"); // enable echo

            await sim7600Modem.WriteCommandGetResult("AT+CPCMFRM=1"); // set audio format to 16k

            await sim7600Modem.WriteCommandGetResult("AT+CECRX=1"); // VOICE_MOD_ENABLE

            await sim7600Modem.WriteCommandGetResult("AT+CMICGAIN=0"); // set mic gain
            await sim7600Modem.WriteCommandGetResult("AT+COUTGAIN=0"); // set out gain
            await sim7600Modem.WriteCommandGetResult("AT+CTXVOL=0x0000"); // set TX voice mic volume

            await sim7600Modem.WriteCommandGetResult("AT+CPCMREG=0"); // disable audio transfer

            await sim7600Modem.WriteCommandGetResult("AT"); // clear all previous commands
        }

        public async Task ForwardIncomingCall(string numberToForwardTo)
        {
            string result = await sim7600Modem.WriteCommandGetResult($"AT+CHLD=2");
            //result = await sim7600Modem.WriteCommandGetResult($"ATD{numberToForwardTo};");
            //result = await sim7600Modem.WriteCommandGetResult($"AT+CHLD=3");
            Console.WriteLine(result);
        }
    }
}
