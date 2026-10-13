# Changes / 更新记录

## v2.0.0 — New UI / 新 UI

### 简体中文

- 主窗口与进程选择器更换新 UI。
- 数值与声望放在左侧，游戏辅助与会话操作放在右侧，操作记录集中显示。
- 主窗口可调整大小并适应屏幕工作区，较小窗口可滚动访问底部内容。
- 改善按钮和开关的禁用状态与键盘焦点显示，补齐新版双语文字和截图。
- 保留 v1.1.2 的连接及游戏数值逻辑，新增滚动回归测试，共 61 项通过。

详见[双语发布说明](docs/releases/v2.0.0.md)。仅供学习，欢迎大家反馈。

### English

- Update the main window and process picker with a new UI.
- Put numeric edits and reputation on the left, game tools and session actions on the right, with a dedicated activity area.
- Make the main window resizable and fit it to the screen working area; smaller windows scroll to lower content.
- Add clearly painted disabled states and keyboard focus to buttons/toggles, plus updated bilingual text and screenshots.
- Retain v1.1.2 connection and game-editing logic. Add a scrolling regression; all 61 checks pass.

See the [bilingual release notes](docs/releases/v2.0.0.md). Provided for learning purposes only. Feedback is welcome.

## v1.1.2 — Loaded reputation type fix / 声望类型识别修复

### 简体中文

- 根据三份实机日志定位连接失败：`StoreReputation.TypeInfo` 的 `0x2000A5A3` 是游戏尚未解析的类型标记，旧版错误地将它当成对象指针。店铺、玩家、日期及声望列表均已加载。
- 改从已加载的声望对象读取类型，核对 `StoreReputation` 类名、空命名空间和所有对象的同一类型身份，不再要求该全局类型槽已解析，也不写入它。
- 保留列表长度与容量、指针、势力 ID、数值、存档身份及写入校验；诊断报告同步检查实际声望对象。
- 增加四项回归测试，使用日志中的原始标记复现旧版三项失败；修复后共 60 项通过，覆盖连接、数值修改、声望撤销和错误类型拒绝。

用户已确认 v1.1.2 在游戏中可用，本版公开发布，详见[双语发布说明](docs/releases/v1.1.2.md)。退出旧修改器后使用新版，进入店铺并重新选择进程。仅供学习，欢迎大家反馈。

### English

- Identify the connection failure from three live-game logs: `StoreReputation.TypeInfo` contains the unresolved type token `0x2000A5A3`, which the trainer incorrectly treated as an object pointer. The shop, player, station and reputation list were already loaded.
- Read the class from an existing reputation object, validate the `StoreReputation` name and empty namespace, and require all entries to share that class. Do not require or write the global TypeInfo slot.
- Preserve list bounds/capacity, pointer, faction-ID, numeric, save-identity and write validation. Update diagnostics to inspect actual reputation objects.
- Add four regressions using the original token from the logs. The old implementation reproduced three failures; all 60 checks now pass, including connection, numeric edits, reputation undo and invalid-type rejection.

The user has confirmed v1.1.2 works in the game. This version is publicly released; see the [bilingual release notes](docs/releases/v1.1.2.md). Close the old trainer, run this build, enter the shop and select the process again. Provided for learning purposes only. Feedback is welcome.

## v1.1.1 — Connection diagnostics / 连接诊断版

### 简体中文

- 将笼统的“游戏对象尚未就绪”替换为具体失败对象，区分空指针、无效指针与读取失败，显示字段名称及地址。
- 通过版本校验后，读取对象失败时自动生成 `logs/connection-*.txt`，记录修改器版本、进程、模块基址、失败链路，以及店铺、玩家、日期与声望类型的独立只读检查。程序目录无法写入时使用 `%LOCALAPPDATA%/ProbablyStolenTrainer/logs`，弹窗显示最终路径。
- 诊断不写入游戏内存，不收集存档字符串或物品内容。日志写入失败不会掩盖原始连接错误。
- 新增四项诊断测试，共 56 项通过，覆盖失败位置、中英文错误、可空标识、独立检查、文件回退及报告保存。

这次尚未确定用户实机连接失败的具体对象；该版本用于取得定位证据，不能视为已经修复连接问题。请在出现问题的 Windows 内重新选择游戏进程，将新生成的连接日志用于排查。版本在本地 Git 记录，GitHub 仓库保持私有。

### English

- Replace the generic object-not-ready message with the failed field and address, distinguishing null pointers, invalid pointers and read failures.
- Automatically save `logs/connection-*.txt` when object reads fail after version verification, including trainer/process/module details, the failed pointer chain and independent read-only checks of the shop, player, station and reputation type. Fall back to `%LOCALAPPDATA%/ProbablyStolenTrainer/logs` when the program directory cannot be written; show the final path in the error dialog.
- Diagnostics write no game memory and collect no save-string values or item contents. A log-write failure preserves the original connection error.
- Add four diagnostic tests, bringing the total to 56 passing checks, covering failure locations, bilingual errors, optional IDs, independent probes, directory fallback and report preservation.

The specific live-game failure is still unknown. This build collects evidence for diagnosis and does not establish that connection is fixed. Reconnect on the Windows installation running the game and use the generated connection log for investigation. This version is recorded locally; the GitHub repository remains private.

## v1.1.0 — Back-room items / 后仓添加物品

### 简体中文

- 新增“开启物品添加菜单”开关，读取当前游戏实际快捷键，打开原生物品库并把物品拖到店铺后仓空位。
- 使用游戏原生物品创建、数量、堆叠和放置逻辑；不提供额外的物品 ID 或数量输入框。添加结果由游戏正常保存，不属于数值撤销范围。
- 开启期间跳过该按键处理器的其他调试命令，不开启全局开发模式。切换时核对代码、检查线程状态，并在写入失败时回退；正常关闭时恢复代码。需先用快捷键关闭物品库窗口，再关闭开关。
- 补齐中英文标签、使用说明、日志、错误信息与新版界面截图。保留 v1.0.1 的存档识别修复。
- 新增 12 项测试，共 52 项通过，包括独立辅助进程上的线程恢复、代码恢复与页面权限检查。静态核对支持版本的物品库路径；尚未完成真实游戏中的拖放和保存回读验证。

本版本在本地 Git 记录并打包，未创建新的 GitHub Release，仓库继续保持私有。

### English

- Add an item-library toggle and read the actual game shortcut. Open the native library and drag items into free back-room inventory space.
- Use native item creation, quantities, stacking and placement. There is no additional item-ID or quantity input box. Added items follow normal game saving and are outside numeric Undo.
- Skip other debug commands in this key handler without enabling global developer mode. Validate code and thread state, roll back failed writes, and restore code on normal shutdown. Close the library using its shortcut before disabling the toggle.
- Add bilingual controls, help, logs, errors and updated screenshots. Retain the v1.0.1 save-detection fix.
- Add 12 tests, bringing the total to 52 passing checks, including thread resumption, code restoration and page permissions in a disposable helper process. Statically verify the supported game's item-library path; actual gameplay placement and save/reload still need confirmation.

This version is recorded and packaged locally, with no new GitHub Release. The repository remains private.

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
