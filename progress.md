# 项目进度记录

## 当前状态

**最后更新：** 2026-08-09 +08:00
**当前功能：** 无；feat-002 Pure C# DI Boundary 已完成。
**状态：** DI V2 已成为 CardGame 唯一 DI 实现，完整 Unity EditMode 证据已更新。

## 本次完成：feat-002 Pure C# DI Boundary

- 以 `Assets/Plugins/RazorFramework/DI/` 中的 `RazorFramework.DI` 替换旧 `DI/DIContainer.cs`；核心程序集不引用 Unity，构建后注册表不可变。
- 支持 Singleton、层级 Scoped、Transient、确定性逆序释放、`TryResolve`、`ResolveAll<T>` 和不影响容器行为的诊断接收器。
- 在 `Assets/Plugins/RazorFramework/Unity/DI/` 增加 Unity 对象注入适配器。它在创建线程上执行，安全处理 Unity 已销毁对象，并为成员定义和依赖失败提供结构化异常。
- Harness 已将“旧 DI 不存在”和“DI V2 纯 C# 边界”设为通过条件；旧 DI 的 Unity 空值判断警告已删除。
- README、架构审查和 Harness 指南均已更新为中文，并记录运行限制与下一阶段。

## 最终验证证据（2026-08-09）

| 检查 | 命令 | 实际结果 |
|---|---|---|
| Node 契约测试 | `node --test scripts/harness/tests/*.test.mjs` | 36/36 通过，0 失败 |
| 便携 Harness | `node scripts/harness/verify.mjs` | 24 passed、1 个预期 warning、0 failure；警告仅表示未运行 Unity |
| Unity 完整 Harness | `$env:UNITY_EDITOR='C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'; node scripts/harness/verify.mjs --full` | 25 passed、0 warning、0 failure |
| Unity EditMode XML | 完整 Harness 新生成的结果 XML | 65 passed、0 failed |
| 差异检查 | `git diff --check` | 无输出，退出码 0 |

## 已知限制与后续边界

- `UnityObjectInjector` 的 owner thread 是其构造时线程；调用方必须在 Unity 线程构造并调用，适配器不提供工作线程调度。
- Unity 反射注入只在 Editor EditMode 验证。IL2CPP、linker stripping 和 AOT 仍未证明；发布前必须增加并验证 `link.xml`/保留策略与目标平台构建，或切换至生成式/显式注入。
- feat-003 Assembly Definition Boundaries 尚未开始。根目录 `Boot`、`Events`、`Input`、`Lifecycle`、`MVVM` 尚未进入 Unity 编译范围，后续迁入必须独立设计依赖方向。
- 卡牌战斗、伤害结算、地图、叙事节点、存档、文案协作规则与数值模拟均尚未实现，不能从 DI V2 完成状态推断其已具备。

## 下一步

1. 对 feat-003 Assembly Definition Boundaries 执行 brainstorming：先确定模块图、允许依赖方向、测试程序集引用与迁移顺序；不要在尚未批准规格前修改程序集边界或玩法代码。
