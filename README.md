# RazorFramework — Unity 通用游戏框架

从 Unsaid Goodbye 项目提取的独立可复用框架。原始项目：https://github.com/E-Dawnize/Unsaid-Goodbye

## 模块

| 模块 | 路径 | 依赖 | 说明 |
|------|------|------|------|
| **DI** | `DI/` | 无 | 轻量 DI 容器（Singleton/Scoped/Transient、属性注入、依赖图验证） |
| **Lifecycle** | `Lifecycle/` | DI | 严格生命周期 + UpdateRunner |
| **Events** | `Events/` | Lifecycle | 强类型事件总线（struct 零 GC） |
| **MVVM** | `MVVM/` | Events, DI | ViewModelBase + RelayCommand + BindingManager |
| **Input** | `Input/` | Lifecycle | 轮询式输入抽象 |
| **Boot** | `Boot/` | 以上全部 | 启动引导 + Installer 编排 |

## 使用方式

详见 `docs/CONTRACT.md`。

## AI Harness

????????? Harness?`AGENTS.md` ???????
`feature_list.json` / `progress.md` / `session-handoff.md` ????????
`.agents/skills/` ???? Superpowers v6.2.0?

????????

```text
node scripts/harness/verify.mjs
```

????? `docs/HARNESS.md`????????????? Unity ???
?? Unity ???????? Unity ???????????
