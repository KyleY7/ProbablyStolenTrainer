using System;
using System.IO;
using System.Linq;

namespace ProbablyStolenTrainer
{
    // This build already draws TRUE VALUE using AppraisalHelper.GetCorrectBaseValue.
    // Only remove the devMode gate in that tooltip; do not enable global dev mode.
    // The short JZ displacement changes from +126 to +0 with one byte. Both
    // condition outcomes then continue to the original valuation/display code.
    public sealed class AppraisalReveal
    {
        public const int DisplacementOffset = 8;
        readonly ICodeMemory memory;
        readonly long address;
        readonly byte[] original = { 0x80, 0xB8, 0x22, 0x01, 0x00, 0x00, 0x00, 0x74, 0x7E, 0x33, 0xD2, 0x48, 0x8B, 0xCB, 0xE8 };
        readonly byte[] revealed;
        public bool Enabled { get; private set; }
        public AppraisalReveal(ICodeMemory process, long moduleBase)
        {
            memory = process; address = moduleBase + BuildProfile.AppraisalTooltipGate;
            revealed = (byte[])original.Clone(); revealed[DisplacementOffset] = 0;
        }
        public void SetEnabled(bool enabled)
        {
            if (!enabled) { Restore(); return; }
            if (Enabled)
            {
                if (!memory.Read(address, original.Length).SequenceEqual(revealed)) throw new IOException(L.T("价值提示代码已变化，请关闭开关或重启游戏。"));
                return;
            }
            if (!memory.IsAlive) throw new IOException(L.T("游戏已退出。"));
            if (!memory.Read(address, original.Length).SequenceEqual(original)) throw new InvalidDataException(L.T("价值提示代码不匹配，已拒绝修改。若修改器曾被强制结束，请先重启游戏。"));
            Enabled = true; // Own any partial result so failures and disposal can restore it.
            try
            {
                memory.WriteCodeByte(address + DisplacementOffset, 0);
                if (!memory.Read(address, original.Length).SequenceEqual(revealed)) throw new IOException(L.T("价值提示写后校验失败。"));
            }
            catch (Exception ex)
            {
                try { Restore(); }
                catch (Exception restore) { throw new IOException(L.T("切换失败且未能恢复，请重启游戏。") + restore.Message, ex); }
                throw new IOException(L.T("切换失败，已恢复原状态。") + ex.Message, ex);
            }
        }
        void Restore()
        {
            if (!Enabled) return;
            if (!memory.IsAlive) { Enabled = false; return; }
            var current = memory.Read(address, original.Length);
            if (current.SequenceEqual(original)) { Enabled = false; return; }
            if (!current.SequenceEqual(revealed)) throw new IOException(L.T("提示代码被其他修改覆盖，无法安全恢复。"));
            memory.WriteCodeByte(address + DisplacementOffset, original[DisplacementOffset]);
            if (!memory.Read(address, original.Length).SequenceEqual(original)) throw new IOException(L.T("恢复价值提示时校验失败。"));
            Enabled = false;
        }
    }
}
