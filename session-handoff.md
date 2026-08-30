# 会话交接

## 当前状态

- `feat-003 Assembly Definition Boundaries` 处于 `in-progress`，形态已按 2026-08-30 定案重塑（`feature_list.json` 已同步）。
- Events 已迁入编译域（`257cd4c`）；MVVM 已退役（`docs/design/04-mvvm.md` 🚫）；Lifecycle/Boot 简化待拍板。
- 设计定案与候选集中记录于 `docs/design/09-ui-resources.md`（UI 全 UITK + PrimeTween、资源 Addressables、数据 Excel → JSON、MVVM 评估、开放问题表）。

## 已完成内容（2026-08-30 会话）

- 创建 `09-ui-resources.md`：UI/资源/数据管线方案对比、MVVM 必要性评估（定案 B）、性能专项、参考资料、连带建议与开放问题表。
- `04-mvvm.md` 退役存档；`07-cardgame.md` 与 `docs/design/README.md` 同步数据管线与退役状态；`feature_list.json` feat-003 重塑。
- 协作模型定案记录：外部协作者只碰配置表/文案，代码单人；数据管线以"协作者无需 Unity"为约束。

## 最新验证证据

| 检查 | 结果 |
|---|---|
| 本轮便携 Harness | 未运行（环境无 Node：`node: command not found`） |
| 历史基线（2026-08-19） | 25 passed、0 failure、1 warning |

## 已知限制

- 本轮文档改动未提交（含未跟踪的 `docs/design/` 整目录）。
- IL2CPP/AOT 未验证；玩法本体未实现。

## 下一可执行步

1. 在装有 Node 的环境运行 `node scripts/harness/verify.mjs` 重建基线（检查 feature_list/文档一致性）。
2. 打开 `docs/design/09-ui-resources.md` 文末开放问题表，拍板 #1（战斗层 spike 立项）与 #8（Boot/Lifecycle 简化）。
3. 确认后：同步改写 03-lifecycle.md / 05-boot.md，提交全部文档改动。
