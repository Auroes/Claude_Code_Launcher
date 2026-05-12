using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;

class ClaudeLauncher {
    static void Main() {
        var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        TrustFolder(exeDir);

        Process.Start(new ProcessStartInfo {
            FileName = "pwsh.exe",
            Arguments = "-NoExit -NoLogo -Command claude",
            WorkingDirectory = exeDir,
            UseShellExecute = true
        });
    }

    static void TrustFolder(string folder) {
        try {
            var cfgPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".claude.json");

            if (!File.Exists(cfgPath)) return;

            var jss = new JavaScriptSerializer();
            var json = File.ReadAllText(cfgPath);
            var cfg = (Dictionary<string, object>)jss.DeserializeObject(json);

            if (!cfg.ContainsKey("projects"))
                cfg["projects"] = new Dictionary<string, object>();

            var projects = (Dictionary<string, object>)cfg["projects"];

            string existingKey = null;
            foreach (string k in projects.Keys) {
                if (string.Equals(k, folder, StringComparison.OrdinalIgnoreCase)) {
                    existingKey = k;
                    break;
                }
            }

            if (existingKey != null) {
                var entry = (Dictionary<string, object>)projects[existingKey];
                entry["hasTrustDialogAccepted"] = true;
            } else {
                projects[folder] = new Dictionary<string, object> {
                    { "hasTrustDialogAccepted", true }
                };
            }

            File.WriteAllText(cfgPath, jss.Serialize(cfg));
        } catch {
        }
    }
}
