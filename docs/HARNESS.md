# CardGame Harness 维护指南

CardGame 使用 `walkinglabs/learn-harness-engineering` 的五子系统思路：指令、状态、验证、范围和生命周期。目标是让协作者在中断后仍能依据仓库中的同一组事实继续工作。

## 工件地图

| 子系统 | 工件 | 职责 |
|---|---|---|
| 指令 | `AGENTS.md` | 启动顺序、项目边界和完成门槛 |
| 状态 | `feature_list.json`、`progress.md` | 任务范围、状态、证据和限制 |
| 验证 | `scripts/harness/verify.mjs`、`init.ps1`、`init.sh` | Node 契约、Unity 宿主与 EditMode 验证 |
| 范围 | 功能依赖和 `doneCriteria` | 只推进一个活动功能 |
| 生命周期 | `session-handoff.md` | 为下一次会话提供可执行的恢复点 |

`feature_list.schema.json` 描述状态文件结构。Harness 还会检查依赖引用、循环依赖、状态合法性、已完成功能证据和“最多一个进行中功能”规则。

## 启动与 Node 契约测试

从仓库根目录开始：

```text
node scripts/harness/verify.mjs
```

然后读取状态工件，只选择一个 `in-progress` 功能。Windows 可以使用 `.\init.ps1`，POSIX 或 Git Bash 可以使用 `./init.sh`；这些入口不会安装依赖或改写项目配置。

修改 `scripts/harness/` 时，先运行：

```text
node --test scripts/harness/tests/*.test.mjs
```

新规则必须先以失败测试表达。测试覆盖 Unity 宿主结构、命令行入口以及完整验证对结果 XML 的判定。

## 便携验证

`node scripts/harness/verify.mjs` 检查：

- 必需 Harness 工件、功能状态和固定版本的 Superpowers 技能库；
- 根目录旧 RazorFramework 的命名空间与纯 C# 边界；
- CardGame Unity 宿主：Unity `6000.3.10f1`、`E-Dawnize / CardGame` 身份、`com.edawnize.cardgame`、四个关键包，以及第一个启用的 Bootstrap 场景；
- `git diff --check`。

便携模式不启动 Unity，因此不能代替实际 C# 编译或场景加载。若宿主检查失败，先检查 `ProjectSettings/`、`Packages/manifest.json` 和 `Assets/CardGame/Scenes/Bootstrap.unity` 是否被移动、删改或使用了错误版本。

当前已知警告是 `DI/DIContainer.cs` 中单个 `UnityEngine.Object` 空值判断债务，由 feat-002 跟踪；新增 DI Unity 引用仍然会失败。

## 完整 Unity 验证

完整模式先运行全部便携检查，再调用 Unity EditMode 测试。Windows PowerShell：

```powershell
$env:UNITY_EDITOR='C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'
node scripts/harness/verify.mjs --full
```

POSIX/Git Bash：

```bash
export UNITY_EDITOR='/path/to/Unity'
node scripts/harness/verify.mjs --full
```

`UNITY_EDITOR` 是完整模式唯一必需的环境变量。默认 Unity 项目为当前仓库根目录；验证另一个兼容宿主时才可选设置：

```powershell
$env:RAZOR_UNITY_PROJECT='D:\path\to\compatible-host'
node scripts/harness/verify.mjs --full
```

Harness 有意不传 `-quit`，避免 Unity 在测试运行前被提前终止。每次完整运行会生成唯一的临时 XML 路径，并在启动 Unity 前清理该路径；只有 Unity 退出码为 0，且新 XML 的 `<test-run>` 同时满足 `result="Passed"`、`total > 0`、`failed = 0` 时，Harness 才报告成功。缺少 XML、零测试、失败测试、进程启动错误、信号退出或非零退出码都会给出可行动的失败消息。

显式 `--full` 代表请求完整证据；如果未设置 `UNITY_EDITOR`，Harness 会输出 `[FAIL]`、提示设置 Unity `6000.3.10f1` 可执行文件并返回非零状态。这个行为与便携模式不同：便携模式不要求编辑器，只会说明没有运行 Unity。完整模式使用小型栈式解析器验证 XML 的单根结构、内部标签嵌套与属性唯一性，并拒绝 DOCTYPE、根外非空文本、`passed=0`、非十进制整数、超出 JavaScript 安全整数范围的计数，以及 `total` 与 `passed + failed + skipped + inconclusive` 不一致的结果。

Harness 不依据 Unity 自动生成的 `.sln` 或 `.csproj` 触发 `dotnet test`，因为它们不是 CardGame 的独立测试宿主。

## 维护规则

- 修改 Unity 宿主契约时，先补充 `scripts/harness/tests/` 中的 Node 行为测试，再修改检查器。
- 包版本、项目身份、Bootstrap 路径或完整验证结果规则变更，都必须同步更新测试、文档和状态证据。
- 只提交源资产、`.meta`、`Packages/`、`ProjectSettings/` 和维护脚本；禁止提交 `Library/`、`Temp/`、`Logs/`、`UserSettings/`、`.vs/`、`.sln`、`.slnx` 或 `.csproj`。
- 不要为了让 Harness 通过而降低边界规则；应修复根因，或把经审阅的例外记录到功能状态。

## 技能与状态维护

官方 `obra/superpowers` 技能库固定在 `.agents/skills/`，版本 `v6.2.0`、提交 `3dcbd5c4b48e02263fbf4a3c01e3fe4f81d584d9`，许可证位于 `third_party/superpowers/LICENSE`。更新时先记录上游标签与提交，在临时目录比较差异，审阅安全影响后再更新明确的技能目录与 `third_party/superpowers/SOURCE.json`，随后重跑便携 Harness。

合法功能状态为 `not-started`、`in-progress`、`blocked` 和 `done`。阻塞状态必须记录具体阻塞点和解除动作；完成状态必须记录实际命令与结果。会话结束时更新 `progress.md`，并在 `session-handoff.md` 留下最新的可执行下一步。

Harness 不会自动安装软件、改写项目配置或上传数据。不要在说明、状态、技能或命令示例中写入凭据；破坏性操作与外部副作用仍需用户明确决定。
