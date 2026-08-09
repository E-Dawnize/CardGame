# 项目进度记录

## 当前状态

**最后更新：** 2026-08-10 +08:00
**当前功能：** 无；feat-002 Pure C# DI Boundary 已完成。
**状态：** DI V2 已成为 CardGame 唯一 DI 实现，完整 Unity EditMode 证据已更新。

## 本次完成：feat-002 Pure C# DI Boundary

- 以 `Assets/Plugins/RazorFramework/DI/` 中的 `RazorFramework.DI` 替换旧 `DI/DIContainer.cs`；核心程序集不引用 Unity，构建后注册表不可变。
- 支持 Singleton、层级 Scoped、Transient、确定性逆序释放、`TryResolve`、`ResolveAll<T>` 和不影响容器行为的诊断接收器。
- 在 `Assets/Plugins/RazorFramework/Unity/DI/` 增加 Unity 对象注入适配器。终审后改为由 Unity `SubsystemRegistration`/`BeforeSceneLoad` 捕获可信主线程；未初始化、工作线程构造或工作线程调用均 fail-closed，并在接触 Unity null 语义前返回 `WrongThread`。
- 生命周期验证会传播作用域要求与依赖路径；间接 singleton captive、默认/集合兄弟 scope 冲突和运行时缺少 scope 都提供精确 `DependencyPath`。
- Harness 递归拒绝根 `DI/` 下任意 C#，识别合法 trivia 分隔的 Unity 限定名，固定 DI asmdef 字段，并检测中文规格/计划的替换问号损坏。
- 两份损坏的中文设计工件已根据真实实现重建；旧 RazorFramework 风险清单恢复到 `docs/legacy-framework-audit.md`，等待 feat-003 逐项复核。
- README、架构审查和 Harness 指南均已同步可信主线程、唯一实现、结构化路径与运行限制。

## 终审修复验证证据（2026-08-10）

| 检查 | 命令 | 实际结果 |
|---|---|---|
| Node 契约测试 | `node --test scripts/harness/tests/*.test.mjs` | 45/45 通过，0 失败 |
| 便携 Harness | `node scripts/harness/verify.mjs` | 25 passed、1 个预期 warning、0 failure；警告仅表示未运行 Unity |
| Unity 完整 Harness | `$env:UNITY_EDITOR='C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'; node scripts/harness/verify.mjs --full` | 26 passed、0 warning、0 failure |
| Unity EditMode XML | 完整 Harness 新生成的结果 XML | 67 passed、0 failed |
| 分支差异检查 | `git diff --check 01ade590338cd683785fb3604d9fb22270b6c266` | 包含当前工作树的净差异无 whitespace 报告；提交后还需按 `01ade59..HEAD` 复核 |

## 已知限制与后续边界

- `UnityObjectInjector` 依赖 Unity runtime initialization callback 捕获主线程，不提供工作线程调度；在回调尚未发生的环境会有意 fail-closed。
- Unity 反射注入只在 Editor EditMode 验证。IL2CPP、linker stripping 和 AOT 仍未证明；发布前必须增加并验证 `link.xml`/保留策略与目标平台构建，或切换至生成式/显式注入。
- feat-003 Assembly Definition Boundaries 尚未开始。根目录 `Boot`、`Events`、`Input`、`Lifecycle`、`MVVM` 尚未进入 Unity 编译范围，后续迁入必须独立设计依赖方向。
- 卡牌战斗、伤害结算、地图、叙事节点、存档、文案协作规则与数值模拟均尚未实现，不能从 DI V2 完成状态推断其已具备。

## 下一步

1. 对 feat-003 Assembly Definition Boundaries 执行 brainstorming：以 `docs/legacy-framework-audit.md` 为历史输入，重新检查真实模块引用图、允许依赖方向、测试程序集引用和迁移顺序；不要在尚未批准规格前修改程序集边界或玩法代码。
