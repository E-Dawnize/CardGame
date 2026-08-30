# UI / 资源 / 数据管线 — 方案与定案记录

> 状态：🧭 候选方案 + 部分定案
> 创建：2026-08-30 · 更新：2026-08-30（按协作模型校正：外部协作者只管配置表/文案，代码单人）
> **协作模型（2026-08-30 定）**：人数很少；外部协作者只碰配置表（Excel）与文案
>   （Markdown/散表），不碰代码与 UI；代码侧单人开发
>   原则：代码侧不留兼容性包袱（旧框架迁移、框架复用性一律让位于真实需要）；
>   协作设施只在数据管线保留并简化（协作者无需 Unity 即可转换/校验）
> **已定案**：资源走 Addressables；数据来源 Excel → 运行时数据（JSON 或编译格式）；
>   MVVM 框架级不实施（选项 B：不建 MVVM 程序集，INPC 基类按需进 CardGame.Runtime）；
>   UI 单一栈 = UI Toolkit 全栈（战斗层 spike 先行，失败才回退战斗层 uGUI）
> **待定**：spike 结果；数据中间格式 JSON vs 编译格式；Lifecycle/Boot 简化方案（见文末）
> 参考：杀戮尖塔（Slay the Spire）——同类型标杆
> 决策落地后：吸收进 04-mvvm / 05-boot / 07-cardgame 对应模块文件，并同步 CONTRACT.md

## 现状盘点（与本主题相关的既有决策）

```text
已定案（约束本方案的输入）
├─ 架构分层    纯 C# 核心 + Unity 适配 + CardGame.Runtime（单一依赖方向）
├─ MVVM 计划   RazorFramework.MVVM（ViewModelBase/Commands/IBinding）+ Unity.MVVM（BindingManager）
│             已知限制：无 PropertyPath，仅单属性通知；BindingManager 锁保护 + 全量列表
├─ Events      struct 事件总线（语义事件用）
├─ Lifecycle   UpdateRunner 快照逐帧 Tick + 错误隔离；StrictLifecycleMonoBehaviour
├─ Boot        旧实现用 Addressables（label="BootConfig"）+ 加载遮罩
├─ 数据契约    Id 交叉引用（UpgradeToId/ChoiceChain/RandomPoolId）+ IconKey 间接层
│             + 策划 Excel/Markdown → 导入脚本 → SO（本条按本文件更新重定向）
└─ DI 作用域   RunScope / EncounterScope / BattleContext 已在 README 示例中

环境事实
├─ manifest.json：无 com.unity.addressables、无 com.unity.textmeshpro；
│   有 uGUI 2.0、UI Toolkit（uielements 模块）、InputSystem、URP 17.3
├─ 中文文本渲染：CJK 字体资产方案未定
└─ 资源定案 Addressables → 需新增 com.unity.addressables 包（独立 feat 处理）
```

## 杀戮尖塔参考

```text
技术事实
├─ 原作 Java + libGDX：UI 全部代码构建（scene2d Table 布局），无场景文件/预制体概念
├─ 数据全 JSON；资源全打包进 jar，启动后全量常驻内存（资源总量数百 MB）
├─ 每屏一个类（ScreenManager 状态机切换）；卡牌视图 AbstractCard 代码绘制 + 池化
├─ 悬停卡预览 HoverCard 全局复用；怪物 Spriter（.scml）、角色 Spine
└─ 移植版沿用 libgdx；续作 StS2 弃 Unity 换 Godot（Triple-I 发布会宣布）

启示
├─ "全量常驻 + 同步访问"在资源量小的卡牌游戏可行，且天然规避战斗中途加载卡顿
├─ 屏幕状态机映射本项目 DI Scope（战斗屏 = EncounterScope 创建/销毁）
├─ 卡牌视图池化 + 变化驱动刷新，是低端设备流畅的核心原因
└─ 不照搬：代码布局（Unity 有 prefab/UXML 布局）、每帧全量 render（Unity 应变化驱动）
```

## 一、UI 架构方案（样式表 + 动效视角）

需求定义：样式表（换肤/主题集中管理，策划或程序改一处全局生效）+ 动效（卡牌拖拽/悬停放大/
屏幕过渡/伤害数字飘动等战斗级动画）。

### 各方案样式表与动效能力（已核实）

