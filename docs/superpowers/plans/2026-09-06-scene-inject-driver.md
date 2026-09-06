# 场景 [Inject] 成员注入驱动 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让场景中容器外 MonoBehaviour 的 `[Inject]`/`[InjectOptional]` 成员在启动期真正被注入（根驱动 `SceneInjection.InjectScene` + `GameBootstrap` 接线 + scope 接缝测试背书）。

**Architecture:** 框架零行为改动（仅 +1 行测试可见性）。新增游戏侧驱动 `SceneInjection`（扫描场景全部 MonoBehaviour 含 inactive，逐个 `UnityObjectInjector.Inject`）；`GameBootstrap` 加 `[DefaultExecutionOrder(-32000)]` 保证注入先于其他脚本 Awake。对齐法则：对象生命周期 ≤ 注入 resolver 生命周期；scope 拥有的屏由未来流程状态机从 scope 注入（本切片以测试背书该接缝）。

**Tech Stack:** Unity 6000.3.10f1 · RazorFramework.DI / RazorFramework.Unity.DI（纯 C# 核心 + Unity 适配层）· NUnit EditMode · bun + `scripts/harness/verify.mjs`

**规格：** `docs/superpowers/specs/2026-09-06-scene-inject-driver-design.md`（决策与对齐法则的唯一权威）

## Global Constraints

- Unity 版本固定 `6000.3.10f1`；验证命令 `bun scripts/harness/verify.mjs`（完整：设 `UNITY_EDITOR` 后 `--full`）。
- RazorFramework.DI / Events / Unity.DI 的公开 API 与行为零变化；`Assets/Plugins/RazorFramework/Unity/DI/` 只允许改 `AssemblyInfo.cs`。
- `CardGame.Runtime` 不建视图级 DI 图（视图不进注册表）；注入错误一律启动期 fail-fast 暴露，禁止静默降级。
- 工作区有无关未提交改动（`docs/design/di-internals.md` 图示扩充、未跟踪 `docs/design/di-internals.html`）——**所有 git add 只用显式文件路径，绝不 `git add -A` / `git add .`**。
- 新代码注释与文档用简体中文；类型/命令/路径保持英文。
- EditMode 测试的 `[SetUp]` 必须先调 `UnityMainThread.InitializeForTests()`（internal，靠 Task 1 的 InternalsVisibleTo 可见）。
- Unity 编辑器若正打开本项目，batchmode 会因项目锁失败——`--full` 前需确认编辑器已关闭，或改用编辑器 Test Runner 手动跑，并在状态文档记录实际方式。

---

### Task 1: 测试装配线（asmdef 引用 + InternalsVisibleTo + 冒烟测试）

**Files:**
- Modify: `Assets/Plugins/RazorFramework/Unity/DI/AssemblyInfo.cs`
- Modify: `Assets/CardGame/Runtime/CardGame.Runtime/CardGame.Runtime.asmdef`
- Modify: `Assets/CardGame/Tests/EditMode/CardGame.Tests.EditMode.asmdef`
- Create: `Assets/CardGame/Tests/EditMode/Bootstrap/SceneInjectionTests.cs`

**Interfaces:**
- Consumes: `RazorFramework.Unity.DI` 现有公开面（`UnityObjectInjector(IServiceResolver)`、`Inject(UnityEngine.Object)`、`InjectAttribute`/`InjectOptionalAttribute`）与 internal `UnityMainThread.InitializeForTests()`（本任务解锁可见性）。
- Produces: 测试类型 `ISceneService`/`SceneService`/`InjectableComponent`（后续任务复用）；测试文件骨架与 `[SetUp]` 模式；游戏测试程序集可驱动注入器这一事实。

- [ ] **Step 1: 写失败的冒烟测试（先建文件，编译会因缺引用失败）**

