using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

using PatternFinder;

using SharpDisasm;

using Steamless.API.Model;
using Steamless.API.Services;

namespace TellTaleWidescreenPatcher
{
    internal static class Program
    {
        private static readonly LoggingService m_LoggingService = new LoggingService();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
            Steamless.Unpacker.Variant31.x64.Main m64 = new Steamless.Unpacker.Variant31.x64.Main();
            Steamless.Unpacker.Variant31.x86.Main m86 = new Steamless.Unpacker.Variant31.x86.Main();
            SteamlessOptions s = new SteamlessOptions();
            Vendor sh = new Vendor();
            string requiredDLLPath0 = Assembly.GetAssembly(m64.GetType()).CodeBase.Substring(8);
            string requiredDLLPath1 = Assembly.GetAssembly(s.GetType()).CodeBase.Substring(8);
            string requiredDLLPath2 = Assembly.GetAssembly(sh.GetType()).CodeBase.Substring(8);
            string requiredDLLPath3 = Assembly.GetAssembly(m86.GetType()).CodeBase.Substring(8);
            if (!File.Exists(requiredDLLPath0) || !File.Exists(requiredDLLPath1) || !File.Exists(requiredDLLPath2) || !File.Exists(requiredDLLPath3))
            {
                MessageBox.Show("Missing dependencies");
                return;
            }
        }

        public static void PatchFunction(string path)
        {
            if (IsFileLocked(path))
            {
                Form1.SetStatus("Error: Executable is locked. Can't patch.", System.Drawing.Color.Red);
                Form1.SetProgress(100, System.Drawing.Color.Red);
                return;
            }
            CheckBackup(path);
            if (IsSteamFile64(path))
            {
                Form1.SetStatus("Steam 64-bit file detected, removing protection...", System.Drawing.Color.YellowGreen);
                Form1.IncrementProgress(1);
                if (!ProcessSteamFile64(path))
                {
                    Form1.SetStatus("Error: Could not patch Steam file.", System.Drawing.Color.Red);
                    Form1.SetProgress(100, System.Drawing.Color.Red);
                    return;
                }
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(path + ".unpacked.exe", path);
                Form1.SetStatus("Protection removed, checking for pattern match...", System.Drawing.Color.YellowGreen);
                Form1.IncrementProgress(1);
            }
            else if (IsSteamFile86(path))
            {
                Form1.SetStatus("Steam 32-bit file detected, removing protection...", System.Drawing.Color.YellowGreen);
                Form1.IncrementProgress(1);
                if (!ProcessSteamFile86(path))
                {
                    Form1.SetStatus("Error: Could not patch Steam file.", System.Drawing.Color.Red);
                    Form1.SetProgress(100, System.Drawing.Color.Red);
                    return;
                }
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(path + ".unpacked.exe", path);
                Form1.SetStatus("Protection removed, checking for pattern match...", System.Drawing.Color.YellowGreen);
                Form1.IncrementProgress(1);
            }
            else if (IsSteamFile20(path))
            {
                Form1.SetStatus("Steam 2.0 32-bit file detected, removing protection...", System.Drawing.Color.YellowGreen);
                Form1.IncrementProgress(1);
                if (!ProcessSteamFile20(path))
                {
                    Form1.SetStatus("Error: Could not patch Steam file.", System.Drawing.Color.Red);
                    Form1.SetProgress(100, System.Drawing.Color.Red);
                    return;
                }
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(path + ".unpacked.exe", path);
                Form1.SetStatus("Protection removed, checking for pattern match...", System.Drawing.Color.YellowGreen);
                Form1.IncrementProgress(1);
            }
            byte[] exe = File.ReadAllBytes(path);
            var fixPattern = Pattern.Transform("F3 0F 11 05 ?? ?? ?? ?? 74 07 C6 05 ?? ?? ?? ?? 01"); // main fix pattern
            var altfixPattern = Pattern.Transform("F3 0F 11 05 ?? ?? ?? ?? C6 05 ?? ?? ?? ?? 01 5D"); // alternative fix pattern
            var altfixPattern2 = Pattern.Transform("F3 0F 11 05 ?? ?? ?? ?? ?? ?? ?? 7B 07"); // michone fix pattern
            var altfixPattern3 = Pattern.Transform("D9 1D 90 D6 A8 00"); // jurassic park fix pattern thanks to Rose
            //var ratioPattern = Pattern.Transform("39 8E E3 3F ?? ?? 00 00 F0"); // more specific ratio pattern
            var ratioPattern = Pattern.Transform("39 8E E3 3F");
            List<long> ratioOffsets = new List<long>();
            List<long> fixOffsets = new List<long>();
            Form1.SetStatus("Scanning for offsets...", System.Drawing.Color.YellowGreen);
            Console.WriteLine("Checking pattern 1...");
            int nops = 8;
            if (!Pattern.Find(exe, altfixPattern3, out long fixOffset))
            {
                if (!Pattern.Find(exe, altfixPattern2, out fixOffset))
                {
                    Console.WriteLine("Checking pattern 2...");
                    if (!Pattern.Find(exe, altfixPattern, out fixOffset))
                    {
                        Console.WriteLine("Checking pattern 3...");
                        if (!Pattern.Find(exe, fixPattern, out fixOffset))
                        {
                            Console.WriteLine("No pattern detected.");
                            fixOffset = 0;
                            Form1.SetStatus("Error: Fix pattern not found. Executable is not supported.", System.Drawing.Color.Red);
                            Form1.SetProgress(100, System.Drawing.Color.Red);
                            return;
                        }
                    }
                }
            }
            else
            {
                nops = 6;
            }
            Form1.IncrementProgress(1);
            if (!Pattern.FindAll(exe, ratioPattern, out ratioOffsets))
            {
                Form1.SetStatus("Error: Ratio pattern not found. Executable is not supported.", System.Drawing.Color.Red);
                Form1.SetProgress(100, System.Drawing.Color.Red);
                return;
            }
            Form1.IncrementProgress(1);
            Console.WriteLine("Ratio offsets found: " + ratioOffsets.Count);
            if (ratioOffsets.Count > 0 && (fixOffset > 0 || fixOffsets.Count > 0))
            {
                Form1.SetStatus("Offsets found, patching game...", System.Drawing.Color.YellowGreen);
                PatchFile(exe, ratioOffsets, path, fixOffset, fixOffsets, nops);
            }
        }

