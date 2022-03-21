using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;

namespace CoreOwnage
{
    class CoreKeeperMain
    {
        static void Main()
        {
            Console.WriteLine("[+] Searching for \"CoreKeeper\" process . . .");   
            try
            {
                Process CoreKeeperProcess = Process.GetProcessesByName("CoreKeeper")[0];
                Pretty.Success("[+] CoreKeeper is running with PID " + CoreKeeperProcess.Id);
                Console.WriteLine("[+] Beginning injection process");
                Payload.insert(CoreKeeperProcess);
            }
            catch (Exception)
            {
                Pretty.Fail("[x] Could not find CoreKeeper.");
                Console.WriteLine("[+] Exiting...");
                Thread.Sleep(5000);
                return;
            }
            
        }
    }

    class Payload
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint dwFreeType);


        public static void insert(Process CoreKeeperProcess)
        {
            string path = Directory.GetCurrentDirectory();
            path += "\\chlorine.dll";
            Pretty.Fail("Loading DLL: " + path);

            // Create handle to our process based on PID. 
            IntPtr handle = OpenProcess(0x001F0FF, false, CoreKeeperProcess.Id);
            

            // Address our DLL will be loaded to via LoadLibraryW
            IntPtr LibraryAddress = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            // Allocate memory & write DLL to memory
            IntPtr AllocatedMemory = VirtualAllocEx(handle, IntPtr.Zero, (uint)path.Length + 1, 0x00001000, 4);
            UIntPtr bytesWritten;
           WriteProcessMemory(handle, AllocatedMemory, Encoding.Default.GetBytes(path), (uint)path.Length + 1, out bytesWritten).ToString();
           
            // Start thread in corekeeper context
            IntPtr threadHandle = CreateRemoteThread(handle, IntPtr.Zero, 0, LibraryAddress, AllocatedMemory, 0, IntPtr.Zero);
            WaitForSingleObject(handle, 0xFFFFFFFF);

            Pretty.Fail("Handle: " + handle.ToString());
            Pretty.Fail("Allocated Mmry: " + AllocatedMemory.ToString());
            Pretty.Fail("LoadLibraryW Add: " + LibraryAddress.ToString());
            Pretty.Fail("Thread Handle: " + threadHandle.ToString());
            Thread.Sleep(1000);

            // Close handle to DLL & deallocate memory
            CloseHandle(threadHandle);
            VirtualFreeEx(handle, AllocatedMemory, path.Length + 1, 0x8000);

            CloseHandle(handle);
            Pretty.Success("[+] chlorine.dll injected");
        }
    }

    class Pretty
    {
        public static void Success(string text)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.Gray;
            return;
        }
        public static void Fail(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.Gray;
            return;
        }

    }
}