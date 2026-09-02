# 项目进度记录

## 当前状态

**最后更新：** 2026-09-02 +08:00
**当前功能：** 无 in-progress 功能——feat-006（数据基础）与 feat-003（程序集边界重塑）均已关闭。
**状态：** 2026-09-02 决策（docs/superpowers/specs/2026-09-02-lifecycle-boot-simplification-design.md）已全部执行：
旧框架根目录源码（DI/Events/MVVM/Lifecycle/Boot/Input）全部删除；框架收敛为 DI / Events / Unity.DI；
composition root 落地 CardGame.Runtime；单场景形态确立。

## 本次完成：feat-003 程序集边界重塑（2026-09-02，按决策规格执行）

- **决策**：开放问题 #8（Boot/Lifecycle 简化）全按建议定案（Q1 A 构造即初始化+显式启动点 / Q2 A 不建通用 UpdateRunner /
  Q3 A 组合根落 CardGame.Runtime / Q4 A 代码注册 / Q5 A1 单场景+流程驱动作用域 / Q6 认可最终程序集图 / Q7 照此执行）。
- **删除**：根目录 `Lifecycle/`（8 文件）、`Boot/`（5 文件）、`Input/`（3 文件）；harness 的模块边界遍历检查
  替换为缺席检查（Lifecycle/Boot/MVVM 各自根目录不得存在任何 .cs）。
- **composition root 落地**：`CardGame.Runtime/Bootstrap/`——`GameBootstrap`（Bootstrap 场景唯一入口，持有数据
  TextAsset）→ `GameComposition`（DefineScope RunScope/EncounterScope + 代码注册 DataRepository/GameFlow + Build）
  → `GameFlow`（占位流程服务，显式 Start 承载屏障语义）。`GameDataCatalog` 删除，数据访问收敛为容器解析；
  Bootstrap 场景已挂载入口组件并绑定 8 个样例 JSON（临时编辑器脚本接线后删除）。
- **测试**：`GameCompositionTests` 4 个（单例解析 / Run→Encounter 作用域层级与祖先服务 / 显式启动屏障 / 释放后
  ContainerDisposed）；`SampleContentTests` 提取 `LoadSampleAssets` 供复用。
- **Input 执行期修正**：CardGame.Runtime 无 PlayerInput 占位（占位在已删除的根 `Input/` 内）；类型化包装类按
  "首个输入消费者出现时再从 InputSystem_Actions.inputactions 生成"处理（与 Q2 同一按需原则），记录于 06-input.md。
- **文档同步**：03-lifecycle / 05-boot 转退役存档（仿 04-mvvm）；06-input 转方向记录；01-di 吸收 DI 切实性论证
  （判据 + bug 发现时机表）；07-cardgame 吸收战斗引擎方向（伤害管线/属性组件/序列化地基）与结构更新；
  design README（最终程序集图/依赖方向/风险）、CONTRACT（框架子树）、DESIGN-REVIEW、README、AGENTS、HARNESS 同步。

## 验证证据

| 检查 | 命令 | 实际结果 |
|---|---|---|
| Unity EditMode（batchmode） | `UNITY_EDITOR=... verify.mjs --full` | **通过**：116/116（含 4 个新组合根测试；XML total=116 passed=116 failed=0），harness 29 项 0 失败 0 警告 |
| 便携 Harness | `bun scripts/harness/verify.mjs` | 通过：28 项 0 失败 1 个预期便携模式警告（本机无 node，用 bun 运行） |
| Harness 测试套件 | `bun test scripts/harness/tests/` | 通过：45/45 |
| git diff --check | 经 harness 内置检查 | 干净 |

## 已知限制与后续边界

- IL2CPP / AOT 未验证；玩法本体未实现；战斗引擎（伤害管线/属性组件/序列化）按决策规格 §1.1 方向待独立功能立项；
  输入包装类按需生成；Excel → JSON 转换器未开始（开放问题 #3 落点待拍板）。
- GameFlow 目前是占位流程服务：新局/遭遇的 RunScope/EncounterScope 创建释放将由未来游戏流程状态机驱动。
- 场景由编辑器重新序列化（serializedVersion 提升属正常 diff）；Unity 重存场景可能再次产生 `m_Name: ` 行尾空格，
  提交前跑 `git diff --check` 并修正新增行。

## 下一步

1. 下一个功能候选（需用户选择）：战斗引擎最小切片（决策规格 §1.1 方向：伤害管线 + 实体属性组件 + 无头模拟接缝）；
   或 Excel/Markdown → JSON 转换器（开放问题 #3）；或 UI 层 spike（开放问题 #1）。
2. feature_list 已全部同步：feat-001/002/003/005/006 done，feat-004（Unity Test Host）待重估——
   其目标已由 feat-005/006 实质满足，建议降级为文档性结论或关闭。
