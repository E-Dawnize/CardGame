# CardGame — 叙事肉鸽卡牌游戏

CardGame 是一个基于 Unity 的叙事肉鸽卡牌项目。它以分支地图、卡牌战斗和可重复挑战为骨架，并让主线事件、特殊节点和碎片文本持续推进故事。仓库是可直接打开的 Unity `6000.3.10f1` 项目宿主。

## 目录职责

| 位置 | 当前职责 | 维护边界 |
|---|---|---|
| `Assets/CardGame/` | CardGame 的场景、项目资源和游戏代码 | 新增玩法脚本、资源、场景与配置只放在这里或明确的子目录 |
| `Assets/Plugins/RazorFramework/DI/` | CardGame 唯一的 DI V2 纯 C# 核心 | `RazorFramework.DI` 必须保持 `noEngineReferences: true`，不得引用 `UnityEngine` |
| `Assets/Plugins/RazorFramework/Unity/DI/` | Unity 对象成员注入适配器 | 只能依赖 DI 核心和 Unity；不把 Unity 规则倒灌进核心 |
| `Assets/Plugins/RazorFramework/Tests/EditMode/` | DI V2 与 Unity 适配器的 EditMode 测试 | 新行为先由测试表达，再修改实现 |
| `Packages/`、`ProjectSettings/` | Unity 版本、包版本、项目身份与构建场景 | 改动后必须运行 Harness；不记录本机临时路径 |
| 根目录 `Boot/`、`Events/`、`Input/`、`Lifecycle/`、`MVVM/` | 尚未迁入的旧 RazorFramework 源码 | 不进入当前 Unity 编译范围，也不添加 CardGame 玩法代码 |
| `scripts/harness/` 与状态文档 | 可重复验证、功能状态和协作交接 | 结构契约变更必须同时更新测试与说明 |

## DI V2 使用方式

`RazorFramework.DI` 是 CardGame 当前唯一的 DI 实现；已移除旧根目录 `DI/DIContainer.cs`。容器通过 `ContainerBuilder` 注册服务并在 `Build()` 后冻结。构建阶段会验证重复注册、构造函数可选性、缺失依赖、依赖环、作用域定义和生命周期穿透，因此不应在运行中修改注册表。

```csharp
var builder = new ContainerBuilder();
builder.DefineScope<RunScope>();
builder.DefineScope<EncounterScope, RunScope>();
builder.AddSingleton<IClock, GameClock>();
builder.AddScoped<RunState, RunScope>();
builder.AddScoped<BattleContext, EncounterScope>();
builder.AddTransient<DamagePreviewService>();

using var container = builder.Build();
using var run = container.CreateScope<RunScope>();
using var encounter = run.CreateScope<EncounterScope>();
var preview = encounter.Resolve<DamagePreviewService>();
```

- `Singleton` 由根容器持有；`Scoped` 由声明的作用域持有；`Transient` 由创建它的容器或作用域持有并以逆序释放。
- `CreateScope<TScope>()` 只允许按已定义的父子关系创建；子作用域可以使用祖先作用域服务，反向访问会在构建时或解析时以结构化异常失败。
- `Resolve<T>()` 用于必需依赖；`TryResolve(Type, out object)` 用于可选依赖；`ResolveAll<T>()` 返回该服务的集合注册，保持注册顺序。`IServiceResolver` 只应注入框架边界或适配器，业务构造函数优先声明具体依赖。
- 可通过 `ContainerOptions.DiagnosticSink` 观察构建、作用域、实例创建、解析失败和释放事件；诊断回调的异常不会改变容器行为。

### Unity 对象成员注入

`RazorFramework.Unity.DI.UnityObjectInjector` 是唯一允许接触 `UnityEngine.Object` 的 DI 层。它支持实例字段和有 setter 的非索引属性：`[Inject]` 表示必需依赖，`[InjectOptional]` 表示未注册时跳过。成员计划按基类到派生类、同一类型内元数据顺序执行；错误会以 `UnityInjectionException` 给出错误码、目标类型、成员名和服务类型。

```csharp
var injector = new UnityObjectInjector(encounter);
injector.Inject(component);
```

注入器必须在它**创建时所在的线程**调用；它首先检查该 owner thread，随后才检查 Unity 对象是否为 `null`（包括已销毁对象）。不要从工作线程创建或调用注入器，也不要把它当作跨线程调度器。

反射成员注入目前只在 Editor EditMode 中得到验证。IL2CPP、托管代码剥离和 AOT 构建覆盖尚未证明；上线前必须为反射访问提供 `link.xml`/保留策略并进行目标平台构建验证，或改为生成式/显式注入方案。

## 验证

便携验证不启动 Unity，会检查 Harness 状态、Unity 宿主结构、旧 DI 删除情况和 DI V2 的纯 C# 边界：

```powershell
node scripts/harness/verify.mjs
```

完整验证会在便携门禁通过后运行实际 Unity EditMode 测试：

```powershell
$env:UNITY_EDITOR='C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'
node scripts/harness/verify.mjs --full
```

POSIX shell 或 Git Bash：

```bash
export UNITY_EDITOR='/path/to/Unity'
node scripts/harness/verify.mjs --full
```

完整模式默认把当前仓库作为 Unity 项目路径，`UNITY_EDITOR` 是唯一必需的环境变量。只有在验证另一个兼容宿主时，才设置可选的 `RAZOR_UNITY_PROJECT`。详细工作流见 [Harness 维护指南](docs/HARNESS.md)。开始实质工作前，请阅读 [AGENTS.md](AGENTS.md)、`feature_list.json`、`progress.md` 与 `session-handoff.md`。

## 版本控制边界

提交源资产、`.meta`、`Packages/`、`ProjectSettings/` 与维护脚本；不要提交 Unity 生成物：`Library/`、`Temp/`、`Logs/`、`UserSettings/`、`.vs/`、`.sln`、`.slnx` 和 `.csproj`。这些本机文件不是可复现的项目输入。
