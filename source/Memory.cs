using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace ProbablyStolenTrainer
{
    public interface IMemory : IDisposable
    {
        byte[] Read(long address, int count);
        void Write(long address, byte[] data);
    }

    public interface ICodeMemory : IMemory
    {
        bool IsAlive { get; }
        void WriteCodeByte(long address, byte value);
    }

    public sealed partial class ProcessMemory : ITransactionalCodeMemory
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr process, IntPtr address, byte[] buffer, UIntPtr size, out UIntPtr read);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr process, IntPtr address, byte[] buffer, UIntPtr size, out UIntPtr written);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetExitCodeProcess(IntPtr process, out uint exitCode);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool VirtualProtectEx(IntPtr process, IntPtr address, UIntPtr size, uint protection, out uint oldProtection);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FlushInstructionCache(IntPtr process, IntPtr address, UIntPtr size);
        readonly int pid;
        IntPtr handle;
        public ProcessMemory(int id)
        {
            pid = id;
            handle = OpenProcess(0x0400 | 0x0010, false, pid);
            if (handle == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), L.T("无法读取游戏进程。请让游戏和修改器使用相同的权限运行。"));
        }
        public byte[] Read(long address, int count)
        {
            if (address < 0x10000 || count < 1 || count > 65536) throw new InvalidDataException(L.T("内存地址或长度无效。"));
            if (handle == IntPtr.Zero) throw new ObjectDisposedException("ProcessMemory");
            byte[] bytes = new byte[count]; UIntPtr read;
            if (!ReadProcessMemory(handle, new IntPtr(address), bytes, new UIntPtr((uint)count), out read) || read.ToUInt64() != (ulong)count)
                throw new IOException(L.T("读取失败。游戏可能正在切换场景；请读入存档后重新连接。"));
            return bytes;
        }
        public void Write(long address, byte[] data)
        {
            if (address < 0x10000 || (data.Length != 4 && data.Length != 8)) throw new InvalidDataException(L.T("拒绝无效写入。"));
            if (handle == IntPtr.Zero) throw new ObjectDisposedException("ProcessMemory");
            // A retained read handle keeps the original process object alive. Never reuse a PID after exit.
            using (Process process = Process.GetProcessById(pid))
            {
                if (process.HasExited) throw new IOException(L.T("游戏已退出。"));
            }
            IntPtr writer = OpenProcess(0x0020 | 0x0008, false, pid);
            if (writer == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), L.T("没有游戏进程写入权限。"));
            try
            {
                UIntPtr written;
                if (!WriteProcessMemory(writer, new IntPtr(address), data, new UIntPtr((uint)data.Length), out written) || written.ToUInt64() != (ulong)data.Length)
                    throw new IOException(L.T("写入失败；未完成本次操作。"));
            }
            finally { CloseHandle(writer); }
        }
        public void Dispose() { if (handle != IntPtr.Zero) { CloseHandle(handle); handle = IntPtr.Zero; } }
        public bool IsAlive
        {
            get
            {
                uint exitCode;
                if (handle == IntPtr.Zero) return false;
                if (!GetExitCodeProcess(handle, out exitCode)) throw new Win32Exception(Marshal.GetLastWin32Error());
                return exitCode == 259;
            }
        }
        public void WriteCodeByte(long address, byte value)
        {
            if (address < 0x10000 || !IsAlive) throw new IOException(L.T("游戏已退出，无法切换价值显示。"));
            IntPtr writer = OpenProcess(0x0400 | 0x0020 | 0x0008, false, pid);
            if (writer == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), L.T("无法切换价值显示，请检查游戏进程权限。"));
            try
            {
                uint original;
                var size = new UIntPtr(1);
                if (!VirtualProtectEx(writer, new IntPtr(address), size, 0x40, out original)) throw new Win32Exception(Marshal.GetLastWin32Error(), L.T("无法访问价值提示代码。"));
                try
                {
                    UIntPtr written;
                    if (!WriteProcessMemory(writer, new IntPtr(address), new byte[] { value }, size, out written) || written.ToUInt64() != 1)
                        throw new IOException(L.T("价值显示开关写入失败。"));
                    if (!FlushInstructionCache(writer, new IntPtr(address), size)) throw new Win32Exception(Marshal.GetLastWin32Error(), L.T("无法刷新游戏指令缓存。"));
                }
                finally
                {
                    uint ignored;
                    if (!VirtualProtectEx(writer, new IntPtr(address), size, original, out ignored)) throw new Win32Exception(Marshal.GetLastWin32Error(), L.T("无法恢复游戏内存页属性。"));
                }
            }
            finally { CloseHandle(writer); }
        }
    }

    public static class BuildProfile
    {
        public const string Build = "25382790";
        public const string GameHash = "3BEA17EEEC77ADAB6418918C28A8AA9A06F290582A2972639A48BD7E5A01AB44";
        public const string MetadataHash = "EFDCA95772FDCE327B51E0C7E8D320E7055917BE5C669DB47C1F7B36389F1F41";
        public const long PlayerType = 0x25CECF8;
        public const long StationType = 0x25EC130;
        public const long ReputationType = 0x25EBA70;
        public const long EntryType = 0x2607A80;
        public const long AppraisalTooltipGate = 0x88F9AC;
        public const long DebugHandlerType = 0x25F94B0;
        public const long GridInventoryType = 0x2611FB8;
        public const long ScrollInventoryType = 0x2612070;
        public const long PixelWindowType = 0x25CDC98;
        public const long ItemMenuGate = 0x7B3F14;
        public const long ItemMenuTail = 0x7B3F70;
        public static string Hash(string path)
        {
            using (var sha = SHA256.Create()) using (var file = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "");
        }
        public static void Verify(string assemblyPath)
        {
            string metadata = Path.Combine(Path.GetDirectoryName(assemblyPath), "Probably Stolen_Data", "il2cpp_data", "Metadata", "global-metadata.dat");
            if (Hash(assemblyPath) != GameHash || !File.Exists(metadata) || Hash(metadata) != MetadataHash)
                throw new InvalidDataException(L.T("游戏版本不匹配。本修改器仅支持 Demo 构建 25382790，已停止连接。"));
        }
    }

    public sealed class Faction
    {
        public string Id, Display; public long Address; public double Amount;
        public override string ToString() { return Display + "  [" + Id + "]"; }
    }
    public sealed class Snapshot
    {
        public long Player, Station, Entry, EntryNative;
        public string RunId;
        public int SaveSlotId, RunNumber;
        public int Cash, Day, RentDays, RentValue;
        public List<Faction> Factions = new List<Faction>();
        public bool SameRun(Snapshot other)
        {
            return other != null && Player == other.Player && Station == other.Station && Entry == other.Entry
                && EntryNative == other.EntryNative && SaveSlotId == other.SaveSlotId && RunNumber == other.RunNumber && RunId == other.RunId;
        }
    }
    public sealed class Edit
    {
        public long Address; public byte[] Before, After; public string Label;
        public Edit(long address, byte[] before, byte[] after, string label) { Address = address; Before = before; After = after; Label = label; }
    }
    public sealed class Session : IDisposable
    {
        readonly IMemory memory;
        readonly long module;
        Snapshot attached;
        List<Edit> undo;
        public string ProcessDescription;
        public string GameDirectory { get; private set; }
        public string CloseWarning { get; private set; }
        AppraisalReveal appraisalReveal;
        ItemLibrary itemLibrary;
        public string ItemMenuKey { get; private set; }
        public Session(IMemory mem, long moduleBase) { memory = mem; module = moduleBase; }
        public static Session Attach(ProcessCandidate selected)
        {
            if (selected == null) throw new ArgumentNullException("selected");
            using (var process = Process.GetProcessById(selected.Id))
            {
                if (selected.StartedUtcTicks == 0 || process.HasExited || process.StartTime.ToUniversalTime().Ticks != selected.StartedUtcTicks)
                    throw new IOException(L.T("所选进程已退出、重新启动或无法验证。请刷新进程列表后重新选择。"));
                var gameModule = process.Modules.Cast<ProcessModule>().SingleOrDefault(m => String.Equals(m.ModuleName, "GameAssembly.dll", StringComparison.OrdinalIgnoreCase));
                if (gameModule == null) throw new IOException(L.T("所选进程未加载 GameAssembly.dll。请选择已读入存档的 Probably Stolen 游戏进程。"));
                BuildProfile.Verify(gameModule.FileName);
                var session = new Session(new ProcessMemory(process.Id), gameModule.BaseAddress.ToInt64());
                try
                {
                    session.Connect();
                    session.GameDirectory = Path.GetDirectoryName(gameModule.FileName);
                    session.ProcessDescription = process.ProcessName + " · PID " + process.Id;
                    return session;
                }
                catch { session.Dispose(); throw; }
            }
        }
        long Ptr(long address, bool aligned = true, bool allowNull = false)
        {
            long p = BitConverter.ToInt64(memory.Read(address, 8), 0);
            if (allowNull && p == 0) return 0;
            if (p < 0x10000 || p >= 0x0000800000000000L || (aligned && (p & 7) != 0)) throw new IOException(L.T("游戏对象尚未就绪，请先读入存档。"));
            return p;
        }
        int Int(long address) { return BitConverter.ToInt32(memory.Read(address, 4), 0); }
        double Double(long address) { return BitConverter.ToDouble(memory.Read(address, 8), 0); }
        string CString(long address)
        {
            var bytes = new List<byte>();
            for (int i = 0; i < 80; i++) { byte b = memory.Read(address + i, 1)[0]; if (b == 0) return Encoding.ASCII.GetString(bytes.ToArray()); bytes.Add(b); }
            throw new InvalidDataException(L.T("类型名称无效。"));
        }
        string StringAt(long obj)
        {
            int length = Int(obj + 0x10);
            if (length < 0 || length > 256) throw new InvalidDataException(L.T("字符串长度无效。"));
            return length == 0 ? "" : Encoding.Unicode.GetString(memory.Read(obj + 0x14, length * 2));
        }
        long Type(long rva, string name)
        {
            long klass = Ptr(module + rva);
            if (CString(Ptr(klass + 0x10, false)) != name) throw new InvalidDataException(L.T("游戏类型校验失败：") + name);
            return klass;
        }
        long Instance(long rva, string name)
        {
            long klass = Type(rva, name);
            long instance = Ptr(Ptr(klass + 0xB8));
            if (Ptr(instance) != klass) throw new InvalidDataException(L.T("游戏对象校验失败：") + name);
            return instance;
        }
        Snapshot ReadIdentity()
        {
            var s = new Snapshot();
            s.Entry = Instance(BuildProfile.EntryType, "EmporiumEntry");
            s.EntryNative = Ptr(s.Entry + 0x10); // Unity native object must still be alive.
            s.Player = Instance(BuildProfile.PlayerType, "PlayerStore");
            s.Station = Instance(BuildProfile.StationType, "StoreStation");
            s.SaveSlotId = Int(s.Player + 0x18);
            s.RunNumber = Int(s.Player + 0x38);
            // This build initializes runID to an empty string; InitialSave fills it later.
            // It is optional identity metadata, not evidence that gameplay has loaded.
            long runId = Ptr(s.Player + 0x30, true, true);
            s.RunId = runId == 0 ? "" : StringAt(runId);
            if (String.IsNullOrWhiteSpace(s.RunId)) s.RunId = "";
            return s;
        }
        public Snapshot Read()
        {
            var s = ReadIdentity();
            s.Cash = Int(s.Player + 0x10); s.RentDays = Int(s.Player + 0x58); s.RentValue = Int(s.Player + 0x5C); s.Day = Int(s.Station + 0x30);
            if (s.Day < 0 || s.Day > 1000000 || Math.Abs((long)s.RentDays) > 1000000) throw new InvalidDataException(L.T("日期字段校验失败。"));
            long list = Ptr(s.Player + 0x110), array = Ptr(list + 0x10), repType = Type(BuildProfile.ReputationType, "StoreReputation");
            int count = Int(list + 0x18);
            long capacity = BitConverter.ToInt64(memory.Read(array + 0x18, 8), 0);
            if (count < 1 || count > 32 || capacity < count || capacity > 1024) throw new InvalidDataException(L.T("声望列表尚未就绪或结构不匹配。"));
            var ids = new HashSet<string>();
            for (int i = 0; i < count; i++)
            {
                long obj = Ptr(array + 0x20 + i * 8);
                if (Ptr(obj) != repType) throw new InvalidDataException(L.T("声望对象类型不匹配。"));
                var f = new Faction { Address = obj, Id = StringAt(Ptr(obj + 0x20)), Display = StringAt(Ptr(obj + 0x18)), Amount = Double(obj + 0x10) };
                if (String.IsNullOrWhiteSpace(f.Id) || !ids.Add(f.Id) || System.Double.IsNaN(f.Amount) || System.Double.IsInfinity(f.Amount) || Math.Abs(f.Amount) > 1000000)
                    throw new InvalidDataException(L.T("声望字段校验失败。"));
                s.Factions.Add(f);
            }
            // Detect a reload while the snapshot was being read.
            if (!s.SameRun(ReadIdentity()))
                throw new IOException(L.T("场景正在切换，请稍后重新连接。"));
            return s;
        }
        public Snapshot Connect() { attached = null; undo = null; attached = Read(); return attached; }
        public Snapshot Refresh()
        {
            Snapshot current;
            try { current = Read(); }
            catch { undo = null; throw; }
            if (!current.SameRun(attached)) { undo = null; throw new IOException(L.T("存档或场景已切换，请重新连接；旧操作已清除。")); }
            return current;
        }
        public List<Edit> Apply(string field, decimal value, string factionId)
        {
            var s = Refresh(); var edits = new List<Edit>();
            if (field == "cash") { Range(value, 0, 999999999, true); Add(edits, s.Player + 0x10, BitConverter.GetBytes((int)value), "金钱"); }
            else if (field == "day") { Range(value, 0, 99999, true); Add(edits, s.Station + 0x30, BitConverter.GetBytes((int)value), "天数"); }
            else if (field == "rent") { Range(value, 0, 9999, true); Add(edits, s.Player + 0x58, BitConverter.GetBytes((int)value), "租金倒计时"); }
            else if (field == "reputation")
            {
                Range(value, -200, 200, false);
                var factions = factionId == null ? s.Factions : s.Factions.Where(f => f.Id == factionId).ToList();
                if (factions.Count == 0) throw new InvalidDataException(L.T("该势力已不存在，请刷新。"));
                foreach (var faction in factions) Add(edits, faction.Address + 0x10, BitConverter.GetBytes((double)value), "声望 " + faction.Id);
            }
            else throw new ArgumentException(L.T("未知字段。"));
            Commit(edits, s); undo = edits; return edits;
        }
        static void Range(decimal n, decimal min, decimal max, bool integer)
        {
            if (n < min || n > max || (integer && Decimal.Truncate(n) != n)) throw new ArgumentOutOfRangeException("value", L.T("输入超出范围或不是整数。"));
        }
        void Add(List<Edit> edits, long address, byte[] after, string label) { edits.Add(new Edit(address, memory.Read(address, after.Length), after, label)); }
        void Commit(List<Edit> edits, Snapshot expected)
        {
            var completed = new List<Edit>();
            try
            {
                foreach (var edit in edits)
                {
                    var fresh = Refresh();
                    if (!fresh.SameRun(expected)) throw new IOException(L.T("存档已变化。"));
                    bool valid = edit.Address == fresh.Player + 0x10 || edit.Address == fresh.Player + 0x58 || edit.Address == fresh.Station + 0x30 || fresh.Factions.Any(f => edit.Address == f.Address + 0x10);
                    if (!valid || !memory.Read(edit.Address, edit.Before.Length).SequenceEqual(edit.Before)) throw new IOException(L.T("数值或对象已发生变化，请暂停游戏并重试。"));
                    completed.Add(edit); // Include a possible partial write in recovery.
                    memory.Write(edit.Address, edit.After);
                    if (!memory.Read(edit.Address, edit.After.Length).SequenceEqual(edit.After)) throw new IOException(L.T("写后校验失败。"));
                }
            }
            catch (Exception original)
            {
                bool restored = true;
                foreach (var edit in completed.AsEnumerable().Reverse())
                {
                    try
                    {
                        var fresh = Refresh();
                        bool valid = fresh.SameRun(expected) && (edit.Address == fresh.Player + 0x10 || edit.Address == fresh.Player + 0x58 || edit.Address == fresh.Station + 0x30 || fresh.Factions.Any(f => edit.Address == f.Address + 0x10));
                        if (!valid) { restored = false; continue; }
                        var bytes = memory.Read(edit.Address, edit.After.Length);
                        if (bytes.SequenceEqual(edit.After)) memory.Write(edit.Address, edit.Before);
                        else if (!bytes.SequenceEqual(edit.Before)) restored = false;
                    }
                    catch { restored = false; }
                }
                throw new IOException(original.Message + (restored ? L.T(" 已写入的数值已回退。") : L.T(" 部分修改无法安全回退，请检查游戏数值。")), original);
            }
        }
        public List<Edit> Undo()
        {
            var current = Refresh();
            if (undo == null) throw new InvalidOperationException(L.T("没有可撤销的操作。"));
            var reverse = undo.Select(e => new Edit(e.Address, e.After, e.Before, "撤销 " + e.Label)).ToList();
            Commit(reverse, current); undo = null; return reverse;
        }
        public bool CanUndo { get { return undo != null; } }
        public bool AppraisalVisible { get { return appraisalReveal != null && appraisalReveal.Enabled; } }
        public bool ItemLibraryEnabled { get { return itemLibrary != null && itemLibrary.Enabled; } }
        public void SetItemLibraryEnabled(bool enabled)
        {
            if (enabled)
            {
                var snapshot = Refresh();
                long inventory = Ptr(snapshot.Entry + 0x98);
                if (Ptr(inventory) != Type(BuildProfile.GridInventoryType, "GameGridInventory")) throw new IOException(L.T("后仓物品栏尚未就绪，请进入店铺后重试。"));
                long handler = Instance(BuildProfile.DebugHandlerType, "DebugHandler");
                Ptr(handler + 0x10);
                if (memory.Read(handler + 0x1D, 1)[0] != 1) throw new IOException(L.T("游戏物品菜单尚未初始化，请进入店铺后重试。"));
                if (Ptr(Ptr(handler + 0x48)) != Type(BuildProfile.PixelWindowType, "PixelWindow") || Ptr(Ptr(handler + 0x50)) != Type(BuildProfile.ScrollInventoryType, "GameGridScrollableInventory"))
                    throw new IOException(L.T("游戏物品菜单尚未初始化，请进入店铺后重试。"));
                ItemMenuKey = ItemLibrary.KeyName(Int(handler + 0x20));
            }
            if (itemLibrary == null)
            {
                if (!enabled) return;
                var code = memory as ITransactionalCodeMemory;
                if (code == null) throw new NotSupportedException(L.T("当前连接不支持物品添加菜单。"));
                itemLibrary = new ItemLibrary(code, module);
            }
            itemLibrary.SetEnabled(enabled);
        }
        public void SetAppraisalVisible(bool visible)
        {
            if (visible) Refresh();
            if (appraisalReveal == null)
            {
                var code = memory as ICodeMemory;
                if (code == null) throw new NotSupportedException(L.T("当前连接不支持价值显示开关。"));
                appraisalReveal = new AppraisalReveal(code, module);
            }
            appraisalReveal.SetEnabled(visible);
        }
        public void Dispose()
        {
            var warnings = new List<string>();
            try { if (itemLibrary != null) itemLibrary.SetEnabled(false); }
            catch (Exception ex) { warnings.Add(L.T("物品菜单未能恢复，请重启游戏。") + ex.Message); }
            try { if (appraisalReveal != null) appraisalReveal.SetEnabled(false); }
            catch (Exception ex) { warnings.Add(L.T("价值显示未能恢复。请重启游戏恢复原始提示。") + ex.Message); }
            finally { memory.Dispose(); attached = null; undo = null; CloseWarning = String.Join(Environment.NewLine, warnings); }
        }
    }
}