| 方案 | 样式表 | 动效能力 | 数据绑定 | 单一方案结论 |
|---|---|---|---|---|
| uGUI | 无内建（自建主题系统成本高） | **强**：DOTween/PrimeTween 成熟，Animator、粒子叠加、世界空间无缝 | 自研 MVVM（04-mvvm 计划） | 动效无忧，样式表缺位 |
| UI Toolkit | **USS 内建**（CSS 类、变量、主题切换） | 中：USS transitions（**无 keyframes**）；官方推荐只对 scale/translate/rotate 过渡，布局属性过渡每帧重算（性能警告）；PrimeTween 支持 UITK 但社区有 translate 抖动报告；无内建关键帧系统 | **内建 BindingPath/数据绑定** | 面板层可行；战斗级动效有风险 |
| NoesisGUI | **XAML Style（真样式系统）** | **Storyboard 关键帧动画（真动效）** | **XAML 数据绑定 + MVVM** | **唯一真单一方案，但商用授权、集成重** |
| 混合 D | USS（面板层） | 两栈各用最强（uGUI 战斗 / UITK 面板） | 两栈各自内建 | —（两个栈） |

### 结论与推荐（2026-08-30 按"个人项目、单一栈"重定）

**定案：单一栈 = UI Toolkit 全栈（UXML/USS + 官方 runtime binding + PrimeTween 补动效），
战斗层 spike 先行。** 个人项目不维持双栈（无美术并行、无历史 uGUI 资产包袱）；
样式表需求是全局的（战斗 UI 的主题/配色同样想换肤），单一栈收益最大。

```text
路线（定案）：全 UI Toolkit + PrimeTween
   采纳条件 = 战斗层 spike 通过：30 张手牌 + 拖拽 + 悬停预览 + 伤害数字 + 10 个
   状态图标的 60fps 实测 + translate 抖动复现检查
   若 spike 失败 → 回退：战斗层 uGUI（PrimeTween 同样支持 uGUI，无需换动效库）
   NoesisGUI 维持否决（商用授权，个人项目性价比差）
```

```text
单一栈的连带简化
├─ 文本：TextCore 全包（卡牌/面板/数字）→ 不需要 TMP 包、不需要 uGUI 文本组件
├─ 绑定：UITK 官方 runtime binding 全包 → 自研绑定彻底出局（见 MVVM 评估）
├─ 动效：PrimeTween 一个库打满（UITK + 2D 场景元素）
├─ 字体：单一 CJK 字体资产，TextCore 动态图集
└─ spike 检查清单：拖拽手感（pointer capture）/ 悬停放大（scale 过渡）/
   伤害数字飘动（translate 过渡或 PrimeTween）/ 60fps / 布局属性过渡禁用
```

### MVVM 必要性评估（定案：选项 B，框架级不实施）

按层核对 MVVM 全套（ViewModelBase/Commands/IBinding/BindingManager/IValueConverter）的实际需求：

```text
面板层（UI Toolkit）
├─ UITK 官方 runtime binding 直接观察任意实现 INotifyPropertyChanged 的 C# 对象
│   （data-source/data-source-path、双向绑定、列表绑定）——绑定引擎 Unity 已内置，
│   自研 IBinding/BindingManager/IValueConverter 与官方功能完全重叠，没有存在理由
├─ 价值残留：一个 INPC 基类（SetProperty 值变才通知）作为 UITK 数据源基类（30 行级）；
│   按钮防重入可选（AsyncCommand，或代码后置自行处理）
└─ 命名 Command 注册表（View/VM 字符串耦合的弱约定）无消费者

战斗层（uGUI 代码视图）
├─ StS 模式视图直接读 CardState，不走绑定器
└─ 测试性来自纯 C# 战斗逻辑（效果引擎/AI/规则），不来自 ViewModel —— MVVM 的主要
   收益（可测 ViewModel）在本层已由"纯 C# 核心 + 状态对象"替代获得

旧 BindingManager 自身问题（即便有消费者也不值得原样迁移）
├─ 锁保护 + LINQ 全量遍历 + 无 PropertyPath，为低频场景设计却带高频成本
└─ 迁移成本：旧代码 + 接口 + EditMode 测试 + 一套 Harness 边界检查
```

选项对比：

| 选项 | 形态 | 评价 |
|---|---|---|
| **B（定案）** | 完全不建 MVVM 程序集，INPC 基类按需写进 CardGame.Runtime | 个人项目无框架复用包袱，"已规划的 asmdef 边界"是兼容性包袱而非理由；30 行基类随用随写 |
| A | `RazorFramework.MVVM` 保留 asmdef，只含 ViewModelBase + Commands | 为"框架完整性"付出无消费者的边界成本 —— 已否决 |
| C | 按原计划全量迁移 | 功能与官方重叠、无消费者、成本最高 —— 已否决 |

