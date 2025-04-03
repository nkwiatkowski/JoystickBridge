using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpDX.DirectInput;

namespace JoystickSender
{
    public partial class Form1 : Form
    {
        
        private TextBox _txtLog;
        private Config config;
        private UdpClient udpClient;
        private IPEndPoint remoteEP;
        private int _frequency;
        
        private DirectInput directInput = new DirectInput();
        private Joystick leftJoystick;
        private Joystick rightJoystick;
        private Timer pollTimer;
        public Form1()
        {
            InitializeComponent();
            _txtLog = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Width = 550,
                Height = 200,
                Top = 10,
                Left = 10
            };
            Controls.Add(_txtLog);
            
            try
            {
                string configText = File.ReadAllText("config.json");
                config = JsonSerializer.Deserialize<Config>(configText);
                remoteEP = new IPEndPoint(IPAddress.Parse(config.Ip), config.Port);
                _frequency = config.Frequency;
                udpClient = new UdpClient();
                _txtLog.AppendText($"Loaded config: {config.Ip}:{config.Port}\r\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load config.json:\r\n" + ex.Message, "Config Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
            
            foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AttachedOnly))
            {
                if (deviceInstance.InstanceName.ToLower().Contains("16000"))
                {
                    if (rightJoystick == null)
                        rightJoystick = new Joystick(directInput, deviceInstance.InstanceGuid);
                    else
                        leftJoystick = new Joystick(directInput, deviceInstance.InstanceGuid);
                }
            }

            if (leftJoystick == null || rightJoystick == null)
            {
                MessageBox.Show("Could not find both T.16000M joysticks!");
                return;
            }

            leftJoystick.Acquire();
            rightJoystick.Acquire();

            pollTimer = new Timer { Interval = _frequency }; // ~60 FPS
            pollTimer.Tick += (s, e) => PollAndSend();
            pollTimer.Start();
        }
        
        private void PollAndSend()
        {
            SendJoystickState(leftJoystick, "left");
            SendJoystickState(rightJoystick, "right");
        }
        
        private void SendJoystickState(Joystick stick, string name)
        {
            try
            {
                stick.Poll();
                var state = stick.GetCurrentState();
                if (state == null) return;
                
                // _txtLog.AppendText($"Z axis ({stick.Information.InstanceName}): {state.Z}\r\n");
                // _txtLog.AppendText($"Z: {state.Z}, RotationZ: {state.RotationZ}\r\n");

                var payload = new
                {
                    stick = name,
                    axes = new
                    {
                        X = state.X,
                        Y = state.Y,
                        Z = state.RotationZ,
                        Slider = state.Sliders.FirstOrDefault()
                    },
                    pov = state.PointOfViewControllers[0],
                    buttons = state.Buttons.Take(16).ToArray()
                };

                string json = JsonSerializer.Serialize(payload);
                byte[] compressed = Compress(json);
                udpClient.Send(compressed, compressed.Length, remoteEP);

                // if (name == "left")
                // {
                //     _txtLog.AppendText($"[SENT {name}] {json}\r\n");
                // }
                // _txtLog.AppendText($"[SENT {name}] {json}\r\n");
            }
            catch (Exception ex)
            {
                _txtLog.AppendText($"Error sending {name} stick: {ex.Message}\r\n");
            }
        }
        
        byte[] Compress(string json)
        {
            var inputBytes = Encoding.UTF8.GetBytes(json);
            var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Fastest))
                gzip.Write(inputBytes, 0, inputBytes.Length);
            return output.ToArray();
        }
    }
    
    public class Config
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public int Frequency { get; set; }
    }
}