```csharp
using System;
using NUnit.Framework;
using RazorFramework.DI;
using RazorFramework.Unity.DI;
using UnityEngine;

namespace CardGame.Tests.EditMode.Bootstrap
{
    /// <summary>场景 [Inject] 注入驱动测试（规格：docs/superpowers/specs/2026-09-06-scene-inject-driver-design.md）。</summary>
    public sealed class SceneInjectionTests
    {
        private interface ISceneService { }

        private sealed class SceneService : ISceneService { }

        private interface INeverRegistered { }

        private sealed class InjectableComponent : MonoBehaviour
        {
            [Inject] public ISceneService Service;
            [InjectOptional] public INeverRegistered Optional;
        }

        [SetUp]
        public void SetUp()
        {
            UnityMainThread.InitializeForTests();
        }

        [Test]
        public void Injector_FromGameTestAssembly_InjectsComponent()
        {
            var builder = new ContainerBuilder();
            builder.AddSingleton<ISceneService, SceneService>();
            using var container = builder.Build();
            var gameObject = new GameObject("scene-injection-smoke");
            var component = gameObject.AddComponent<InjectableComponent>();
            try
            {
                new UnityObjectInjector(container).Inject(component);

                Assert.That(component.Service, Is.SameAs(container.Resolve<ISceneService>()));
                Assert.That(component.Optional, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
```

- [ ] **Step 2: 跑验证确认失败（编译错误：测试 asmdef 缺 Unity.DI 引用）**

Run: `bun scripts/harness/verify.mjs`
Expected: 失败或编译不可达（测试程序集解析不到 `RazorFramework.Unity.DI`）；若便携模式不编译 Unity，记录此点，靠 Step 4 的 `--full` 确认。

- [ ] **Step 3: 接装配线（三处最小改动）**

`Assets/Plugins/RazorFramework/Unity/DI/AssemblyInfo.cs` 全文替换为：

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("RazorFramework.Unity.DI.Tests")]
[assembly: InternalsVisibleTo("CardGame.Tests.EditMode")]
```

`Assets/CardGame/Runtime/CardGame.Runtime/CardGame.Runtime.asmdef` 的 `"references"` 数组改为：

```json
  "references": [
    "CardGame.Domain",
    "RazorFramework.DI",
    "RazorFramework.Unity.DI"
  ],
```

`Assets/CardGame/Tests/EditMode/CardGame.Tests.EditMode.asmdef` 的 `"references"` 数组改为：

```json
  "references": [
    "CardGame.Domain",
    "CardGame.Runtime",
    "RazorFramework.DI",
    "RazorFramework.Unity.DI"
  ],
```

- [ ] **Step 4: 跑验证确认通过（Unity 编译 + 冒烟测试绿）**

Run: `bun scripts/harness/verify.mjs`（快速门禁）与 `UNITY_EDITOR='C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe' bun scripts/harness/verify.mjs --full`（EditMode 全量；编辑器开着则先关，或用 Test Runner 手动跑并记录）
Expected: 便携通过；`--full` 全绿且测试数 = 原 116 + 1 = 117。

- [ ] **Step 5: Commit**

```bash
git add "Assets/Plugins/RazorFramework/Unity/DI/AssemblyInfo.cs" \
        "Assets/CardGame/Runtime/CardGame.Runtime/CardGame.Runtime.asmdef" \
        "Assets/CardGame/Tests/EditMode/CardGame.Tests.EditMode.asmdef" \
        "Assets/CardGame/Tests/EditMode/Bootstrap/SceneInjectionTests.cs" \
        "Assets/CardGame/Tests/EditMode/Bootstrap/SceneInjectionTests.cs.meta"
git commit -m "feat: wire game test assembly into Unity member injection seam"
```

（`.meta` 由 Unity 生成；若 `--full` 用 batchmode 已产出 meta，一并提交；若没有 meta 文件则删掉该行。）

---

### Task 2: SceneInjection 根驱动实现（场景扫描注入）

**Files:**
- Create: `Assets/CardGame/Runtime/CardGame.Runtime/Bootstrap/SceneInjection.cs`
- Test: `Assets/CardGame/Tests/EditMode/Bootstrap/SceneInjectionTests.cs`（追加）

**Interfaces:**
- Consumes: Task 1 的测试类型与装配线；`UnityObjectInjector(IServiceResolver).Inject(UnityEngine.Object)`；`Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)`。
- Produces: `public static void SceneInjection.InjectScene(IServiceResolver resolver)`（`CardGame.Runtime` 命名空间）——Task 3 的 GameBootstrap 调用它；测试类型 `RunState`/`MissingRequiredComponent`/`ScopedWantingComponent` 供 Task 3 复用。

- [ ] **Step 1: 写失败的测试（追加到 SceneInjectionTests.cs；5 个）**

在文件顶部 `using` 区追加：`using CardGame.Runtime;`（RunScope 所在）——注意 Task 3 才加 GameBootstrap 断言，本步先不加多余 using。在类型体内（`InjectableComponent` 之后）追加：

```csharp
        private sealed class RunState { }

        private sealed class MissingRequiredComponent : MonoBehaviour
        {
            [Inject] public ISceneService Service;
        }

        private sealed class ScopedWantingComponent : MonoBehaviour
        {
            [Inject] public RunState State;
        }
