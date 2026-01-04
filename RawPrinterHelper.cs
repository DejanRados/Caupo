using System;
using System.Runtime.InteropServices;


namespace Caupo
{
    public class RawPrinterHelper
    {
        [StructLayout (LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFOA
        {
            [MarshalAs (UnmanagedType.LPStr)]
            public string pDocName;

            [MarshalAs (UnmanagedType.LPStr)]
            public string pOutputFile;

            [MarshalAs (UnmanagedType.LPStr)]
            public string pDataType;
        }

        [DllImport ("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true)]
        static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport ("winspool.Drv", SetLastError = true)]
        static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport ("winspool.Drv", SetLastError = true)]
        static extern bool StartDocPrinter(IntPtr hPrinter, int level, DOCINFOA di);

        [DllImport ("winspool.Drv", SetLastError = true)]
        static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport ("winspool.Drv", SetLastError = true)]
        static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport ("winspool.Drv", SetLastError = true)]
        static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport ("winspool.Drv", SetLastError = true)]
        static extern bool WritePrinter(IntPtr hPrinter, byte[] pBytes, int dwCount, out int dwWritten);

        public static void SendBytes(string printerName, byte[] bytes)
        {
            if(!OpenPrinter (printerName, out IntPtr hPrinter, IntPtr.Zero))
                throw new Exception ("Ne mogu otvoriti printer.");

            var docInfo = new DOCINFOA
            {
                pDocName = "ESC/POS Print",
                pDataType = "RAW"
            };

            if(!StartDocPrinter (hPrinter, 1, docInfo))
                throw new Exception ("StartDocPrinter nije uspio.");

            StartPagePrinter (hPrinter);
            WritePrinter (hPrinter, bytes, bytes.Length, out _);
            EndPagePrinter (hPrinter);
            EndDocPrinter (hPrinter);
            ClosePrinter (hPrinter);
        }
    }


}
