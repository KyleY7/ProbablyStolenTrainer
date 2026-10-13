# Validation / 验证范围

## English

The v1.0.0 release passed 33 automated tests on Windows x64. Coverage includes pointer/type/string/list validation, Unicode game data, numeric bounds, faction identity, stale saves and objects, undo conflicts, multi-write rollback, process filtering and identity, appraisal signature checks, one-byte patching and restoration, failed writes, conflicting patches, game-exit handling, and native page-protection restoration in a dedicated test allocation.

Language tests cover preference persistence and invalid settings, English exceptions, Chinese/English round trips, and switching language with a synthetic connection. The live form test verifies that input values, faction selection, undo availability, appraisal state and the session object are preserved, and that changing language performs no game-memory writes.

The supported game's two file hashes and the tooltip call path were checked locally. Chinese and English main-window and process-picker previews were rendered using sample data and inspected. Release executables carry file version `1.0.0.0`.

**Not yet validated:** connection to a live game session, visible in-game appraisal values, edits during actual gameplay, reward/UI refresh timing, and save/reload persistence. Automated tests and static analysis are not a substitute for those checks. No real save was modified during validation.

Run `build.ps1`, then `test.ps1`. The release ZIP contains the generated test report. A live-game check should use a backed-up save and confirm each numeric field, undo, appraisal enable/disable/normal-exit restoration, and save/reload behavior.

## 简体中文

v1.0.0 在 Windows x64 通过了 33 项自动测试，覆盖指针、类型、字符串与列表校验，Unicode 游戏文字，输入范围，势力身份，失效存档与对象，撤销冲突，多项写入回退，进程搜索与身份验证，鉴定代码签名，单字节变更及恢复，写入失败，代码冲突，游戏退出，以及独立测试内存页的 Win32 页面权限恢复。

语言测试覆盖偏好保存与无效设置、英文错误信息、中英文来回切换，以及带模拟连接的窗口切换。窗口测试确认输入值、势力选择、撤销记录、鉴定状态和会话对象均保留，切换语言不会写入游戏内存。

已在本地核对支持版本的两份文件哈希和鉴定提示调用路径；用示例数据渲染并检查了中英文主窗口与进程选择窗口。发布程序的文件版本为 `1.0.0.0`。

**尚未验证：** 真实游戏会话连接、游戏内鉴定显示、实际游玩时的数值修改、奖励与界面刷新时机、保存后回读。自动测试和静态分析不能替代这些检查。验证过程中没有改动真实存档。

先运行 `build.ps1`，再运行 `test.ps1`。发布 ZIP 内附生成的测试报告。实机检查应使用已备份存档，逐项核对数值、撤销、鉴定开关及正常退出恢复、保存回读效果。
