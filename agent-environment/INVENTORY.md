# Agent 环境清单

> 记录 CardGame 仓库内已版本化的 agent 协作环境，以及从全局 Codex 配置目录复制进仓库的配置副本。
> 生成日期：2026-08-29

## 一、项目内 agent 环境（已随仓库版本化）

### 1. 项目本地 Superpowers 技能库 `.agents/skills/`

固定在 Superpowers `v6.2.0`（提交 `3dcbd5c4b48e02263fbf4a3c01e3fe4f81d584d9`），许可证与来源元数据见 `third_party/superpowers/`。共 14 个技能：

| 技能 | 用途 |
|---|---|
| brainstorming | 创意工作前梳理意图、需求与设计 |
| dispatching-parallel-agents | 两个以上互相独立的并行任务 |
| executing-plans | 在独立会话中执行已写实施计划并做检查点 |
| finishing-a-development-branch | 实现完成、测试通过后决定如何集成 |
| receiving-code-review | 接收代码评审反馈，核实后再实现 |
| requesting-code-review | 完成任务或合并前请求评审 |
| subagent-driven-development | 当前会话内以独立子任务执行实施计划 |
| systematic-debugging | 任何 bug、测试失败或异常行为排查 |
| test-driven-development | 实现功能或修复前先写失败测试 |
| using-git-worktrees | 需要隔离工作区时 |
| using-superpowers | 会话开始时发现并调用技能 |
| verification-before-completion | 声称完成、修复或通过前先运行验证 |
| writing-plans | 有多步骤规格后、写代码前制定计划 |
| writing-skills | 创建、编辑或验证技能 |

### 2. 项目本地 Codex 配置 `.codex/config.toml`

仅开启 `multi_agent = true`，用于 Superpowers 的并行与子代理流程；是否实际使用仍受 `AGENTS.md` 约束。

### 3. 项目指令 `AGENTS.md`

仓库根目录的协作指令，定义启动顺序、目录边界、验证与完成条件。

## 二、全局 Codex 配置副本（本次复制进仓库）

以下文件来自全局配置目录 `C:\Users\13746\.codex\`，按“拷贝一份”的要求放入 `global-config/`。只复制配置，不含凭据与运行数据：

| 源文件 | 仓库副本 | 说明 |
|---|---|---|
| `~/.codex/config.toml` | `global-config/config.toml` | Codex 全局主配置 |
| `~/.codex/deepseek.config.toml` | `global-config/deepseek.config.toml` | DeepSeek profile 配置 |

> 注意：这些配置含本机绝对路径与本地回环地址，仅作为环境记录保存，不能直接当作可移植配置使用。

## 三、全局 Codex 环境清点（未整体复制）

全局目录 `C:\Users\13746\.codex\` 除配置外还包含以下内容。本次只复制了上面两个配置文件，其余内容留在本机。

### 全局技能 `~/.codex/skills/`

| 技能 | 位置 |
|---|---|
| imagegen | `.system/imagegen` |
| openai-docs | `.system/openai-docs` |
| plugin-creator | `.system/plugin-creator` |
| review-agent | `.system/review-agent` |
| skill-creator | `.system/skill-creator` |
| skill-installer | `.system/skill-installer` |
| pdf | `pdf` |

另有 bundled 插件技能（如 `browser`、`visualize`），位于 `~/.codex/plugins/cache/openai-bundled/`。

### 未复制内容及原因

| 内容 | 未复制原因 |
|---|---|
| `auth.json` | 含 `OPENAI_API_KEY` 凭据 |
| `.sandbox-secrets/`、`.sandbox/`、`.sandbox-bin/` | 沙箱密钥与运行态 |
| `*.sqlite`、`*.sqlite-wal`、`*.sqlite-shm`（如 `logs_2.sqlite` 约 278 MB） | 日志、会话、记忆等运行数据库，体量大且含私有数据 |
| `sessions/`、`archived_sessions/`、`memories/` | 会话与记忆数据 |
| `backups/`、`backups_state/`、`cache/`、`tmp/`、`.tmp/`、`visualizations/`、`plugins/` 等 | 备份、缓存与运行时生成物 |
| `*.bak`、`.codex-global-state.json` 等本地状态文件 | 备份与本地状态，非当前配置 |
