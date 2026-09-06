# 会话交接

## 当前状态

- **无 in-progress 功能**：feat-007（场景成员注入驱动）已关闭（feature_list 全同步）；feat-003/006 已关闭。
- 2026-09-06 feat-007 已执行：场景容器外脚本的 `[Inject]`/`[InjectOptional]` 成员注入真正生效（`SceneInjection.InjectScene` 根驱动 + `GameBootstrap` 接线 + scope 接缝测试背书）。

## 本会话已完成

### feat-007 场景成员注入驱动（2026-09-06）

- 新增 `CardGame.Runtime/Bootstrap/SceneInjection.cs`：扫描场景全部 MonoBehaviour（含 inactive）逐个注入，fail-fast。
- `GameBootstrap` 加 `[DefaultExecutionOrder(-32000)]`，Awake 顺序 = 建组合根 → 注入 → StartFlow。
- 框架仅 `AssemblyInfo.cs` +1 行 `InternalsVisibleTo("CardGame.Tests.EditMode")`；asmdef 引用接线。
- 新增 `Tests/EditMode/Bootstrap/SceneInjectionTests.cs`（8 测试）。
- 文档：di-internals §13.3 驱动现状、README 场景驱动段、feat-007 规格（2026-09-06）登记决策表、feature_list 同步。

### 早前（feat-003 执行，2026-09-02）

- 删除根目录 Lifecycle/（8）/Boot/（5）/Input/（3）；verify.mjs 的模块边界遍历替换为三类旧源码缺席检查。
- 新增 `CardGame.Runtime/Bootstrap/`：Scopes.cs（RunScope/EncounterScope 标记）、GameComposition.cs（组合根）、
  GameFlow.cs（占位流程服务）、GameBootstrap.cs（场景入口）；`GameDataCatalog` 删除（数据访问收敛为容器解析）；
  CardGame.Runtime.asmdef 与测试 asmdef 增加 RazorFramework.DI 引用。
- 新增 `Tests/EditMode/Bootstrap/GameCompositionTests.cs`（4 测试）；SampleContentTests 提取 LoadSampleAssets。
- Bootstrap 场景：挂载 GameBootstrap + 绑定 8 个数据资产（临时 executeMethod 脚本已删除）。
- 文档：03/05 转退役存档、06 转方向记录、01-di/07-cardgame 吸收决策论证、design README / CONTRACT /
  DESIGN-REVIEW / HARNESS / README / AGENTS 同步；决策规格填入决策表与执行记录。

## 最新验证证据

| 检查 | 结果 |
|---|---|
| Unity batchmode EditMode（--full） | **通过**：124/124，harness 29 项 0 失败 0 警告 |
| 便携 Harness（bun） | 通过：28 项，1 个预期便携模式警告（本机无 node） |
| Harness 测试套件 | 通过：45/45 |
| git diff --check | 干净 |

## 已知限制

- IL2CPP/AOT 未验证；玩法本体未实现；GameFlow 为占位（新局/遭遇作用域将由未来流程状态机驱动）；
  输入包装类按需生成（06-input.md）；Excel→JSON 转换器未开始。
- Unity 重存场景可能再次产生 `m_Name: ` 行尾空格，提交前 `git diff --check` 并修正新增行。

## 下一可执行步

1. 与用户确定下一个功能：战斗引擎最小切片（决策规格 §1.1）/ Excel→JSON 转换器（开放问题 #3）/ UI spike（开放问题 #1）。
2. 顺带评估 feat-004（Unity Test Host）：其目标已由 feat-005/006 实质满足，建议降级为文档性结论或关闭。
3. `docs/design/di-internals.html`（未跟踪）不属于任何功能，确认其用途后决定保留/删除。
