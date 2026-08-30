# 设计文档（权威参考）

> **地位：** 本目录是除源码外**唯一**的权威设计参考。按模块分文件、树状描述，定期维护。
> **触发更新的变更：** 程序集边界、模块职责、依赖方向、生命周期语义、公共 API 契约。
> **与状态文档分工：** 本目录回答"设计是什么"；`progress.md`、`session-handoff.md`、`feature_list.json` 回答"进展到哪"。
> 每个功能的 spec/plan 落地后，其架构结论必须吸收进对应模块文件。
>
> **最后更新：** 2026-08-30 · 当前功能：feat-003 in-progress（Events 已迁移；MVVM 已退役；
> Lifecycle/Boot 简化待定案；Input 迁 CardGame.Runtime）

## 文件索引

图例：✅ 已迁入编译域 · ⏳ 迁移中 · 🧭 候选方案（决策依据，未定案） · 🚫 已退役

```text
docs/design/
├─ README.md          总览：原则、依赖图、顶层结构、共享限制、维护规则（本文件）
├─ 01-di.md           DI 核心 + Unity 注入适配器              ✅
├─ 02-events.md       强类型事件总线                            ✅
├─ 03-lifecycle.md    生命周期引擎                              ⏳
├─ 04-mvvm.md         MVVM 与绑定（已退役，历史存档）            🚫
├─ 05-boot.md         启动编排与 Installer                       ⏳
├─ 06-input.md        玩家输入                                  ⏳
├─ 07-cardgame.md     游戏层与数据契约                          ✅
├─ 08-verification.md 验证体系（Harness + EditMode）            ✅
└─ 09-ui-resources.md UI/资源/数据管线方案（资源定案 Addressables）  🧭
```

## 架构原则

1. **纯 C# 核心 / Unity 适配分层** — 框架核心（DI、Events、Lifecycle、MVVM）不含 `UnityEngine` 编译引用，由 asmdef `noEngineReferences: true` 强制；Unity 行为收敛到 `Unity.*` 适配程序集。
2. **单一依赖方向** — `DI → Events → Lifecycle → MVVM → Unity.* → Boot → CardGame.Runtime`；框架不引用游戏，游戏玩法规则不进入框架。
3. **构建期验证优先** — 依赖图、作用域树、生命周期穿透在容器构建期校验并失败，而非运行期。
4. **机器验证边界** — Node Harness（便携 + 完整 Unity EditMode）递归拒绝越界代码，守住每一条边界。

## 程序集与依赖图

```text
Unity 编译域 — 现状
├─ RazorFramework.DI              ✅  纯 C# (noEngineReferences)  [BCL only]
├─ RazorFramework.Events         ✅  纯 C# (noEngineReferences)  [BCL only]
├─ RazorFramework.Unity.DI       ✅  [Unity, refs DI]  autoReferenced: false
├─ RazorFramework.DI.Tests       ✅  [refs DI]  Editor only
├─ RazorFramework.Unity.DI.Tests ✅  [refs DI, Unity.DI]  Editor only
├─ RazorFramework.Events.Tests   ✅  [refs Events]  Editor only
└─ CardGame.Tests.EditMode       ✅  Editor only（项目身份 + Bootstrap 构建场景）

feat-003 目标 — 剩余（2026-08-30 更新：MVVM 两程序集退役；Lifecycle/Boot 简化建议见 09-ui-resources.md，待定案）
├─ RazorFramework.Lifecycle            ⏳  纯 C#：生命周期接口 + LifecycleEngine
├─ RazorFramework.Unity.Lifecycle      ⏳  [Unity, refs DI/Lifecycle/Unity.DI]  StrictLifecycleMonoBehaviour + UpdateRunner
├─ RazorFramework.Unity.Boot           ⏳  [Unity, refs 全部框架]  Installer + ProjectContext + SceneScopeRunner
└─ CardGame.Runtime                    ⏳  [Unity, refs 框架]  游戏输入 + 游戏启动细节
```

