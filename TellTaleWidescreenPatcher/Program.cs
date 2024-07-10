#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Steamless.API.Model;
using Steamless.API.Services;

#endregion

namespace TellTaleWidescreenPatcher;

internal static class Program
{
    private static readonly LoggingService MLoggingService = new();

    /// <summary>
    ///     The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Form1());
        var dependencies = new List<Type>
        {
            typeof(Steamless.Unpacker.Variant31.x64.Main),
            typeof(Steamless.Unpacker.Variant31.x86.Main),
            typeof(Steamless.API.SteamlessEvents),
            typeof(SharpDisasm.Vendor)
        };

        foreach (var requiredDllPath in dependencies
                     .Select(dependency => Assembly.GetAssembly(dependency).CodeBase.Substring(8))
                     .Where(requiredDllPath => !File.Exists(requiredDllPath)))
        {
            MessageBox.Show($"Missing dependency: {requiredDllPath}");
            break;
        }
    }

    public static void PatchFunction(string path)
    {
        if (IsFileLocked(path))
        {
            Form1.SetStatus("Error: Executable is locked. Can't patch.", Color.Red);
            Form1.SetProgress(100, Color.Red);
            return;
        }
        CheckBackup(path);
        if (IsSteamFile64(path))
        {
            Form1.SetStatus("Steam 64-bit file detected, removing protection...", Color.YellowGreen);
            Form1.IncrementProgress(1);
            if (!ProcessSteamFile64(path))
            {
                Form1.SetStatus("Error: Could not patch Steam file.", Color.Red);
                Form1.SetProgress(100, Color.Red);
                return;
            }
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(path + ".unpacked.exe", path);
            Form1.SetStatus("Protection removed, checking for pattern match...", Color.YellowGreen);
            Form1.IncrementProgress(1);
        }
        else if (IsSteamFile86(path))
        {
            Form1.SetStatus("Steam 32-bit file detected, removing protection...", Color.YellowGreen);
            Form1.IncrementProgress(1);
            if (!ProcessSteamFile86(path))
            {
                Form1.SetStatus("Error: Could not patch Steam file.", Color.Red);
                Form1.SetProgress(100, Color.Red);
                return;
            }
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(path + ".unpacked.exe", path);
            Form1.SetStatus("Protection removed, checking for pattern match...", Color.YellowGreen);
            Form1.IncrementProgress(1);
        }
        else if (IsSteamFile20(path))
        {
            Form1.SetStatus("Steam 2.0 32-bit file detected, removing protection...", Color.YellowGreen);
            Form1.IncrementProgress(1);
            if (!ProcessSteamFile20(path))
            {
                Form1.SetStatus("Error: Could not patch Steam file.", Color.Red);
                Form1.SetProgress(100, Color.Red);
                return;
            }
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(path + ".unpacked.exe", path);
            Form1.SetStatus("Protection removed, checking for pattern match...", Color.YellowGreen);
            Form1.IncrementProgress(1);
        }
        var exe = File.ReadAllBytes(path);
        var fixPattern = Pattern.Transform("F3 0F 11 05 ?? ?? ?? ?? 74 07 C6 05 ?? ?? ?? ?? 01"); // main fix pattern
        var altFixPattern = Pattern.Transform("F3 0F 11 05 ?? ?? ?? ?? C6 05 ?? ?? ?? ?? 01 5D"); // alternative fix pattern
        var altFixPattern2 = Pattern.Transform("F3 0F 11 05 ?? ?? ?? ?? ?? ?? ?? 7B 07"); // michone fix pattern
        var altFixPattern3 = Pattern.Transform("D9 1D 90 D6 A8 00"); // jurassic park fix pattern thanks to Rose
        //var ratioPattern = Pattern.Transform("39 8E E3 3F ?? ?? 00 00 F0"); // more specific ratio pattern
        var ratioPattern = Pattern.Transform("39 8E E3 3F");
        var fixOffsets = new List<long>();
        Form1.SetStatus("Scanning for offsets...", Color.YellowGreen);
        Console.WriteLine("Checking pattern 1...");
        var nops = 8;
        if (!Pattern.Find(exe, altFixPattern3, out var fixOffset))
        {
            if (!Pattern.Find(exe, altFixPattern2, out fixOffset))
            {
                Console.WriteLine("Checking pattern 2...");
                if (!Pattern.Find(exe, altFixPattern, out fixOffset))
                {
                    Console.WriteLine("Checking pattern 3...");
                    if (!Pattern.Find(exe, fixPattern, out fixOffset))
                    {
                        Console.WriteLine("No fix pattern detected.");
                        fixOffset = 0;
                        if (MessageBox.Show("No fix detected, apply aspect ratio patch anyway?", "Prompt",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) !=
                            DialogResult.Yes)
                        {
                            Form1.SetStatus("Error: Fix pattern not found. Executable is not supported.", Color.Red);
                            Form1.SetProgress(100, Color.Red);
                            return;
                        }
                    }
                }
            }
        }
        else
        {
            nops = 6;
        }
        Form1.IncrementProgress(1);
        if (!Pattern.FindAll(exe, ratioPattern, out var ratioOffsets))
        {
            Form1.SetStatus("Error: Ratio pattern not found. Executable is not supported.", Color.Red);
            Form1.SetProgress(100, Color.Red);
            return;
        }
        Form1.IncrementProgress(1);
        Console.WriteLine("Ratio offsets found: " + ratioOffsets.Count);
        if (ratioOffsets.Count <= 0) return;
        Form1.SetStatus("Offsets found, patching game...", Color.YellowGreen);
        PatchFile(exe, ratioOffsets, path, fixOffset, fixOffsets, nops);
    }

    private static byte[] GetRatioBytes()
    {
        var resolution = Form1.GetResolution();
        float aspectRatio;

        if (resolution.Contains("x"))
        {
            var parts = resolution.Split('x');
            aspectRatio = float.Parse(parts[0]) / float.Parse(parts[1]);
        }
        else if (resolution.Contains(":"))
        {
            var parts = resolution.Split(':');
            aspectRatio = float.Parse(parts[0]) / float.Parse(parts[1]);
        }
        else
        {
            throw new ArgumentOutOfRangeException($"Unknown resolution format {resolution}");
        }

        return BitConverter.GetBytes(aspectRatio);
    }

    private static void PatchFile(byte[] exe, List<long> ratioOffset, string path, long fixOffset = 0,
        List<long> fixOffsets = null, int nops = 8)
    {
        using var memStream = new MemoryStream(exe);
        var nop = Enumerable.Repeat((byte)0x90, nops).ToArray(); // 8 for gog, 6 otherwise

        var hexRatio = GetRatioBytes();

        Console.WriteLine(hexRatio);

        if (fixOffset > 0)
        {
            Console.WriteLine("Fix offset exists.");
            memStream.Seek(fixOffset, SeekOrigin.Begin);
            memStream.Write(nop, 0, nop.Length);
        }
        else
        {
            if (fixOffsets != null)
            {
                Console.WriteLine("Multiple fix offsets found.");
                foreach (var l in fixOffsets)
                {
                    memStream.Seek(l, SeekOrigin.Begin);
                    memStream.Write(nop, 0, nop.Length);
                    memStream.Seek(0, SeekOrigin.Begin);
                    Form1.IncrementProgress(1);
                }
            }
        }

        Form1.IncrementProgress(1);

        foreach (var l in ratioOffset)
        {
            memStream.Seek(l, SeekOrigin.Begin);
            memStream.Write(hexRatio, 0, hexRatio.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Form1.IncrementProgress(1);
        }

        Form1.SetStatus("Writing to disk...", Color.YellowGreen);

        using (var fs = new FileStream(path, FileMode.OpenOrCreate))
        {
            memStream.CopyTo(fs);
            fs.Flush();
        }

        Form1.SetStatus("Game patched!", Color.Green);
    }

    private static bool IsSteamFile64(string file)
    {
        var m = new Steamless.Unpacker.Variant31.x64.Main();
        m.Initialize(MLoggingService);
        return m.CanProcessFile(file);
    }

    private static bool IsSteamFile86(string file)
    {
        var m = new Steamless.Unpacker.Variant31.x86.Main();
        m.Initialize(MLoggingService);
        return m.CanProcessFile(file);
    }

    private static bool IsSteamFile20(string file)
    {
        var m = new Steamless.Unpacker.Variant20.x86.Main();
        m.Initialize(MLoggingService);
        return m.CanProcessFile(file);
    }

    private static bool ProcessSteamFile64(string file)
    {
        var s = new SteamlessOptions
        {
            VerboseOutput = false,
            KeepBindSection = false,
            DumpPayloadToDisk = false,
            DumpSteamDrmpToDisk = false
        };

        var m = new Steamless.Unpacker.Variant31.x64.Main();
        m.Initialize(MLoggingService);
        return m.ProcessFile(file, s);
    }

    private static bool ProcessSteamFile86(string file)
    {
        var s = new SteamlessOptions
        {
            VerboseOutput = false,
            KeepBindSection = false,
            DumpPayloadToDisk = false,
            DumpSteamDrmpToDisk = false
        };

        var m = new Steamless.Unpacker.Variant31.x86.Main();
        m.Initialize(MLoggingService);
        return m.ProcessFile(file, s);
    }

    private static bool ProcessSteamFile20(string file)
    {
        var s = new SteamlessOptions
        {
            VerboseOutput = false,
            KeepBindSection = false,
            DumpPayloadToDisk = false,
            DumpSteamDrmpToDisk = false
        };

        var m = new Steamless.Unpacker.Variant20.x86.Main();
        m.Initialize(MLoggingService);
        return m.ProcessFile(file, s);
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
        var file = new FileInfo(path);
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
            stream?.Close();
        }

        //file is not locked
        return false;
    }
}
