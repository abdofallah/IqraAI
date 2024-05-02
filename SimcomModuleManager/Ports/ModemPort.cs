using RJCP.IO.DeviceMgr;
using RJCP.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;

namespace SimcomModuleManager.Ports
{
    public class ModemPort
    {
        private readonly DeviceInstance _instance;

        private readonly SerialPortStream _serialPort;

        public ModemPort(DeviceInstance portInstance)
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

            _serialPort.ReadBufferSize = 16384;
            _serialPort.WriteBufferSize = 16384;

            _serialPort.ReadTimeout = 100;
            _serialPort.WriteTimeout = 100;

            try
            {
                _serialPort.Open();
            }
            catch (Exception ex) { }
        }

        public async Task WriteCommand(string Command, int delay = 100)
        {
            byte[] commandBuffer = Encoding.UTF8.GetBytes(Command + Environment.NewLine);

            try
            {
                await _serialPort.WriteAsync(commandBuffer, 0, commandBuffer.Length, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            await Task.Delay(delay);
        }

        public async Task<string> ReadBuffer()
        {
            int buffSize = _serialPort.ReadBufferSize;
            byte[] buffer = new byte[buffSize];
            int commandResultBytes = await _serialPort.ReadAsync(buffer, CancellationToken.None);

            if (commandResultBytes > 0)
            {
                return Encoding.Default.GetString(buffer, 0, commandResultBytes);
            }
            else
            {
                return "";
            }
        }

        public async Task<string> WriteCommandGetResult(string Command, int delay = 100)
        {
            await WriteCommand(Command, delay);

            return await ReadBuffer();
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
