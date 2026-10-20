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
        Finish,
        Undo,
        ConfirmNewOrder
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
        private const ushort VK_F6 = 0x75;
        private const ushort VK_F7 = 0x76;
        private const ushort VK_F11 = 0x7A;
        private const ushort VK_F12 = 0x7B;

        private readonly Dispatcher _dispatcher;
        private HwndSource? _source;
        private bool _started;
        private bool _disposed;
        private bool _f5Pressed;
        private bool _f6Pressed;
        private bool _f7Pressed;
        private bool _f11Pressed;
        private bool _f12Pressed;
        private bool _capturing;
        private string _keyboardIdentifier;

        public static KitchenKeyboardService? Active { get; private set; }

        public event EventHandler<KitchenKeyCommand>? CommandReceived;
        public event EventHandler<string>? KeyboardDetected;
        public event Action<int, bool>? ScrollKeyChanged;

        public bool IsStarted => _started;
        public bool IsCapturing => _capturing;

        public KitchenKeyboardService()
        {
            _dispatcher = Application.Current.Dispatcher;
            _keyboardIdentifier =
                Properties.Settings.Default.KitchenKeyboardId
                ?? string.Empty;
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

            if (Active != null && Active != this)
                throw new InvalidOperationException(
                    "Raw Input servis tastature već je aktivan.");

            IntPtr hwnd = new WindowInteropHelper(window).Handle;

            if (hwnd == IntPtr.Zero)
                throw new InvalidOperationException(
                    "Windows prozor još nema kreiran HWND.");

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
                    "Registracija Raw Input tastature nije uspjela. " +
                    $"Win32 greška: {error}");
            }

            _source = source;
            _source.AddHook(WndProc);
            _f5Pressed = false;
            _f6Pressed = false;
            _f7Pressed = false;
            _f11Pressed = false;
            _f12Pressed = false;
            _started = true;
            Active = this;
        }

        public void BeginKeyboardSelection()
        {
            _dispatcher.VerifyAccess();

            if (!_started)
                throw new InvalidOperationException(
                    "Raw Input servis nije pokrenut.");

            if (_capturing)
                throw new InvalidOperationException(
                    "Odabir tastature već je pokrenut.");

            _capturing = true;
        }

        public void CancelKeyboardSelection()
        {
            _dispatcher.VerifyAccess();
            _capturing = false;
        }

        public void SetKeyboardIdentifier(string identifier)
        {
            _dispatcher.VerifyAccess();

            _keyboardIdentifier = identifier ?? string.Empty;
            _f5Pressed = false;
            ReleaseScrollKeys();
            _f11Pressed = false;
            _f12Pressed = false;
        }

        public void Stop()
        {
            _dispatcher.VerifyAccess();

            if (!_started)
                return;

            _capturing = false;
            ReleaseScrollKeys();
            _source?.RemoveHook(WndProc);

            if (Active == this)
            {
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
                    System.Diagnostics.Debug.WriteLine(
                        "[KITCHEN KEYBOARD] Raw Input odjava nije uspjela: " +
                        Marshal.GetLastWin32Error());
                }

                Active = null;
            }

            _source = null;
            _f5Pressed = false;
            _f11Pressed = false;
            _f12Pressed = false;
            _started = false;
        }

        private void ReleaseScrollKeys()
        {
            if (_f6Pressed)
            {
                _f6Pressed = false;
                ScrollKeyChanged?.Invoke(1, false);
            }

            if (_f7Pressed)
            {
                _f7Pressed = false;
                ScrollKeyChanged?.Invoke(-1, false);
            }
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
                    Marshal.PtrToStructure<RAWKEYBOARD>(
                        keyboardPtr);

                bool isKeyRelease =
                    (keyboard.Flags & RI_KEY_BREAK) != 0;

                if (_capturing)
                {
                    if (keyboard.VKey != VK_F1 ||
                        isKeyRelease)
                        return;

                    string detectedId =
                        GetDeviceName(header.hDevice);

                    if (string.IsNullOrWhiteSpace(detectedId))
                        return;

                    _capturing = false;
                    KeyboardDetected?.Invoke(this, detectedId);
                    return;
                }

                bool isScrollKey = keyboard.VKey == VK_F6 || keyboard.VKey == VK_F7;

                if (!isScrollKey && !TryGetCommand(keyboard.VKey, out _))
                    return;

                if (string.IsNullOrWhiteSpace(
                    _keyboardIdentifier))
                    return;

                string deviceName =
                    GetDeviceName(header.hDevice);

                if (!string.Equals(
                    deviceName,
                    _keyboardIdentifier,
                    StringComparison.OrdinalIgnoreCase))
                    return;

                if (isScrollKey)
                {
                    bool pressed = keyboard.VKey == VK_F6 ? _f6Pressed : _f7Pressed;

                    if (isKeyRelease)
                    {
                        if (!pressed)
                            return;

                        if (keyboard.VKey == VK_F6)
                            _f6Pressed = false;
                        else
                            _f7Pressed = false;

                        ScrollKeyChanged?.Invoke(keyboard.VKey == VK_F6 ? 1 : -1, false);
                        return;
                    }

                    if (pressed)
                        return;

                    if (keyboard.VKey == VK_F6)
                        _f6Pressed = true;
                    else
                        _f7Pressed = true;

                    ScrollKeyChanged?.Invoke(keyboard.VKey == VK_F6 ? 1 : -1, true);
                    return;
                }

                if (!TryGetCommand(keyboard.VKey, out KitchenKeyCommand command))
                    return;

                if (keyboard.VKey == VK_F5 || keyboard.VKey == VK_F11 || keyboard.VKey == VK_F12)
                {
                    bool pressed = keyboard.VKey switch
                    {
                        VK_F5 => _f5Pressed,
                        VK_F11 => _f11Pressed,
                        VK_F12 => _f12Pressed,
                        _ => false
                    };

                    if (isKeyRelease)
                    {
                        switch (keyboard.VKey)
                        {
                            case VK_F5: _f5Pressed = false; break;
                            case VK_F11: _f11Pressed = false; break;
                            case VK_F12: _f12Pressed = false; break;
                        }

                        return;
                    }

                    if (pressed)
                        return;

                    switch (keyboard.VKey)
                    {
                        case VK_F5: _f5Pressed = true; break;
                        case VK_F11: _f11Pressed = true; break;
                        case VK_F12: _f12Pressed = true; break;
                    }

                    CommandReceived?.Invoke(this, command);
                    return;
                }

                if (isKeyRelease)
                    return;

                CommandReceived?.Invoke(this, command);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static bool TryGetCommand(ushort virtualKey, out KitchenKeyCommand command)
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

                case VK_F11:
                    command = KitchenKeyCommand.Undo;
                    return true;

                case VK_F12:
                    command = KitchenKeyCommand.ConfirmNewOrder;
                    return true;

                default:
                    command = default;
                    return false;
            }
        }

        private static string GetDeviceName(
            IntPtr deviceHandle)
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
            KeyboardDetected = null;
            ScrollKeyChanged = null;
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