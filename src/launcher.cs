using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

class ClaudeLauncher {
    static string LogPath;

    static void Main() {
        var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        LogPath = Path.Combine(exeDir, "launcher_debug.log");
        Log("START, exeDir=" + exeDir);

        var python = FindPython();
        var shell = FindShell("pwsh.exe")
                 ?? FindShell("powershell.exe")
                 ?? FindShell("powershell_ise.exe")
                 ?? "powershell.exe";
        var hasWt = FindShell("wt.exe") != null;

        Log("python=" + (python ?? "NULL"));
        Log("shell=" + shell);
        Log("hasWt=" + hasWt);

        if (python != null) {
            var ok = TrustWithPython(python, exeDir);
            Log("TrustWithPython result=" + ok);
        } else {
            var ok = TrustWithPowerShell(shell, exeDir);
            Log("TrustWithPowerShell result=" + ok);
        }

        var shellArgs = "-NoExit -NoLogo -Command claude";

        if (hasWt) {
            Process.Start(new ProcessStartInfo {
                FileName = "wt.exe",
                Arguments = shell + " " + shellArgs,
                WorkingDirectory = exeDir,
                UseShellExecute = true
            });
        } else {
            Process.Start(new ProcessStartInfo {
                FileName = shell,
                Arguments = shellArgs,
                WorkingDirectory = exeDir,
                UseShellExecute = true
            });
        }
    }

    static void Log(string msg) {
        try {
            File.AppendAllText(LogPath, DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + "\n");
        } catch {}
    }

    static string FindShell(string exeName) {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(';')) {
            var full = Path.Combine(dir.Trim(), exeName);
            if (File.Exists(full)) return full;
        }
        return null;
    }

    static string FindPython() {
        // First, try known real Python installations (skip Store stubs)
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(';')) {
            var d = dir.Trim();
            if (d.Contains("WindowsApps")) continue;  // skip Microsoft Store stubs
            var full = Path.Combine(d, "python.exe");
            if (File.Exists(full)) return full;
        }
        // Fallback: check common conda paths
        string[] known = {
            @"D:\Apps\miniconda3\python.exe",
            @"C:\ProgramData\miniconda3\python.exe",
            @"C:\Users\25104\AppData\Local\Programs\Python\python.exe"
        };
        foreach (var p in known) {
            if (File.Exists(p)) return p;
        }
        return null;
    }

    static bool TrustWithPython(string python, string folder) {
        try {
            var tmpDir = Path.GetTempPath();
            var tmpScript = Path.Combine(tmpDir, "claude_launcher_trust.py");
            var script =
                "import json,os,sys\n" +
                "cfg=os.path.join(os.environ.get('USERPROFILE',''),'.claude.json')\n" +
                "if not os.path.exists(cfg):sys.exit(1)\n" +
                "d=json.load(open(cfg,encoding='utf-8'))\n" +
                "d.setdefault('projects',{})\n" +
                "p=sys.argv[1]\n" +
                "d['projects'][p]={'hasTrustDialogAccepted':True}\n" +
                "d['projects'][p.replace(chr(92),'/')]={'hasTrustDialogAccepted':True}\n" +
                "f=open(cfg,'w',encoding='utf-8')\n" +
                "json.dump(d,f,indent=2,ensure_ascii=False)\n" +
                "f.flush()\nos.fsync(f.fileno())\nf.close()\n";

            File.WriteAllText(tmpScript, script);

            var p1 = Process.Start(new ProcessStartInfo {
                FileName = python,
                Arguments = "\"" + tmpScript + "\" \"" + folder + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            });
            if (p1 == null) {
                Log("Python process failed to start (null)");
                return false;
            }
            var err = p1.StandardError.ReadToEnd();
            p1.WaitForExit(5000);
            Log("Python exit=" + p1.ExitCode + " err=" + (err.Length > 0 ? err : "(none)"));

            try { File.Delete(tmpScript); } catch {}

            System.Threading.Thread.Sleep(200);
            return p1.ExitCode == 0;
        } catch (Exception ex) {
            Log("TrustWithPython EXCEPTION: " + ex.Message);
            return false;
        }
    }

    static bool TrustWithPowerShell(string shell, string folder) {
        try {
            var script =
                "$cfgPath=Join-Path $env:USERPROFILE '.claude.json';" +
                "if(-not (Test-Path $cfgPath)){exit 1};" +
                "$cfg=Get-Content $cfgPath -Raw -Encoding UTF8|ConvertFrom-Json;" +
                "if(-not $cfg.projects){$cfg|Add-Member -Force -Name projects -Value @{}};" +
                "$entry=@{hasTrustDialogAccepted=$true};" +
                "$cfg.projects|Add-Member -Force -Name ([string]'" + folder + "') -Value $entry;" +
                "$cfg.projects|Add-Member -Force -Name ([string]'" + folder.Replace('\\', '/') + "') -Value $entry;" +
                "$cfg|ConvertTo-Json -Depth 100|Set-Content $cfgPath -Encoding UTF8 -NoNewline";

            var p2 = Process.Start(new ProcessStartInfo {
                FileName = shell,
                Arguments = "-NoProfile -Command \"" + script + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            });
            if (p2 == null) {
                Log("PS process failed to start (null)");
                return false;
            }
            var err = p2.StandardError.ReadToEnd();
            p2.WaitForExit(5000);
            Log("PS exit=" + p2.ExitCode + " err=" + (err.Length > 0 ? err : "(none)"));
            return p2.ExitCode == 0;
        } catch (Exception ex) {
            Log("TrustWithPowerShell EXCEPTION: " + ex.Message);
            return false;
        }
    }
}
