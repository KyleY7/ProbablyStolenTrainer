using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace ProbablyStolenTrainer
{
    public static class ConnectionDiagnostics
    {
        public static string Save(string report, params string[] directories)
        {
            Exception last = null;
            foreach (string directory in directories)
            {
                try
                {
                    Directory.CreateDirectory(directory);
                    string path = Path.Combine(directory, "connection-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".txt");
                    using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(file, new UTF8Encoding(true))) writer.Write(report);
                    return path;
                }
                catch (Exception ex) { last = ex; }
            }
            throw new IOException(L.T("无法保存连接诊断。"), last);
        }
    }

    public sealed partial class Session
    {
        readonly Queue<string> pointerTrace = new Queue<string>();
        void TracePointer(string field, long address, string result)
        {
            if (pointerTrace.Count >= 256) pointerTrace.Dequeue();
            pointerTrace.Enqueue(field + " @ 0x" + address.ToString("X") + " = " + result);
        }
        internal string ConnectionReport(Exception failure)
        {
            var report = new StringBuilder();
            report.AppendLine("Probably Stolen Trainer connection diagnostics / 连接诊断");
            report.AppendLine("Trainer: " + Assembly.GetExecutingAssembly().GetName().Version);
            report.AppendLine("UTC: " + DateTime.UtcNow.ToString("o"));
            report.AppendLine("Supported Steam build: " + BuildProfile.Build);
            report.AppendLine("Verified GameAssembly SHA256: " + BuildProfile.GameHash);
            report.AppendLine("Verified metadata SHA256: " + BuildProfile.MetadataHash);
            report.AppendLine("Module base: 0x" + module.ToString("X"));
            report.AppendLine("Process: " + ProcessDescription);
            report.AppendLine("Failure: " + failure);
            report.AppendLine("Pointer trace before failure:");
            foreach (string line in pointerTrace) report.AppendLine(line);
            report.AppendLine("Independent read-only probes (the game may advance between reads):");
            Probe(report, "Shop", delegate { long entry = Instance(BuildProfile.EntryType, "EmporiumEntry"); Ptr(entry + 0x10, field: "EmporiumEntry.m_CachedPtr"); });
            Probe(report, "Player", delegate
            {
                long player = Instance(BuildProfile.PlayerType, "PlayerStore");
                Ptr(player + 0x30, allowNull: true, field: "PlayerStore.runID");
                long list = Ptr(player + 0x110, field: "PlayerStore.storeReputations");
                Ptr(list + 0x10, field: "storeReputations._items");
            });
            Probe(report, "Station", delegate { Instance(BuildProfile.StationType, "StoreStation"); });
            Probe(report, "Reputation type", delegate { Type(BuildProfile.ReputationType, "StoreReputation"); });
            report.AppendLine("Read-only diagnostics. No game memory or save was written. No save strings or item contents collected.");
            return report.ToString();
        }
        void Probe(StringBuilder report, string name, Action read)
        {
            pointerTrace.Clear();
            string outcome = "OK";
            try { read(); }
            catch (Exception ex) { outcome = ex.GetType().Name + ": " + ex.Message; }
            report.AppendLine(name + ": " + outcome);
            foreach (string line in pointerTrace) report.AppendLine("  " + line);
        }
    }
}
