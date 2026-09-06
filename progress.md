# 项目进度记录

## 当前状态

**最后更新：** 2026-09-06 +08:00
**当前功能：** 无 in-progress 功能——feat-007（场景成员注入驱动）已关闭；此前 feat-003/006 已关闭。
**状态：** feat-007（场景 `[Inject]` 成员注入驱动）已全部执行：场景容器外脚本的 `[Inject]`/`[InjectOptional]` 成员在启动期被 `SceneInjection.InjectScene` 真实注入。

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

## 本次完成：feat-007 场景成员注入驱动（2026-09-06，按决策规格执行）

- **问题**：`UnityObjectInjector` + `[Inject]`/`[InjectOptional]` 机制完备但无驱动——场景容器外脚本（Unity 实例化的 MonoBehaviour）的标记成员从不被赋值。
- **落地**：`CardGame.Runtime` 新增 `SceneInjection.InjectScene(IServiceResolver)`（扫描场景全部 MonoBehaviour 含 inactive 逐个注入）；`GameBootstrap` 加 `[DefaultExecutionOrder(-32000)]` 并在 Awake 中按「建组合根 → 注入场景 → StartFlow」接线，保证注入先于其他脚本 Awake/OnEnable。
- **框架改动**：仅 `AssemblyInfo.cs` +1 行 `InternalsVisibleTo("CardGame.Tests.EditMode")`；公开面零变化。
- **对齐法则**：对象的生命周期 ≤ 注入 resolver 的生命周期（常驻对象仅 Singleton；scope 拥有的屏由创建 scope 的驱动方从该 scope 注入）。
- **测试**：`SceneInjectionTests` 共 8 个（冒烟 / active+inactive 扫描 / 可选跳过 / 必需缺失 fail-fast / 根注入 Scoped 护栏 / scope 接缝 / 执行顺序）。
- **文档同步**：di-internals §13.3 驱动现状、README 场景驱动段、规格状态、feature_list feat-007 登记。

## 验证证据

| 检查 | 命令 | 实际结果 |
|---|---|---|
| Unity EditMode（batchmode） | `UNITY_EDITOR=... verify.mjs --full` | **通过**：124/124（含 8 个新 SceneInjectionTests；XML total=124 passed=124 failed=0），harness 29 项 0 失败 0 警告 |
| 便携 Harness | `bun scripts/harness/verify.mjs` | 通过：28 项 0 失败 1 个预期便携模式警告（本机无 node，用 bun 运行） |
| Harness 测试套件 | `bun test scripts/harness/tests/` | 通过：45/45（2026-09-06 修复波实跑确认：45 pass / 0 fail） |
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

## 追加：技能规模分级修订（2026-09-06，会话内决策）

- **背景**：feat-007 复盘确认「小任务大投入」（562 行计划 vs 265 行实现、11 次子代理派发、6 次评审仅 1 条实质 finding、计划内未编译代码的两个 API 笔误获得虚假权威）。
- **修订**（commit 73f7e71）：`writing-plans` 增加规模分级（小规格 → lean 计划 + inline 执行 + 单次终审）、「必须逐字钉接口/值/测试/命令，不得预写无法编译的实现体」、昂贵套件的验证节奏；`subagent-driven-development` 的 When-to-Use 增加规模门（小计划走 inline）+ 转录任务用机械保真核对替代模型评审。
- **验证**：harness 28/0（技能完整性门禁不受内容编辑影响）；writing-skills 流程按真实事故为 RED 基线，6 个全新上下文微测 5/6 达标，REFACTOR 收紧文件数谓词为行数谓词后复测收敛。
- **注意**：`.agents/skills/` 与 vendored `third_party/superpowers` v6.2.0 从此存在**有意的本地分歧**（用户授权）；升级上游时需重放本修订。
