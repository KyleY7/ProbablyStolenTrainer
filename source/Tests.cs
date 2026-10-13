using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ProbablyStolenTrainer
{
    // Test-only memory layout. No game DLL, code, assets or save is shipped.
    sealed class FakeMemory : ITransactionalCodeMemory
    {
        readonly Dictionary<long, byte> data = new Dictionary<long, byte>();
        public int Writes, FailOnWrite;
        public int CodeWrites, FailOnCodeWrite;
        public bool WriteThenFail;
        public bool Alive = true;
        public bool PatchBusy;
        public int Transactions;
        public Action<long, int> BeforeRead;
        public bool IsAlive { get { return Alive; } }
        public byte[] Read(long address, int size)
        {
            if (BeforeRead != null) BeforeRead(address, size);
            byte[] result = new byte[size];
            for (int i = 0; i < size; i++) if (!data.TryGetValue(address + i, out result[i])) throw new IOException("Unmapped test memory");
            return result;
        }
        public void Put(long address, byte[] bytes) { for (int i = 0; i < bytes.Length; i++) data[address + i] = bytes[i]; }
        public void Write(long address, byte[] bytes) { Writes++; if (Writes == FailOnWrite) throw new IOException("Simulated write failure"); Put(address, bytes); }
        public void Long(long address, long value) { Put(address, BitConverter.GetBytes(value)); }
        public void Int(long address, int value) { Put(address, BitConverter.GetBytes(value)); }
        public void Double(long address, double value) { Put(address, BitConverter.GetBytes(value)); }
        public void String(long obj, string value) { Int(obj + 0x10, value.Length); Put(obj + 0x14, Encoding.Unicode.GetBytes(value)); }
        public void Dispose() { }
        public void WriteCodeByte(long address, byte value)
        {
            CodeWrites++;
            if (CodeWrites == FailOnCodeWrite)
            {
                if (WriteThenFail) Put(address, new byte[] { value });
                throw new IOException("Simulated code write failure");
            }
            Put(address, new byte[] { value });
        }
        public void ApplyCodeChanges(IList<CodeChange> changes, long busyStart, long busyEnd)
        {
            Transactions++;
            if (PatchBusy) throw new IOException("Simulated busy menu handler");
            CodeTransaction.Apply(this, changes, delegate(long address, byte[] data) { for (int i = 0; i < data.Length; i++) WriteCodeByte(address + i, data[i]); });
        }
    }
    sealed class Fixture
    {
        public const long Base = 0x180000000, Player = 0x100000, Station = 0x200000, Entry = 0x300000, Rep = 0x400000;
        public readonly FakeMemory Memory = new FakeMemory();
        public Session Session;
        public Fixture(string runId = "fixture-run-1", int saveSlot = 0)
        {
            Memory.Put(Base + BuildProfile.AppraisalTooltipGate, new byte[] { 0x80, 0xB8, 0x22, 0x01, 0x00, 0x00, 0x00, 0x74, 0x7E, 0x33, 0xD2, 0x48, 0x8B, 0xCB, 0xE8 });
            MakeType(BuildProfile.PlayerType, 0x500000, Player, "PlayerStore");
            MakeType(BuildProfile.StationType, 0x510000, Station, "StoreStation");
            MakeType(BuildProfile.EntryType, 0x520000, Entry, "EmporiumEntry");
            MakeType(BuildProfile.ReputationType, 0x530000, Rep, "StoreReputation");
            MakeType(BuildProfile.DebugHandlerType, 0x540000, 0xA00000, "DebugHandler");
            MakeType(BuildProfile.GridInventoryType, 0x550000, 0xA10000, "GameGridInventory");
            MakeType(BuildProfile.PixelWindowType, 0x560000, 0xA20000, "PixelWindow");
            MakeType(BuildProfile.ScrollInventoryType, 0x570000, 0xA30000, "GameGridScrollableInventory");
            Memory.Long(Entry + 0x98, 0xA10000);
            Memory.Long(0xA00010, 0xA40000); Memory.Put(0xA0001D, new byte[] { 1 }); Memory.Int(0xA00020, 291);
            Memory.Long(0xA00048, 0xA20000); Memory.Long(0xA00050, 0xA30000);
            Memory.Put(Player + 0x122, new byte[] { 0 });
            foreach (var change in ItemLibrary.Profile(Base)) Memory.Put(change.Address, change.Before);
            Memory.Long(Entry + 0x10, 0x900000);
            Memory.Int(Player + 0x10, 12345); Memory.Int(Player + 0x58, 3); Memory.Int(Player + 0x5C, 210); Memory.Int(Station + 0x30, 18);
            Memory.Int(Player + 0x18, saveSlot); Memory.Int(Player + 0x38, 1);
            Memory.Long(Player + 0x30, runId == null ? 0 : 0x600000);
            if (runId != null) Memory.String(0x600000, runId);
            Memory.Long(Player + 0x110, 0x610000); Memory.Long(0x610010, 0x620000); Memory.Int(0x610018, 6); Memory.Long(0x620018, 8);
            for (int i = 0; i < 6; i++)
            {
                long obj = Rep + i * 0x1000, id = 0x700000 + i * 0x1000, display = id + 0x400;
                Memory.Long(0x620020 + i * 8, obj); Memory.Long(obj, 0x530000); Memory.Double(obj + 0x10, 24.5 + i);
                Memory.Long(obj + 0x20, id); Memory.String(id, "Faction" + i); Memory.Long(obj + 0x18, display); Memory.String(display, "测试势力 " + i);
            }
            Session = new Session(Memory, Base); Session.Connect();
        }
        void MakeType(long rva, long klass, long instance, string name)
        {
            Memory.Long(Base + rva, klass); Memory.Long(klass + 0x10, klass + 0x403);
            Memory.Put(klass + 0x403, Encoding.ASCII.GetBytes(name + "\0"));
            Memory.Long(klass + 0x18, klass + 0x480); Memory.Put(klass + 0x480, new byte[] { 0 });
            Memory.Long(klass + 0xB8, klass + 0x800); Memory.Long(klass + 0x800, instance); Memory.Long(instance, klass);
        }
    }
    public static class Tests
    {
        [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr VirtualAlloc(IntPtr address, UIntPtr size, uint allocation, uint protection);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool VirtualFree(IntPtr address, UIntPtr size, uint freeType);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool VirtualProtect(IntPtr address, UIntPtr size, uint protection, out uint previous);
        static readonly List<string> results = new List<string>();
        static int failed;
        static void Assert(bool condition, string description) { if (!condition) throw new Exception(description); }
        static void Reject(Action action)
        {
            bool threw = false; try { action(); } catch { threw = true; } Assert(threw, "Expected rejection");
        }
        static void Test(string name, Action action)
        {
            try { action(); results.Add("PASS " + name); }
            catch (Exception ex) { failed++; results.Add("FAIL " + name + ": " + ex.Message); }
        }
        public static int RunPatchHelper(string folder)
        {
            IntPtr page = VirtualAlloc(IntPtr.Zero, new UIntPtr(4096), 0x3000, 0x04);
            if (page == IntPtr.Zero) return 2;
            try
            {
                byte[] original = Enumerable.Range(16, 16).Select(i => (byte)i).ToArray();
                Marshal.Copy(original, 0, page, original.Length);
                uint old;
                if (!VirtualProtect(page, new UIntPtr(4096), 0x20, out old)) return 3;
                File.WriteAllText(Path.Combine(folder, "ready.txt"), page.ToInt64().ToString(System.Globalization.CultureInfo.InvariantCulture));
                for (int i = 0; i < 1000; i++)
                {
                    if (File.Exists(Path.Combine(folder, "stop.txt")))
                    {
                        byte[] current = new byte[16]; Marshal.Copy(page, current, 0, current.Length);
                        bool protection = VirtualProtect(page, new UIntPtr(4096), 0x20, out old) && old == 0x20;
                        return current.SequenceEqual(original) && protection ? 0 : 4;
                    }
                    System.Threading.Thread.Sleep(10);
                }
                return 5;
            }
            finally { VirtualFree(page, UIntPtr.Zero, 0x8000); }
        }
        public static int Run(string report)
        {
            Test("Loaded reputation objects connect with the real unresolved TypeInfo token", delegate
            {
                var f = new Fixture(); f.Memory.Long(Fixture.Base + BuildProfile.ReputationType, 0x2000A5A3);
                var snapshot = f.Session.Connect();
                Assert(snapshot.Factions.Count == 6 && snapshot.Factions[0].Display == "测试势力 0", "loaded objects remain readable");
                Assert(f.Memory.Writes == 0 && f.Memory.CodeWrites == 0, "read-only connection");
                Assert(BitConverter.ToInt64(f.Memory.Read(Fixture.Base + BuildProfile.ReputationType, 8), 0) == 0x2000A5A3, "metadata slot stays untouched");
            });
            Test("Lazy reputation metadata permits numeric edits and reputation undo", delegate
            {
                var f = new Fixture(); f.Memory.Long(Fixture.Base + BuildProfile.ReputationType, 0x2000A5A3); f.Session.Connect();
                f.Session.Apply("cash", 45678, null); f.Session.Apply("day", 20, null); f.Session.Apply("rent", 30, null);
                f.Session.Apply("reputation", -40.25m, "Faction2");
                var snapshot = f.Session.Refresh();
                Assert(snapshot.Cash == 45678 && snapshot.Day == 20 && snapshot.RentDays == 30 && snapshot.Factions[2].Amount == -40.25, "edits target existing objects");
                f.Session.Undo(); snapshot = f.Session.Refresh();
                Assert(snapshot.Factions[2].Amount == 26.5 && snapshot.Factions[1].Amount == 25.5, "undo preserves faction values");
            });
            Test("Object-derived reputation classes still reject wrong names, null and mixed classes", delegate
            {
                var f = new Fixture(); f.Memory.Long(Fixture.Base + BuildProfile.ReputationType, 0x2000A5A3);
                f.Memory.Put(0x530403, Encoding.ASCII.GetBytes("OtherReputation\0"));
                Reject(delegate { f.Session.Apply("cash", 1, null); }); Assert(f.Memory.Writes == 0, "wrong class name rejected");
                f = new Fixture(); f.Memory.Put(0x530480, Encoding.ASCII.GetBytes("UnrelatedNamespace\0"));
                Reject(delegate { f.Session.Apply("cash", 1, null); }); Assert(f.Memory.Writes == 0, "same-name class in another namespace rejected");
                f = new Fixture(); f.Memory.Long(Fixture.Rep, 0);
                Reject(delegate { f.Session.Apply("reputation", 1, null); }); Assert(f.Memory.Writes == 0, "null class rejected");
                f = new Fixture(); f.Memory.Long(Fixture.Rep + 0x1000, 0x580000);
                f.Memory.Long(0x580010, 0x580403); f.Memory.Put(0x580403, Encoding.ASCII.GetBytes("StoreReputation\0"));
                Reject(delegate { f.Session.Apply("reputation", 1, null); }); Assert(f.Memory.Writes == 0, "same-name mixed classes rejected");
            });
            Test("Reputation diagnostics inspect loaded objects without resolving or writing metadata", delegate
            {
                var f = new Fixture(); f.Memory.Long(Fixture.Base + BuildProfile.ReputationType, 0x2000A5A3);
                string diagnostic = f.Session.ConnectionReport(new IOException("test failure"));
                Assert(diagnostic.Contains("Reputation type: OK") && diagnostic.Contains("storeReputations[0].class"), "actual reputation class diagnosed");
                Assert(!diagnostic.Contains("Reputation type: IOException") && !diagnostic.Contains("测试势力"), "no stale-slot failure or save text");
                Assert(f.Memory.Writes == 0 && f.Memory.CodeWrites == 0, "diagnostic reads only");
            });
            Test("Connection failure identifies the exact null pointer without writing memory", delegate
            {
                var f = new Fixture(); f.Memory.Long(0x520800, 0);
                Exception failure = null;
                try { f.Session.Connect(); } catch (IOException ex) { failure = ex; }
                Assert(failure != null && failure.Message.Contains("EmporiumEntry.Instance") && failure.Message.Contains("0x520800 -> 0x0"), "exact null location");
                string diagnostic = f.Session.ConnectionReport(failure);
                Assert(diagnostic.Contains("EmporiumEntry.Instance @ 0x520800 = 0x0"), "failed pointer recorded");
                Assert(diagnostic.Contains("Shop: IOException") && diagnostic.Contains("Player: OK") && diagnostic.Contains("Station: OK"), "independent probes continue after failure");
                Assert(f.Memory.Writes == 0 && f.Memory.CodeWrites == 0, "read-only diagnosis");
            });
            Test("Connection failure distinguishes corrupt and unreadable pointers in both languages", delegate
            {
                string previous = L.Language;
                try
                {
                    L.SetLanguage("en"); var f = new Fixture(); f.Memory.Long(Fixture.Player + 0x30, 0x123);
                    Exception failure = null;
                    try { f.Session.Connect(); } catch (IOException ex) { failure = ex; }
                    Assert(failure != null && failure.Message.StartsWith("Invalid object pointer: PlayerStore.runID"), "corrupt pointer in English");
                    f = new Fixture(); f.Memory.BeforeRead = delegate(long address, int size)
                    {
                        if (address == Fixture.Base + BuildProfile.StationType) throw new IOException("Simulated unreadable pointer");
                    };
                    failure = null;
                    try { f.Session.Connect(); } catch (IOException ex) { failure = ex; }
                    Assert(failure != null && failure.Message.StartsWith("Cannot read object: StoreStation.TypeInfo"), "unreadable field identified");
                    Assert(f.Session.ConnectionReport(failure).Contains("READ FAILED: Simulated unreadable pointer"), "original read error preserved");
                    L.SetLanguage("zh-CN"); f = new Fixture(); f.Memory.Long(Fixture.Entry + 0x10, 0);
                    try { f.Session.Connect(); } catch (IOException ex) { failure = ex; }
                    Assert(failure.Message.StartsWith("对象为空：EmporiumEntry.m_CachedPtr"), "Chinese field-specific error");
                    Assert(f.Memory.Writes == 0 && f.Memory.CodeWrites == 0, "no writes");
                }
                finally { L.SetLanguage(previous); }
            });
            Test("Connection report excludes game-owned strings and preserves optional null run IDs", delegate
            {
                var f = new Fixture("private-run-marker");
                string diagnostic = f.Session.ConnectionReport(new IOException("test failure"));
                Assert(!diagnostic.Contains("private-run-marker") && !diagnostic.Contains("测试势力"), "no save string values");
                f.Memory.Long(Fixture.Player + 0x30, 0);
                Assert(f.Session.Connect().RunId == "", "optional null run ID still accepted");
                diagnostic = f.Session.ConnectionReport(new IOException("test failure"));
                Assert(diagnostic.Contains("Player: OK") && diagnostic.Contains("PlayerStore.runID @ 0x100030 = 0x0"), "null optional field reported without rejection");
                Assert(f.Memory.Writes == 0 && f.Memory.CodeWrites == 0, "report reads only");
            });
            Test("Diagnostic log fallback preserves UTF-8 and never replaces an existing report", delegate
            {
                string folder = Path.Combine(Path.GetTempPath(), "ProbablyStolenDiagnosticTest-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(folder);
                string blocked = Path.Combine(folder, "blocked"), fallback = Path.Combine(folder, "logs");
                File.WriteAllText(blocked, "file used to force directory failure");
                try
                {
                    string first = ConnectionDiagnostics.Save("中文诊断", blocked, fallback);
                    string second = ConnectionDiagnostics.Save("second report", fallback);
                    Assert(Path.GetDirectoryName(first) == fallback && File.ReadAllText(first) == "中文诊断", "fallback and UTF-8");
                    Assert(first != second && File.ReadAllText(first) == "中文诊断", "unique reports preserve previous evidence");
                    Reject(delegate { ConnectionDiagnostics.Save("failure", blocked); });
                }
                finally
                {
                    if (Directory.Exists(fallback)) { foreach (string file in Directory.GetFiles(fallback)) File.Delete(file); Directory.Delete(fallback); }
                    File.Delete(blocked); Directory.Delete(folder);
                }
            });
            Test("Item library is opt-in, reads the actual hotkey and never enables global dev mode", delegate
            {
                var f = new Fixture(); Assert(!f.Session.ItemLibraryEnabled && f.Memory.CodeWrites == 0, "default off");
                f.Session.SetItemLibraryEnabled(true);
                Assert(f.Session.ItemLibraryEnabled && f.Session.ItemMenuKey == "F10", "enabled and actual key");
                foreach (var change in ItemLibrary.Profile(Fixture.Base)) Assert(f.Memory.Read(change.Address, change.After.Length).SequenceEqual(change.After), "verified patches");
                Assert(f.Memory.Read(Fixture.Player + 0x122, 1)[0] == 0 && f.Memory.Writes == 0, "no global developer mode or item memory writes");
                int count = f.Memory.CodeWrites; f.Session.SetItemLibraryEnabled(true); Assert(f.Memory.CodeWrites == count, "idempotent enable");
                f.Session.SetItemLibraryEnabled(false);
                Assert(!f.Session.ItemLibraryEnabled, "disabled");
                foreach (var change in ItemLibrary.Profile(Fixture.Base)) Assert(f.Memory.Read(change.Address, change.Before.Length).SequenceEqual(change.Before), "restored");
            });
            Test("Library branch skips all later debug actions using the original function epilogue", delegate
            {
                var changes = ItemLibrary.Profile(Fixture.Base);
                var tail = changes.Single(c => c.Address == Fixture.Base + BuildProfile.ItemMenuTail);
                Assert(tail.After[0] == 0xE9 && tail.Address + 5 + BitConverter.ToInt32(tail.After, 1) == Fixture.Base + 0x7B4613, "epilogue target");
                var gate = changes.Single(c => c.Address == Fixture.Base + BuildProfile.ItemMenuGate);
                Assert(gate.Before.Where((b, i) => b != gate.After[i]).Count() == 1 && gate.After[6] == 255, "only comparison immediate differs");
            });
            Test("Unavailable warehouse, uninitialized menu or missing hotkey rejects before patching", delegate
            {
                foreach (int kind in new[] { 0, 1, 2 })
                {
                    var f = new Fixture();
                    if (kind == 0) f.Memory.Long(Fixture.Entry + 0x98, 0);
                    if (kind == 1) f.Memory.Put(0xA0001D, new byte[] { 0 });
                    if (kind == 2) f.Memory.Int(0xA00020, 0);
                    Reject(delegate { f.Session.SetItemLibraryEnabled(true); });
                    Assert(f.Memory.CodeWrites == 0 && !f.Session.ItemLibraryEnabled, "no patch");
                }
            });
            Test("Foreign menu code rejects the entire transaction before any write", delegate
            {
                var f = new Fixture(); f.Memory.Put(Fixture.Base + BuildProfile.ItemMenuGate, new byte[] { 0x90 });
                Reject(delegate { f.Session.SetItemLibraryEnabled(true); });
                Assert(f.Memory.CodeWrites == 0 && !f.Session.ItemLibraryEnabled, "foreign code preserved");
            });
            Test("Failed menu patch transactions roll back partial and completed regions", delegate
            {
                foreach (bool after in new[] { false, true })
                foreach (int position in new[] { 1, 3, 12, 22 })
                {
                    var f = new Fixture(); f.Memory.FailOnCodeWrite = position; f.Memory.WriteThenFail = after;
                    Reject(delegate { f.Session.SetItemLibraryEnabled(true); });
                    foreach (var c in ItemLibrary.Profile(Fixture.Base)) Assert(f.Memory.Read(c.Address, c.Before.Length).SequenceEqual(c.Before), "full rollback");
                    Assert(!f.Session.ItemLibraryEnabled, "no lingering ownership after rollback");
                }
            });
            Test("Busy item menu defers patching and can be retried", delegate
            {
                var f = new Fixture(); f.Memory.PatchBusy = true;
                Reject(delegate { f.Session.SetItemLibraryEnabled(true); }); Assert(f.Memory.CodeWrites == 0, "busy enable skipped");
                f.Memory.PatchBusy = false; f.Session.SetItemLibraryEnabled(true); f.Memory.PatchBusy = true;
                Reject(delegate { f.Session.SetItemLibraryEnabled(false); }); Assert(f.Session.ItemLibraryEnabled, "busy restore remains owned");
                f.Memory.PatchBusy = false; f.Session.SetItemLibraryEnabled(false); Assert(!f.Session.ItemLibraryEnabled, "retry succeeds");
            });
            Test("Library and appraisal restore independently on disconnect, including after a save change", delegate
            {
                var f = new Fixture(); f.Session.SetItemLibraryEnabled(true); f.Session.SetAppraisalVisible(true);
                f.Memory.String(0x600000, "another-run"); f.Session.Dispose();
                foreach (var c in ItemLibrary.Profile(Fixture.Base)) Assert(f.Memory.Read(c.Address, c.Before.Length).SequenceEqual(c.Before), "library restored");
                Assert(f.Memory.Read(Fixture.Base + BuildProfile.AppraisalTooltipGate + 8, 1)[0] == 0x7E && String.IsNullOrEmpty(f.Session.CloseWarning), "appraisal restored");
            });
            Test("Foreign library conflict is preserved without preventing appraisal cleanup", delegate
            {
                var f = new Fixture(); f.Session.SetItemLibraryEnabled(true); f.Session.SetAppraisalVisible(true);
                f.Memory.Put(Fixture.Base + BuildProfile.ItemMenuTail, new byte[] { 0x90 }); f.Session.Dispose();
                Assert(f.Memory.Read(Fixture.Base + BuildProfile.ItemMenuTail, 1)[0] == 0x90 && !String.IsNullOrEmpty(f.Session.CloseWarning), "conflict reported");
                Assert(f.Memory.Read(Fixture.Base + BuildProfile.AppraisalTooltipGate + 8, 1)[0] == 0x7E, "independent appraisal restoration");
            });
            Test("Game exit skips library writes and overlapping code changes are rejected", delegate
            {
                var f = new Fixture(); f.Session.SetItemLibraryEnabled(true); int writes = f.Memory.CodeWrites;
                f.Memory.Alive = false; f.Session.Dispose(); Assert(f.Memory.CodeWrites == writes, "no writes after exit");
                var m = new FakeMemory(); m.Put(0x10000, new byte[] { 1, 2, 3 });
                Reject(delegate { m.ApplyCodeChanges(new[] { new CodeChange(0x10000, new byte[] { 1, 2 }, new byte[] { 3, 4 }), new CodeChange(0x10001, new byte[] { 2, 3 }, new byte[] { 4, 5 }) }, 0, 0); });
                Assert(m.CodeWrites == 0, "overlap rejected before write");
            });
            Test("Native code transaction suspends and resumes an isolated helper, preserves RX protection", delegate
            {
                string folder = Path.Combine(Path.GetTempPath(), "ProbablyStolenPatchTest-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(folder); Process helper = null;
                try
                {
                    helper = Process.Start(new ProcessStartInfo(System.Reflection.Assembly.GetExecutingAssembly().Location, "--patch-helper \"" + folder + "\"") { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden });
                    string ready = Path.Combine(folder, "ready.txt");
                    for (int i = 0; i < 100 && !File.Exists(ready) && !helper.HasExited; i++) System.Threading.Thread.Sleep(20);
                    Assert(File.Exists(ready) && !helper.HasExited, "helper started");
                    long address = long.Parse(File.ReadAllText(ready), System.Globalization.CultureInfo.InvariantCulture);
                    var changes = new[] { new CodeChange(address, new byte[] { 16, 17, 18, 19 }, new byte[] { 1, 2, 3, 4 }), new CodeChange(address + 8, new byte[] { 24, 25, 26, 27 }, new byte[] { 5, 6, 7, 8 }) };
                    using (var memory = new ProcessMemory(helper.Id))
                    {
                        // Mark every user address busy to verify the real thread-context guard and finally-resume path.
                        bool busy = false;
                        try { memory.ApplyCodeChanges(changes, 0x10000, 0x0000800000000000L); }
                        catch (IOException ex) { busy = ex.Message == L.T("物品菜单正在处理操作，请松开按键并稍后重试。"); }
                        Assert(busy, "native instruction pointer guard rejects active code");
                        Assert(memory.Read(address, 4).SequenceEqual(changes[0].Before), "busy rejection made no change");
                        memory.ApplyCodeChanges(changes, 0, 0);
                        Assert(changes.All(c => memory.Read(c.Address, c.After.Length).SequenceEqual(c.After)), "native patch");
                        memory.ApplyCodeChanges(changes.Reverse().Select(c => c.Reverse()).ToArray(), 0, 0);
                    }
                    File.WriteAllText(Path.Combine(folder, "stop.txt"), "stop");
                    Assert(helper.WaitForExit(3000) && helper.ExitCode == 0, "helper resumed; original bytes and RX protection restored");
                }
                finally
                {
                    if (helper != null) { if (!helper.HasExited) { helper.Kill(); helper.WaitForExit(3000); } helper.Dispose(); }
                    foreach (string file in Directory.GetFiles(folder)) File.Delete(file); Directory.Delete(folder);
                }
            });
            Test("Trainer refuses to suspend itself", delegate
            {
                using (var memory = new ProcessMemory(Process.GetCurrentProcess().Id))
                    Reject(delegate { memory.ApplyCodeChanges(ItemLibrary.Profile(Fixture.Base), 0, 0); });
            });
            Test("Known Unity key codes map to readable item-menu shortcuts", delegate
            {
                Assert(ItemLibrary.KeyName(282) == "F1" && ItemLibrary.KeyName(291) == "F10" && ItemLibrary.KeyName(96) == "`" && ItemLibrary.KeyName(97) == "A", "key names");
                Reject(delegate { ItemLibrary.KeyName(0); });
            });
            Test("Loaded game with empty, whitespace or null run ID connects and edits correctly", delegate
            {
                foreach (string runId in new string[] { "", "   ", null })
                {
                    var f = new Fixture(runId);
                    Assert(f.Memory.Writes == 0 && f.Memory.CodeWrites == 0, "connection is read-only");
                    f.Session.Apply("cash", 4567, null); f.Session.Apply("day", 20, null);
                    f.Session.Apply("rent", 10, null); f.Session.Apply("reputation", 100, "Faction2");
                    var s = f.Session.Refresh();
                    Assert(s.Cash == 4567 && s.Day == 20 && s.RentDays == 10 && s.Factions[2].Amount == 100, "edits work without run ID");
                    f.Session.Undo(); Assert(f.Session.Refresh().Factions[2].Amount == 26.5, "undo retains original value");
                }
            });
            Test("New game with unassigned save slot and empty run ID is supported", delegate
            {
                var f = new Fixture("", -1); f.Session.Apply("cash", 800, null);
                Assert(f.Session.Refresh().Cash == 800, "not-yet-saved game");
            });
            Test("Save slot or run number change with empty run ID blocks stale edits", delegate
            {
                foreach (long field in new long[] { 0x18, 0x38 })
                {
                    var f = new Fixture(""); f.Session.Apply("cash", 400, null);
                    f.Memory.Int(Fixture.Player + field, 2);
                    Reject(delegate { f.Session.Apply("cash", 500, null); });
                    Assert(f.Memory.Writes == 1 && !f.Session.CanUndo, "save identity change clears undo without another write");
                }
            });
            Test("Native scene replacement invalidates the session even when managed addresses match", delegate
            {
                var f = new Fixture(); f.Session.Apply("cash", 400, null);
                f.Memory.Long(Fixture.Entry + 0x10, 0x900100);
                Reject(delegate { f.Session.Undo(); });
                Assert(f.Memory.Writes == 1 && !f.Session.CanUndo, "scene lifetime changed");
            });
            Test("Save identity changes during a snapshot are rejected and clear pending undo", delegate
            {
                var f = new Fixture(); f.Session.Apply("cash", 400, null);
                f.Memory.BeforeRead = delegate(long address, int size)
                {
                    if (address == Fixture.Player + 0x10) { f.Memory.BeforeRead = null; f.Memory.Int(Fixture.Player + 0x18, 7); }
                };
                Reject(delegate { f.Session.Refresh(); });
                Assert(f.Memory.Writes == 1 && !f.Session.CanUndo, "torn snapshot not accepted");
            });
            Test("Nullable run ID does not permit invalid non-null pointers or missing scene objects", delegate
            {
                var f = new Fixture(); f.Memory.Long(Fixture.Player + 0x30, 0x123);
                Reject(delegate { f.Session.Apply("cash", 1, null); }); Assert(f.Memory.Writes == 0, "bad pointer rejected");
                f = new Fixture(); f.Memory.Long(Fixture.Player + 0x30, 0); f.Memory.Long(Fixture.Entry + 0x10, 0);
                Reject(delegate { f.Session.Connect(); }); Assert(f.Memory.Writes == 0, "scene still required");
            });
            Test("Normal save timestamps and elapsed play time do not invalidate the session", delegate
            {
                var f = new Fixture(); f.Session.Apply("cash", 400, null);
                f.Memory.Int(Fixture.Player + 0x40, 100); f.Memory.Int(Fixture.Player + 0x44, 200);
                f.Memory.Int(Fixture.Player + 0x48, 300); f.Memory.Long(Fixture.Player + 0x50, 400);
                Assert(f.Session.Refresh().Cash == 400 && f.Session.CanUndo, "ordinary saving keeps the connection");
                f.Session.Undo(); Assert(f.Session.Refresh().Cash == 12345, "undo still available");
            });
            Test("Read verified pointer chains and Unicode factions without any write", delegate
            {
                var f = new Fixture(); var s = f.Session.Refresh();
                Assert(s.Cash == 12345 && s.Day == 18 && s.RentDays == 3 && s.Factions.Count == 6, "snapshot");
                Assert(s.Factions[0].Display == "测试势力 0" && f.Memory.Writes == 0, "unicode/read only");
            });
            Test("Money edit changes only the intended 4 bytes and supports undo", delegate
            {
                var f = new Fixture(); f.Session.Apply("cash", 999999, null); var s = f.Session.Refresh();
                Assert(s.Cash == 999999 && s.Day == 18 && s.RentDays == 3 && s.Factions[0].Amount == 24.5, "unrelated fields");
                f.Session.Undo(); Assert(f.Session.Refresh().Cash == 12345 && !f.Session.CanUndo, "undo");
            });
            Test("Day and rent countdown are independent", delegate
            {
                var f = new Fixture(); f.Session.Apply("day", 45, null); f.Session.Apply("rent", 100, null);
                var s = f.Session.Refresh(); Assert(s.Day == 45 && s.RentDays == 100 && s.RentValue == 210, "countdowns");
                f.Session.Undo(); s = f.Session.Refresh(); Assert(s.Day == 45 && s.RentDays == 3, "undo latest only");
            });
            Test("Selected faction is addressed by identity, not UI index", delegate
            {
                var f = new Fixture(); f.Session.Apply("reputation", -100.25m, "Faction3"); var s = f.Session.Refresh();
                Assert(s.Factions[3].Amount == -100.25 && s.Factions[0].Amount == 24.5 && s.Factions[4].Amount == 28.5, "selected faction");
            });
            Test("All-faction edit and undo preserve fractional originals", delegate
            {
                var f = new Fixture(); f.Session.Apply("reputation", 200, null); Assert(f.Session.Refresh().Factions.All(x => x.Amount == 200), "all factions");
                f.Session.Undo(); Assert(f.Session.Refresh().Factions.Select((x, i) => x.Amount == 24.5 + i).All(x => x), "fractional originals");
            });
            Test("Out-of-range, fractional integer, unknown field and missing faction are refused before writing", delegate
            {
                var f = new Fixture();
                Reject(delegate { f.Session.Apply("cash", -1, null); }); Reject(delegate { f.Session.Apply("cash", 1000000000, null); });
                Reject(delegate { f.Session.Apply("day", 2.5m, null); }); Reject(delegate { f.Session.Apply("rent", 10000, null); });
                Reject(delegate { f.Session.Apply("reputation", 201, null); }); Reject(delegate { f.Session.Apply("unknown", 1, null); });
                Reject(delegate { f.Session.Apply("reputation", 1, "missing"); }); Assert(f.Memory.Writes == 0, "no writes");
            });
            Test("Changing the save identity prevents edits and clears undo", delegate
            {
                var f = new Fixture(); f.Session.Apply("cash", 400, null); f.Memory.String(0x600000, "fixture-run-2");
                Reject(delegate { f.Session.Apply("cash", 500, null); }); Assert(f.Memory.Writes == 1 && !f.Session.CanUndo, "stale save");
            });
            Test("Undo refuses to overwrite values changed by gameplay", delegate
            {
                var f = new Fixture(); f.Session.Apply("cash", 500, null); f.Memory.Int(Fixture.Player + 0x10, 490);
                Reject(delegate { f.Session.Undo(); }); Assert(f.Session.Refresh().Cash == 490 && f.Memory.Writes == 1, "gameplay preserved");
            });
            Test("Failed multi-faction write rolls back earlier successful writes", delegate
            {
                var f = new Fixture(); f.Memory.FailOnWrite = 3; Reject(delegate { f.Session.Apply("reputation", 200, null); });
                Assert(f.Session.Refresh().Factions.Select((x, i) => x.Amount == 24.5 + i).All(x => x), "rollback");
            });
            Test("Destroyed Unity scene object blocks access", delegate
            {
                var f = new Fixture(); f.Memory.Long(Fixture.Entry + 0x10, 0); Reject(delegate { f.Session.Apply("cash", 50, null); }); Assert(f.Memory.Writes == 0, "no write");
            });
            Test("Corrupt type name and wrong faction class are refused", delegate
            {
                var f = new Fixture(); f.Memory.Put(0x500403, Encoding.ASCII.GetBytes("OtherStore\0")); Reject(delegate { f.Session.Refresh(); });
                f = new Fixture(); f.Memory.Long(Fixture.Rep, 0x500000); Reject(delegate { f.Session.Apply("reputation", 10, null); }); Assert(f.Memory.Writes == 0, "type mismatch");
            });
            Test("Uninitialized metadata pointers and malformed array length are refused", delegate
            {
                var f = new Fixture(); f.Memory.Long(Fixture.Base + BuildProfile.PlayerType, 0x123); Reject(delegate { f.Session.Refresh(); });
                f = new Fixture(); f.Memory.Int(0x610018, 900); Reject(delegate { f.Session.Refresh(); });
                f = new Fixture(); f.Memory.Long(0x620018, 2); Reject(delegate { f.Session.Refresh(); });
            });
            Test("Duplicate faction IDs, oversized strings and NaN values are refused", delegate
            {
                var f = new Fixture(); f.Memory.String(0x701000, "Faction0"); Reject(delegate { f.Session.Refresh(); });
                f = new Fixture(); f.Memory.Int(0x600010, 100000); Reject(delegate { f.Session.Refresh(); });
                f = new Fixture(); f.Memory.Double(Fixture.Rep + 0x10, double.NaN); Reject(delegate { f.Session.Refresh(); });
            });
            Test("Changed faction object prevents stale-address undo", delegate
            {
                var f = new Fixture(); f.Session.Apply("reputation", 100, "Faction0");
                long replacement = 0x480000;
                foreach (long offset in new long[] { 0, 0x10, 0x18, 0x20 }) f.Memory.Put(replacement + offset, f.Memory.Read(Fixture.Rep + offset, 8));
                f.Memory.Long(0x620020, replacement); Reject(delegate { f.Session.Undo(); }); Assert(f.Memory.Writes == 1, "stale address");
            });
            Test("Native Win32 process read/write round trip on this test process only", delegate
            {
                IntPtr region = Marshal.AllocHGlobal(8);
                try
                {
                    Marshal.WriteInt64(region, 123456);
                    using (var memory = new ProcessMemory(Process.GetCurrentProcess().Id))
                    {
                        Assert(BitConverter.ToInt64(memory.Read(region.ToInt64(), 8), 0) == 123456, "native read");
                        memory.Write(region.ToInt64(), BitConverter.GetBytes(987654L)); Assert(Marshal.ReadInt64(region) == 987654, "native write");
                    }
                }
                finally { Marshal.FreeHGlobal(region); }
            });
            Test("Different game binary is rejected", delegate
            {
                string path = Path.GetTempFileName(); try { File.WriteAllText(path, "test"); Reject(delegate { BuildProfile.Verify(path); }); } finally { File.Delete(path); }
            });
            Test("Process search supports names, exact PID and either drive letter", delegate
            {
                var choices = new[] {
                    new ProcessCandidate { Id = 123, Name = "Probably Stolen", ExecutablePath = @"C:\Games\Probably Stolen.exe" },
                    new ProcessCandidate { Id = 1234, Name = "Probably Stolen", ExecutablePath = @"D:\Games\Probably Stolen.exe" },
                    new ProcessCandidate { Id = 555, Name = "AnotherGame", ExecutablePath = @"C:\Games\AnotherGame.exe" }
                };
                Assert(ProcessCatalog.Filter(choices, "PROBABLY").Count == 2, "case insensitive name");
                Assert(ProcessCatalog.Filter(choices, "123").Single().Id == 123, "exact PID");
                Assert(ProcessCatalog.Filter(choices, @"C:\Games\Probably").Single().Id == 123, "C drive");
                Assert(ProcessCatalog.Filter(choices, @"D:\Games\Probably").Single().Id == 1234, "D drive");
                Assert(ProcessCatalog.Filter(choices, "").Count == 3, "all processes");
            });
            Test("Multiple game instances remain individually selectable", delegate
            {
                var choices = new[] {
                    new ProcessCandidate { Id = 41, Name = "Probably Stolen", ExecutablePath = "game", StartedUtcTicks = 100 },
                    new ProcessCandidate { Id = 42, Name = "Probably Stolen", ExecutablePath = "game", StartedUtcTicks = 200 }
                };
                var filtered = ProcessCatalog.Filter(choices, "Probably");
                Assert(filtered.Count == 2 && filtered[0].Id == 41 && filtered[1].Id == 42, "distinct identities");
            });
            Test("Live process enumeration returns the test process and its start identity", delegate
            {
                using (var own = Process.GetCurrentProcess())
                {
                    var entry = ProcessCatalog.Capture().Single(p => p.Id == own.Id);
                    Assert(entry.StartedUtcTicks == own.StartTime.ToUniversalTime().Ticks, "start identity");
                    Assert(entry.ExecutablePath == own.MainModule.FileName, "actual path");
                }
            });
            Test("Stale selected PID identity is rejected before connection", delegate
            {
                using (var own = Process.GetCurrentProcess())
                {
                    bool rejected = false;
                    try { Session.Attach(new ProcessCandidate { Id = own.Id, StartedUtcTicks = own.StartTime.ToUniversalTime().Ticks + 1 }); }
                    catch (IOException ex) { rejected = ex.Message.Contains("重新启动"); }
                    Assert(rejected, "stale selection rejected");
                }
            });
            Test("Selecting a non-game process is refused without opening a memory session", delegate
            {
                using (var own = Process.GetCurrentProcess())
                {
                    bool rejected = false;
                    try { Session.Attach(new ProcessCandidate { Id = own.Id, StartedUtcTicks = own.StartTime.ToUniversalTime().Ticks }); }
                    catch (IOException ex) { rejected = ex.Message.Contains("GameAssembly.dll"); }
                    Assert(rejected, "non game rejected");
                }
            });
            Test("Appraisal reveal is opt-in and changes only one branch displacement byte", delegate
            {
                var f = new Fixture(); long gate = Fixture.Base + BuildProfile.AppraisalTooltipGate;
                var before = f.Memory.Read(gate, 15);
                Assert(!f.Session.AppraisalVisible && f.Memory.CodeWrites == 0, "off by default");
                f.Session.SetAppraisalVisible(true); var after = f.Memory.Read(gate, 15);
                Assert(f.Session.AppraisalVisible && after[8] == 0 && after[7] == 0x74, "zero displacement");
                Assert(before.Where((b, i) => i != 8).SequenceEqual(after.Where((b, i) => i != 8)), "surrounding code unchanged");
                Assert(f.Memory.Writes == 0 && f.Session.Refresh().Cash == 12345, "gameplay fields untouched");
                f.Session.SetAppraisalVisible(true); Assert(f.Memory.CodeWrites == 1, "idempotent enable");
            });
            Test("Disabling and disposing restore the original tooltip gate", delegate
            {
                var f = new Fixture(); long gate = Fixture.Base + BuildProfile.AppraisalTooltipGate;
                var before = f.Memory.Read(gate, 15);
                f.Session.SetAppraisalVisible(true); f.Session.SetAppraisalVisible(false);
                Assert(!f.Session.AppraisalVisible && before.SequenceEqual(f.Memory.Read(gate, 15)), "disable restored");
                f.Session.SetAppraisalVisible(true); f.Session.Dispose();
                Assert(before.SequenceEqual(f.Memory.Read(gate, 15)) && String.IsNullOrEmpty(f.Session.CloseWarning), "dispose restored");
            });
            Test("Mismatched tooltip signature is rejected without code writes", delegate
            {
                var f = new Fixture(); f.Memory.Put(Fixture.Base + BuildProfile.AppraisalTooltipGate, new byte[] { 0x90 });
                Reject(delegate { f.Session.SetAppraisalVisible(true); }); Assert(f.Memory.CodeWrites == 0, "no code write");
            });
            Test("Failed tooltip writes restore both before-write and after-write failures", delegate
            {
                foreach (bool partial in new[] { false, true })
                {
                    var f = new Fixture(); long gate = Fixture.Base + BuildProfile.AppraisalTooltipGate;
                    var before = f.Memory.Read(gate, 15); f.Memory.FailOnCodeWrite = 1; f.Memory.WriteThenFail = partial;
                    Reject(delegate { f.Session.SetAppraisalVisible(true); });
                    Assert(!f.Session.AppraisalVisible && before.SequenceEqual(f.Memory.Read(gate, 15)), "failure restored");
                }
            });
            Test("Failed disable remains owned and can be retried", delegate
            {
                var f = new Fixture(); f.Session.SetAppraisalVisible(true); f.Memory.FailOnCodeWrite = 2;
                Reject(delegate { f.Session.SetAppraisalVisible(false); }); Assert(f.Session.AppraisalVisible, "still owned");
                f.Session.SetAppraisalVisible(false); Assert(!f.Session.AppraisalVisible, "retry succeeded");
            });
            Test("Foreign tooltip changes are preserved and reported on disposal", delegate
            {
                var f = new Fixture(); f.Session.SetAppraisalVisible(true);
                f.Memory.Put(Fixture.Base + BuildProfile.AppraisalTooltipGate, new byte[] { 0x90 }); f.Session.Dispose();
                Assert(f.Memory.CodeWrites == 1 && !String.IsNullOrEmpty(f.Session.CloseWarning), "no overwrite of foreign changes");
            });
            Test("Game exit skips unnecessary tooltip restoration writes", delegate
            {
                var f = new Fixture(); f.Session.SetAppraisalVisible(true); f.Memory.Alive = false; f.Session.Dispose();
                Assert(f.Memory.CodeWrites == 1 && String.IsNullOrEmpty(f.Session.CloseWarning), "exited game skipped");
            });
            Test("Tooltip restoration still works after a save identity change", delegate
            {
                var f = new Fixture(); f.Session.SetAppraisalVisible(true); f.Memory.String(0x600000, "new-run"); f.Session.Dispose();
                Assert(f.Memory.Read(Fixture.Base + BuildProfile.AppraisalTooltipGate + 8, 1)[0] == 0x7E, "scene-independent restore");
            });
            Test("Native single-byte code write restores execute-read page protection", delegate
            {
                IntPtr page = VirtualAlloc(IntPtr.Zero, new UIntPtr(4096), 0x3000, 0x20);
                Assert(page != IntPtr.Zero, "allocate dedicated RX test page");
                try
                {
                    using (var mem = new ProcessMemory(Process.GetCurrentProcess().Id))
                    {
                        mem.WriteCodeByte(page.ToInt64(), 0x7E); Assert(mem.Read(page.ToInt64(), 1)[0] == 0x7E, "byte write");
                        uint old; Assert(VirtualProtect(page, new UIntPtr(4096), 0x20, out old) && old == 0x20, "protection restored");
                        mem.WriteCodeByte(page.ToInt64(), 0); Assert(mem.Read(page.ToInt64(), 1)[0] == 0, "byte restore");
                    }
                }
                finally { VirtualFree(page, UIntPtr.Zero, 0x8000); }
            });
            Test("Language settings round trip, invalid preference fallback, unknown code rejection", delegate
            {
                string folder = Path.Combine(Path.GetTempPath(), "ProbablyStolenTrainerTest-" + Guid.NewGuid().ToString("N"));
                string settings = Path.Combine(folder, "language.txt");
                string previous = L.Language;
                try
                {
                    L.SetLanguage("en"); L.Save(settings); L.SetLanguage("zh-CN"); L.Load(settings);
                    Assert(L.Language == "en", "English preference round trip");
                    L.SetLanguage("zh-CN"); L.Save(settings); L.SetLanguage("en"); L.Load(settings);
                    Assert(L.Language == "zh-CN", "Chinese preference round trip");
                    File.WriteAllText(settings, "invalid"); L.Load(settings);
                    Assert(L.Language == "en" || L.Language == "zh-CN", "invalid preference fallback");
                    Reject(delegate { L.SetLanguage("invalid"); });
                }
                finally { L.SetLanguage(previous); if (File.Exists(settings)) File.Delete(settings); if (Directory.Exists(folder)) Directory.Delete(folder); }
            });
            Test("Compact v2 window keeps lower controls reachable through scrolling", delegate
            {
                using (var form = new TrainerForm())
                {
                    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                    ((System.Windows.Forms.Timer)typeof(TrainerForm).GetField("timer", flags).GetValue(form)).Stop();
                    typeof(TrainerForm).GetField("renderingPreview", flags).SetValue(form, true);
                    form.ShowInTaskbar = false; form.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
                    form.Location = new System.Drawing.Point(-20000, -20000); form.Show(); System.Windows.Forms.Application.DoEvents();
                    form.ClientSize = new System.Drawing.Size(820, 540); form.PerformLayout();
                    Assert(form.AutoScroll && form.AutoScrollMinSize.Width > form.ClientSize.Width && form.AutoScrollMinSize.Height > form.ClientSize.Height, "content can exceed a small viewport");
                    form.AutoScrollPosition = new System.Drawing.Point(220, 182); form.PerformLayout();
                    Assert(form.AutoScrollPosition.X < 0 && form.AutoScrollPosition.Y < 0, "horizontal and vertical scrolling work");
                    var log = (System.Windows.Forms.TextBox)typeof(TrainerForm).GetField("log", flags).GetValue(form);
                    var logPosition = form.PointToClient(log.PointToScreen(System.Drawing.Point.Empty));
                    Assert(logPosition.Y >= 0 && logPosition.Y + log.Height <= form.ClientSize.Height, "activity log is reachable");
                    form.Close();
                }
            });
            Test("Live UI language change preserves connection, edits, selection and appraisal state", delegate
            {
                string previous = L.Language;
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var fixture = new Fixture(); fixture.Session.Apply("cash", 777, null); fixture.Session.SetAppraisalVisible(true); fixture.Session.SetItemLibraryEnabled(true);
                try
                {
                    L.SetLanguage("zh-CN");
                    using (var form = new TrainerForm())
                    {
                        var type = typeof(TrainerForm);
                        ((System.Windows.Forms.Timer)type.GetField("timer", flags).GetValue(form)).Stop();
                        type.GetField("session", flags).SetValue(form, fixture.Session);
                        type.GetMethod("RefreshValues", flags).Invoke(form, new object[] { true });
                        type.GetMethod("SetConnected", flags).Invoke(form, new object[] { true });
                        type.GetMethod("SetStatus", flags).Invoke(form, new object[] { "已连接 · " });
                        var amount = (System.Windows.Forms.NumericUpDown)type.GetField("cash", flags).GetValue(form);
                        var factions = (System.Windows.Forms.ComboBox)type.GetField("factions", flags).GetValue(form);
                        var toggle = (System.Windows.Forms.CheckBox)type.GetField("appraisal", flags).GetValue(form);
                        type.GetField("changingAppraisal", flags).SetValue(form, true); toggle.Checked = true;
                        type.GetField("changingAppraisal", flags).SetValue(form, false);
                        var libraryToggle = (System.Windows.Forms.CheckBox)type.GetField("itemLibrary", flags).GetValue(form);
                        type.GetField("changingItemLibrary", flags).SetValue(form, true); libraryToggle.Checked = true;
                        type.GetField("changingItemLibrary", flags).SetValue(form, false);
                        amount.Value = 654321; factions.SelectedIndex = 2;
                        int writes = fixture.Memory.Writes, codeWrites = fixture.Memory.CodeWrites;
                        form.ChangeLanguage("en", false);
                        Assert(form.Text.Contains("Money / Time"), "English title");
                        Assert(((System.Windows.Forms.Label)type.GetField("currentCash", flags).GetValue(form)).Text.Contains("777"), "value retained");
                        Assert(((System.Windows.Forms.Label)type.GetField("status", flags).GetValue(form)).Text.StartsWith("Connected"), "connection status translated");
                        Assert(amount.Value == 654321 && factions.SelectedIndex == 2 && toggle.Checked, "UI inputs and toggle retained");
                        Assert(libraryToggle.Checked && fixture.Session.ItemLibraryEnabled && libraryToggle.Text == "Enable item library", "item library state and translation retained");
                        Assert(((System.Windows.Forms.Label)type.GetField("itemMenuKey", flags).GetValue(form)).Text == "In-game key: F10", "actual item-menu shortcut translated");
                        Assert(fixture.Session.CanUndo && fixture.Session.AppraisalVisible && ReferenceEquals(type.GetField("session", flags).GetValue(form), fixture.Session), "session retained");
                        Assert(fixture.Memory.Writes == writes && fixture.Memory.CodeWrites == codeWrites, "language switch writes no game memory");
                        form.ChangeLanguage("zh-CN", false);
                        Assert(form.Text.Contains("金钱") && toggle.Text == "直接显示鉴定物真实价值", "Chinese round trip");
                        Assert(amount.Value == 654321 && factions.SelectedIndex == 2, "round trip preserves inputs");
                    }
                }
                finally { fixture.Session.Dispose(); L.SetLanguage(previous); }
            });
            Test("English errors and undo labels, game-owned Unicode remains unchanged", delegate
            {
                string previous = L.Language;
                try
                {
                    L.SetLanguage("en"); var fixture = new Fixture();
                    Assert(fixture.Session.Refresh().Factions[0].Display == "测试势力 0", "game text unchanged");
                    bool englishError = false;
                    try { fixture.Session.Undo(); } catch (InvalidOperationException ex) { englishError = ex.Message == "There is no edit to undo."; }
                    Assert(englishError, "English exception");
                    fixture.Session.Apply("cash", 200, null); L.SetLanguage("zh-CN");
                    Assert(L.EditLabel(fixture.Session.Undo()[0].Label) == "撤销 金钱", "undo uses current language");
                    L.SetLanguage("en"); Assert(L.EditLabel("撤销 声望 Faction1") == "Undo Reputation Faction1", "faction label translation");
                }
                finally { L.SetLanguage(previous); }
            });
            results.Add("\nResult: " + (results.Count - failed) + " passed, " + failed + " failed.");
            results.Add("Scope: synthetic IL2CPP layout + native APIs in this test process and an isolated test helper. No real game session was modified or validated.");
            File.WriteAllLines(report, results, Encoding.UTF8); return failed == 0 ? 0 : 1;
        }
    }
}
