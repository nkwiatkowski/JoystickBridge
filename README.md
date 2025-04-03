# GhostStick 🎮👻

**Seamless remote joystick mirroring over network**  
Control your Shadow PC (or any VM) using physical joysticks from your main PC — with full vJoy integration and recovery from device disconnections.

---

## ✨ Features

- 🔄 Mirrors joystick input from host to remote via UDP
- 🎮 Full support for dual Thrustmaster T.16000M
- 🧠 Maps X, Y, Z (rotation), Slider, POV hat and buttons
- 🕹️ vJoy support for virtual joystick devices on the VM
- ⚡ Supports configurable update rate (e.g. 60Hz, 30Hz, 10Hz)
- 🗜️ Optional payload compression to reduce latency
- 💥 Works around Shadow’s USB instability
- ✅ Reliable recovery from joystick reconnects without restarting the game

---

## 📦 Components

### Sender App (runs on your main PC)
- Windows Forms app using SharpDX
- Reads joystick input
- Sends compressed JSON over UDP to the receiver

### Receiver App (runs on Shadow PC / VM)
- Listens for UDP joystick input
- Uses `vJoyInterfaceWrap.dll` to control virtual joysticks
- Maps axes, buttons, and POV input to vJoy

---

## 🛠️ Setup

### ✅ Requirements

- Windows on both PCs
- [vJoy](https://sourceforge.net/projects/vjoystick/) installed on the VM
- [ZeroTier](https://www.zerotier.com/) or similar for network bridging
- Open UDP port (default: `25555`)
- .NET Framework 4.8 or newer

### ⚙️ Config

Place a `config.json` file next to the `.exe`:

```json
{
  "ip": "172.23.172.215", // Your Shadow PC's IP
  "port": 25555,
  "frequency": 60         // Polling frequency in Hz
}
