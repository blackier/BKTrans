using System;
using System.Runtime.InteropServices;

namespace BKAssembly
{
    public class BKHotKey
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Win32API: RegisterHotKey function
        public enum Modifiers
        {
            alt = 0x0001,
            control = 0x0002,
            norepeat = 0x4000,
            shift = 0x0004,
            win = 0x0008
        }

        public static bool Register(IntPtr hWnd, int id, uint fsModifiers, uint vk)
        {
            return RegisterHotKey(hWnd, id, fsModifiers, vk);
        }
        public static bool UnRegister(IntPtr hWnd, int id)
        {
            return UnregisterHotKey(hWnd, id);
        }

    }
}
