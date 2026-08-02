# 项目进度记录

## 当前状态

**最后更新：** 2026-08-02 +08:00
**当前功能：** 无；feat-005 CardGame Unity Project Host 已完成。
**状态：** 等待下一项独立功能立项。

## 本次完成

- 完成 Unity `6000.3.10f1` 项目宿主迁移与验证；允许迁入的项目根目录为 `Assets/`、`Packages/` 与 `ProjectSettings/`，并保留所需 `.meta` 文件。
- 模板场景与设置已归入 `Assets/CardGame/`：首个启用构建场景为 `Assets/CardGame/Scenes/Bootstrap.unity`；项目身份为 `E-Dawnize / CardGame`，Standalone identifier 为 `com.edawnize.cardgame`。
- Harness 已固定验证 Unity 版本、项目身份、首个启用场景和四个关键包版本：URP `17.3.0`、Input System `1.18.0`、Unity Test Framework `1.6.0`、UGUI `2.0.0`。
- 旧 RazorFramework 源码仍位于仓库根目录，只作为后续重构输入；本次未将其迁入 `Assets/Plugins/RazorFramework/`，因此不属于 Unity 编译对象。

## 最终验证证据（2026-08-02）

| 检查 | 命令 | 实际结果 |
|---|---|---|
| Node 契约测试 | `node --test scripts/harness/tests/*.test.mjs` | 25/25 通过，0 失败 |
| 便携 Harness | `node scripts/harness/verify.mjs` | 22 passed、2 warning(s)、0 failure(s) |
| Unity 完整 Harness | `$env:UNITY_EDITOR='C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'; node scripts/harness/verify.mjs --full` | 23 passed、1 warning(s)、0 failure(s) |
| Unity EditMode XML | 完整 Harness 新生成的结果 XML | `result=Passed`、`total=3`、`passed=3`、`failed=0` |
| 差异检查 | `git diff --check` | 无输出，退出码 0 |

完整验证实际使用 Unity `6000.3.10f1`。首次导入后的工作树保持干净，`Packages/packages-lock.json` 没有额外差异；`Library/`、`Temp/`、`Logs/`、`UserSettings/`、`.sln`、`.slnx` 与 `.csproj` 均未被 Git 跟踪。

## 已知限制与后续边界

- 便携模式仍保留两项预期警告：`DI/DIContainer.cs` 的 Unity 空值判断债务（由 feat-002 跟踪）以及未运行 Unity；完整模式已实际完成 Unity 编译与 EditMode 测试，只保留前者警告。
- 本功能未精简模板包；包精简必须在独立功能中以导入测试保护。
- 本功能只建立项目宿主、迁移基底和验证基础设施，尚未实现卡牌战斗、地图、剧情、存档、文案内容管线或数值模拟。

## 下一步

1. 为 RazorFramework Core、DI 与 Lifecycle 编写独立重构规格和实施计划，先消除 DI 对 `UnityEngine` 的编译时依赖。

## 最终审查修复证据

- 显式执行 `node scripts/harness/verify.mjs --full` 且清除 `UNITY_EDITOR` 时，命令输出 `[FAIL]` 与 Unity `6000.3.10f1` 设置提示，并以非零状态退出；完整模式不再以“未运行”警告假绿。
- Unity 结果验证使用栈式 XML 解析，拒绝内部标签未闭合或错配、多个根元素、重复属性、DOCTYPE、根外非空文本、非整数计数、超出 JavaScript 安全整数范围的计数与分类计数矛盾；实际 Unity 新鲜 XML 仍为 `result=Passed`、`total=3`、`passed=3`、`failed=0`。
- 最终 Node 契约测试为 25/25 通过；便携 Harness 为 22 passed、2 warning(s)、0 failure(s)；真实 Unity 完整 Harness 为 23 passed、1 warning(s)、0 failure(s)。
