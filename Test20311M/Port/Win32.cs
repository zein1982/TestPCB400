using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Test20311M
{
    public static class Win32
    {
        /// <summary>
        /// Отправка оконного сообщения
        /// </summary>
        /// <param name="hWnd">Дескриптор окна, котророе должно получить сообщение</param>
        /// <param name="Msg">Сообщение</param>
        /// <param name="wParam">Параметр сообщения</param>
        /// <param name="lParam">Параметр сообщения</param>
        /// <returns>Код завершения задачи:
        /// 0 - ошибка;
        /// другие значения - успех</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetCommState(IntPtr hFile, /*[In] ref*/ DCB lpDCB);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetCommTimeouts(IntPtr hFile, [In] ref COMMTIMEOUTS lpCommTimeouts);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupComm(IntPtr hFile, int dwInQueue, int dwOutQueue);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FlushFileBuffers(IntPtr hFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PurgeComm(IntPtr hFile, int dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetOverlappedResult(IntPtr hFile, ref OVERLAPPED lpOverlapped, out uint lpNumberOfBytesTransferred, bool bWait);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ClearCommError(IntPtr hFile, out uint lpErrors, out COMSTAT lpStat);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateEvent(IntPtr SecurityAttributes, int bManualReset, int bInitialState, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, ref OVERLAPPED lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, ref OVERLAPPED lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDistribution,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile
        );

        /// <summary>
        /// Регистрация собственного оконного сообщения Windows
        /// </summary>
        /// <param name="lpString">Регистрируемое сообщение в виде строки</param>
        /// <returns>Код сообщения</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int RegisterWindowMessage(string lpString);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SetupDiGetClassDevs(IntPtr ClassGuid, [MarshalAs(UnmanagedType.LPTStr)] string Enumerator, IntPtr hwndParent, uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        /// <summary>
        /// The SetupDiGetDeviceRegistryProperty function retrieves the specified device property.
        /// This handle is typically returned by the SetupDiGetClassDevs or SetupDiGetClassDevsEx function.
        /// </summary>
        /// <param Name="DeviceInfoSet">Handle to the device information set that contains the interface and its underlying device.</param>
        /// <param Name="DeviceInfoData">Pointer to an SP_DEVINFO_DATA structure that defines the device instance.</param>
        /// <param Name="Property">Device property to be retrieved. SEE MSDN</param>
        /// <param Name="PropertyRegDataType">Pointer to a variable that receives the registry progData Type. This parameter can be NULL.</param>
        /// <param Name="PropertyBuffer">Pointer to a buffer that receives the requested device property.</param>
        /// <param Name="PropertyBufferSize">Size of the buffer, in bytes.</param>
        /// <param Name="RequiredSize">Pointer to a variable that receives the required buffer size, in bytes. This parameter can be NULL.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            uint Property,
            out UInt32 PropertyRegDataType,
            byte[] PropertyBuffer,
            uint PropertyBufferSize,
            out UInt32 RequiredSize
        );

        [DllImport("setupapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [StructLayout(LayoutKind.Sequential)]
        public class DCB
        {
            private UInt32 DCBlength;
            public UInt32 BaudRate;
            private BitVector32 Control;
            public UInt16 wReserved;
            public UInt16 XonLim;
            public UInt16 XoffLim;
            public byte ByteSize;
            public ParityFlags Parity;
            public StopBitsFlags StopBits;
            public sbyte XonChar;
            public sbyte XoffChar;
            public sbyte ErrorChar;
            public sbyte EofChar;
            public sbyte EvtChar;
            public UInt16 wReserved1;
            private readonly BitVector32.Section sect1;
            private readonly BitVector32.Section DTRsect;
            private readonly BitVector32.Section sect2;
            private readonly BitVector32.Section RTSsect;

            public DCB()
            {
                //
                // Initialize the length of the structure. Marshal.SizeOf returns
                // the size of the unmanaged object (basically the object that
                // gets marshalled).
                //
                this.DCBlength = (uint)Marshal.SizeOf(this);
                // initialize BitVector32
                Control = new BitVector32(0);
                // of the following 4 sections only 2 are needed
                sect1 = BitVector32.CreateSection(0x0f);
                // this is where the DTR setting is stored
                DTRsect = BitVector32.CreateSection(3, sect1);
                sect2 = BitVector32.CreateSection(0x3f, DTRsect);
                // this is where the RTS setting is stored
                RTSsect = BitVector32.CreateSection(3, sect2);
            }

            //
            // We need to have to define reserved fields in the DCB class definition
            // to preserve the size of the 
            // underlying structure to match the Win32 structure when it is 
            // marshaled. Use these fields to suppress compiler warnings.
            //

            private void _SuppressCompilerWarnings()
            {
                wReserved += 0;
                wReserved1 += 0;
            }

            // Helper constants for manipulating the bit fields.
            // these are defined as an enum in order to preserve memory

            [Flags]
            enum ctrlBit
            {
                fBinaryMask = 0x001,
                fParityMask = 0x0002,
                fOutxCtsFlowMask = 0x0004,
                fOutxDsrFlowMask = 0x0008,
                fDtrControlMask = 0x0030,
                fDsrSensitivityMask = 0x0040,
                fTXContinueOnXoffMask = 0x0080,
                fOutXMask = 0x0100,
                fInXMask = 0x0200,
                fErrorCharMask = 0x0400,
                fNullMask = 0x0800,
                fRtsControlMask = 0x3000,
                fAbortOnErrorMask = 0x4000
            }

            // get and set of bool works with the underlying BitVector32
            // by using a mask for each bit field we can let the compiler
            // and JIT do the work
            //

            public bool fBinary
            {
                get { return (Control[(int)ctrlBit.fBinaryMask]); }
                set { Control[(int)ctrlBit.fBinaryMask] = value; }
            }
            public bool fParity
            {
                get { return (Control[(int)ctrlBit.fParityMask]); }
                set { Control[(int)ctrlBit.fParityMask] = value; }
            }
            public bool fOutxCtsFlow
            {
                get { return (Control[(int)ctrlBit.fOutxCtsFlowMask]); }
                set { Control[(int)ctrlBit.fOutxCtsFlowMask] = value; }
            }
            public bool fOutxDsrFlow
            {
                get { return (Control[(int)ctrlBit.fOutxDsrFlowMask]); }
                set { Control[(int)ctrlBit.fOutxDsrFlowMask] = value; }
            }

            // we have to use a segment because the width of the underlying information
            // is wider than just one bit
            public DtrControlFlags fDtrControl
            {
                get { return (DtrControlFlags)Control[DTRsect]; }
                set { Control[DTRsect] = (int)value; }
            }
            public bool fDsrSensitivity
            {
                get { return Control[(int)ctrlBit.fDsrSensitivityMask]; }
                set { Control[(int)ctrlBit.fDsrSensitivityMask] = value; }
            }
            public bool fTXContinueOnXoff
            {
                get { return Control[(int)ctrlBit.fTXContinueOnXoffMask]; }
                set { Control[(int)ctrlBit.fTXContinueOnXoffMask] = value; }
            }
            public bool fOutX
            {
                get { return Control[(int)ctrlBit.fOutXMask]; }
                set { Control[(int)ctrlBit.fOutXMask] = value; }
            }
            public bool fInX
            {
                get { return Control[(int)ctrlBit.fInXMask]; }
                set { Control[(int)ctrlBit.fInXMask] = value; }
            }
            public bool fErrorChar
            {
                get { return Control[(int)ctrlBit.fErrorCharMask]; }
                set { Control[(int)ctrlBit.fErrorCharMask] = value; }
            }
            public bool fNull
            {
                get { return Control[(int)ctrlBit.fNullMask]; }
                set { Control[(int)ctrlBit.fNullMask] = value; }
            }

            // we have to use a segment because the width of the underlying information
            // is wider than just one bit
            public RtsControlFlags fRtsControl
            {
                get { return (RtsControlFlags)Control[RTSsect]; }
                set { Control[RTSsect] = (int)value; }
            }

            public bool fAbortOnError
            {
                get { return Control[(int)ctrlBit.fAbortOnErrorMask]; }
                set { Control[(int)ctrlBit.fAbortOnErrorMask] = value; }
            }

            //
            // Method to dump the DCB to take a look and help debug issues.
            //

            public override String ToString()
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("DCB:\r\n");
                sb.AppendFormat(null, "  BaudRate:     {0}\r\n", BaudRate);
                sb.AppendFormat(null, "  Control:      0x{0:x}\r\n", Control.Data);
                sb.AppendFormat(null, "    fBinary:           {0}\r\n", fBinary);
                sb.AppendFormat(null, "    fParity:           {0}\r\n", fParity);
                sb.AppendFormat(null, "    fOutxCtsFlow:      {0}\r\n", fOutxCtsFlow);
                sb.AppendFormat(null, "    fOutxDsrFlow:      {0}\r\n", fOutxDsrFlow);
                sb.AppendFormat(null, "    fDtrControl:       {0}\r\n", fDtrControl);
                sb.AppendFormat(null, "    fDsrSensitivity:   {0}\r\n", fDsrSensitivity);
                sb.AppendFormat(null, "    fTXContinueOnXoff: {0}\r\n", fTXContinueOnXoff);
                sb.AppendFormat(null, "    fOutX:             {0}\r\n", fOutX);
                sb.AppendFormat(null, "    fInX:              {0}\r\n", fInX);
                sb.AppendFormat(null, "    fNull:             {0}\r\n", fNull);
                sb.AppendFormat(null, "    fRtsControl:       {0}\r\n", fRtsControl);
                sb.AppendFormat(null, "    fAbortOnError:     {0}\r\n", fAbortOnError);
                sb.AppendFormat(null, "  XonLim:       {0}\r\n", XonLim);
                sb.AppendFormat(null, "  XoffLim:      {0}\r\n", XoffLim);
                sb.AppendFormat(null, "  ByteSize:     {0}\r\n", ByteSize);
                sb.AppendFormat(null, "  Parity:       {0}\r\n", Parity);
                sb.AppendFormat(null, "  StopBits:     {0}\r\n", StopBits);
                sb.AppendFormat(null, "  XonChar:      {0}\r\n", XonChar);
                sb.AppendFormat(null, "  XoffChar:     {0}\r\n", XoffChar);
                sb.AppendFormat(null, "  ErrorChar:    {0}\r\n", ErrorChar);
                sb.AppendFormat(null, "  EofChar:      {0}\r\n", EofChar);
                sb.AppendFormat(null, "  EvtChar:      {0}\r\n", EvtChar);

                return sb.ToString();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct COMMTIMEOUTS
        {
            public UInt32 ReadIntervalTimeout;
            public UInt32 ReadTotalTimeoutMultiplier;
            public UInt32 ReadTotalTimeoutConstant;
            public UInt32 WriteTotalTimeoutMultiplier;
            public UInt32 WriteTotalTimeoutConstant;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OVERLAPPED
        {
            public IntPtr Internal;
            public IntPtr InternalHigh;
            public int Offset;
            public int OffsetHigh;
            public IntPtr hEvent;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct COMSTAT
        {
            public UInt32 Flags;
            public UInt32 cbInQue;
            public UInt32 cbOutQue;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        public enum DtrControlFlags : int
        {
            Disable = 0,
            Enable = 1,
            Handshake = 2
        }
        public enum RtsControlFlags : int
        {
            Disable = 0,
            Enable = 1,
            Handshake = 2,
            Toggle = 3
        }
        public enum ParityFlags : byte
        {
            None = 0,
            Odd = 1,
            Even = 2,
            Mark = 3,
            Space = 4
        }
        public enum StopBitsFlags : byte
        {
            One = 0,
            OnePointFive = 1,
            Two = 2,
        }
    }
}