```text
定案 B 的附带效应
├─ feat-003 Task 4（MVVM 迁移）整体取消：根目录旧 MVVM/ 源码直接删除（不入编译域）
├─ 04-mvvm.md 改为退役存档（状态 🚫）；README feat-003 依赖图移除两个 MVVM 程序集
├─ INPC 基类（SetProperty 值变才通知）在第一个 UITK 面板需要时写进 CardGame.Runtime，
│   约 30 行，不设框架边界
└─ 若未来出现第二个消费者，直接吃 UITK 官方绑定，不复活自研
```

## 二、数据管线方案（Excel → 运行时数据）

需求：外部协作者的 Excel/Markdown/散表是唯一内容源；产物是运行时直接加载的数据
（可读 JSON 或编译格式），替代"Excel → SO"。

```text
协作约束（按协作模型）
├─ 协作者改 Excel/文案后，应能在本地跑一条命令完成"转换 + 校验"，无需 Unity 环境
├─ 转换失败要给出可读错误（哪张表哪一行哪个字段），而不是堆栈
└─ 生成的 JSON 可 git diff 审查，协作者能自查改动效果
```

### 方案对比

| 维度 | X1 Excel → JSON（推荐） | X2 Excel → 编译格式（MessagePack/.bytes） | X3 Excel → SO（现状） |
|---|---|---|---|
| git diff 可读性 | **优**（数值审查、merge 友好） | 差（需保留 JSON 中间产物） | 差（二进制） |
| 启动加载 | 快（JsonUtility 反序列化数 MB ≈ 毫秒级） | **更快**（量级差在数据量大时才显现） | 中（SO 反序列化 + 引用解析） |
| AOT/IL2CPP | **优**（JsonUtility 内置、零依赖、AOT 安全） | 需序列化器 + AOT codegen 成本 | 优 |
| Addressables 接入 | **优**（每个类别一个 JSON 条目） | 优 | 差（每个 SO 一个条目，百级资产清单） |
| 导入步骤 | Harness/CI 自动转换，无手动步骤 | 同左 + 编译步 | 程序手动跑 Editor 导入 |
| 策划协作 | 生成的 JSON 可自查 | 靠中间 JSON | 依赖程序跑导入 |
| 约束 | JsonUtility 不支持 Dictionary/多态 → 磁盘格式用数组，启动时构建字典 | 序列化器选型 | Inspector 调试方便（唯一优势） |

### 推荐：X1（Excel → JSON → Addressables 预载 → 启动字典）

```text
管线形态
├─ 源：docs/card-design.xlsx、relic-design.xlsx、dialogue/*.md（与 CONTRACT 相同）
├─ 转换器：Node 脚本（SheetJS 解析 .xlsx）+ Markdown 解析器 → scripts/data/ 下
│   （进便携 Harness 管线：协作者/程序都能本地跑，无需 Unity；校验产物与源同步）
├─ 产物：Assets/CardGame/Content/Data/ 下 cards.json / relics.json / enemies.json /
│   events.json / status.json / world.json / dialogue/*.json —— 提交仓库（可 diff 审查）
├─ 运行时：ResourceRegistry 预载全部 JSON → JsonUtility 反序列化 → Id → 定义字典
│   （JsonUtility 磁盘格式用数组：{ "cards": [ {...}, ... ] }，启动构建 Dictionary）
├─ 校验（Harness 便携门禁）：
│   JSON 与 Excel 同步（无过期产物）、schema 字段合法、Id 唯一、
│   UpgradeToId/ChoiceChain/RandomPoolId 交叉引用完整、IconKey 有对应资源
└─ Description/PreviewText 自动生成逻辑：移到转换器（Node）或运行时，二选一
   （建议运行时：数值被遗物/状态修正时文案仍正确——但若只依赖静态字段则放转换器更省）
```

```text
X2 的触发条件（暂不做）：卡牌/文本量到数千条、多语言全量、加载时间实测超预算时，
JSON 保留为中间产物，追加 MessagePack 编译步。X3 与"运行时数据"诉求不符，淘汰。
```

### 对 CONTRACT.md 的影响

