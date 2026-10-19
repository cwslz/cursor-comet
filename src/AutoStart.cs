using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CursorComet
{
    /// <summary>当前用户的开机自启动（HKCU Run）。</summary>
    internal static class AutoStart
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "CursorComet";
        private const string LegacyValueName = "MouseTrail";

        public static bool IsEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    return key != null && key.GetValue(ValueName) != null;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void Set(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key == null)
                    {
                        return;
                    }

                    if (enabled)
                    {
                        key.SetValue(ValueName, "\"" + Application.ExecutablePath + "\"", RegistryValueKind.String);
                    }
                    else
                    {
                        key.DeleteValue(ValueName, false);
                    }
                    key.DeleteValue(LegacyValueName, false);
                }
            }
            catch (Exception ex)
            {
                Program.Log(ex);
            }
        }

        /// <summary>程序被移动过时，自动修正注册表里的路径。</summary>
        public static void RepairIfNeeded()
        {
            try
            {
                MigrateLegacyValue();
                if (!IsEnabled())
                {
                    return;
                }

                string current = null;
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    if (key != null)
                    {
                        current = key.GetValue(ValueName) as string;
                    }
                }

                if (current == null)
                {
                    return;
                }

                string wanted = "\"" + Application.ExecutablePath + "\"";
                if (!string.Equals(current, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    Set(true);
                }
            }
            catch
            {
            }
        }

        private static void MigrateLegacyValue()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key == null)
                    {
                        return;
                    }

                    object legacy = key.GetValue(LegacyValueName);
                    if (legacy == null)
                    {
                        return;
                    }

                    if (key.GetValue(ValueName) == null)
                    {
                        key.SetValue(ValueName, "\"" + Application.ExecutablePath + "\"", RegistryValueKind.String);
                    }
                    key.DeleteValue(LegacyValueName, false);
                }
            }
            catch (Exception ex)
            {
                Program.Log(ex);
            }
        }
    }
}
