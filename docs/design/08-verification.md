# Verification — 验证体系

> 位置：`scripts/harness/` · 状态：✅（feat-001/002/005 建立，feat-003 扩展中）
> 操作细节见 `docs/HARNESS.md`；本文件只描述结构。

## 结构

```text
scripts/harness/
├─ verify.mjs   验证入口
│  ├─ 便携模式（默认）：不启动 Unity，纯 Node 检查
│  └─ --full：先便携，再启动真实 Unity EditMode（需 UNITY_EDITOR 环境变量）
│     ├─ checkDurableDiDocuments     中文持久文档完整性
│     ├─ checkFeatureState           功能状态一致（唯一 in-progress 等）
│     ├─ checkSuperpowers            本地技能库可发现
│     ├─ checkUnityProjectHost       Unity 宿主结构 / 项目身份 / 构建场景
│     ├─ checkCSharpBoundaries       递归拒绝根目录旧框架 C# 进入编译
│     ├─ checkPureCSharpDiBoundary   DI asmdef 字段 + token 级拒绝 UnityEngine（含 trivia）
│     ├─ checkPureCSharpEventsBoundary  Events 同上
│     └─ checkGitDiff                无未提交漂移
├─ tests/*.test.mjs   node --test 契约测试
│  ├─ unity-project.test.mjs          Unity 宿主 fixture 解析
│  ├─ unity-test-runner.test.mjs      测试结果 XML 解析（严格栈式，防畸形输入）
│  └─ verify-entrypoint.test.mjs      入口行为（--full 缺 UNITY_EDITOR 须非零退出并给出可行动错误）
└─ unity-project.mjs / verify-unity-project.mjs / xml.mjs   Unity 启动与结果 XML 解析
```

## Unity EditMode 分层

```text
测试程序集（Editor only）
├─ RazorFramework.DI.Tests        详见 01-di.md「测试覆盖」
├─ RazorFramework.Unity.DI.Tests  详见 01-di.md「测试覆盖」
├─ RazorFramework.Events.Tests    详见 02-events.md「测试覆盖」
└─ CardGame.Tests.EditMode        项目身份 + Bootstrap 构建场景（07-cardgame.md）

历史证据（feature_list.json）
├─ feat-002：完整 Harness 26 检查 0 失败，EditMode 67 用例全过
└─ feat-005：完整 Harness 23 检查，EditMode 3 用例全过
```

## 验证原则

- **便携优先**：不依赖 Unity 的门禁先行，快速失败。
- **完整证据**：宣称功能完成必须含真实 Unity EditMode XML 证据。
- **基线先行**：任务前运行 `verify.mjs`，区分既有失败与本次引入的失败。
- **边界即测试**：每条架构边界（纯 C# 门禁、编译域、asmdef 字段）都有机器检查，不靠人肉。

## 已知限制

- 便携模式含 1 个预期 warning（未启动 Unity）。
- 完整模式需要本机 Unity `6000.3.10f1` 路径（`UNITY_EDITOR`）。
- feat-003 计划中的 `assembly-boundaries.test.mjs`（Task 1）尚未落盘；当前 Events 边界由 `verify.mjs` 的 `checkPureCSharpEventsBoundary` 直接承担。
