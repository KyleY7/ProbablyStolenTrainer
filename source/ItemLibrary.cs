using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProbablyStolenTrainer
{
    public sealed class ItemLibrary
    {
        readonly ITransactionalCodeMemory memory;
        readonly long module;
        readonly CodeChange[] changes;
        public bool Enabled { get; private set; }
        internal static CodeChange[] Profile(long module)
        {
            byte[] gate = { 0x80, 0xB8, 0x22, 0x01, 0x00, 0x00, 0x00, 0x0F, 0x84, 0xF2, 0x06, 0x00, 0x00 };
            byte[] open = (byte[])gate.Clone(); open[6] = 0xFF; // A bool never equals 255; leave global devMode unchanged.
            byte[] tail = { 0xB9, 0x04, 0x00, 0x00, 0x00, 0x44, 0x8B, 0x4F, 0x24 };
            byte[] limited = { 0xE9, 0x9E, 0x06, 0x00, 0x00, 0x44, 0x8B, 0x4F, 0x24 }; // Jump to original epilogue at 0x7B4613.
            return new[] { new CodeChange(module + BuildProfile.ItemMenuTail, tail, limited), new CodeChange(module + BuildProfile.ItemMenuGate, gate, open) };
        }
        public ItemLibrary(ITransactionalCodeMemory memory, long module)
        {
            this.memory = memory; this.module = module; changes = Profile(module);
        }
        public void SetEnabled(bool enabled)
        {
            if (!enabled) { Restore(); return; }
            if (!memory.IsAlive) throw new IOException(L.T("游戏已退出。"));
            if (Enabled)
            {
                foreach (var change in changes)
                    if (!memory.Read(change.Address, change.After.Length).SequenceEqual(change.After)) throw new IOException(L.T("物品菜单代码已被其他修改改变，请重启游戏。"));
                return;
            }
            foreach (var change in changes)
                if (!memory.Read(change.Address, change.Before.Length).SequenceEqual(change.Before)) throw new IOException(L.T("物品菜单代码不匹配，已停止操作。请重启游戏并确认版本。"));
            Enabled = true;
            try { memory.ApplyCodeChanges(changes, module + 0x7B3220, module + 0x7B4A20); }
            catch
            {
                // A failed transaction normally rolls back, but retain ownership of any remaining change.
                try { Enabled = changes.Any(c => !memory.Read(c.Address, c.Before.Length).SequenceEqual(c.Before)); }
                catch { Enabled = true; }
                throw;
            }
        }
        void Restore()
        {
            if (!Enabled) return;
            if (!memory.IsAlive) { Enabled = false; return; }
            var restore = new List<CodeChange>();
            bool conflict = false;
            foreach (var change in changes.Reverse())
            {
                var current = memory.Read(change.Address, change.Before.Length);
                if (current.SequenceEqual(change.Before)) continue;
                if (current.SequenceEqual(change.After)) restore.Add(change.Reverse());
                else conflict = true;
            }
            if (conflict) throw new IOException(L.T("物品菜单代码已被其他修改改变，请重启游戏。"));
            if (restore.Count > 0) memory.ApplyCodeChanges(restore, module + 0x7B3220, module + 0x7B4A20);
            Enabled = false;
        }
        public static string KeyName(int key)
        {
            if (key == 32) return "Space";
            if (key >= 256 && key <= 265) return "Num " + (key - 256);
            if (key >= 282 && key <= 296) return "F" + (key - 281);
            if (key >= 97 && key <= 122) return ((char)(key - 32)).ToString();
            if (key >= 32 && key <= 126) return ((char)key).ToString();
            switch (key)
            {
                case 8: return "Backspace"; case 9: return "Tab"; case 13: return "Enter"; case 27: return "Esc";
                case 127: return "Delete"; case 273: return "↑"; case 274: return "↓"; case 275: return "→"; case 276: return "←";
                case 277: return "Insert"; case 278: return "Home"; case 279: return "End"; case 280: return "PageUp"; case 281: return "PageDown";
                default: throw new IOException(L.T("物品菜单快捷键无法识别，请检查游戏按键设置。"));
            }
        }
    }
}
