using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace TokenDump
{
    // Thanks ChatGPT
    internal sealed class ProcessScan
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        private const uint 
            PROCESS_VM_READ = 0x0010,
            PROCESS_QUERY_INFORMATION = 0x0400,
            MEM_COMMIT = 0x1000,
            PAGE_READWRITE = 0x04;

        private static List<string> ExtractStringsByRegex(byte[] data, Encoding encoding, Regex regex)
        {
            List<string> matchedStrings = new List<string>();
            StringBuilder currentString = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                char character = encoding.GetChars(new byte[] { data[i] })[0];

                if (char.IsLetterOrDigit(character) || char.IsPunctuation(character) || char.IsWhiteSpace(character))
                {
                    currentString.Append(character);
                }
                else
                {
                    if (currentString.Length > 0)
                    {
                        string result = currentString.ToString();
                        foreach (Match m in regex.Matches(result))
                        {
                            if (!matchedStrings.Contains(m.Value))
                            {
                                matchedStrings.Add(m.Value);
                            }
                        }
                    }
                    currentString.Clear();
                }
            }
            return matchedStrings;
        }

        public static string[] ScanProcessMemory(Process process, Encoding encoding, Regex regex)
        {
            List<string> results = new List<string>();
            IntPtr processHandle = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, process.Id);

            if (processHandle != IntPtr.Zero)
            {
                try
                {
                    IntPtr address = IntPtr.Zero;
                    MEMORY_BASIC_INFORMATION mbi = new MEMORY_BASIC_INFORMATION();

                    while (VirtualQueryEx(processHandle, address, out mbi, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) != 0)
                    {
                        if (mbi.State == MEM_COMMIT && (mbi.Protect & PAGE_READWRITE) == PAGE_READWRITE)
                        {
                            byte[] buffer = new byte[(long)mbi.RegionSize];
                            int bytesRead;

                            if (ReadProcessMemory(processHandle, mbi.BaseAddress, buffer, buffer.Length, out bytesRead) && bytesRead > 0)
                            {
                                results.AddRange(ExtractStringsByRegex(buffer, encoding, regex));
                            }
                        }

                        try
                        {
                            long nextAddress = mbi.BaseAddress.ToInt64() + mbi.RegionSize.ToInt64();

                            if (nextAddress < 0 || nextAddress > (IntPtr.Size == 4 ? int.MaxValue : long.MaxValue))
                            {
                                break;
                            }
                            address = IntPtr.Add(mbi.BaseAddress, (int)mbi.RegionSize.ToInt64());
                        }
                        catch (OverflowException)
                        {
                            break;
                        }
                    }

                }
                finally
                {
                    CloseHandle(processHandle);
                }
            }

            return results.ToArray();
        }
    }
}
