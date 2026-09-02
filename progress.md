# 项目进度记录

## 当前状态

**最后更新：** 2026-09-02 +08:00
**当前功能：** feat-006 CardGame Data Foundation 进行中（Unity 验证已全绿，剩 .meta 提交与关闭）；feat-003 的 MVVM 子项已推进。
**状态：** feat-003 转 blocked（Lifecycle/Boot 简化待拍板，09 开放问题 #8）；feat-006 代码/样例/测试/文档齐备，Unity EditMode 112 测试全部通过。

## 本次完成：设计文档配套 UML 图（2026-09-02）

按用户确认的范围（方案 A：Mermaid 内嵌 + 1/2/4 三块），为三份设计文档新增 5 张 Mermaid 图（每张上方有斜体“图示辅助”说明）：

- `01-di.md`：构建期校验与冻结流程（flowchart）、层级作用域解析路径（sequenceDiagram）、确定性释放顺序与共享不变量（flowchart）。
- `07-cardgame.md`：新增「数据管线工作原理」节 + feat-006 加载/校验/冻结全流程图（flowchart）。
- `02-events.md`：发布/订阅快照分发时序图（sequenceDiagram）。
- 验证：5 张图均经 mermaid.ink 实际渲染校验成功（无语法错误）；便携 Harness 28 项 0 失败。

## 本次完成：feat-003 的 MVVM 退役（2026-09-02）

按 09 开放问题 #6 定案（选项 B：不建 MVVM 程序集，旧源码删除）：

- 删除根目录 `MVVM/` 全部 5 个旧源码文件（BindingManager/IBinding/RelayCommand/AsyncCommand/ViewModelBase）。
- Harness：`checkCSharpBoundaries` 的 modules map 移除 MVVM；新增 `checkLegacyRootMvvmAbsent`（仿 Events/DI 的 legacy-absent 检查），要求根 `MVVM/` 不再有任何 C#。
- 文档同步：README、docs/design/README、07-cardgame、CONTRACT、legacy-framework-audit 移除 MVVM 引用；04-mvvm.md 新增「保留模式：INPC 基类（留待后续按需使用）」节，留存约 30 行的 `ObservableObject`/`SetProperty` 形态供面板层未来按需实现。
- feature_list.json：feat-003 首条 doneCriteria 标注 MVVM 移除完成。

## 本次完成：feat-006 Unity 验证与 3 处根因修复（2026-09-02）

首跑 Unity batchmode EditMode 失败 4 个测试，逐一定位根因并修复：

- **SampleContent 2 个测试（加载失败）**：`GameDataLoader.Load(IReadOnlyList<TextAsset>)` 用 `asset.name` 作字典键，但 Unity 的 `TextAsset.name` 不含 `.json` 扩展名（如 `cards`），与约定文件名（`cards.json`）不匹配 → 16 处“缺失必需文件 + 未知数据文件”。修复：键规范化补回 `.json`。
- **GameDataLoaderTests.MultipleBrokenFiles（错误数不符）**：JsonUtility 在 JSON 缺失 `condition` 键时仍会实例化嵌套类（type 为 null/空），`if (dto.condition != null)` 误判 → 幻影 `ConditionEntry` 与“未知枚举值 ""”误报（用 -executeMethod 探针确认第 3 个错误来源）。修复：`DtoMapper.MapEffects` 以 `condition.type` 非空判定真实条件。
- **JsonSerializationTests.MissingOptionalFields（断言错误）**：JsonUtility 实际语义为缺失字符串 → null、缺失 List → 空列表（非 null）、缺失嵌套类 → 实例化；原测试对 `effects` 断言 `Is.Null` 错误。修复：断言对齐真实语义，并修正 `Dtos.cs` 顶部过时的语义注释。

另记录：`MapLayer.nodeDistribution` 存在同类幻影实例化，但全零分布与“无分布”语义等价且不产生错误，未改（最小改动）。

## 验证证据

| 检查 | 命令 | 实际结果 |
|---|---|---|
| Unity EditMode（batchmode） | `UNITY_EDITOR=... verify.mjs --full` | **通过**：112 测试全部通过（XML total=112 passed=112 failed=0），harness 29 项检查 0 失败 0 警告 |
| 便携 Harness | `bun scripts/harness/verify.mjs` | 通过：28 项检查 0 失败 1 个预期便携模式警告（本机无 node，用 bun 运行） |
| Harness 测试套件 | `bun test scripts/harness/tests/` | 通过：45/45 |
| git diff --check | 经 harness 内置检查 | 干净 |

## 已知限制与后续边界

- 全部新增资产（asmdef/脚本/JSON）的 .meta 尚未生成：编辑器刷新/下次打开后生成，需提交。
- IL2CPP/AOT 未验证；玩法本体未实现；Excel → JSON 转换器未开始（协议先行）。

## 下一步

1. 提交：feat-006 修复（4 个文件）+ MVVM 删除（5 个文件）+ 文档/harness 同步 + 生成的 3 个目录 .meta（Content/Runtime/Tests.EditMode.Data）与行尾差异（EditorBuildSettings.asset 仅 LF/CRLF，建议一并规范化提交）。
2. 证据齐后关闭 feat-006，更新 feature_list。
3. feat-003 剩余依赖：Lifecycle/Boot 简化待拍板（09 开放问题 #8）；下一个功能候选：Excel/Markdown → JSON 转换器（09 开放问题 #3 落点待拍板）。
