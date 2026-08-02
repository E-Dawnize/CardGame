# CardGame — 叙事肉鸽卡牌游戏

CardGame 是一个基于 Unity 的叙事肉鸽卡牌项目。它以分支地图、卡牌战斗和可重复挑战为骨架，并让主线事件、特殊节点和碎片文本持续推进故事。当前仓库已经是可直接打开的 Unity `6000.3.10f1` 项目宿主。

## 目录职责

| 位置 | 当前职责 | 维护边界 |
|---|---|---|
| `Assets/CardGame/` | Bootstrap 场景、项目设置资产和 CardGame 的 EditMode 测试 | 新增游戏脚本、资源、场景与配置只放在这里或明确的子目录 |
| `Packages/`、`ProjectSettings/` | Unity 版本、包版本、项目身份与构建场景 | 改动后必须运行 Harness；不记录本机临时路径 |
| 根目录 `Boot/`、`DI/`、`Events/`、`Input/`、`Lifecycle/`、`MVVM/` | 历史 RazorFramework 源码，作为后续重构输入 | 不在此阶段进入 Unity 编译范围，也不添加 CardGame 玩法代码 |
| `scripts/harness/` 与状态文档 | 可重复验证、功能状态和协作交接 | 结构契约变更必须同时更新测试与说明 |

这种划分先保证 CardGame 有可验证的 Unity 地基；旧框架的迁移或重构会在独立功能中进行，避免未经验证的通用代码阻塞游戏开发。

## 验证

便携验证不启动 Unity，但会检查 Harness 状态、静态边界和 Unity 项目宿主结构：

```powershell
node scripts/harness/verify.mjs
```

完整验证会运行实际 Unity EditMode 测试。Windows PowerShell：

```powershell
$env:UNITY_EDITOR='C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'
node scripts/harness/verify.mjs --full
```

POSIX shell 或 Git Bash：

```bash
export UNITY_EDITOR='/path/to/Unity'
node scripts/harness/verify.mjs --full
```

完整模式默认把当前仓库作为 Unity 项目路径，`UNITY_EDITOR` 是唯一必需的环境变量。只有在验证另一个兼容宿主时，才设置可选的 `RAZOR_UNITY_PROJECT`。完整验证同时要求 Unity 进程成功退出、生成新的结果 XML，且 XML 显示至少一个通过且没有失败的测试。

显式执行 `--full` 表示调用方要求取得 Unity 运行证据。如果没有设置 `UNITY_EDITOR`，Harness 会输出 `[FAIL]`、给出设置 Unity `6000.3.10f1` 可执行文件的提示，并以非零状态退出；只有便携模式允许不启动编辑器。结果 XML 还必须完整闭合，且 `total`、`passed`、`failed`、`skipped` 与 `inconclusive` 计数自洽。

详细工作流见 [Harness 维护指南](docs/HARNESS.md)。开始实质工作前，请阅读 [AGENTS.md](AGENTS.md)、`feature_list.json`、`progress.md` 与 `session-handoff.md`。

## 版本控制边界

提交源资产、`.meta`、`Packages/`、`ProjectSettings/` 与维护脚本；不要提交 Unity 生成物：`Library/`、`Temp/`、`Logs/`、`UserSettings/`、`.vs/`、`.sln`、`.slnx` 和 `.csproj`。这些本机文件不是可复现的项目输入。
