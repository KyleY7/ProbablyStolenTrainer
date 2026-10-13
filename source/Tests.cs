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
    sealed class FakeMemory : ICodeMemory
    {
        readonly Dictionary<long, byte> data = new Dictionary<long, byte>();
        public int Writes, FailOnWrite;
        public int CodeWrites, FailOnCodeWrite;
        public bool WriteThenFail;
        public bool Alive = true;
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
        public static int Run(string report)
        {
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
            Test("Live UI language change preserves connection, edits, selection and appraisal state", delegate
            {
                string previous = L.Language;
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var fixture = new Fixture(); fixture.Session.Apply("cash", 777, null); fixture.Session.SetAppraisalVisible(true);
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
                        amount.Value = 654321; factions.SelectedIndex = 2;
                        int writes = fixture.Memory.Writes, codeWrites = fixture.Memory.CodeWrites;
                        form.ChangeLanguage("en", false);
                        Assert(form.Text.Contains("Money / Time"), "English title");
                        Assert(((System.Windows.Forms.Label)type.GetField("currentCash", flags).GetValue(form)).Text.Contains("777"), "value retained");
                        Assert(((System.Windows.Forms.Label)type.GetField("status", flags).GetValue(form)).Text.StartsWith("Connected"), "connection status translated");
                        Assert(amount.Value == 654321 && factions.SelectedIndex == 2 && toggle.Checked, "UI inputs and toggle retained");
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
            results.Add("Scope: synthetic IL2CPP layout + native memory API in test process. No real game session was modified or validated.");
            File.WriteAllLines(report, results, Encoding.UTF8); return failed == 0 ? 0 : 1;
        }
    }
}
