using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Caupo.Services
{
    public enum KitchenKeyCommand
    {
        PreviousOrder,
        NextOrder,
        PreviousItem,
        NextItem,
        Finish
    }

    public sealed class KitchenKeyboardService : IDisposable
    {
        private const int WM_INPUT = 0x00FF;

        private const uint RIM_TYPEKEYBOARD = 1;
        private const uint RID_INPUT = 0x10000003;
        private const uint RIDI_DEVICENAME = 0x20000007;

        private const uint RIDEV_INPUTSINK = 0x00000100;
        private const uint RIDEV_REMOVE = 0x00000001;

        private const ushort RI_KEY_BREAK = 0x0001;

        private const ushort VK_F1 = 0x70;
        private const ushort VK_F2 = 0x71;
        private const ushort VK_F3 = 0x72;
        private const ushort VK_F4 = 0x73;
        private const ushort VK_F5 = 0x74;

        private readonly string _keyboardIdentifier;
        private readonly Dispatcher _dispatcher;

        private HwndSource? _source;
        private IntPtr _windowHandle;

        private bool _started;
        private bool _disposed;
        private bool _f5Pressed;

        public event EventHandler<KitchenKeyCommand>? CommandReceived;

        public bool IsStarted => _started;

        public KitchenKeyboardService(
            string keyboardIdentifier = "VID_0461&PID_554A")
        {
            if (string.IsNullOrWhiteSpace(keyboardIdentifier))
                throw new ArgumentException(
                    "Identifikator tastature nije definisan.",
                    nameof(keyboardIdentifier));

            _keyboardIdentifier = keyboardIdentifier;
            _dispatcher = Application.Current.Dispatcher;
        }

        public void Start(Window window)
        {
            if (_disposed)
                throw new ObjectDisposedException(
                    nameof(KitchenKeyboardService));

            if (window == null)
                throw new ArgumentNullException(nameof(window));

            _dispatcher.VerifyAccess();

            if (_started)
                return;

            IntPtr hwnd = new WindowInteropHelper(window).Handle;

            if (hwnd == IntPtr.Zero)
                throw new InvalidOperationException(
                    "Windows prozor još nema kreiran HWND. " +
                    "Pokreni servis nakon SourceInitialized.");

            HwndSource? source = HwndSource.FromHwnd(hwnd);

            if (source == null)
                throw new InvalidOperationException(
                    "Nije moguće pronaći HwndSource prozora.");

            var devices = new[]
            {
                new RAWINPUTDEVICE
                {
                    usUsagePage = 0x01,
                    usUsage = 0x06,
                    dwFlags = RIDEV_INPUTSINK,
                    hwndTarget = hwnd
                }
            };

            if (!RegisterRawInputDevices(
                    devices,
                    (uint)devices.Length,
                    (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
            {
                int error = Marshal.GetLastWin32Error();

                throw new InvalidOperationException(
                    $"Registracija kuhinjske tastature nije uspjela. " +
                    $"Win32 greška: {error}");
            }

            _source = source;
            _windowHandle = hwnd;
            _f5Pressed = false;

            _source.AddHook(WndProc);

            _started = true;
        }

        public void Stop()
        {
            _dispatcher.VerifyAccess();

            if (!_started)
                return;

            _source?.RemoveHook(WndProc);

            var devices = new[]
            {
                new RAWINPUTDEVICE
                {
                    usUsagePage = 0x01,
                    usUsage = 0x06,
                    dwFlags = RIDEV_REMOVE,
                    hwndTarget = IntPtr.Zero
                }
            };

            if (!RegisterRawInputDevices(
                    devices,
                    (uint)devices.Length,
                    (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
            {
                int error = Marshal.GetLastWin32Error();

                System.Diagnostics.Debug.WriteLine(
                    $"[KITCHEN KEYBOARD] Odjava Raw Input nije uspjela: {error}");
            }

            _source = null;
            _windowHandle = IntPtr.Zero;
            _f5Pressed = false;
            _started = false;
        }

        private IntPtr WndProc(
            IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (msg == WM_INPUT)
                ProcessRawInput(lParam);

            // Ne označavamo WM_INPUT kao handled.
            // Windows nastavlja standardnu obradu tastature.
            return IntPtr.Zero;
        }

        private void ProcessRawInput(IntPtr rawInputHandle)
        {
            uint size = 0;

            uint headerSize =
                (uint)Marshal.SizeOf<RAWINPUTHEADER>();

            uint result = GetRawInputData(
                rawInputHandle,
                RID_INPUT,
                IntPtr.Zero,
                ref size,
                headerSize);

            if (result == uint.MaxValue || size == 0)
                return;

            IntPtr buffer = Marshal.AllocHGlobal(
                checked((int)size));

            try
            {
                result = GetRawInputData(
                    rawInputHandle,
                    RID_INPUT,
                    buffer,
                    ref size,
                    headerSize);

                if (result == uint.MaxValue || result != size)
                    return;

                RAWINPUTHEADER header =
                    Marshal.PtrToStructure<RAWINPUTHEADER>(buffer);

                if (header.dwType != RIM_TYPEKEYBOARD)
                    return;

                IntPtr keyboardPtr = IntPtr.Add(
                    buffer,
                    Marshal.SizeOf<RAWINPUTHEADER>());

                RAWKEYBOARD keyboard =
                    Marshal.PtrToStructure<RAWKEYBOARD>(keyboardPtr);

                if (!TryGetCommand(
                        keyboard.VKey,
                        out KitchenKeyCommand command))
                    return;

                string deviceName =
                    GetDeviceName(header.hDevice);

                if (!deviceName.Contains(
                        _keyboardIdentifier,
                        StringComparison.OrdinalIgnoreCase))
                    return;

                bool isKeyRelease =
                    (keyboard.Flags & RI_KEY_BREAK) != 0;

                // F5: jedna komanda po fizičkom pritisku.
                if (keyboard.VKey == VK_F5)
                {
                    if (isKeyRelease)
                    {
                        _f5Pressed = false;
                        return;
                    }

                    if (_f5Pressed)
                        return;

                    _f5Pressed = true;
                    CommandReceived?.Invoke(this, command);
                    return;
                }

                // F1-F4: zadržavamo postojeće ponavljanje.
                if (isKeyRelease)
                    return;

                CommandReceived?.Invoke(this, command);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static bool TryGetCommand(
            ushort virtualKey,
            out KitchenKeyCommand command)
        {
            switch (virtualKey)
            {
                case VK_F1:
                    command = KitchenKeyCommand.PreviousOrder;
                    return true;

                case VK_F2:
                    command = KitchenKeyCommand.NextOrder;
                    return true;

                case VK_F3:
                    command = KitchenKeyCommand.PreviousItem;
                    return true;

                case VK_F4:
                    command = KitchenKeyCommand.NextItem;
                    return true;

                case VK_F5:
                    command = KitchenKeyCommand.Finish;
                    return true;

                default:
                    command = default;
                    return false;
            }
        }

        private static string GetDeviceName(IntPtr deviceHandle)
        {
            if (deviceHandle == IntPtr.Zero)
                return string.Empty;

            uint size = 0;

            uint result = GetRawInputDeviceInfo(
                deviceHandle,
                RIDI_DEVICENAME,
                IntPtr.Zero,
                ref size);

            if (result == uint.MaxValue || size == 0)
                return string.Empty;

            IntPtr buffer = Marshal.AllocHGlobal(
                checked((int)size * sizeof(char)));

            try
            {
                result = GetRawInputDeviceInfo(
                    deviceHandle,
                    RIDI_DEVICENAME,
                    buffer,
                    ref size);

                if (result == uint.MaxValue)
                    return string.Empty;

                return Marshal.PtrToStringUni(buffer)
                    ?? string.Empty;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Stop();

            CommandReceived = null;
            _disposed = true;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWKEYBOARD
        {
            public ushort MakeCode;
            public ushort Flags;
            public ushort Reserved;
            public ushort VKey;
            public uint Message;
            public uint ExtraInformation;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(
            RAWINPUTDEVICE[] devices,
            uint numDevices,
            uint size);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputData(
            IntPtr rawInput,
            uint command,
            IntPtr data,
            ref uint size,
            uint headerSize);

        [DllImport(
            "user32.dll",
            EntryPoint = "GetRawInputDeviceInfoW",
            CharSet = CharSet.Unicode,
            SetLastError = true)]
        private static extern uint GetRawInputDeviceInfo(
            IntPtr device,
            uint command,
            IntPtr data,
            ref uint size);
    }
}