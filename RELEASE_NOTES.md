# v1.0.0 — Chinese / English · 中英文版

## 简体中文

Probably Stolen Demo 修改器首次公开发布。适配 **Steam 构建 25382790 / Windows x64**。

- 右上角新增 **简体中文 / English** 即时切换，自动记忆语言偏好。切换保留连接、输入值、势力选择、撤销和鉴定开关。
- 支持金钱、天数、租金剩余天数、单个或全部势力声望。
- 可选择游戏进程，搜索名称、PID 或路径；自动识别安装目录，无需手动设置 C/D 盘符。
- 开启鉴定价值显示后，重新悬停支持的鉴定物，查看游戏原生 `TRUE VALUE` 提示。
- 包含存档备份、最近一次数值修改撤销、窗口置顶和操作日志。
- 附完整源码、中英文说明、示例界面截图和 SHA-256 校验文件。

**下载：** `ProbablyStolenTrainer-v1.0.0-win-x64.zip`，解压后运行其中的 `ProbablyStolenTrainer.exe`。在正在运行游戏的同一套 Windows 中使用。单独 EXE 也可直接运行；ZIP 另附说明与源码。

**验证：** 33 项自动测试通过，双语界面已检查，支持版本的两份游戏文件哈希已核对。尚未进行真实游戏会话的连接、修改、鉴定提示及保存回读测试。请先备份存档。本版仅支持指定试玩版构建。

**行为说明：** 天数修改不补跑剧情或结算；声望奖励由游戏自身刷新。鉴定开关默认关闭，取消勾选或正常退出会恢复；强制结束修改器后可重启游戏恢复。游戏中的势力名称和 `TRUE VALUE` 字样保持游戏原样。

## English

First public release of the Probably Stolen Demo trainer. Supports **Steam build 25382790 / Windows x64**.

- Instant **简体中文 / English** switch with a remembered preference. Switching preserves the connection, inputs, selected faction, undo and appraisal toggle.
- Edit money, day count, rent days, and one or all factions' reputation.
- Select a process by name, PID or path. The game directory is detected automatically, including changed C:/D: drive letters.
- Reveal the game's native `TRUE VALUE` tooltip by enabling the appraisal toggle and hovering over a supported appraisal item again.
- Save backups, undo of the last numeric edit, always-on-top mode and change logs.
- Includes source, Chinese and English documentation, sample UI screenshots and SHA-256 checksums.

**Download:** extract `ProbablyStolenTrainer-v1.0.0-win-x64.zip` and run `ProbablyStolenTrainer.exe` inside the same Windows system as the game. The standalone EXE also runs directly; the ZIP additionally includes documentation and source.

**Validation:** 33 automated checks passed; both UI languages and supported game hashes were checked. Live game connection, edits, appraisal tooltips and save reloads have not yet been tested. Back up saves first. Only the specified Demo build is supported.

**Behavior:** day edits do not replay events or payments; reputation rewards refresh through the game. Appraisal display starts off and is restored when disabled or on normal exit. After a forced trainer exit, restart the game to restore it. Game-provided faction names and `TRUE VALUE` text retain their original language.