```text
├─ 字段契约（§1-§10 的字段/类型/约束）不变 —— 它现在成为 JSON 的 schema 规范
├─ §11 解析器职责：输出从 DialogueSequence SO 改为 dialogue JSON
├─ §12 协作流程：删除"程序跑导入脚本 → SO 刷新"手动步骤，
│   改为策划 push Excel → Harness/CI 转换校验（或策划本地跑一条命令）
├─ §13 仓库文件结构：Assets/Resources/{Cards,Relics,...} 改为
│   Assets/CardGame/Content/Data/*.json；Editor 导入脚本（CardImporter 等）
│   被 scripts/data/ 的 Node 转换器替代
└─ 决策落地后统一修改，避免契约与实现漂移
```

## 三、资源管理（已定案：Addressables）

### 分组设计

```text
Addressables 分组（本地构建组起步，不配远端 URL）
├─ SharedUI      面板 UXML/USS、图标/UI 图集、字体资产 —— 启动预载，常驻
├─ ContentData   生成的 JSON（cards/relics/…）+ 对话 JSON —— 进入跑局预载
├─ CardArt       卡框/图标/状态/遗物图集 —— 按层预载或进入战斗前预载
├─ Audio         BGM（按屏预载）/ SFX（按组）
├─ Scenes        Addressables 场景加载（Bootstrap 之外的玩法场景）
└─ 升级空间：将来热更/DLC/移动端包体控制 → 该组加远端 URL 即可，代码不动
```

### 关键设计：ResourceRegistry

```text
ResourceRegistry（CardGame.Runtime，Singleton）
├─ 启动/跑局预载：全部 ContentData → Id → 定义字典（战斗/叙事零查找开销、零异步）
├─ IconKey → Sprite 字典（目录约定扫描或资产清单 SO）
├─ 同步访问面：GetCard(id) / GetSprite(iconKey) —— 全游戏唯一取资源入口
└─ 构建期校验（Harness）：IconKey 缺失、Id 交叉引用悬空、Addressable 组漏配
```

### 配套决策

```text
├─ 数据资产物理路径：Assets/CardGame/Content/（JSON、图集、字体、音频），
│   不放 Assets/Resources/（Resources 语义与 Addressables 互斥，双份打包风险）
├─ 卡面策略：部件拼装（卡框/图标/底板进图集 + 文本动态渲染），不整卡贴图
│   （整卡 512×768 RGBA ≈ 1.5MB/张，60 张 ≈ 90MB；部件拼装全卡组 < 10MB；
│   策划改描述/数值无需重出卡面图）
├─ 图集：卡框、图标、状态、遗物各一张 SpriteAtlas（同图集切换不触发 Canvas 重建）
├─ 字体：单一 CJK 字体资产全游戏共用（动态图集模式）
├─ 文本组件：全走 UI Toolkit TextCore（单一栈）；不引入 TMP
│   （若战斗层回退 uGUI 再评估 TMP）
├─ 屏幕切换：遮罩下预载下一屏资源，预载完成才切屏（沿用 Boot 加载遮罩模式）
└─ 战斗中零异步：战斗所需资源全部在遮罩内预载完毕
```

## 四、代码绑定方案（代码 ↔ UI ↔ 数据）

### 4.1 代码 ↔ UI 元素

```text
面板层（UI Toolkit）：官方 runtime binding（data-source/data-source-path，观察
   INotifyPropertyChanged 数据源）为主，代码后置做命令逻辑；
   不用自研 BindingManager（见「MVVM 必要性评估」节）。
战斗部件：若全 UITK（spike 通过）→ UXML 模板 + VisualElement 代码视图 + 元素池化
   （池化策略与 prefab 同理：元素不销毁，Rebind 数据）；
   若回退 uGUI → 模板 prefab + 代码视图（CardView 组件序列化字段拖一次，池化 Rebind）。
明确禁止：运行时字符串查找、每个实例手拖引用。
生成式绑定（编辑器生成强类型代理）作为 prefab 数量上来后的中期可选，不进第一版。
```

### 4.2 代码 ↔ 数据

```text
Id + 注册表（CONTRACT 的 Id 交叉引用已决定此方向）：
  预载全部 JSON → Dictionary<string, CardDef>；运行时只按 Id 取，永不查路径。
CardDef（不可变定义）与 CardState（运行时状态：费用/升级/临时属性）分离：
  视图读 CardState 渲染动态部分，引用 CardDef 渲染静态部分；
  状态对象可池化、可序列化（存档）。
```

### 4.3 数据 ↔ 资源

