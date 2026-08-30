# Boot — 启动编排与 Installer

> 旧实现：根目录 `Boot/`（非编译域，引用已删除的 DI V1 API）
> 目标程序集：`RazorFramework.Unity.Boot`（Unity，refs 全部框架程序集）
> 状态：⏳ 迁移中（feat-003 Task 5 未开始） · 游戏细节外迁 CardGame.Runtime

## 旧实现（迁移输入）

```text
根目录 Boot/（非编译域）
├─ ProjectContext : MonoBehaviour
│  ├─ Ensure() 单例创建（DontDestroyOnLoad）+ 异步 Boot()
│  ├─ 启动顺序：加载遮罩 → 创建 DI 容器与 Project Scope → Addressables 加载
│  │           BootConfig(label) → 全局 Installers 注册 → 依赖校验 → LifecycleRegistry
│  │           接线 → 全局 View 钩子 → 初始场景 Scope → InitializeAll/StartAll
│  │           → ITickable 注册到 UpdateRunner → 完成钩子
│  ├─ virtual 钩子：OnGlobalViewsReady / OnBootComplete / OnSceneScopeCreated
│  ├─ 失败降级：记 Debug.LogError 并隐藏遮罩，不崩溃
│  └─ OnDestroy 逆序清理（Scope → 容器 → LifecycleRegistry）
├─ ProjectBootstrap
│  ├─ SubsystemRegistration 静态重置 → BeforeSceneLoad 启动（EnhancedTouch + ProjectContext.Ensure）
│  └─ FixEventSystemInputModules：StandaloneInputModule → InputSystemUIInputModule
│     （游戏细节，目标外迁）
├─ SceneScopeRunner   场景加载/卸载时创建/清理 Scope，运行场景 Installers 与生命周期
└─ Installer 资产（ScriptableObject）
   ├─ InstallerAsset : IInstaller   abstract Register(DIContainer)；order 字段控制执行序
   └─ InstallerConfig   global/scene 两级列表按 order 排序；Addressables label="BootConfig"
```

## 目标形态（feat-003 D4/D5）

```text
RazorFramework.Unity.Boot
├─ IInstaller.Register(ContainerBuilder)   改接 DI V2（D4）
├─ InstallerAsset / InstallerConfig / SceneScopeRunner 迁入
├─ ProjectContext / ProjectBootstrap 保留通用启动组合与 virtual 钩子
└─ 框架不含游戏细节

CardGame.Runtime（游戏侧，见 07-cardgame.md）
├─ 加载遮罩、Addressables BootConfig 加载、输入系统修复（D5）
└─ Input/ 全部迁入（见 06-input.md）
```

## 关键决策与不变量

- Installer 是 SO 资产：策划/程序通过 BootConfig 组合启动流程，不写代码。
- 全局 Installer 应用生命周期执行一次；场景 Installer 每次场景加载执行（Scoped 服务在此注册）。
- 启动失败降级：记录错误并隐藏遮罩，不崩溃。
- 框架 Boot 只保留通用组合与 `virtual` 钩子，游戏特定逻辑通过继承/外迁注入。

## 已知限制

- 旧代码深度引用 DI V1（`DIContainer`/`IScope`/`ResolveAll(scope)`/`OnInstanceCreated`），属重写而非迁移。
- BootConfig 依赖 Addressables 加载，完整验证需要真实资源环境。
- 初始场景 Scope 的预创建与 SceneScopeRunner 的接力逻辑（"初始场景跳过"）需在重写时保留语义。