```

在测试类尾部追加（`RunScope` 在 `CardGame.Runtime`，需 `using CardGame.Runtime;`）：

```csharp
        [Test]
        public void InjectScene_InjectsActiveComponentFromSceneScan()
        {
            var builder = new ContainerBuilder();
            builder.AddSingleton<ISceneService, SceneService>();
            using var container = builder.Build();
            var gameObject = new GameObject("scene-injection-active");
            gameObject.AddComponent<InjectableComponent>();
            try
            {
                SceneInjection.InjectScene(container);

                var component = gameObject.GetComponent<InjectableComponent>();
                Assert.That(component.Service, Is.SameAs(container.Resolve<ISceneService>()));
                Assert.That(component.Optional, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void InjectScene_InjectsInactiveComponent()
        {
            var builder = new ContainerBuilder();
            builder.AddSingleton<ISceneService, SceneService>();
            using var container = builder.Build();
            var gameObject = new GameObject("scene-injection-inactive");
            gameObject.SetActive(false);
            gameObject.AddComponent<InjectableComponent>();
            try
            {
                SceneInjection.InjectScene(container);

                var component = gameObject.GetComponent<InjectableComponent>();
                Assert.That(component.Service, Is.SameAs(container.Resolve<ISceneService>()));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void InjectScene_ThrowsMissingDependencyWhenRequiredServiceNotRegistered()
        {
            var builder = new ContainerBuilder();
            using var container = builder.Build();
            var gameObject = new GameObject("scene-injection-missing");
            gameObject.AddComponent<MissingRequiredComponent>();
            try
            {
                var exception = Assert.Throws<UnityInjectionException>(
                    () => SceneInjection.InjectScene(container));

                Assert.That(exception.Code, Is.EqualTo(UnityInjectionErrorCode.MissingDependency));
                Assert.That(exception.TargetType, Is.EqualTo(typeof(MissingRequiredComponent)));
                Assert.That(exception.MemberName, Is.EqualTo("Service"));
                Assert.That(exception.ServiceType, Is.EqualTo(typeof(ISceneService)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void InjectScene_ThrowsScopeMismatchWhenSceneComponentWantsScopedService()
        {
            var builder = new ContainerBuilder();
            builder.DefineScope<RunScope>();
            builder.AddScoped<RunState, RunScope>();
            using var container = builder.Build();
            var gameObject = new GameObject("scene-injection-scoped");
            gameObject.AddComponent<ScopedWantingComponent>();
            try
            {
                var exception = Assert.Throws<DependencyInjectionException>(
                    () => SceneInjection.InjectScene(container));

                Assert.That(exception.Code, Is.EqualTo(DependencyErrorCode.ScopeMismatch));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void InjectScene_LeavesOptionalMemberUnsetWhenNotRegistered()
        {
            var builder = new ContainerBuilder();
            builder.AddSingleton<ISceneService, SceneService>();
            using var container = builder.Build();
            var gameObject = new GameObject("scene-injection-optional");
            gameObject.AddComponent<InjectableComponent>();
            try
            {
                SceneInjection.InjectScene(container);

                var component = gameObject.GetComponent<InjectableComponent>();
                Assert.That(component.Optional, Is.Null);
                Assert.That(component.Service, Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
```

- [ ] **Step 2: 跑验证确认失败**

Run: `bun scripts/harness/verify.mjs`（+可行时 `--full`）
Expected: 编译失败——`CardGame.Runtime` 找不到 `SceneInjection`（T4 的 `MissingRequiredComponent` 断言等实现后才有意义；编译失败即失败证据）。

- [ ] **Step 3: 最小实现**

创建 `Assets/CardGame/Runtime/CardGame.Runtime/Bootstrap/SceneInjection.cs`：

```csharp
using RazorFramework.DI;
using RazorFramework.Unity.DI;
using UnityEngine;

namespace CardGame.Runtime
{
    /// <summary>
    /// 场景 [Inject] 成员注入驱动（规格：docs/superpowers/specs/2026-09-06-scene-inject-driver-design.md）。
    /// 对齐法则：对象的生命周期必须 ≤ 它被注入的 resolver 的生命周期——
    /// 常驻场景对象只能由根容器注入（仅 Singleton；注入 Scoped 服务会抛 ScopeMismatch）；
    /// scope 拥有的屏/对象由创建该 scope 的驱动方从 scope 注入，且必须先于 scope 销毁。
    /// 运行时动态创建的对象不在本驱动范围：谁创建谁注入（拿 UnityObjectInjector 显式调用）。
    /// </summary>
    public static class SceneInjection
    {
        /// <summary>扫描当前场景（含 inactive）全部 MonoBehaviour，注入 [Inject]/[InjectOptional] 成员；必需依赖缺失原样抛出。</summary>
        public static void InjectScene(IServiceResolver resolver)
        {
            var injector = new UnityObjectInjector(resolver);
            var behaviours = Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var behaviour in behaviours)
            {
                injector.Inject(behaviour);
            }
        }
    }
}
```

- [ ] **Step 4: 跑验证确认通过**

Run: `bun scripts/harness/verify.mjs`（+ `--full`）
Expected: `--full` 全绿，测试数 = 117 + 5 = 122。

- [ ] **Step 5: Commit**

```bash
git add "Assets/CardGame/Runtime/CardGame.Runtime/Bootstrap/SceneInjection.cs" \
        "Assets/CardGame/Runtime/CardGame.Runtime/Bootstrap/SceneInjection.cs.meta" \
        "Assets/CardGame/Tests/EditMode/Bootstrap/SceneInjectionTests.cs"
git commit -m "feat: add SceneInjection root driver for scene member injection"
```

---

### Task 3: GameBootstrap 接线 + scope 接缝背书

**Files:**
- Modify: `Assets/CardGame/Runtime/CardGame.Runtime/Bootstrap/GameBootstrap.cs`
- Test: `Assets/CardGame/Tests/EditMode/Bootstrap/SceneInjectionTests.cs`（追加）

**Interfaces:**
- Consumes: Task 2 的 `SceneInjection.InjectScene(IServiceResolver)`；`GameComposition.Container`（`ServiceContainer : IServiceResolver`）；`RunScope`（`CardGame.Runtime`）。
- Produces: 启动期接线行为（Awake 顺序：建组合根 → 注入场景 → StartFlow）；`DefaultExecutionOrder(-32000)` 时序保证；scope 接缝使用模式的测试背书（未来流程状态机的参照）。

- [ ] **Step 1: 写失败的测试（追加 2 个）**

测试文件 `using` 区追加 `using System.Reflection;`。类型体内追加：

```csharp
        private sealed class ScopeSeamComponent : MonoBehaviour
        {
            [Inject] public ISceneService Service;
            [Inject] public RunState State;
        }
```

测试类尾部追加：

```csharp
        [Test]
        public void GameBootstrap_RunsBeforeAllOtherSceneScripts()
        {
            var attribute = typeof(GameBootstrap)
                .GetCustomAttribute<DefaultExecutionOrderAttribute>();

            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute.order, Is.LessThanOrEqualTo(-30000));
        }

        [Test]
        public void Injector_FromRunScope_ResolvesScopedAndSingletonMembers()
        {
            var builder = new ContainerBuilder();
            builder.DefineScope<RunScope>();
            builder.AddSingleton<ISceneService, SceneService>();
            builder.AddScoped<RunState, RunScope>();
            using var container = builder.Build();
            using var runScope = container.CreateScope<RunScope>();
            var gameObject = new GameObject("scope-seam");
            var component = gameObject.AddComponent<ScopeSeamComponent>();
            try
            {
                new UnityObjectInjector(runScope).Inject(component);

                Assert.That(component.Service, Is.SameAs(container.Resolve<ISceneService>()));
                Assert.That(component.State, Is.SameAs(runScope.Resolve<RunState>()));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
```

- [ ] **Step 2: 跑验证确认失败**

Run: `bun scripts/harness/verify.mjs`（+ `--full`）
Expected: `GameBootstrap_RunsBeforeAllOtherSceneScripts` 失败（`attribute` 为 null，当前无 DefaultExecutionOrder）；scope 测试此时应已通过（纯框架能力）——两个都要跑，只断言前者失败。

- [ ] **Step 3: 最小实现（GameBootstrap 接线）**

`Assets/CardGame/Runtime/CardGame.Runtime/Bootstrap/GameBootstrap.cs` 全文替换为：

```csharp
using System;
using CardGame.Domain;
using UnityEngine;

namespace CardGame.Runtime
{
    /// <summary>
    /// 场景唯一入口（单场景形态，见 2026-09-02 决策问题 5 A1）：
    /// 持有数据 TextAsset 引用 → 加载仓库 → 建组合根 → 场景 [Inject] 注入 → 显式启动 → 退出时确定性释放。
    /// [DefaultExecutionOrder(-32000)] 保证本组件 Awake 全场景最先：注入先于其他脚本的
    /// Awake/OnEnable，其他脚本可在自己的 Awake/OnEnable 中安全使用 [Inject] 成员。
    /// 数据加载的 Addressables 换装点在 LoadRepository()（替换此处的 TextAsset 管线，外部不变）。
    /// </summary>
    [DefaultExecutionOrder(-32000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private TextAsset[] dataFiles = Array.Empty<TextAsset>();

        private GameComposition _composition;

        private void Awake()
        {
            _composition = new GameComposition(LoadRepository());
            SceneInjection.InjectScene(_composition.Container);
            _composition.StartFlow();
        }

        private void OnDestroy()
        {
            _composition?.Dispose();
            _composition = null;
        }

        private DataRepository LoadRepository()
        {
            var loader = new GameDataLoader(new JsonUtilityGameDataSerializer());
            return loader.Load(dataFiles);
        }
    }
}
```

- [ ] **Step 4: 跑验证确认通过**

Run: `bun scripts/harness/verify.mjs`（+ `--full`）
Expected: `--full` 全绿，测试数 = 122 + 2 = 124。

- [ ] **Step 5: Commit**

```bash
git add "Assets/CardGame/Runtime/CardGame.Runtime/Bootstrap/GameBootstrap.cs" \
        "Assets/CardGame/Tests/EditMode/Bootstrap/SceneInjectionTests.cs"
git commit -m "feat: drive scene member injection from GameBootstrap startup"
```

---

### Task 4: 文档与状态同步 + 最终验证

**Files:**
- Modify: `docs/design/di-internals.md`（§13.3 末段）
- Modify: `README.md`（「Unity 对象成员注入」节）
- Modify: `feature_list.json`（feat-007 条目）
- Modify: `progress.md`、`session-handoff.md`
- Modify: `docs/superpowers/specs/2026-09-06-scene-inject-driver-design.md`（状态行）

**Interfaces:**
- Consumes: Task 1–3 的落地结果与测试数（124）。
- Produces: 文档与状态的最终一致性；完整验证证据。

- [ ] **Step 1: di-internals.md §13.3 更新「当前无消费者」段**

把现有段落：

```text
**当前无消费者**：UI/输入层尚未接入，注入器是"将来视图接 DI 的预留通道"（`docs/design/README.md` 全局风险 3 与 code-reading-order 第 18 项）。所以改动它前务必先补 UnityObjectInjectorTests 类测试。
```

替换为：

```text
**驱动现状（2026-09-06 起有根驱动）**：`GameBootstrap`（`[DefaultExecutionOrder(-32000)]`，Awake 全场景最先）建组合根后调用 `CardGame.Runtime` 的 `SceneInjection.InjectScene(Container)`——扫描场景全部 MonoBehaviour（含 inactive）逐个注入，先于其他脚本的 Awake/OnEnable。对齐法则：对象的生命周期必须 ≤ 注入 resolver 的生命周期；常驻场景对象只能拿 Singleton（注入 Scoped 抛 `ScopeMismatch`）；scope 拥有的屏由创建 scope 的驱动方从该 scope 注入并先于 scope 销毁（规格：`docs/superpowers/specs/2026-09-06-scene-inject-driver-design.md`）。运行时动态创建的对象不在根驱动范围——谁创建谁注入。改动注入器前务必先补 UnityObjectInjectorTests 类测试。
```

- [ ] **Step 2: README.md「Unity 对象成员注入」节末尾补驱动说明**

在「反射成员注入目前只在 Editor EditMode 中得到验证……」段之后、`## 验证` 之前插入：

```text
场景驱动（2026-09-06）：`GameBootstrap` 在组合根构建完成后、`StartFlow` 之前调用 `CardGame.Runtime` 的 `SceneInjection.InjectScene`，对当前场景全部 MonoBehaviour（含 inactive）执行一次成员注入；`GameBootstrap` 以 `[DefaultExecutionOrder(-32000)]` 保证该注入先于其他脚本的 Awake/OnEnable。必需依赖未注册、或常驻场景对象注入 Scoped 服务，都会在启动期原样抛出。scope 拥有的屏由创建该 scope 的驱动方从 scope 注入（对象生命周期必须 ≤ 注入上下文生命周期）；运行时动态创建的对象由创建方显式注入。
```

- [ ] **Step 3: 规格状态行更新**

`docs/superpowers/specs/2026-09-06-scene-inject-driver-design.md` 的 `**状态：** 已决策（2026-09-06，待实现）` 改为 `**状态：** 已决策并执行（2026-09-06）`。

- [ ] **Step 3b: feature_list.json 登记 feat-007**

在 `features` 数组末尾（feat-006 之后）追加：

```json
    {
      "id": "feat-007",
      "name": "Scene Member Injection Driver",
      "description": "Drive UnityObjectInjector from the composition root so scene-loaded MonoBehaviours outside the container receive [Inject]/[InjectOptional] member injection at startup, with the scope-following injection seam test-backed.",
      "dependencies": [
        "feat-002",
        "feat-003"
      ],
      "status": "done",
      "doneCriteria": [
        "GameBootstrap (DefaultExecutionOrder -32000) builds the composition, then injects the whole scene (including inactive) before any other script Awake/OnEnable and before StartFlow",
        "SceneInjection is a game-side driver only; framework public surface unchanged apart from one InternalsVisibleTo line for the game EditMode test assembly",
        "Required-missing and scoped-from-root injection failures surface at startup (MissingDependency / ScopeMismatch) with no silent degradation",
        "Scope-following injection seam is test-backed (injector built on a RunScope resolves both scoped and singleton members)",
        "EditMode tests cover smoke, active/inactive scan, optional skip, missing required, scoped-from-root guard, scope seam, and execution order (8 tests)",
        "Portable and full Harness verification pass with real Unity EditMode evidence"
      ],
      "evidence": "（执行后填入实际命令与结果，含 EditMode 测试总数）"
    }
```

注意：`evidence` 必须按实际运行结果填写，不得预填。

- [ ] **Step 4: progress.md / session-handoff.md 更新**

`progress.md`：新增「本次完成」小节（场景注入驱动：SceneInjection + GameBootstrap 接线 + InternalsVisibleTo + 8 个测试），验证证据表补一行（命令 + 实际测试数/结果）。
`session-handoff.md`：「本会话已完成」补条目；「最新验证证据」表更新为本次实际结果；「下一可执行步」改为：提交剩余文档改动（di-internals 图示扩充与本批），下一个功能候选保持原三项。

- [ ] **Step 5: 最终验证**

Run: `bun scripts/harness/verify.mjs` 与 `UNITY_EDITOR='C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe' bun scripts/harness/verify.mjs --full`
Expected: 便携通过；`--full` 全绿（124 EditMode 测试，0 失败）；`git diff --check` 干净（不含 di-internals.md 图示与 html 的无关改动——它们保持未提交，留给用户单独处理）。

- [ ] **Step 6: Commit**

```bash
git add "docs/design/di-internals.md" "README.md" "feature_list.json" \
        "docs/superpowers/specs/2026-09-06-scene-inject-driver-design.md" \
        "progress.md" "session-handoff.md"
git commit -m "docs: sync scene injection driver wiring into design and status docs"
```

注意：di-internals.md 的图示改动已在执行前单独提交（分支上的独立 docs commit），本提交只含本任务改动。未跟踪的 `docs/design/di-internals.html` 不属于本功能，保持未提交。