```text
IconKey 间接层（CONTRACT 已定，保留）：ResourceRegistry 解析 key → Sprite；
策划改 key 不需知道资源在哪；换美术风格只改注册表；缺失 key 进 Harness 门禁。
```

## 五、性能专项

```text
1. 对象池      卡视图 / 敌人视图 / 伤害数字 / 状态图标 / 飘字全池化；Rebind 而非重建
2. 变化驱动 + 帧末合并
               Events 只发语义事件（卡牌打出/回合开始），不发值事件（能量变化）；
               数值变化 → 脏标记 → UpdateRunner Tick 末尾统一刷新一轮
3. Canvas 分层  静态/动态/高频小部件层分离；高频改 text/color 的部件（费用、伤害数字）
               隔离到独立小 Canvas；transform 动画不触发重建
4. UITK 陷阱   面板动画只用 scale/translate/rotate/opacity 过渡（transform 类属性）；
               避开布局属性过渡（每帧布局重算）；ListView 用虚拟化而非全量实例
5. 图集       同图集 sprite 切换不重建 Canvas mesh；避免跨图集交替（合批中断）
6. 文本       静态文本（卡名/描述）只在 SetData 时写一次；高频数字只刷变化的实例
7. 屏幕切换    遮罩下预载 + Scope 创建/销毁（战斗屏 = EncounterScope）；
               ScreenManager 状态机与 Lifecycle 引擎顺序对齐
8. 数据层      CardState 池化；CardDef 常驻只读；敌人 AI/意图纯 C#（EditMode 可测）
```

## 推荐组合（一页纸）

```text
UI      单一栈：UI Toolkit 全栈（UXML/USS + runtime binding + PrimeTween），
        spike 先行验证战斗层；失败回退 = 战斗层 uGUI；NoesisGUI 否决
数据    方案 X1：Excel/Markdown → Node 转换器（进 Harness）→ JSON（提交，可 diff）
        → Addressables 预载 → ResourceRegistry 字典；X2 编译格式作为量级升级项
资源    Addressables 本地组 + 按屏预载 + 战斗中零异步；图集化卡面部件；
        单一 CJK 字体；文本全走 TextCore（不需要 TMP）
绑定    UITK 官方 runtime binding + INPC 轻基类（CardGame.Runtime，随用随写）；
        Id 注册表（数据）+ IconKey（资源）；校验进 Harness；MVVM 框架不建
```

## 对既有模块计划的连带建议（按"代码单人、去兼容性包袱"原则，待确认）

```text
Boot（05-boot.md）
├─ Installer 由 SO 资产改为纯 C# 代码：InstallerAsset/InstallerConfig/BootConfig
│   label 流程删除 —— 协作者只碰配置表/文案，从不组合启动流程；代码单人，
│   SO Installer 无消费者
├─ ProjectContext 精简版（容器构建 + 启动顺序 + 场景 Scope + 加载遮罩 + 预载）
│   直接进 CardGame.Runtime；不建 RazorFramework.Unity.Boot 程序集
│   （与 D5"游戏启动细节外迁游戏侧"方向一致，只是干脆全部外迁）
└─ SceneScopeRunner 简化为场景入口组件创建 Scope

Lifecycle（03-lifecycle.md）
├─ 两阶段引擎（IInitializable/IStartable + pending 补跑）与 StrictLifecycleMonoBehaviour
│   不迁移 —— 单人项目用 DI 构造顺序 + MonoBehaviour 原生回调足够，旧源码删除
├─ 唯一保留候选：最小 UpdateRunner（约 50 行，快照 tick）进 CardGame.Runtime，
│   服务"战斗 UI 脏标记帧末刷新"的真实需求；无此需求则一并删除
└─ ScopeProvider 不需要：DI V2 已支持作用域层级，场景 Scope 由场景组件创建

CONTRACT（协作流程 §12 / 文件结构 §13）—— 协作保留，管线更新
├─ 协作流程保留并简化：协作者改 Excel/Markdown/ui-strings.json → 提交 →
│   本地跑转换命令（Node，无需 Unity）→ 生成的 JSON 提交（可 diff 自查）
├─ 程序侧职责：维护转换器与校验；合并协作者提交后跑一次 Harness 即可放行
├─ "Assets/Scripts/Editor/ 导入脚本"职责由 scripts/data/ 的 Node 转换器接管
└─ 其余字段契约不变（仍是 JSON 的 schema 规范）

feat-003 净效应
├─ Task 4（MVVM）取消；Lifecycle/Boot 从"迁移"变"删除旧源码 + CardGame.Runtime 按需实现"
├─ 最终程序集图候选：DI ✅ / Events ✅ / Unity.DI ✅ / CardGame.Runtime（新建）
└─ 上述 Boot/Lifecycle/CONTRACT 三项确认后，同步 03/05/07 模块文档与 feature_list.json
```

