using Magic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace WoWLoginAutomator
{
    public class LoginAutomator
    {
        public const uint KEYDOWN = 0x100;
        public const uint KEYUP = 0x101;
        public const uint WM_CHAR = 0x0102;

        public static uint characterSlotSelected = 0x6C436C;
        public static uint gameState = 0xB6A9E0;
        public static uint worldLoaded = 0xBEBA40;

        /// <summary>
        /// Login into WoW
        /// </summary>
        /// <param name="pid">Process ID</param>
        /// <param name="characterSlot">Character slot to select 0-9</param>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        public static void DoLogin(int pid, int characterSlot, string username, string password)
        {
            Process wow = GetWoWbyPID(pid);
            BlackMagic blackMagic = new BlackMagic(wow.Id);

            while (blackMagic.ReadInt(worldLoaded) != 1)
            {
                if (blackMagic.ReadInt(worldLoaded) != 1)
                {
                    switch (blackMagic.ReadASCIIString(gameState, 10))
                    {
                        case "login":
                            HandleLogin(blackMagic, wow, username, password);
                            break;

                        case "charselect":
                            HandleCharSelect(blackMagic, wow, characterSlot);
                            break;

                        default:
                            break;
                    }
                }

                Thread.Sleep(2000);
            }
            Console.WriteLine("Logged in");
        }

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private static Process GetWoWbyPID(int pid)
        {
            List<Process> processList = new List<Process>(Process.GetProcessesByName("Wow"));

            foreach (Process p in processList)
            {
                if (p.Id == pid)
                {
                    return p;
                }
            }

            return processList[0];
        }

        private static void HandleCharSelect(BlackMagic blackMagic, Process wow, int characterSlot)
        {
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
        }

        private static void HandleLogin(BlackMagic blackMagic, Process wow, string username, string password)
        {
            Console.WriteLine("We are in Login Window");
            Console.Write("Username Sending: ");
            foreach (char c in username)
            {
                SendKeyToProcess(wow, c, char.IsUpper(c));
                Console.Write(c);
                Thread.Sleep(10);
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
                    SendKeyToProcess(wow, c, char.IsUpper(c));
                    Console.Write("*");
                    Thread.Sleep(10);
                }
                Console.Write("\n");
                Thread.Sleep(500);
                SendKeyToProcess(wow, 0x0D);
                Console.WriteLine("Sending ENTER");
                Thread.Sleep(3000);
                firstTime = false;
            } while (blackMagic.ReadASCIIString(gameState, 10) == "login");
        }

        private static void Main(string[] args)
        {
            Console.Title = "Logging into: ACCOUNTNAME";
            if (args.Length < 4)
            {
                Console.WriteLine("Error: to few arguments... Use it like this: ./WoW-LoginAutomator.exe ..PID.. ..CHARACTERSLOT.. ..USERNAME.. ..PASSWORD..");
                Console.ReadKey();
            }
            else
            {
                Console.Title = "Logging into: " + args[2];
                DoLogin(Convert.ToInt32(args[0]), Convert.ToInt32(args[1]), args[2], args[3]);
            }
        }

        private static void SendKeyToProcess(Process process, int c)
        {
            IntPtr windowHandle = process.MainWindowHandle;

            SendMessage(windowHandle, KEYDOWN, new IntPtr(c), new IntPtr(0));
            Thread.Sleep(new Random().Next(20, 40));
            SendMessage(windowHandle, KEYUP, new IntPtr(c), new IntPtr(0));
        }

        private static void SendKeyToProcess(Process process, int c, bool shift)
        {
            IntPtr windowHandle = process.MainWindowHandle;

            if (shift)
            {
                PostMessage(windowHandle, KEYDOWN, new IntPtr(0x10), new IntPtr(0));
            }

            PostMessage(windowHandle, WM_CHAR, new IntPtr(c), new IntPtr(0));

            if (shift)
            {
                PostMessage(windowHandle, KEYUP, new IntPtr(0x10), new IntPtr(0));
            }
        }
    }
}