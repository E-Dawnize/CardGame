# 项目进度记录

## 当前状态

**最后更新：** 2026-08-19 +08:00
**当前功能：** feat-003 Assembly Definition Boundaries 进行中。
**状态：** feat-002 已合并到 `main` 并推送；feat-003 的设计规格与实施计划已写入仓库。

## 本次完成：feat-002 整合与 feat-003 设计

- 发现 `codex/di-v2` 已完整实现并验证 feat-002，但未合并到 `main`；`main` 状态文档相对该工作线滞后。
- 以快进方式把 `codex/di-v2`（`9bf9e80`）合并到 `main`，清理 `.worktrees/di-v2` 与 `codex/di-v2` 分支，并推送到 `origin/main`。
- 合并后在 `main` 重跑便携 Harness：`25 passed、0 failure、1 warning`（warning 仅为便携模式未启动 Unity）。
- 读取 `docs/legacy-framework-audit.md` 与真实源码，确认根目录 `Events`、`Lifecycle`、`MVVM`、`Input`、`Boot` 大量引用已删除的 DI V1 API，feat-003 属于重写而非机械迁移。
- 写出 `docs/superpowers/specs/2026-08-19-assembly-definition-boundaries-design.md` 与 `docs/superpowers/plans/2026-08-19-assembly-definition-boundaries.md`。

## 验证证据（2026-08-19）

| 检查 | 命令 | 实际结果 |
|---|---|---|
| 合并后便携 Harness | `node scripts/harness/verify.mjs` | 25 passed、0 failure、1 warning（未启动 Unity） |
| 分支关系 | `git merge-base main codex/di-v2` | `01ade59`，即 `main`，可快进 |
| 推送 | `git push origin main` | `01ade59..9bf9e80 main -> main` |

Node 全量契约测试在本会话沙箱只读限制下无法完整重跑：`mkdtemp` 与 fixture 写入返回 `EPERM`。feat-002 历史证据为 45/45 通过；实际 Unity 完整验证待 feat-003 迁入后进行。

## 已知限制与后续边界

- 旧框架模块与 DI V2 不兼容，需按规格重写后才能进入 Unity 编译范围。
- IL2CPP、托管代码剥离、AOT 与目标平台构建仍未验证。
- 卡牌战斗、伤害结算、地图、叙事、存档、文案与数值均未实现。

## 下一步

1. 按 `docs/superpowers/plans/2026-08-19-assembly-definition-boundaries.md` 执行 Task 1（激活 feat-003 并固定 asmdef 边界契约），随后 Task 2 迁移 Events。
