# RazorFramework Agent Guide

RazorFramework is a source-only Unity framework extracted for reuse under
`Assets/Plugins/RazorFramework/`. This repository is not a complete Unity
project: it currently has no `Assets/`, `Packages/`, `.sln`, or `.csproj`.

## Startup Workflow

Before changing files:

1. Confirm that the working directory is the repository root.
2. Read this file completely.
3. Read `feature_list.json`, `progress.md`, and `session-handoff.md`.
4. Read only the project documents relevant to the task:
   - `README.md` for the public module overview.
   - `DESIGN-REVIEW.md` for known architecture risks and backlog.
   - `docs/CONTRACT.md` only for the game-data contract.
   - `docs/HARNESS.md` for harness operation and maintenance.
5. Inspect `git status --short` and `git log --oneline -5`.
6. Run `node scripts/harness/verify.mjs` before editing to establish a baseline.

If the baseline fails, distinguish an existing failure from one introduced by
the current task. Do not expand scope silently.

## Superpowers Skill Workflow

Project-local skills live in `.agents/skills/`. Load them progressively; do not
read the entire library into context.

1. Start with `using-superpowers` and check whether another skill applies.
2. For new behavior or architecture changes, use `brainstorming` before coding.
3. For an approved multi-step change, use `writing-plans`.
4. For features and bug fixes, use `test-driven-development`.
5. For failures or unexpected behavior, use `systematic-debugging`.
6. Before a completion claim, use `verification-before-completion`.
7. Use review, worktree, parallel-agent, and branch-finishing skills only when
   their trigger conditions apply and the current runtime/user permissions
   allow the required actions.

Direct user and runtime instructions take precedence over project skills.

## Documentation Language

- 新建或实质性修订的项目设计、规格、实施计划、进度和交接文档默认使用简体中文。
- C# 类型名、命令名、字段名、文件路径、配置键和外部工具名称保留英文。
- 面向非程序协作者的文档必须用中文解释专业概念，不要求读者理解代码。
- 第三方许可证、来源记录和必须保留原文的外部资料不强制翻译。
- 中英文项目决策冲突时，以仓库中已确认的中文文档为准。

## Architecture Boundaries

| Module | Allowed dependency direction | Project invariant |
|---|---|---|
| `DI/` | BCL only | Must not reference `UnityEngine` |
| `Lifecycle/` | `DI` + Unity | Owns initialization/start/tick ordering |
| `Events/` | `Lifecycle` | Events remain strongly typed value types |
| `MVVM/` | `Events`, `DI`; Unity only in view/binding code | ViewModels and commands stay pure C# |
| `Input/` | `Lifecycle` + Unity Input System | Input abstractions stay behind `IPlayerInput` |
| `Boot/` | All framework modules + Unity/Addressables | Orchestrates startup; no game-specific behavior |

Keep the `RazorFramework.*` namespace prefix. Do not add gameplay rules,
project-specific events, scene content, art constants, secrets, or generated
Unity assets to the reusable framework.

## Scope and State: One Feature at a Time

- Work on at most one `in-progress` feature in `feature_list.json`.
- Add or select a feature before material implementation work.
- Keep dependencies and done criteria explicit.
- Small read-only investigations and typo-only edits do not require a new
  feature, but their conclusions must not be reported as implementation.
- Do not modify unrelated user changes in a dirty worktree.
- Record durable decisions in repository documents, not only in chat history.

## Verification Commands

Portable repository checks:

```text
node scripts/harness/verify.mjs
```

Windows convenience entrypoint:

```text
powershell -ExecutionPolicy Bypass -File .\init.ps1
```

POSIX/Git Bash convenience entrypoint:

```text
./init.sh
```

Full verification, when a consuming Unity project and editor are configured:

```text
node scripts/harness/verify.mjs --full
```

The portable check validates harness state, vendored skills, module/namespace
boundaries, and repository whitespace. It is not a substitute for Unity
compilation or EditMode/PlayMode tests. See `docs/HARNESS.md`.

## Definition of Done

A feature is done only when all applicable items are true:

- Requested behavior and acceptance criteria are satisfied.
- A failing test or equivalent reproducible check preceded a bug fix when
  practical.
- `node scripts/harness/verify.mjs` passes after the change.
- Unity compilation/tests run in a consuming project when Unity-dependent code
  changed; otherwise the missing check is recorded as a limitation.
- `git diff --check` is clean and the diff contains no unrelated changes.
- Public API or workflow changes are reflected in `README.md` or `docs/`.
- `feature_list.json`, `progress.md`, and `session-handoff.md` contain current,
  concrete evidence.

Never mark a feature done based on expected rather than observed results.

## End of Session

Before ending material work:

1. Re-run the relevant verification commands.
2. Update the active feature status and evidence.
3. Update `progress.md` with changes, decisions, blockers, and exact commands.
4. Refresh `session-handoff.md` with the next executable step.
