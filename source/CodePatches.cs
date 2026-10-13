using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ProbablyStolenTrainer
{
    public sealed class CodeChange
    {
        public readonly long Address;
        public readonly byte[] Before, After;
        public CodeChange(long address, byte[] before, byte[] after)
        {
            if (address < 0x10000 || before == null || after == null || before.Length == 0 || before.Length > 32 || before.Length != after.Length)
                throw new ArgumentException("Invalid code change");
            Address = address; Before = (byte[])before.Clone(); After = (byte[])after.Clone();
        }
        public CodeChange Reverse() { return new CodeChange(Address, After, Before); }
    }
    public interface ITransactionalCodeMemory : ICodeMemory
    {
        void ApplyCodeChanges(IList<CodeChange> changes, long busyStart, long busyEnd);
    }
    public static class CodeTransaction
    {
        // The native caller holds the target suspended for the entire transaction.
        public static void Apply(IMemory memory, IList<CodeChange> changes, Action<long, byte[]> write)
        {
            if (changes == null || changes.Count == 0 || changes.Count > 8) throw new ArgumentException("Invalid change count");
            for (int i = 0; i < changes.Count; i++)
            {
                var change = changes[i];
                for (int j = 0; j < i; j++)
                    if (change.Address < changes[j].Address + changes[j].Before.Length && changes[j].Address < change.Address + change.Before.Length)
                        throw new ArgumentException("Overlapping code changes");
                if (!memory.Read(change.Address, change.Before.Length).SequenceEqual(change.Before))
                    throw new InvalidDataException(L.T("物品菜单代码不匹配，已停止操作。请重启游戏并确认版本。"));
            }
            var attempted = new List<CodeChange>();
            try
            {
                foreach (var change in changes)
                {
                    attempted.Add(change);
                    write(change.Address, change.After);
                    if (!memory.Read(change.Address, change.After.Length).SequenceEqual(change.After)) throw new IOException(L.T("物品菜单写后校验失败。"));
                }
            }
            catch (Exception original)
            {
                bool restored = true;
                foreach (var change in attempted.AsEnumerable().Reverse())
                {
                    try
                    {
                        write(change.Address, change.Before);
                        if (!memory.Read(change.Address, change.Before.Length).SequenceEqual(change.Before)) restored = false;
                    }
                    catch { restored = false; }
                }
                throw new IOException(L.T(restored ? "物品菜单切换失败，已恢复原状态。" : "物品菜单切换失败且未能完全恢复，请重启游戏。") + original.Message, original);
            }
        }
    }
    public sealed partial class ProcessMemory
    {
        [DllImport("ntdll.dll")] static extern int NtSuspendProcess(IntPtr process);
        [DllImport("ntdll.dll")] static extern int NtResumeProcess(IntPtr process);
        [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenThread(uint access, bool inherit, uint id);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool GetThreadContext(IntPtr thread, IntPtr context);
        [DllImport("kernel32.dll", SetLastError = true)] static extern UIntPtr VirtualQueryEx(IntPtr process, IntPtr address, out MemoryRegion region, UIntPtr size);
        [StructLayout(LayoutKind.Sequential)] struct MemoryRegion
        {
            public IntPtr BaseAddress, AllocationBase;
            public uint AllocationProtect;
            public UIntPtr RegionSize;
            public uint State, Protect, Type;
        }
        public void ApplyCodeChanges(IList<CodeChange> changes, long busyStart, long busyEnd)
        {
            if (pid == Process.GetCurrentProcess().Id) throw new InvalidOperationException("Cannot suspend the trainer itself.");
            if (!IsAlive) throw new IOException(L.T("游戏已退出。"));
            // PROCESS_SUSPEND_RESUME | QUERY_INFORMATION | VM_OPERATION | VM_WRITE | VM_READ.
            IntPtr writer = OpenProcess(0x0800 | 0x0400 | 0x0008 | 0x0020 | 0x0010, false, pid);
            if (writer == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), L.T("无法安全切换物品菜单，请检查游戏进程权限。"));
            bool suspended = false;
            try
            {
                if (NtSuspendProcess(writer) < 0) throw new IOException(L.T("无法安全切换物品菜单，请检查游戏进程权限。"));
                suspended = true;
                if (busyEnd > busyStart) CheckMenuIdle(writer, busyStart, busyEnd);
                CodeTransaction.Apply(this, changes, delegate(long address, byte[] data) { WriteCodeBlock(writer, address, data); });
            }
            finally
            {
                int resume = 0;
                try { if (suspended) resume = NtResumeProcess(writer); }
                finally { CloseHandle(writer); }
                if (resume < 0) throw new IOException(L.T("游戏线程未能恢复运行，请重启游戏。"));
            }
        }
        void CheckMenuIdle(IntPtr process, long start, long end)
        {
            using (var target = Process.GetProcessById(pid))
            {
                foreach (ProcessThread thread in target.Threads)
                using (thread)
                {
                    IntPtr th = OpenThread(0x0008, false, (uint)thread.Id);
                    if (th == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), L.T("无法检查游戏线程状态，请稍后重试。"));
                    IntPtr buffer = Marshal.AllocHGlobal(1248);
                    try
                    {
                        IntPtr context = new IntPtr((buffer.ToInt64() + 15) & ~15L);
                        Marshal.Copy(new byte[1232], 0, context, 1232);
                        Marshal.WriteInt32(context, 48, 0x00100001); // AMD64 CONTEXT_CONTROL.
                        if (!GetThreadContext(th, context)) throw new Win32Exception(Marshal.GetLastWin32Error(), L.T("无法检查游戏线程状态，请稍后重试。"));
                        long ip = Marshal.ReadInt64(context, 248), sp = Marshal.ReadInt64(context, 152);
                        if (ip >= start && ip < end) throw new IOException(L.T("物品菜单正在处理操作，请松开按键并稍后重试。"));
                        MemoryRegion region;
                        if (VirtualQueryEx(process, new IntPtr(sp), out region, new UIntPtr((uint)Marshal.SizeOf(typeof(MemoryRegion)))).ToUInt64() == 0)
                            throw new Win32Exception(Marshal.GetLastWin32Error(), L.T("无法检查游戏线程状态，请稍后重试。"));
                        int size = (int)Math.Min(16384L, region.BaseAddress.ToInt64() + (long)region.RegionSize.ToUInt64() - sp);
                        if (size < 8) throw new IOException(L.T("无法检查游戏线程状态，请稍后重试。"));
                        // Also defer while a callee may return into the menu handler.
                        byte[] stack = Read(sp, size);
                        for (int i = 0; i + 8 <= stack.Length; i += 8)
                        {
                            long address = BitConverter.ToInt64(stack, i);
                            if (address >= start && address < end) throw new IOException(L.T("物品菜单正在处理操作，请松开按键并稍后重试。"));
                        }
                    }
                    finally { Marshal.FreeHGlobal(buffer); CloseHandle(th); }
                }
            }
        }
        void WriteCodeBlock(IntPtr writer, long address, byte[] data)
        {
            uint original;
            var size = new UIntPtr((uint)data.Length);
            if (!VirtualProtectEx(writer, new IntPtr(address), size, 0x40, out original)) throw new Win32Exception(Marshal.GetLastWin32Error());
            try
            {
                UIntPtr written;
                if (!WriteProcessMemory(writer, new IntPtr(address), data, size, out written) || written.ToUInt64() != (ulong)data.Length) throw new IOException(L.T("物品菜单写入失败。"));
                if (!FlushInstructionCache(writer, new IntPtr(address), size)) throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                uint ignored;
                if (!VirtualProtectEx(writer, new IntPtr(address), size, original, out ignored)) throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }
}
