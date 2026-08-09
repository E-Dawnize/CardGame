# 会话交接

## 当前状态

- `feat-002 Pure C# DI Boundary` 已完成；当前没有 `in-progress` 功能。
- 工作分支：`codex/di-v2`；DI V2 实现、Harness 门禁和中文维护文档均在该分支。
- CardGame 唯一 DI 实现在 `Assets/Plugins/RazorFramework/DI/`；旧根目录 `DI/DIContainer.cs` 已删除。
- Unity 注入适配器位于 `Assets/Plugins/RazorFramework/Unity/DI/`；它绑定 Unity 初始化阶段捕获的主线程，未初始化或错误线程会 fail-closed。

## 已完成内容

- `ContainerBuilder` 在 `Build()` 后冻结注册并验证依赖图；容器支持 Singleton、层级 Scoped、Transient、集合解析、可选解析、诊断和确定性释放。
- `RazorFramework.DI` 通过程序集设置和 Harness 同时保证为纯 C#；`RazorFramework.Unity.DI` 负责 Unity 对象的成员注入及结构化错误。
- 生命周期与作用域错误保留间接 captive、兄弟 scope、集合冲突和 runtime mismatch 的类型路径。
- Harness 会递归拒绝根 `DI/` 的任何 C#，验证 DI V2 命名空间/程序集/Unity 引用、中文设计工件质量，以及真实 Unity EditMode 结果。
- README、`DESIGN-REVIEW.md` 和 `docs/HARNESS.md` 已同步；损坏的规格/计划已重建，旧框架风险恢复到 `docs/legacy-framework-audit.md`。

## 最新验证证据（2026-08-10）

| 检查 | 结果 |
|---|---|
| `node --test scripts/harness/tests/*.test.mjs` | 45/45 通过，0 失败 |
| `node scripts/harness/verify.mjs` | 25 passed、1 个预期 warning、0 failure；警告仅为便携模式未启动 Unity |
| 设置 `UNITY_EDITOR='C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'` 后运行 `node scripts/harness/verify.mjs --full` | 26 passed、0 warning、0 failure；新鲜 EditMode XML 为 67 passed、0 failed |
| `git diff --check 01ade590338cd683785fb3604d9fb22270b6c266` | 当前完整净差异无 whitespace 报告；提交后按 `01ade59..HEAD` 再确认 |

## 已知限制

- 当前 Unity 注入基于反射；IL2CPP、托管代码剥离和 AOT 兼容性尚未验证。发布前必须加入并验证 `link.xml`/保留策略和目标平台构建，或采用生成式/显式注入。
- 主线程守卫不替代 Unity 主线程调度；在 `BeforeSceneLoad` 初始化尚未发生的生产环境中，构造注入器会有意失败。
- 本分支未实现卡牌战斗、伤害结算、地图、叙事节点、存档、文案流程或数值系统。

## 下一可执行步

为 `feat-003 Assembly Definition Boundaries` 执行 **brainstorming**。先读取 `docs/legacy-framework-audit.md`，再以当前真实源码产出已批准的程序集依赖边界与迁移策略；不要假定其已经开始，也不要未经规格批准迁入根目录旧模块。