## 开放问题（待用户决策）

| # | 问题 | 影响 | 建议 |
|---|---|---|---|
| 1 | 战斗层 spike 结果（全 UITK 可行性） | 单一栈定案成立与否 | spike 通过 = 全 UITK；失败 = 战斗层回退 uGUI |
| 2 | 数据中间格式：JSON（X1）还是直接上编译格式（X2） | 启动加载/体积 | 默认 X1，量大再 X2 |
| 3 | 转换器落点：Node + SheetJS（新增 npm 依赖）vs Unity Editor + EPPlus | 协作者是否需 Unity 才能校验 | Node（协作者无需 Unity；Harness 可校验同步） |
| 4 | 生成的 JSON 是否提交仓库 | diff 审查能力 | 提交 |
| 5 | Description 自动生成放转换器还是运行时 | 数值修正时文案正确性 | 运行时 |
| 6 | ~~MVVM 处置~~ 已定案：选项 B（不建 MVVM 程序集，旧源码删除） | 04-mvvm.md 已退役 + feat-003 Task 4 取消 | — |
| 7 | 新增包清单：addressables（+ npm 依赖 SheetJS） | feat-003 之外的独立 feat | 一次功能点内处理 |
| 8 | Boot/Lifecycle 简化方案（见上节） | feat-003 剩余任务形态 | 确认后同步 03/05 文档与 feature_list |
```

## 参考资料

```text
Unity 官方手册
├─ USS 属性参考（过渡属性清单与 animatable 分类）：
│   https://docs.unity3d.com/Manual/UIE-USS-Properties-Reference.html
├─ USS transitions（过渡语法与用法）：
│   https://docs.unity3d.com/Manual/UIE-Transitions.html
├─ UI Toolkit 运行时绑定入门（data-source/data-source-path）：
│   https://docs.unity3d.com/Manual/UIE-get-started-runtime-binding.html
├─ 运行时绑定多属性：
│   https://docs.unity3d.com/Manual/UIE-bind-to-multiple-properties-with-runtime-binding.html
├─ 绑定系统对比（binding-path vs runtime binding）：
│   https://docs.unity3d.com/Manual/UIE-comparison-binding.html
├─ Unity Learn：UI Toolkit 数据绑定实战：
│   https://learn.unity.com/tutorial/ui-toolkit-in-unity-6-crafting-custom-controls-with-data-bindings
├─ JsonUtility（JSON 序列化）：
│   https://docs.unity3d.com/Manual/JSONSerialization.html
└─ Addressables 包手册：
    https://docs.unity3d.com/Packages/com.unity.addressables@2.5/manual/index.html

动效与第三方库
├─ PrimeTween（uGUI/UITK 补间库，UI Toolkit 支持自 1.0.7）：
│   https://github.com/shujunqiao/PrimeTween/blob/main/changelog.md
├─ UI Toolkit: Tween Engine（Asset Store，为 USS 增加 @keyframes 语法扩展）：
│   https://assetstore.unity.com/packages/tools/utilities/ui-toolkit-tween-engine-365906
├─ VisualElement style.translate 补间抖动社区报告（UITK 动效风险证据）：
│   https://discussions.unity.com/t/something-as-basic-as-translating-visualelement-in-tween-is-not-smooth-and-rather-vibrating-etc/1703142/3
├─ DOTween（uGUI 战斗层动效候选）：
│   http://dotween.demigiant.com/
├─ NoesisGUI（XAML 样式 + Storyboard 动画 + 绑定一体，商用单一方案）：
│   https://www.noesisgui.com/
└─ MessagePack-CSharp（X2 编译格式候选）：
    https://github.com/MessagePack-CSharp/MessagePack-CSharp

数据管线
└─ SheetJS（Node 解析 .xlsx，转换器依赖候选）：
    https://www.npmjs.com/package/xlsx

杀戮尖塔
├─ 引擎与开发事实（libGDX）：https://en.wikipedia.org/wiki/Slay_the_Spire
└─ StS2 弃 Unity 换 Godot（Triple-I 发布会宣布）：
    https://80.lv/articles/slay-the-spire-2-is-sticking-with-godot-leaving-unity-behind
```
