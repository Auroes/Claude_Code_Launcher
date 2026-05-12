using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

class ClaudeLauncher {

    static void Main() {
        var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        var python = FindPython();
        var shell = FindShell("pwsh.exe")
                 ?? FindShell("powershell.exe")
                 ?? FindShell("powershell_ise.exe")
                 ?? "powershell.exe";
        var hasWt = FindShell("wt.exe") != null;

        if (python != null) {
            TrustWithPython(python, exeDir);
        } else {
            TrustWithPowerShell(shell, exeDir);
        }

        if (python != null) {
            ConfigureBypass(python, exeDir);
        } else {
            ConfigureBypassPS(shell, exeDir);
        }

        var cmd = "claude --dangerously-skip-permissions";
        var shellArgs = "-NoExit -NoLogo -Command \"" + cmd + "\"";

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

    static string FindShell(string exeName) {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(';')) {
            var full = Path.Combine(dir.Trim(), exeName);
            if (File.Exists(full)) return full;
        }
        return null;
    }

    static string FindPython() {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(';')) {
            var d = dir.Trim();
            if (d.Contains("WindowsApps")) continue;
            var full = Path.Combine(d, "python.exe");
            if (File.Exists(full)) return full;
        }
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

    static void TrustWithPython(string python, string folder) {
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

            var p = Process.Start(new ProcessStartInfo {
                FileName = python,
                Arguments = "\"" + tmpScript + "\" \"" + folder + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            });
            if (p == null) return;
            p.WaitForExit(5000);

            try { File.Delete(tmpScript); } catch {}

            System.Threading.Thread.Sleep(200);
        } catch {}
    }

    static void TrustWithPowerShell(string shell, string folder) {
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

            var p = Process.Start(new ProcessStartInfo {
                FileName = shell,
                Arguments = "-NoProfile -Command \"" + script + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            });
            if (p == null) return;
            p.WaitForExit(5000);
        } catch {}
    }

    static void ConfigureBypass(string python, string folder) {
        try {
            var tmpScript = Path.Combine(Path.GetTempPath(), "claude_launcher_bypass.py");
            var script =
                "import json,os,sys\n" +
                "d=os.path.join(sys.argv[1],'.claude')\n" +
                "f=os.path.join(d,'settings.local.json')\n" +
                "if not os.path.exists(d):os.makedirs(d)\n" +
                "cfg={}\n" +
                "if os.path.exists(f):\n" +
                " try:cfg=json.load(open(f,encoding='utf-8'))\n" +
                " except:cfg={}\n" +
                "cfg['defaultMode']='bypassPermissions'\n" +
                "g=open(f,'w',encoding='utf-8')\n" +
                "json.dump(cfg,g,indent=2,ensure_ascii=False)\n" +
                "g.flush()\nos.fsync(g.fileno())\ng.close()\n";

            File.WriteAllText(tmpScript, script);

            var p = Process.Start(new ProcessStartInfo {
                FileName = python,
                Arguments = "\"" + tmpScript + "\" \"" + folder + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            });
            if (p == null) return;
            p.WaitForExit(5000);

            try { File.Delete(tmpScript); } catch {}
        } catch {}
    }

    static void ConfigureBypassPS(string shell, string folder) {
        try {
            var script =
                "$dir=Join-Path ([string]'" + folder + "') '.claude';" +
                "if(-not (Test-Path $dir)){New-Item -ItemType Directory -Path $dir|Out-Null};" +
                "$path=Join-Path $dir 'settings.local.json';" +
                "$cfg=@{};" +
                "if(Test-Path $path){" +
                " try{$cfg=(Get-Content $path -Raw -Encoding UTF8|ConvertFrom-Json)}catch{$cfg=@{}}};" +
                "$cfg|Add-Member -Force -Name defaultMode -Value 'bypassPermissions';" +
                "$cfg|ConvertTo-Json -Depth 10|Set-Content $path -Encoding UTF8 -NoNewline";

            var p = Process.Start(new ProcessStartInfo {
                FileName = shell,
                Arguments = "-NoProfile -Command \"" + script + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            });
            if (p == null) return;
            p.WaitForExit(5000);
        } catch {}
    }
}
