using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Magic;

namespace WoWLoginAutomator
{
    public class LoginAutomator
    {
        // - Imports for the SendMessage Windows interactions
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        // - Imports for the SendMessage Windows interactions

        public static uint worldLoaded = 0xBEBA40;
        public static uint gameState = 0xB6A9E0;
        public static uint characterSlotSelected = 0x6C436C;

        static void Main(string[] args)
        {
            Console.Title = "Logging into: " + args[2];

            if (args.Length < 4)
                Console.WriteLine("Error: to few arguments... Use it like this: ./WoW-LoginAutomator.exe ..PID.. ..CHARACTERSLOT.. ..USERNAME.. ..PASSWORD..");
            else
                DoLogin(Convert.ToInt32(args[0]), Convert.ToInt32(args[1]), args[2], args[3]);
        }

        public static void DoLogin(int pid, int characterSlot, string username, string password)
        {
            Process wow = GetWoWbyPID(pid);
            BlackMagic blackMagic = new BlackMagic(wow.Id);

            while (blackMagic.ReadInt(worldLoaded) != 1)
            {
                if (blackMagic.ReadInt(worldLoaded) != 1)
                    switch (blackMagic.ReadASCIIString(gameState, 10))
                    {
                        case "login":
                            Console.WriteLine("We are in Login Window");
                            Console.Write("Username Sending: ");
                            foreach (char c in username)
                            {
                                SendKeyToProcess(wow, c, Char.IsUpper(c));
                                Console.Write(c);
                                Thread.Sleep(100);
                            }
                            Console.Write("\n");
                            Thread.Sleep(100);
                            Console.WriteLine("Sending TAB");
                            SendKeyToProcess(wow, 0x09);
                            Thread.Sleep(100);
                            bool firstTime = true;
                            do
                            {
                                if (!firstTime)
                                {
                                    SendKeyToProcess(wow, 0x0D);
                                    Console.WriteLine("Sending ENTER");
                                }
                                Console.WriteLine("Writing Password: ");
                                foreach (char c in password)
                                {
                                    SendKeyToProcess(wow, c, Char.IsUpper(c));
                                    Console.Write("*");
                                    Thread.Sleep(100);
                                }
                                Console.Write("\n");
                                Thread.Sleep(500);
                                SendKeyToProcess(wow, 0x0D);
                                Console.WriteLine("Sending ENTER");
                                Thread.Sleep(2000);
                                firstTime = false;
                            } while (blackMagic.ReadASCIIString(gameState, 10) == "login");
                            break;

                        case "charselect":
                            Console.WriteLine("We are in Charselect Window");
                            int currentSlot = blackMagic.ReadInt((uint)blackMagic.MainModule.BaseAddress + characterSlotSelected);
                            while (currentSlot != characterSlot)
                            {
                                Console.WriteLine("Current selection: " + currentSlot);
                                SendKeyToProcess(wow, 0x28);
                                Thread.Sleep(200);
                                currentSlot = blackMagic.ReadInt((uint)blackMagic.MainModule.BaseAddress + characterSlotSelected);
                            }
                            Console.WriteLine("Sending ENTER");
                            SendKeyToProcess(wow, 0x0D);
                            break;

                        default:
                            break;
                    }
                Thread.Sleep(2000);
            }
            Console.WriteLine("Logged in");
        }

        private static void SendKeyToProcess(Process process, int c)
        {
            const uint KEYDOWN = 0x100;
            const uint KEYUP = 0x101;

            IntPtr windowHandle = process.MainWindowHandle;

            SendMessage(windowHandle, KEYDOWN, new IntPtr(c), new IntPtr(0));
            Thread.Sleep(new Random().Next(20, 40));
            SendMessage(windowHandle, KEYUP, new IntPtr(c), new IntPtr(0));
        }

        private static void SendKeyToProcess(Process process, int c, bool shift)
        {
            const uint KEYDOWN = 0x100;
            const uint KEYUP = 0x101;
            const uint WM_CHAR = 0x0102;

            IntPtr windowHandle = process.MainWindowHandle;

            if (shift)
                PostMessage(windowHandle, KEYDOWN, new IntPtr(0x10), new IntPtr(0));

            PostMessage(windowHandle, WM_CHAR, new IntPtr(c), new IntPtr(0));

            if (shift)
                PostMessage(windowHandle, KEYUP, new IntPtr(0x10), new IntPtr(0));
        }

        private static Process GetWoWbyPID(int pid)
        {
            List<Process> processList = new List<Process>(Process.GetProcessesByName("Wow"));

            foreach (Process p in processList)
                if (p.Id == pid)
                    return p;

            return processList[0];
        }
    }
}