        public static void PatchFile(byte[] exe, List<long> ratioOffset, string path, long fixOffset = 0, List<long> fixOffsets = null, int nops = 8)
        {
            using (MemoryStream memStream = new MemoryStream(exe))
            {
                byte[] nop = { };
                if (nops == 8)
                {
                    nop = new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 }; // 8 nops for gog
                }
                else
                {
                    nop = new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 }; // 8 nops for gog
                }
                byte[] hexRatio = { };
                if (Form1.GetResolution() == 0)
                {
                    hexRatio = new byte[] { 0x26, 0xB4, 0x17, 0x40 };
                }
                else if (Form1.GetResolution() == 1)
                {
                    hexRatio = new byte[] { 0x8E, 0xE3, 0x18, 0x40 };
                }
                else if (Form1.GetResolution() == 2)
                {
                    hexRatio = new byte[] { 0x39, 0x8E, 0x63, 0x40 };
                }

                if (fixOffset > 0)
                {
                    Console.WriteLine("Fix offset exists.");
                    memStream.Seek(fixOffset, SeekOrigin.Begin);
                    memStream.Write(nop, 0, nop.Length);
                }
                else
                {
                    Console.WriteLine("Multiple fix offsets found.");
                    foreach (long l in fixOffsets)
                    {
                        memStream.Seek(l, SeekOrigin.Begin);
                        memStream.Write(nop, 0, nop.Length);
                        memStream.Seek(0, SeekOrigin.Begin);
                        Form1.IncrementProgress(1);
                    }
                }
                Form1.IncrementProgress(1);
                foreach (long l in ratioOffset)
                {
                    memStream.Seek(l, SeekOrigin.Begin);
                    memStream.Write(hexRatio, 0, hexRatio.Length);
                    memStream.Seek(0, SeekOrigin.Begin);
                    Form1.IncrementProgress(1);
                }
                Form1.SetStatus("Writing to disk...", System.Drawing.Color.YellowGreen);
                using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
                {
                    memStream.CopyTo(fs);
                    fs.Flush();
                }
                Form1.SetStatus("Game patched!", System.Drawing.Color.Green);
            }
        }

        private static bool IsSteamFile64(string file)
        {
            Steamless.Unpacker.Variant31.x64.Main m = new Steamless.Unpacker.Variant31.x64.Main();
            m.Initialize(m_LoggingService);
            if (m.CanProcessFile(file))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool IsSteamFile86(string file)
        {
            Steamless.Unpacker.Variant31.x86.Main m = new Steamless.Unpacker.Variant31.x86.Main();
            m.Initialize(m_LoggingService);
            if (m.CanProcessFile(file))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool IsSteamFile20(string file)
        {
            Steamless.Unpacker.Variant20.x86.Main m = new Steamless.Unpacker.Variant20.x86.Main();
            m.Initialize(m_LoggingService);
            if (m.CanProcessFile(file))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool ProcessSteamFile64(string file)
        {
            SteamlessOptions s = new SteamlessOptions
            {
                VerboseOutput = false,
                KeepBindSection = false,
                DumpPayloadToDisk = false,
                DumpSteamDrmpToDisk = false
            };

            Steamless.Unpacker.Variant31.x64.Main m = new Steamless.Unpacker.Variant31.x64.Main();
            m.Initialize(m_LoggingService);
            if (m.ProcessFile(file, s))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool ProcessSteamFile86(string file)
        {
            SteamlessOptions s = new SteamlessOptions
            {
                VerboseOutput = false,
                KeepBindSection = false,
                DumpPayloadToDisk = false,
                DumpSteamDrmpToDisk = false
            };

            Steamless.Unpacker.Variant31.x86.Main m = new Steamless.Unpacker.Variant31.x86.Main();
            m.Initialize(m_LoggingService);
            if (m.ProcessFile(file, s))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool ProcessSteamFile20(string file)
        {
            SteamlessOptions s = new SteamlessOptions
            {
                VerboseOutput = false,
                KeepBindSection = false,
                DumpPayloadToDisk = false,
                DumpSteamDrmpToDisk = false
            };

            Steamless.Unpacker.Variant20.x86.Main m = new Steamless.Unpacker.Variant20.x86.Main();
            m.Initialize(m_LoggingService);
            if (m.ProcessFile(file, s))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void CheckBackup(string path)
        {
            if (!File.Exists(path + ".bak"))
            {
                File.Copy(path, path + ".bak");
            }
            else
            {
                File.Delete(path);
                File.Copy(path + ".bak", path);
            }
        }

        private static bool IsFileLocked(string path)
        {
            FileInfo file = new FileInfo(path);
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Write, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }

            //file is not locked
            return false;
        }
    }
}