# Changes / 更新记录

## v1.0.1 — Save detection fix / 存档识别修复

### 简体中文

- 修正已进入店铺却提示“没有已加载的游戏进度”的判断错误：`runID` 允许为空，不能据此认定未读档。
- 用存档槽位、游戏轮次以及场景对象辅助识别会话；读取前后核对身份，防止切换存档时使用混合数据。失效会话会清除撤销记录。
- 保留版本哈希、对象类型、指针、声望列表和写入前后校验。`runID` 可以为空，但损坏的非空指针仍会被拒绝。
- 连接成功后，在日志中显示当前金钱、天数和租金剩余天数，便于核对。
- 增加 7 项回归测试。旧代码在相关用例中复现问题，修复后共 40 项通过。

这是本地修复版，尚未创建新的 GitHub Release；仓库继续保持私有。仍需在真实游戏中确认连接与修改效果。退出旧修改器后运行 v1.0.1，进入店铺并重新选择游戏进程。若首次保存导致存档身份改变，重新连接即可。

### English

- Fix the erroneous “No loaded game progress was found” rejection while already in the shop. An empty `runID` is valid and is no longer treated as proof that no game is loaded.
- Track the save slot, run number and scene object alongside the optional run ID. Compare identity before and after reading a snapshot to reject mixed data during reloads. Invalidate pending undo when a session becomes stale.
- Preserve binary hash, type, pointer, reputation-list and before/after-write validation. Null run IDs are accepted; corrupt non-null pointers are still rejected.
- Log the current money, day and rent countdown after connecting.
- Add seven regression tests. Relevant tests reproduced the failure on the old code; all 40 checks pass with the fix.

This is a local repair build, with no new GitHub Release. The repository remains private. Live gameplay validation is still required. Close the old trainer, run v1.0.1, enter the shop and select the process again. Reconnect if the game's first save changes its identity.

## v1.0.0

Initial version with process selection, numeric edits, appraisal display and Chinese/English UI. See [the original release notes](RELEASE_NOTES.md).

首个版本，包含进程选择、数值修改、鉴定价值显示与中英文界面。详见[原始发布说明](RELEASE_NOTES.md)。
