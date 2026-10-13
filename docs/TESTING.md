# Validation / 验证范围

## English

The v1.1.1 diagnostic build passed 56 automated tests on Windows x64. Coverage includes pointer/type/string/list validation, Unicode game data, numeric bounds, faction identity, stale saves and objects, undo conflicts, multi-write rollback, process filtering and identity, appraisal signature checks, one-byte patching and restoration, failed writes, conflicting patches, game-exit handling, and native page-protection restoration in a dedicated test allocation.

Four diagnostic tests identify null, corrupt and unreadable pointer fields, confirm that independent probes continue after another probe fails, preserve optional null run IDs, verify bilingual errors and zero memory writes, exclude save-string values, and exercise UTF-8 report creation, unique filenames and directory fallback. These tests verify the diagnostic mechanism; they do not identify the user's live-game failure.

The 12 item-library checks cover default-off behavior, the actual configured key, independent global developer mode, branch destinations, object readiness, signature mismatch, partial-write rollback, busy-thread deferral, restoration with appraisal enabled, foreign code conflicts, game exit, overlapping writes, key labels, and refusal to suspend the trainer itself. A disposable helper process verifies the native thread-context guard, suspended multi-region writes, rollback to the original bytes, restored executable-page permissions and process resumption. It contains test data, not game code.

Language tests cover preference persistence and invalid settings, English exceptions, Chinese/English round trips, and switching language with a synthetic connection. The live form test verifies that input values, faction selection, undo availability, appraisal and item-library states, shortcut display and the session object are preserved, and that changing language performs no game-memory writes.

The supported game's two file hashes, tooltip call path, item-library signatures, shortcut-handler exit branch, native library initialization and template quantity were checked locally. Singleton getters were also checked against the native code: the supported build uses class offset `0xB8` and static instance offset zero. Chinese and English main-window and process-picker previews were rendered using sample data and inspected. The executable carries file version `1.1.1.0`.

**Not yet validated:** connection to a live game session, visible in-game appraisal values, opening/closing the native item library, dragging items into the back room, edits during actual gameplay, reward/UI refresh timing, and save/reload persistence. Automated tests and static analysis are not a substitute for those checks. No real save was modified during validation.

Run `build.ps1`, then `test.ps1`. The release ZIP contains the generated test report. A live-game check should use a backed-up save and confirm each numeric field, undo, appraisal enable/disable/normal-exit restoration, item-library open/drag/close/disable, and save/reload behavior. Close the library window with its shortcut before disabling it or exiting the trainer.

## 简体中文

v1.1.1 诊断版在 Windows x64 通过了 56 项自动测试，覆盖指针、类型、字符串与列表校验，Unicode 游戏文字，输入范围，势力身份，失效存档与对象，撤销冲突，多项写入回退，进程搜索与身份验证，鉴定代码签名，单字节变更及恢复，写入失败，代码冲突，游戏退出，以及独立测试内存页的 Win32 页面权限恢复。

新增四项诊断测试，定位空指针、损坏指针和读取失败字段；确认某项检查失败后其他只读检查仍继续；保留可空标识；验证双语提示、无内存写入、不收集存档字符串，以及 UTF-8 日志、唯一文件名与目录回退。测试验证的是诊断机制，不能据此确定用户实机失败的原因。

物品库新增的 12 项测试覆盖默认关闭、实际快捷键、不改变全局开发模式、跳转目标、对象就绪检查、签名不匹配、部分写入回退、线程忙时拒绝修改、与鉴定功能分别恢复、外部代码冲突、游戏退出、重叠写入、按键名称和拒绝暂停修改器自身。临时辅助进程验证了原生线程上下文检查、暂停期间的多处代码修改、原始字节恢复、可执行页面权限恢复与进程继续运行。辅助进程只包含测试数据，不含游戏代码。

语言测试覆盖偏好保存与无效设置、英文错误信息、中英文来回切换，以及带模拟连接的窗口切换。窗口测试确认输入值、势力选择、撤销记录、鉴定与物品库状态、快捷键显示和会话对象均保留，切换语言不会写入游戏内存。

已在本地核对支持版本的两份文件哈希、鉴定提示调用路径、物品库代码签名、按键处理器退出跳转、原生物品库初始化与模板数量；同时复核单例原生代码，确认该版本使用类偏移 `0xB8` 和静态实例偏移零。用示例数据渲染并检查了中英文主窗口与进程选择窗口。程序文件版本为 `1.1.1.0`。

**尚未验证：** 真实游戏会话连接、游戏内鉴定显示、打开与关闭原生物品库、拖放物品到后仓、实际游玩时的数值修改、奖励与界面刷新时机、保存后回读。自动测试和静态分析不能替代这些检查。验证过程中没有改动真实存档。

先运行 `build.ps1`，再运行 `test.ps1`。发布 ZIP 内附生成的测试报告。实机检查应使用已备份存档，逐项核对数值、撤销、鉴定开关及正常退出恢复、物品库打开／拖放／关闭／取消开关、保存回读效果。取消物品库开关或退出修改器前，先按快捷键关闭游戏内物品库窗口。

The seven v1.0.1 regression tests cover blank/null run IDs, an unassigned save slot, save-slot/run-number changes, native scene replacement, identity changes during a read, invalid pointers, and ordinary save timestamps. The original code reproduced five failures; the corrected code passes all 40 tests.

v1.0.1 新增七项回归测试，覆盖空标识、未分配槽位、切换槽位或轮次、原生场景对象替换、读取期间身份变化、损坏指针及正常保存时间戳。旧代码复现五项失败，修正后全部 40 项通过。
