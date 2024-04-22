using RJCP.IO.DeviceMgr;
using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimcomModuleManager.Ports
{
    public class ModemAudioPort
    {
        private readonly DeviceInstance _instance;

        private readonly SerialPortStream _serialPort;

        public ModemAudioPort(DeviceInstance portInstance)
        {
            _instance = portInstance;

            _serialPort = new SerialPortStream();

            Match match = Regex.Match(_instance.FriendlyName, @"\((.*?)\)");
            _serialPort.PortName = match.Groups[1].Value;

            _serialPort.BaudRate = 115200;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;

            _serialPort.ReadBufferSize = 4096;
            _serialPort.WriteBufferSize = 4096;

            _serialPort.ReadTimeout = 3000;
            _serialPort.WriteTimeout = 3000;
        }

        public async Task WriteData(byte[] buffer, int length, CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("[WriteData] " + length);
                await _serialPort.WriteAsync(buffer, 0, length, cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(IOException))
                {
                    Console.WriteLine("[WriteData] IOException" + ex.ToString());
                }
                else if (ex.GetType() == typeof(TimeoutException))
                {
                    Console.WriteLine("[WriteData] Timeout" + ex.ToString());
                }
                else
                {
                    Console.WriteLine("[WriteData] " + ex.ToString());
                }
            }
        }

        public async Task<(byte[], int)> ReadData(CancellationToken cancellationToken)
        {
            int buffSize = _serialPort.ReadBufferSize;

            byte[] buffer = new byte[buffSize];
            int dataResultbytes = 0;
            try
            {
                dataResultbytes = await _serialPort.ReadAsync(buffer, cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex.GetType() != typeof(IOException))
                {
                    Console.WriteLine("[ReadAudioData] " + ex.ToString());
                }
            }

            return (buffer, dataResultbytes);
        }

        public void ClearReadBuffer()
        {
            try
            {
                _serialPort.DiscardInBuffer();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public bool OpenModemSerialConnection()
        {
            try
            {
                _serialPort.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        public bool CloseModemSerialConnection()
        {
            try
            {
                _serialPort.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }
    }
}
