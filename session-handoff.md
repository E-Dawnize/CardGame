# 会话交接

## 当前状态

- `feat-002 Pure C# DI Boundary` 已完成；当前没有 `in-progress` 功能。
- 工作分支：`codex/di-v2`；DI V2 实现、Harness 门禁和中文维护文档均在该分支。
- CardGame 唯一 DI 实现在 `Assets/Plugins/RazorFramework/DI/`；旧根目录 `DI/DIContainer.cs` 已删除。
- Unity 注入适配器位于 `Assets/Plugins/RazorFramework/Unity/DI/`，必须在其创建时所在的 owner thread 使用。

## 已完成内容

- `ContainerBuilder` 在 `Build()` 后冻结注册并验证依赖图；容器支持 Singleton、层级 Scoped、Transient、集合解析、可选解析、诊断和确定性释放。
- `RazorFramework.DI` 通过程序集设置和 Harness 同时保证为纯 C#；`RazorFramework.Unity.DI` 负责 Unity 对象的成员注入及结构化错误。
- Harness 会验证旧 DI 已删除、DI V2 命名空间/程序集/Unity 引用边界，以及真实 Unity EditMode 结果。
- README、`DESIGN-REVIEW.md` 和 `docs/HARNESS.md` 已中文化并记录 API 使用方式与维护限制。

## 最新验证证据（2026-08-09）

| 检查 | 结果 |
|---|---|
| `node --test scripts/harness/tests/*.test.mjs` | 36/36 通过，0 失败 |
| `node scripts/harness/verify.mjs` | 24 passed、1 个预期 warning、0 failure；警告仅为便携模式未启动 Unity |
| 设置 `UNITY_EDITOR='C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'` 后运行 `node scripts/harness/verify.mjs --full` | 25 passed、0 warning、0 failure；新鲜 EditMode XML 为 65 passed、0 failed |
| `git diff --check` | 干净，退出码 0 |

## 已知限制

- 当前 Unity 注入基于反射；IL2CPP、托管代码剥离和 AOT 兼容性尚未验证。发布前必须加入并验证 `link.xml`/保留策略和目标平台构建，或采用生成式/显式注入。
- owner-thread 检查只保证在注入器构造线程调用；它不替代 Unity 主线程调度。
- 本分支未实现卡牌战斗、伤害结算、地图、叙事节点、存档、文案流程或数值系统。

## 下一可执行步

为 `feat-003 Assembly Definition Boundaries` 执行 **brainstorming**，先产出已批准的程序集依赖边界与迁移策略；不要假定其已经开始，也不要未经规格批准迁入根目录旧模块。
