# CardGame 协作指南

CardGame 是一个可运行的 Unity 项目，固定使用 Unity `6000.3.10f1`。项目只服务于 CardGame 的叙事肉鸽卡牌体验。根目录保留的 RazorFramework 源码是后续重构输入，不是本阶段的 Unity 编译对象。

## 开始前

在修改文件前必须：

1. 确认当前目录是仓库根目录，并确认不会覆盖无关的未提交改动。
2. 完整阅读本文件。
3. 阅读 `feature_list.json`、`progress.md` 和 `session-handoff.md`。
4. 按任务读取相关文档：`README.md`（目录与入口）、`DESIGN-REVIEW.md`（旧框架风险）、`docs/CONTRACT.md`（游戏数据契约）和 `docs/HARNESS.md`（验证流程）。
5. 查看 `git status --short` 与 `git log --oneline -5`。
6. 运行 `node scripts/harness/verify.mjs` 建立基线。此命令也会检查 Unity 宿主结构；若失败，必须区分既有失败和当前任务引入的失败。

## Superpowers 流程

项目本地技能位于 `.agents/skills/`，按需渐进读取：

1. 从 `using-superpowers` 开始，确认是否有更匹配的技能。
2. 新行为或架构变更先使用 `brainstorming`。
3. 已确认的多步骤工作先使用 `writing-plans`。
4. 功能与缺陷修复使用 `test-driven-development`；失败或异常先使用 `systematic-debugging`。
5. 宣称完成、提交或交接前使用 `verification-before-completion`。
6. 仅在触发条件与当前权限都满足时，使用工作树、评审、并行代理和分支收尾技能。

直接用户指令与运行时权限优先于技能指令。

## CardGame 目录边界

| 区域 | 可以放置 | 禁止放置 |
|---|---|---|
| `Assets/CardGame/` | CardGame 场景、脚本、资源、配置和 EditMode 测试 | 未审计的旧框架迁移品、生成缓存 |
| `Packages/`、`ProjectSettings/` | 经审阅的包与项目配置 | 临时本机路径、凭据、生成解决方案 |
| 根目录旧 RazorFramework 模块 | 只读重构输入与既有静态边界检查对象 | 新的玩法规则、事件、场景和美术常量 |
| `scripts/harness/`、状态文档 | 可重复验证、任务状态和交接信息 | 绕过验证的临时开关、密钥 |

不要把 CardGame 的玩法、叙事事件、场景内容或美术常量加入旧框架目录。若要将框架迁入 `Assets/Plugins/RazorFramework/`，必须建立独立功能、先写测试并在 Unity 中验证。

## 范围、文档与状态

- 任一时刻最多一个功能处于 `in-progress`。
- 实质实现前选择或新增功能，明确依赖和完成条件。
- 不修改脏工作区中的无关改动；长期决定必须记录在仓库内，而不是只存在对话中。
- 新建或实质修改的设计、规格、计划、进度、交接和面向非程序协作者的说明默认使用简体中文；类型、命令、路径和配置键保持英文。

## 验证

便携验证：

```text
node scripts/harness/verify.mjs
```

Windows 便利入口：

```text
powershell -ExecutionPolicy Bypass -File .\init.ps1
```

POSIX/Git Bash 便利入口：

```text
./init.sh
```

完整 Unity 验证只需要设置 `UNITY_EDITOR`：

```powershell
$env:UNITY_EDITOR='C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'
node scripts/harness/verify.mjs --full
```

完整模式默认检查当前仓库；`RAZOR_UNITY_PROJECT` 只是可选的兼容宿主覆盖路径。Harness 不会把 Unity 自动生成的 `.sln` 或 `.csproj` 当成独立 `.NET` 测试入口。完整模式会要求 Unity 退出成功，并验证新生成的 EditMode XML 至少包含一个通过且没有失败的测试。

## 完成条件

功能完成前，适用项必须全部满足：

- 请求的行为和验收条件已经满足；缺陷修复在可行时先有失败测试或等价复现。
- `node scripts/harness/verify.mjs` 在修改后通过。
- Unity 相关内容已经在 Unity `6000.3.10f1` 中实际运行编译和 EditMode 测试；无法运行时，在状态文档记录具体限制。
- `git diff --check` 干净，变更不含无关文件。
- 公开行为或流程同步到 `README.md` 或 `docs/`；状态文件保留实际命令和结果。

不要用预期替代实际观察结果。会话结束前重跑相关检查，更新 `progress.md` 的历史证据，并在 `session-handoff.md` 留下下一位协作者可直接执行的一步。

## 生成物与安全

绝不提交 `Library/`、`Temp/`、`Logs/`、`UserSettings/`、`.vs/`、Unity 生成的 `.sln`、`.slnx` 或 `.csproj`。不要在代码、文档、状态文件或命令示例中记录凭据。破坏性操作与外部副作用需要用户明确授权。
