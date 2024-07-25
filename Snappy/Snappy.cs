using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SnapKey
{
    static class Snappy
    {
        struct KeyState
        {
            public bool Pressed;
        }

        static int keyA_code = (int)Keys.A;
        static int keyD_code = (int)Keys.D;
        static Dictionary<int, KeyState> keyStates = new Dictionary<int, KeyState>();
        static int activeKey = 0;
        static int previousKey = 0;
        static IntPtr hHook = IntPtr.Zero;
        static NotifyIcon? trayIcon;

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static HookProc proc = HookCallback;

        [STAThread]
        static void Main()
        {
            LoadConfig("config.cfg");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            trayIcon = new NotifyIcon()
            {
                Icon = new System.Drawing.Icon("icon.ico"),
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true
            };

            // Adding items to the ContextMenuStrip
            trayIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Exit", null, Exit));


            hHook = SetWindowsHookEx(13, proc, GetModuleHandle(null), 0);

            Application.Run();

            UnhookWindowsHookEx(hHook);
            trayIcon.Dispose();
        }

        private static void LoadConfig(string filename)
        {
            try
            {
                var lines = System.IO.File.ReadAllLines(filename);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        if (parts[0] == "keyA") keyA_code = int.Parse(parts[1]);
                        if (parts[0] == "keyD") keyD_code = int.Parse(parts[1]);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load configuration file!\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (wParam == (IntPtr)0x100) // WM_KEYDOWN
                {
                    HandleKeyDown(vkCode);
                }
                else if (wParam == (IntPtr)0x101) // WM_KEYUP
                {
                    HandleKeyUp(vkCode);
                }
            }
            return CallNextHookEx(hHook, nCode, wParam, lParam);
        }

        private static void HandleKeyDown(int keyCode)
        {
            if (keyCode == keyA_code || keyCode == keyD_code)
            {
                if (!keyStates.TryGetValue(keyCode, out var keyState) || !keyState.Pressed)
                {
                    keyState.Pressed = true;
                    keyStates[keyCode] = keyState;

                    if (activeKey == 0 || activeKey == keyCode)
                    {
                        activeKey = keyCode;
                    }
                    else
                    {
                        previousKey = activeKey;
                        activeKey = keyCode;

                        SendKey(previousKey, true);
                    }
                }
            }
        }

        private static void HandleKeyUp(int keyCode)
        {
            if (keyCode == keyA_code || keyCode == keyD_code)
            {
                if (keyStates.TryGetValue(keyCode, out var keyState) && keyState.Pressed)
                {
                    keyState.Pressed = false;
                    keyStates[keyCode] = keyState;

                    if (activeKey == keyCode && previousKey != 0)
                    {
                        activeKey = previousKey;
                        previousKey = 0;

                        SendKey(activeKey, false);
                    }
                }
            }
        }

        private static void SendKey(int keyCode, bool keyUp)
        {
            // Implement key sending logic if needed
        }

        private static void Exit(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
