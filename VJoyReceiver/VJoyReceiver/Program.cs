using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using vJoyInterfaceWrap;

namespace VJoyReceiver
{
    class Program
    {
        static void Main(string[] args)
        {
            const int port = 25555;
            var listener = new UdpClient(port);
            Console.WriteLine($"Listening for joystick input on port {port}...");

            vJoy vjoy = new vJoy();
            uint vjoyIdLeft = 1;
            uint vjoyIdRight = 2;

            if (!vjoy.vJoyEnabled())
            {
                Console.WriteLine("vJoy driver not enabled!");
                return;
            }

            bool acquiredLeft = vjoy.AcquireVJD(vjoyIdLeft);
            bool acquiredRight = vjoy.AcquireVJD(vjoyIdRight);

            if (!acquiredLeft || !acquiredRight)
            {
                Console.WriteLine("Failed to acquire vJoy devices!");
                return;
            }
            
            Console.WriteLine("vJoy devices acquired!");


            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, port);

            while (true)
            {
                try
                {
                    byte[] data = listener.Receive(ref remoteEP);
                    string json = Decompress(data);
                    // Console.WriteLine($"[RECV] From {remoteEP.Address}: {json}");

                    var state = JsonSerializer.Deserialize<JoystickState>(json);
                    if (state == null) continue;

                    uint id = state.stick == "left" ? vjoyIdLeft : vjoyIdRight;

                    vjoy.SetAxis(NormalizeAxis(state.axes.X), id, HID_USAGES.HID_USAGE_X);
                    vjoy.SetAxis(NormalizeAxis(state.axes.Y), id, HID_USAGES.HID_USAGE_Y);
                    vjoy.SetAxis(NormalizeAxis(state.axes.Z), id, HID_USAGES.HID_USAGE_RZ);
                    vjoy.SetContPov(state.pov, id, 1);
                    vjoy.SetAxis(NormalizeAxis(state.axes.Slider), id, HID_USAGES.HID_USAGE_SL0);

                    for (int i = 0; i < state.buttons.Length; i++)
                    {
                        vjoy.SetBtn(state.buttons[i], id, (uint)(i + 1));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
        
        static string Decompress(byte[] data)
        {
            var input = new MemoryStream(data);
            var gzip = new GZipStream(input, CompressionMode.Decompress);
            var reader = new StreamReader(gzip);
            return reader.ReadToEnd();
        }
        
        static int NormalizeAxis(int value)
        {
            // Most physical joysticks use 0–65535; vJoy expects 0–32768
            return Clamp(value / 2, 0, 32768);
        }
        
        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public class JoystickState
        {
            public string stick { get; set; }
            public AxisValues axes { get; set; }
            public int pov { get; set; }
            public bool[] buttons { get; set; }
        }

        public class AxisValues
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Z { get; set; }
            public int Slider { get; set; }
        }
    }
}
