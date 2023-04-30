using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ProcessWatcher
{
    class Program
    {
        private static readonly string processToWatch = "dwm.exe";

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenThread(ThreadAccessFlags dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint ResumeThread(IntPtr hThread);

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            SuspendResume = 0x0002,
        }

        [Flags]
        private enum ThreadAccessFlags : uint
        {
            SuspendResume = 0x0002,
        }

        

        static void Main(string[] args)
        {
            // Set the priority of the current process to high
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

            Process targetProcess = GetProcessByName(processToWatch);
            if (targetProcess != null)
            {
                targetProcess.EnableRaisingEvents = true;
                targetProcess.Exited += TargetProcess_Exited;
                Console.WriteLine($"Watching for {processToWatch} termination...");
                Console.ReadLine();
            }
            else
            {
                Console.WriteLine($"{processToWatch} not found.");
            }
           
            
        }

        private static async void TargetProcess_Exited(object sender, EventArgs e)
        {
            Thread.Sleep(2000);
            SuspendProcessByName("winlogon.exe");
            TerminateProcessByName("ApplicationFrameHost.exe");
            TerminateProcessByName("RuntimeBroker.exe");
            TerminateProcessByName("ShellExperienceHost.exe");
            TerminateProcessByName("SystemSettings.exe");

        }

        private static Process GetProcessByName(string processName)
        {
            return Process.GetProcessesByName(processName.Replace(".exe", "")).FirstOrDefault();
        }

        private static void TerminateProcessByName(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName.Replace(".exe", ""));
            foreach (Process process in processes)
            {
                try
                {
                    process.Kill();
                    Console.WriteLine($"Terminated {processName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to terminate {processName}: {ex.Message}");
                }
            }
        }

        private static void SuspendProcessByName(string processName)
        {
            Process process = GetProcessByName(processName);
            if (process != null)
            {
                IntPtr hProcess = OpenProcess(ProcessAccessFlags.SuspendResume, false, process.Id);
                if (hProcess != IntPtr.Zero)
                {
                    foreach (ProcessThread thread in process.Threads)
                    {
                        IntPtr hThread = OpenThread(ThreadAccessFlags.SuspendResume, false, (uint)thread.Id);
                        if (hThread != IntPtr.Zero)
                        {
                            SuspendThread(hThread);
                            CloseHandle(hThread);
                        }
                    }
                    CloseHandle(hProcess);
                }
            }
        }
    }
}