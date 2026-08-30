# 项目进度记录

## 当前状态

**最后更新：** 2026-08-30 +08:00
**当前功能：** feat-003 Assembly Definition Boundaries 进行中（形态按 2026-08-30 定案重塑）。
**状态：** Events 已迁入编译域（`257cd4c`）；MVVM 已定案退役；Lifecycle/Boot 简化与 CardGame.Runtime 建立待定案（09 开放问题 #8）；数据管线（Excel→JSON）与 Addressables 资源管线已定案、待开工。

## 本次完成：UI/资源/数据管线设计定案（2026-08-30）

- 创建 `docs/design/09-ui-resources.md`：UI 单一栈全 UITK + PrimeTween（战斗层 spike 先行、失败回退战斗层 uGUI）、资源 Addressables 分组 + 按屏预载、数据管线 Excel → JSON（Node 转换器进 Harness）、MVVM 必要性评估（定案 B：不建程序集）、性能专项、参考资料与开放问题表。
- `docs/design/04-mvvm.md` 改标 🚫 退役存档；`docs/design/README.md` 索引/图例/feat-003 目标图同步；`docs/design/07-cardgame.md` 数据契约改为 JSON schema 表述；`feature_list.json` feat-003 的 description/doneCriteria 重塑。
- 协作模型定案：人数很少，外部协作者只碰配置表与文案（Excel/Markdown/ui-strings.json），代码侧单人；协作设施只在数据管线保留（协作者无需 Unity 即可转换校验）。
- 连带建议（待确认，09 开放问题 #8）：Boot 不建框架程序集（Installer 纯 C#，启动编排进 CardGame.Runtime）；Lifecycle 不迁两阶段引擎（最多 50 行 UpdateRunner 进 CardGame.Runtime）。

## 验证证据

| 检查 | 命令 | 实际结果 |
|---|---|---|
| 便携 Harness（本轮） | `node scripts/harness/verify.mjs` | 未运行：本环境无 Node（`node: command not found`），不构成验证证据 |
| 便携 Harness（历史基线） | 同命令，2026-08-19 | 25 passed、0 failure、1 warning（未启动 Unity） |

## 已知限制与后续边界

- 本轮文档改动未提交（`docs/design/` 整目录未跟踪）。
- IL2CPP、托管代码剥离、AOT 与目标平台构建仍未验证。
- 卡牌战斗、伤害结算、地图、叙事、存档、文案与数值均未实现。

## 下一步

1. 在装有 Node 的环境重跑便携 Harness，随后按需跑完整 Unity 验证。
2. 拍板 09 开放问题 #1（战斗层 spike 立项）与 #8（Boot/Lifecycle 简化）；确认后同步 03/05 模块文档。
3. 立新功能点：Addressables 装包分组 + 数据管线（Node 转换器 + Harness 校验 + CONTRACT 同步）。
4. 提交本轮全部文档改动。