**依赖规则（Harness 强制）：**

- 纯 C# asmdef：`noEngineReferences: true`，`references` 只指向更底层的纯 C# asmdef。
- Unity asmdef 只被 Unity 层与 CardGame 引用；框架永不引用 CardGame。
- 根目录 `Boot/`、`Lifecycle/`、`MVVM/`、`Input/` 下的任何 C# 被 Harness 递归拒绝进入 Unity 编译（根 `Events/` 已删除）。
- 测试程序集 `includePlatforms: [Editor]`、`autoReferenced: false`。

## 仓库顶层结构

```text
CardGame/
├─ Assets/                          ← Unity 编译域（唯一）
│  ├─ CardGame/                     游戏资产：Scenes/、Settings/、Tests/
│  └─ Plugins/RazorFramework/       框架：DI/、Events/、Unity/DI/、Tests/
├─ Boot/  Lifecycle/  MVVM/  Input/ 旧框架源码（非编译域，feat-003 迁移输入）
├─ docs/                            本目录 + CONTRACT + HARNESS + superpowers/
├─ scripts/harness/                 可重复验证
├─ AGENTS.md                        协作工作流（开始工作前必读）
├─ README.md                        目录职责与 DI V2 使用方式
├─ DESIGN-REVIEW.md                 架构审查结论与已知限制
├─ feature_list.json / progress.md / session-handoff.md   功能状态追踪
└─ .agents/skills/                  项目本地 Superpowers 技能库
```

## 全局风险与限制

各模块文件含模块级限制；以下为跨模块风险：

1. **IL2CPP / 托管代码剥离 / AOT 未验证** — Unity 注入依赖反射；目标平台发布前需 `link.xml`/保留策略与平台构建验证，或改为生成式注入。
2. **Unity 注入器不是主线程调度器** — 依赖 Unity 初始化流程捕获主线程，任何非主线程接触 fail-closed。
3. **feat-003 剩余模块属重写** — 旧 Boot/Lifecycle/MVVM/Input 引用已删除的 DI V1 API，不能机械迁移；迁移顺序自底向上：Lifecycle → MVVM → Boot → Input。
4. **场景运行时流程未验证** — 当前证据仅覆盖 Editor 编译与 EditMode 行为，不证明玩法运行时、输入、存档、网络。
5. **Input 层为占位** — `PlayerInput.cs` 是 InputSystem 生成类占位，需用真实 `.inputactions` 生成版本替换后才可用。

## 与其他文档的关系

| 文档 | 定位 | 与本目录关系 |
|---|---|---|
| **docs/design/（本目录）** | 权威架构设计，按模块分文件 | — |
| README.md | 目录职责与框架使用方式（代码示例） | 使用视角 |
| AGENTS.md | 协作工作流（开始前必读清单） | 流程视角 |
| DESIGN-REVIEW.md | 架构审查结论与已知限制 | 审查快照，结论应收敛进本目录 |
| docs/CONTRACT.md | 游戏数据契约（策划/程序接口） | 数据层契约，见 07-cardgame.md |
| docs/HARNESS.md | 验证流程维护指南 | 验证操作细节，见 08-verification.md |
| docs/legacy-framework-audit.md | 旧框架历史风险快照（feat-003 重构输入） | 历史记录，迁移完成后归档 |
| docs/superpowers/specs/、plans/ | 各功能的临时设计规格与实施计划 | 一次性产物；落地后结论吸收进本目录 |
| feature_list.json、progress.md、session-handoff.md | 功能状态与进展 | 状态追踪，非设计 |

## 维护规则

- 修改某模块 → 同步该模块文件（结构树、API 契约、测试覆盖）。
- 程序集边界/依赖方向变更 → 更新本索引的依赖图 + 涉及模块文件。
- spec/plan 落地 → 结论吸收进对应模块文件，更新状态标记（✅/⏳）。
- 模块新增测试行为 → 更新该模块文件的"测试覆盖"节。
- 提交前核对：模块状态标记与 `feature_list.json` 一致。
