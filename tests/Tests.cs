using System;
using System.Drawing;
using System.IO;
using System.Text;
using CursorComet;

namespace CursorComet.Tests
{
    internal static class TestProgram
    {
        private static int _failed;

        private static void Check(bool ok, string name)
        {
            Console.WriteLine((ok ? "PASS " : "FAIL ") + name);
            if (!ok)
            {
                _failed++;
            }
        }

        private static void Main()
        {
            string temp = Path.Combine(Path.GetTempPath(), "CursorCometTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            AppSettings.SetDataDirectoryForTests(temp);
            try
            {
                ConfigRoundTrip();
                ConfigClamps();
                PaletteNames();
                SaveFailureKeepsOldConfig();
                CorruptLoadFallsBackAndLogs();
                ConfigMigration();
            }
            finally
            {
                AppSettings.ResetDataDirectoryForTests();
                try
                {
                    Directory.Delete(temp, true);
                }
                catch
                {
                }
            }

            Console.WriteLine(_failed == 0 ? "ALL TESTS PASSED" : _failed + " TEST(S) FAILED");
            Environment.ExitCode = _failed == 0 ? 0 : 1;
        }

        private static void ConfigRoundTrip()
        {
            AppSettings s = new AppSettings();
            s.Style = "rainbow";
            s.ColorName = "pink";
            s.Scale = 1.25f;
            s.Life = 0.9;
            s.Enabled = false;
            s.ShowTip = false;
            s.Language = "en";
            s.GlowEnabled = false;
            s.GlowStrength = 1.6;
            s.GlowRadius = 33;
            s.GlowColor = "gold";
            s.HaloEnabled = false;
            s.HaloRadius = 40;
            s.HaloThickness = 4.5;
            s.HaloOpacity = 0.4;
            s.HaloColor = "mint";
            s.RainbowSpeed = 1.3;
            s.RainbowSaturation = 0.6;
            s.RainbowOffset = 0.2;
            s.SparkRate = 1.4;
            s.SparkSize = 1.2;
            s.SparkLife = 1.1;
            s.SparkSpeed = 0.9;
            s.MinimalWidth = 1.5;
            s.MinimalOpacity = 0.7;
            s.Save();

            AppSettings loaded = AppSettings.Load();
            Check(loaded.Style == "rainbow", "config: style round-trip");
            Check(loaded.ColorName == "pink", "config: color round-trip");
            Check(Math.Abs(loaded.Scale - 1.25f) < 0.001f, "config: scale round-trip");
            Check(Math.Abs(loaded.Life - 0.9) < 0.001, "config: life round-trip");
            Check(!loaded.Enabled && !loaded.ShowTip, "config: booleans round-trip");
            Check(loaded.Language == "en", "config: language round-trip");
            Check(loaded.GlowColor == "gold" && loaded.HaloColor == "mint", "config: color choices round-trip");
            Check(Math.Abs(loaded.HaloThickness - 4.5) < 0.001, "config: halo thickness round-trip");
            Check(Math.Abs(loaded.MinimalOpacity - 0.7) < 0.001, "config: minimal opacity round-trip");
        }

        private static void ConfigClamps()
        {
            File.WriteAllText(AppSettings.FilePath,
                "style=rainbow\ncolor=pink\nscale=9\nlife=0.1\nglowradius=1\nhaloopacity=9\nminimalopacity=0.1\nrainbowspeed=9\n",
                new UTF8Encoding(false));
            AppSettings s = AppSettings.Load();
            Check(Math.Abs(s.Scale - 2f) < 0.001f, "clamp: scale max 2");
            Check(Math.Abs(s.Life - 0.2) < 0.001, "clamp: life min 0.2");
            Check(Math.Abs(s.GlowRadius - 8) < 0.001, "clamp: glow radius min 8");
            Check(Math.Abs(s.HaloOpacity - 1) < 0.001, "clamp: halo opacity max 1");
            Check(Math.Abs(s.MinimalOpacity - 0.3) < 0.001, "clamp: minimal opacity min 0.3");
            Check(Math.Abs(s.RainbowSpeed - 2) < 0.001, "clamp: rainbow speed max 2");
        }

        private static void PaletteNames()
        {
            Check(Palette.Normalize("冰蓝") == "cyan", "palette: Chinese name");
            Check(Palette.Normalize("Ice Blue") == "cyan", "palette: English name");
            Check(Palette.Normalize("cyan") == "cyan", "palette: stable key");
            Check(Palette.Normalize("nope") == null, "palette: unknown returns null");
            Check(Palette.Exists("pink"), "palette: exists");
            Color c = Palette.Get("pink");
            Check(c.R == 255 && c.G == 128 && c.B == 196, "palette: color value");
        }

        private static void SaveFailureKeepsOldConfig()
        {
            AppSettings oldSettings = new AppSettings();
            oldSettings.Life = 0.7;
            oldSettings.Save();

            AppSettings changed = new AppSettings();
            changed.Life = 1.1;
            using (FileStream fs = new FileStream(AppSettings.FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                changed.Save();
            }

            AppSettings loaded = AppSettings.Load();
            Check(Math.Abs(loaded.Life - 0.7) < 0.001, "atomic save: failed save keeps previous file");
            Check(File.Exists(Path.Combine(AppSettings.Folder, "error.log")), "atomic save: failure logged");
        }

        private static void CorruptLoadFallsBackAndLogs()
        {
            File.WriteAllText(AppSettings.FilePath, "style=rainbow\nlife=0.9\n", new UTF8Encoding(false));
            using (FileStream fs = new FileStream(AppSettings.FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                AppSettings loaded = AppSettings.Load();
                Check(loaded.Style == AppSettings.DefaultStyle, "config: unreadable file falls back to defaults");
            }
            Check(File.Exists(Path.Combine(AppSettings.Folder, "error.log")), "config: load failure logged");
        }

        private static void ConfigMigration()
        {
            string newDir = Path.Combine(Path.GetTempPath(), "CursorCometTests-new-" + Guid.NewGuid().ToString("N"));
            string oldDir = Path.Combine(Path.GetTempPath(), "CursorCometTests-old-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(newDir);
            Directory.CreateDirectory(oldDir);
            try
            {
                AppSettings.SetDataDirectoryForTests(newDir);
                AppSettings.SetLegacyDataDirectoryForTests(oldDir);
                File.WriteAllText(Path.Combine(oldDir, "config.ini"),
                    "style=rainbow\ncolor=gold\nlife=0.9\n", new UTF8Encoding(false));

                AppSettings loaded = AppSettings.Load();
                Check(loaded.Style == "rainbow" && loaded.ColorName == "gold", "migration: legacy config loaded");
                Check(File.Exists(Path.Combine(newDir, "config.ini")), "migration: copied to CursorComet folder");
            }
            finally
            {
                AppSettings.SetDataDirectoryForTests(null);
                AppSettings.SetLegacyDataDirectoryForTests(null);
                try
                {
                    Directory.Delete(newDir, true);
                }
                catch
                {
                }
                try
                {
                    Directory.Delete(oldDir, true);
                }
                catch
                {
                }
            }
        }
    }
}
