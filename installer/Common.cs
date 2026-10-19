using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CursorCometSetup
{
    internal static class AppInfo
    {
        public const string Name = "Cursor Comet";
        public const string Version = "1.5.0";
        public const string Exe = "cursor-comet.exe";
        public const string UninstallExe = "uninstall.exe";
        public const string AppResource = "CursorCometApp.exe";
        public const string UninstallResource = "CursorCometUninstall.exe";
        public const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\CursorComet";
        public const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        public const string RunValue = "MouseTrail";
        public const string RepoUrl = "https://github.com/cwslz/cursor-comet";
        public const string ConfigFolder = "MouseTrail";
    }

    internal static class L
    {
        private static string _language = "zh";

        public static string Language
        {
            get { return _language; }
        }

        public static bool IsChinese
        {
            get { return _language != "en"; }
        }

        public static void Set(string language)
        {
            _language = language == "en" ? "en" : "zh";
        }

        public static string T(string zh, string en)
        {
            return _language == "en" ? en : zh;
        }

        public static string SystemDefault()
        {
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" ? "zh" : "en";
        }
    }

    internal static class AppPaths
    {
        public static string DefaultDir
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Programs\CursorComet");
            }
        }

        public static string StartMenuShortcut
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                    AppInfo.Name + ".lnk");
            }
        }

        public static string DesktopShortcut
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    AppInfo.Name + ".lnk");
            }
        }

        public static string ConfigDir
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    AppInfo.ConfigFolder);
            }
        }

        public static string ConfigFile
        {
            get { return Path.Combine(ConfigDir, "config.ini"); }
        }
    }

    internal static class InstallRegistry
    {
        public static string GetInstalledDir()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(AppInfo.UninstallKey, false))
                {
                    if (key != null)
                    {
                        string dir = key.GetValue("InstallLocation") as string;
                        if (!string.IsNullOrEmpty(dir))
                        {
                            return Environment.ExpandEnvironmentVariables(dir);
                        }
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        public static bool IsInstalled()
        {
            string dir = GetInstalledDir();
            return !string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, AppInfo.Exe));
        }

        public static void Write(string dir)
        {
            string exe = Path.Combine(dir, AppInfo.Exe);
            string uninstaller = Path.Combine(dir, AppInfo.UninstallExe);

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(AppInfo.UninstallKey))
            {
                key.SetValue("DisplayName", AppInfo.Name);
                key.SetValue("DisplayVersion", AppInfo.Version);
                key.SetValue("Publisher", "cwslz");
                key.SetValue("DisplayIcon", exe + ",0");
                key.SetValue("InstallLocation", dir);
                key.SetValue("UninstallString", "\"" + uninstaller + "\"");
                key.SetValue("QuietUninstallString", "\"" + uninstaller + "\" --silent");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                key.SetValue("URLInfoAbout", AppInfo.RepoUrl);
                key.SetValue("HelpLink", AppInfo.RepoUrl);
                try
                {
                    key.SetValue("EstimatedSize", (int)(new FileInfo(exe).Length / 1024), RegistryValueKind.DWord);
                }
                catch
                {
                }
            }
        }

        public static void Remove()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(AppInfo.UninstallKey, false);
            }
            catch
            {
            }
        }
    }

    internal static class AutoStartRegistry
    {
        public static void Set(string dir, bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(AppInfo.RunKey, true))
                {
                    if (key == null)
                    {
                        return;
                    }

                    if (enabled)
                    {
                        key.SetValue(AppInfo.RunValue,
                            "\"" + Path.Combine(dir, AppInfo.Exe) + "\"", RegistryValueKind.String);
                    }
                    else
                    {
                        key.DeleteValue(AppInfo.RunValue, false);
                    }
                }
            }
            catch
            {
            }
        }

        public static void Remove()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(AppInfo.RunKey, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(AppInfo.RunValue, false);
                    }
                }
            }
            catch
            {
            }
        }
    }

    internal static class Shortcuts
    {
        public static void Create(string dir, bool desktopShortcut)
        {
            string target = Path.Combine(dir, AppInfo.Exe);
            CreateShortcut(AppPaths.StartMenuShortcut, target, dir,
                L.T("Cursor Comet - 鼠标拖尾特效", "Cursor Comet - cursor trail effect"));

            if (desktopShortcut)
            {
                CreateShortcut(AppPaths.DesktopShortcut, target, dir,
                    L.T("Cursor Comet - 鼠标拖尾特效", "Cursor Comet - cursor trail effect"));
            }
        }

        public static void RemoveAll()
        {
            DeleteIfExists(AppPaths.StartMenuShortcut);
            DeleteIfExists(AppPaths.DesktopShortcut);
        }

        private static void CreateShortcut(string lnkPath, string targetPath, string workingDir, string description)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                throw new InvalidOperationException("WScript.Shell is not available.");
            }

            object shell = Activator.CreateInstance(shellType);
            object shortcut = null;
            try
            {
                shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod,
                    null, shell, new object[] { lnkPath });
                Type shortcutType = shortcut.GetType();
                shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null,
                    shortcut, new object[] { targetPath });
                shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null,
                    shortcut, new object[] { workingDir });
                shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null,
                    shortcut, new object[] { targetPath + ",0" });
                shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null,
                    shortcut, new object[] { description });
                shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
            }
            finally
            {
                if (shortcut != null)
                {
                    Marshal.ReleaseComObject(shortcut);
                }
                if (shell != null)
                {
                    Marshal.ReleaseComObject(shell);
                }
            }
        }

        private static void DeleteIfExists(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }
    }

    internal static class AppProcess
    {
        public static void Kill()
        {
            string[] names = new string[] { "cursor-comet", "鼠标拖尾" };
            for (int i = 0; i < names.Length; i++)
            {
                Process[] processes = Process.GetProcessesByName(names[i]);
                for (int j = 0; j < processes.Length; j++)
                {
                    try
                    {
                        processes[j].Kill();
                        processes[j].WaitForExit(3000);
                    }
                    catch
                    {
                    }
                    finally
                    {
                        processes[j].Dispose();
                    }
                }
            }
        }
    }

    internal static class AppConfig
    {
        public static string ReadLanguage()
        {
            try
            {
                if (!File.Exists(AppPaths.ConfigFile))
                {
                    return null;
                }

                string[] lines = File.ReadAllLines(AppPaths.ConfigFile, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0 || line[0] == '#' || line[0] == ';')
                    {
                        continue;
                    }

                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                    {
                        continue;
                    }

                    string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                    string value = line.Substring(eq + 1).Trim().ToLowerInvariant();
                    if (key == "lang" && (value == "auto" || value == "zh" || value == "en"))
                    {
                        return value;
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        public static void WriteLanguage(string language)
        {
            try
            {
                Directory.CreateDirectory(AppPaths.ConfigDir);
                List<string> lines = new List<string>();
                if (File.Exists(AppPaths.ConfigFile))
                {
                    lines.AddRange(File.ReadAllLines(AppPaths.ConfigFile, Encoding.UTF8));
                }

                bool replaced = false;
                for (int i = 0; i < lines.Count; i++)
                {
                    string trimmed = lines[i].Trim();
                    if (trimmed.StartsWith("lang=", StringComparison.OrdinalIgnoreCase))
                    {
                        lines[i] = "lang=" + language;
                        replaced = true;
                    }
                }

                if (!replaced)
                {
                    if (lines.Count > 0 && lines[lines.Count - 1].Trim().Length > 0)
                    {
                        lines.Add(string.Empty);
                    }
                    lines.Add("# 语言 / Language: auto / zh / en");
                    lines.Add("lang=" + language);
                }

                File.WriteAllLines(AppPaths.ConfigFile, lines.ToArray(), new UTF8Encoding(false));
            }
            catch
            {
            }
        }

        public static void Remove()
        {
            try
            {
                if (Directory.Exists(AppPaths.ConfigDir))
                {
                    Directory.Delete(AppPaths.ConfigDir, true);
                }
            }
            catch
            {
            }
        }
    }

    internal static class Resources
    {
        public static void Extract(string resourceName, string targetPath)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("Missing resource: " + resourceName);
                }

                using (FileStream file = new FileStream(targetPath, FileMode.Create,
                    FileAccess.Write, FileShare.None))
                {
                    stream.CopyTo(file);
                }
            }
        }
    }

    internal static class FsUtil
    {
        public static bool IsSafeInstallDir(string dir)
        {
            if (string.IsNullOrEmpty(dir) || dir.Length < 5)
            {
                return false;
            }

            string root = Path.GetPathRoot(dir);
            if (string.IsNullOrEmpty(root) ||
                string.Equals(dir.TrimEnd('\\'), root.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (!string.IsNullOrEmpty(windows) &&
                dir.StartsWith(windows, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        public static void DeleteIfExists(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }

        public static void TryDeleteEmptyDir(string dir)
        {
            try
            {
                if (Directory.Exists(dir) && Directory.GetFileSystemEntries(dir).Length == 0)
                {
                    Directory.Delete(dir);
                }
            }
            catch
            {
            }
        }

        public static void ScheduleSelfDelete(string dir)
        {
            try
            {
                string exe = Application.ExecutablePath;
                string selfDir = Path.GetDirectoryName(exe);
                if (string.IsNullOrEmpty(selfDir) ||
                    !string.Equals(Path.GetFullPath(selfDir).TrimEnd('\\'),
                        Path.GetFullPath(dir).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                {
                    TryDeleteEmptyDir(dir);
                    return;
                }

                string command = "/c ping 127.0.0.1 -n 3 >nul & del /f /q \"" + exe +
                                 "\" & rd /q \"" + dir + "\"";
                Process.Start(new ProcessStartInfo("cmd.exe", command)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
            catch
            {
            }
        }
    }
}
