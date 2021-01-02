using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BKAssembly
{
    public class BKHotKey
    {

        #region 成员变量定义
        // 热键详见：Win32API: RegisterHotKey function
        public enum Modifiers
        {
            alt = 0x0001,
            control = 0x0002,
            norepeat = 0x4000,
            shift = 0x0004,
            win = 0x0008
        }
        #endregion 成员变量定义

        #region 公有成员函数定义
        public static bool Register(IntPtr hWnd, int id, uint fsModifiers, uint vk)
        {
            return RegisterHotKey(hWnd, id, fsModifiers, vk);
        }
        public static bool Unregister(IntPtr hWnd, int id)
        {
            return UnregisterHotKey(hWnd, id);
        }
        #endregion 公有成员函数定义

        #region 私有成员函数定义
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        #endregion 私有成员函数定义
    }
}
