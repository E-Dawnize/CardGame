# CardGame — 游戏层与数据契约

> 位置：`Assets/CardGame/` · 状态：✅ 骨架已就位（feat-005）；数据基础已落地（feat-006 in-progress），玩法未开始
> 目标运行时程序集：`CardGame.Runtime`（Unity，refs 框架，feat-003 Task 5 建立）

## 结构

```text
Assets/CardGame/
├─ Scenes/Bootstrap.unity          启动场景（构建列表唯一场景）
├─ Runtime/
│  ├─ CardGame.Domain/    纯 C# 数据模型/DTO 映射/校验/文案生成（noEngineReferences，feat-006）
│  └─ CardGame.Runtime/   Unity 侧 JsonUtility 加载 + GameDataCatalog（refs Domain，feat-006）
├─ Content/Data/          配置数据 JSON（cards/relics/enemies/status/events/dialogues/world/ui-strings）
├─ Settings/
│  ├─ UniversalRP.asset / Renderer2D.asset / DefaultVolumeProfile.asset
│  │  UniversalRenderPipelineGlobalSettings.asset    URP 管线配置
│  ├─ InputSystem_Actions.inputactions               输入资产（生成 PlayerInput 的输入源）
│  └─ Lit2DSceneTemplate.scenetemplate / URP2DSceneTemplate.unity   场景模板
└─ Tests/EditMode/（CardGame.Tests.EditMode，Editor only）
   ├─ ProjectFoundationTests   项目身份 + Bootstrap 构建场景配置
   └─ Data/                    协议测试（枚举/映射/序列化/校验/文案/加载/样例自证）
```

## 数据管线工作原理

*图示：feat-006 数据管线的加载与校验流程（图示辅助，细节以正文与 [CONTRACT.md](../CONTRACT.md) 为准）*

```mermaid
flowchart TD
    TA["TextAsset × 8<br/>cards / relics / enemies / status / events / dialogues / world / ui-strings.json<br/>（asset.name 不含 .json 扩展名）"]
    TA --> L["GameDataLoader.Load(assets)<br/>键规范化：补回 .json 与固定文件名约定匹配"]
    L --> LOOP{"遍历 RequiredFiles"}
    LOOP -->|"文件缺失"| E1["错误汇：缺失必需文件"]
    LOOP -->|"JsonUtility → DTO 失败 / 空"| E2["错误汇：JSON 解析失败 / 结构不符"]
    LOOP -->|"解析成功"| MAP["DtoMapper：DTO → 类型化模型<br/>未知枚举 → 错误 + 回退值继续映射<br/>condition.type 为空 → 视为无条件"]
    MAP -->|"映射问题"| E3["错误汇：未知枚举值 / null 条目等"]
    L --> UNK{"存在非约定文件名？"}
    UNK -->|"是"| E4["错误汇：未知数据文件"]
    E1 --> AGG{"错误汇非空？"}
    E2 --> AGG
    E3 --> AGG
    E4 --> AGG
    AGG -->|"是（聚合全部错误）"| T1["DataValidationException<br/>一次性抛出完整报告"]
    AGG -->|"否"| B["DataRepositoryBuilder.Build"]
    B --> W{"world 缺失或无法解析？"}
    W -->|"是"| T2["错误并入 DataValidationException 抛出"]
    W -->|"否"| V["ContentValidator.ValidateAll<br/>id 格式 / cost 范围 / damage 与 block 互斥<br/>status / relic / enemy / speaker / faction / boss 交叉引用<br/>对话 index 边界"]
    V -->|"有错误"| T2
    V -->|"全部通过"| D["DescriptionBuilder<br/>FillCardDescriptions / FillIntentPreviews"]
    D --> R["DataRepository（构建后冻结）<br/>id 索引 Get / TryGet：cards / relics / enemies / statuses / events / dialogues<br/>+ World + UiStrings"]
    R --> CAT["GameDataCatalog<br/>（MonoBehaviour 引导入口，留 Addressables 换装点）"]
```

## 数据契约（docs/CONTRACT.md）

> 程序与协作者的共享接口：协作者用 Excel/Markdown 填内容，程序写规则引擎与转换器；
> 数据管线已定案为 Excel → JSON（2026-08-30，见 [09-ui-resources.md](09-ui-resources.md) 数据管线节）。

```text
数据契约
├─ 卡牌系统      CardDef
├─ 效果原语      EffectEntry
├─ 遗物系统      RelicDef
├─ 敌人系统      EnemyDef
├─ 状态系统      StatusDef
├─ 对话系统      DialogueSequence
├─ 叙事事件系统  EventDef
├─ 世界观骨架    WorldDef
├─ 地图层配置    MapLayerDef
├─ UI 文本散表   UIStringsJson
├─ 对话剧本格式  Markdown
├─ 协作流程
└─ 仓库文件结构
```

字段、类型与约束以 `docs/CONTRACT.md` 为唯一契约（现作为 JSON 的 schema 规范）；字段变更必须同步该文档。
存储形态由 ScriptableObject 改为 JSON（2026-08-30 定案，见 [09-ui-resources.md](09-ui-resources.md)）；CONTRACT 的协作流程与文件结构节随数据管线定案更新。

## 关键决策与不变量

- 游戏玩法规则不得进入 RazorFramework（框架只服务通用机制：DI/Events/Lifecycle/Boot）。
- 新增玩法脚本、资源、场景与配置只放在 `Assets/CardGame/` 或明确的子目录。
- 框架与游戏的边界（输入、启动细节）由 feat-003 D5 确定：游戏专属细节一律在 `CardGame.Runtime`。

## 已知限制

- 无玩法实现：卡牌、战斗、地图、叙事、存档、文案均未开始（配置表数据协议已由 feat-006 落地）。
- 场景运行时流程未验证（仅项目身份与构建配置有 EditMode 覆盖）。
- Excel → JSON 转换器未开始（协议先行，转换器为下一功能）。
