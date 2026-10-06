using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

[assembly: AssemblyTitle("Asbury Pines Simplified Chinese Setup")]
[assembly: AssemblyDescription("Offline setup and management for Asbury Pines Simplified Chinese 0.1.14")]
[assembly: AssemblyProduct("Asbury_Pines_TC_pack")]
[assembly: AssemblyVersion("0.1.14.2")]
[assembly: AssemblyFileVersion("0.1.14.2")]

internal static class Bootstrap
{
    [STAThread]
    private static int Main(string[] args)
    {
        bool commandLine = args.Length > 0 && args[0] != "--game-root";
        try
        {
            string action = null, gameRoot = "", output = null;
            if (args.Length > 0)
            {
                if (args[0] == "--extract" && args.Length == 2) output = args[1];
                else if (args[0] == "--game-root" && args.Length == 2) gameRoot = args[1];
                else if ((args.Length == 2 || args.Length == 4) &&
                    (args[0] == "--verify" || args[0] == "--install" || args[0] == "--uninstall"))
                {
                    action = args[0] == "--verify" ? "Verify" : args[0] == "--install" ? "Install" : "Uninstall";
                    gameRoot = args[1];
                    if (args.Length == 4)
                    {
                        if (args[2] != "--work-dir") throw new ArgumentException("Expected --work-dir.");
                        output = args[3];
                    }
                }
                else throw new ArgumentException("Usage: --extract <empty folder>, --game-root <game folder>, or --verify/--install/--uninstall <game folder> [--work-dir <empty folder>].");
            }
            if (output == null) output = Path.Combine(Path.GetTempPath(), "AsburyPines-TC-Pack", Guid.NewGuid().ToString("N"));
            output = Path.GetFullPath(output);
            if (Directory.Exists(output) && Directory.GetFileSystemEntries(output).Length != 0)
                throw new IOException("Extraction folder must be empty.");
            Directory.CreateDirectory(output);
            string boundary = output.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream package = assembly.GetManifestResourceStream("AsburyPines.Package"))
            {
                if (package == null) throw new IOException("Embedded package is missing.");
                string expected;
                using (Stream hashStream = assembly.GetManifestResourceStream("AsburyPines.PackageSHA256"))
                using (StreamReader reader = new StreamReader(hashStream)) expected = reader.ReadToEnd().Trim();
                string actual;
                using (SHA256 hash = SHA256.Create()) actual = BitConverter.ToString(hash.ComputeHash(package)).Replace("-", "");
                if (!String.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Embedded package checksum does not match.");
                package.Position = 0;
                using (ZipArchive zip = new ZipArchive(package, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        string target = Path.GetFullPath(Path.Combine(output, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
                        if (!target.StartsWith(boundary, StringComparison.OrdinalIgnoreCase)) throw new IOException("Unsafe archive path.");
                        if (String.IsNullOrEmpty(entry.Name)) { Directory.CreateDirectory(target); continue; }
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                        using (Stream input = entry.Open())
                        using (FileStream destination = new FileStream(target, FileMode.CreateNew)) input.CopyTo(destination);
                    }
                }
            }
            if (args.Length > 0 && args[0] == "--extract") return 0;
            string script = Path.Combine(output, "Easy-Setup.ps1");
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe");
            start.Arguments = "-NoProfile -ExecutionPolicy Bypass -STA -File " + Quote(script);
            if (action != null) start.Arguments += " -NonInteractive -Action " + action + " -GameRoot " + Quote(gameRoot);
            else if (!String.IsNullOrEmpty(gameRoot)) start.Arguments += " -InitialGameRoot " + Quote(gameRoot);
            else start.Arguments += " -InitialGameRoot " + Quote(Path.GetDirectoryName(assembly.Location));
            start.WorkingDirectory = output;
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.WindowStyle = ProcessWindowStyle.Hidden;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            StringBuilder errors = new StringBuilder();
            using (Process process = new Process())
            {
                process.StartInfo = start;
                process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e) { if (e.Data != null) errors.AppendLine(e.Data); };
                process.Start();
                process.BeginErrorReadLine();
                string result = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                File.WriteAllText(Path.Combine(output, "setup-result.log"), result + errors.ToString(), new UTF8Encoding(false));
                if (process.ExitCode != 0 && !commandLine)
                    MessageBox.Show("无法启动安装介面。\n" + errors.ToString() + "\n纪录：" + Path.Combine(output, "setup-result.log"), "Asbury Pines 简体中文补丁", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return process.ExitCode;
            }
        }
        catch (Exception error)
        {
            if (commandLine) Console.Error.WriteLine(error.Message);
            else MessageBox.Show(error.Message, "Asbury Pines 简体中文补丁", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    private static string Quote(string value)
    {
        if (value.IndexOf('"') >= 0 || value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0)
            throw new ArgumentException("Invalid path.");
        // Escape trailing backslashes before the closing Windows command-line quote.
        int trailing = value.Length - value.TrimEnd('\\').Length;
        return "\"" + value + new string('\\', trailing) + "\"";
    }
}
