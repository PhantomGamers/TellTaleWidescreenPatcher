using PatternFinder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace TellTaleWidescreenPatcher
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        public static void PatchFunction(string path)
        {
            if (!File.Exists(path + ".bak"))
                File.Copy(path, path + ".bak");
            else
            {
                File.Delete(path);
                File.Copy(path + ".bak", path);
            }
            byte[] exe = File.ReadAllBytes(path);
            var fixPattern = Pattern.Transform("F3 0F 11 05 ?? ?? ?? ?? 74 07 C6 05 ?? ?? ?? ?? 01");
            //var ratioPattern = Pattern.Transform("39 8E E3 3F ?? ?? 00 00 F0");
            var ratioPattern = Pattern.Transform("39 8E E3 3F ?? ??");
            List<long> ratioOffsets = new List<long>();
            bool foundFix = true;
            if (!Pattern.Find(exe, fixPattern, out long fixOffset))
            {
                Form1.SetStatus("Error: Fix pattern not found. Executable is not supported.", System.Drawing.Color.Red);
                foundFix = false;
            }
            if (foundFix && !Pattern.FindAll(exe, ratioPattern, out ratioOffsets))
                Form1.SetStatus("Error: Ratio pattern not found. Executable is not supported.", System.Drawing.Color.Red);
            Console.WriteLine("Ratio offsets found: " + ratioOffsets.Count);
            if (ratioOffsets.Count > 0 && fixOffset > 0)
                PatchFile(exe, fixOffset, ratioOffsets, path);
        }

        public static void PatchFile(byte[] exe, long fixOffset, List<long> ratioOffset, string path)
        {
            //ratioOffset.Clear();

            using (MemoryStream memStream = new MemoryStream(exe))
            {
                int count = 0;
                byte[] nop = { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };
                byte[] hexRatio = { };
                if (Form1.GetResolution() == 0)
                    hexRatio = new byte[] { 0x26, 0xB4, 0x17, 0x40 };
                else if (Form1.GetResolution() == 1)
                    hexRatio = new byte[] { 0x8E, 0xE3, 0x18, 0x40 };
                memStream.Seek(fixOffset, SeekOrigin.Begin);
                memStream.Write(nop, 0, nop.Length);
                foreach (long l in ratioOffset)
                {
                    memStream.Seek(l, SeekOrigin.Begin);
                    memStream.Write(hexRatio, 0, hexRatio.Length);
                    memStream.Seek(0, SeekOrigin.Begin);
                }
                while (count < memStream.Length)
                {
                    exe[count++] = Convert.ToByte(memStream.ReadByte());
                }
                File.WriteAllBytes(path, exe);
                Form1.SetStatus("Game patched!", System.Drawing.Color.Green);
            }
        }
    }
}