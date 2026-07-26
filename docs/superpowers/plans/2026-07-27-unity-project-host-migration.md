# CardGame Unity 项目宿主迁移实施计划

> **面向智能代理执行者（For agentic workers）：** 必须使用
> `superpowers:subagent-driven-development`（推荐）或
> `superpowers:executing-plans` 逐项执行本计划。所有执行步骤使用
> `- [ ]` 复选框追踪。

**目标：** 将 `D:\Unity Project\Temp` 中必要的 Unity 6 项目文件安全迁移到
`CardGame`，建立可编译、可运行 EditMode 测试、可由 Harness 验证的 Unity 项目宿主。

**架构：** `CardGame` 成为 Unity 项目根目录，模板资源整理到
`Assets/CardGame/`，现有 RazorFramework 源码在本计划中继续留在仓库根目录，
不进入 Unity 编译范围。后续独立计划再以测试驱动方式重构并迁入
`Assets/Plugins/RazorFramework/`，避免未验证框架阻塞宿主迁移。

**技术栈：** Unity `6000.3.10f1`、URP `17.3.0`、Input System `1.18.0`、
Unity Test Framework `1.6.0`、C#、Node.js `>=18`、PowerShell、Git。

## 全局约束

- 目标 Unity 版本固定为 `6000.3.10f1`。
- Unity 可执行文件为
  `C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe`。
- 迁移 `Assets/`、`Packages/`、`ProjectSettings/`，并保留所有必要 `.meta` 文件。
- 不迁移 `Library/`、`Temp/`、`Logs/`、`UserSettings/`、`.vscode/`、
  `.sln`、`.slnx` 和 `.csproj`。
- `companyName` 使用 `E-Dawnize`，`productName` 使用 `CardGame`，
  Standalone application identifier 使用 `com.edawnize.cardgame`。
- 新建或实质性修订的项目文档使用简体中文；代码标识符和路径保留英文。
- 现有框架 API 不构成兼容约束，但本计划不修改框架行为。
- 任一时刻最多一个功能处于 `in-progress`。
- 每个提交只包含当前任务相关文件，不推送远端。
- 所有完成结论必须引用实际运行的命令和输出。

---

## 文件结构与职责

本计划完成后，新增或修改的关键文件如下：

```text
CardGame/
├─ Assets/
│  └─ CardGame/
│     ├─ Runtime/
│     │  ├─ CardGame.Runtime.asmdef       # 纯 C# 初始运行时程序集
│     │  └─ ProjectIdentity.cs            # 可测试的项目身份常量
│     ├─ Scenes/
│     │  └─ Bootstrap.unity               # 从模板场景保留 GUID 后重命名
│     ├─ Settings/                         # 2D URP 与输入模板设置
│     └─ Tests/EditMode/
│        ├─ CardGame.Tests.EditMode.asmdef
│        └─ ProjectFoundationTests.cs
├─ Packages/
│  ├─ manifest.json
│  └─ packages-lock.json
├─ ProjectSettings/
├─ scripts/harness/
│  ├─ unity-project.mjs                   # Unity 宿主结构检查库
│  ├─ verify-unity-project.mjs            # 独立检查入口
│  ├─ verify.mjs                          # Harness 汇总入口
│  └─ tests/
│     ├─ unity-project.test.mjs
│     └─ verify-entrypoint.test.mjs
├─ AGENTS.md
├─ README.md
├─ DESIGN-REVIEW.md
├─ docs/HARNESS.md
├─ feature_list.json
├─ progress.md
└─ session-handoff.md
```

根目录现有的 `Boot/`、`DI/`、`Events/`、`Input/`、`Lifecycle/` 和 `MVVM/`
在本计划中保持原位。这样 Unity 首次导入只编译经过验证的最小 CardGame 程序集。

---

### Task 1：激活 Unity 项目宿主迁移功能

**文件：**

- 修改：`feature_list.json`
- 修改：`progress.md`
- 修改：`session-handoff.md`

**接口：**

- 输入：已完成的 `feat-001 Project Harness Foundation`
- 输出：唯一处于 `in-progress` 的 `feat-005 CardGame Unity Project Host`
- 后续依赖：`feat-002` 改为依赖 `feat-005`

- [ ] **Step 1：记录修改前基线**

运行：

```powershell
node scripts/harness/verify.mjs
git -c safe.directory='D:/Unity Project/CardGame' status --short --branch
git -c safe.directory='D:/Unity Project/CardGame' log --oneline -5
```

预期：

- Harness 为 `21 passed, 2 warning(s), 0 failure(s)`。
- 工作区没有未提交文件。
- 两项警告分别是既有 DI Unity 空值判断和未运行 Unity 测试。

- [ ] **Step 2：更新功能依赖和状态**

在 `feature_list.json` 中：

1. 将 `feat-002.dependencies` 从 `["feat-001"]` 改为 `["feat-005"]`。
2. 在 `features` 数组末尾加入：

```json
{
  "id": "feat-005",
  "name": "CardGame Unity Project Host",
  "description": "Migrate the approved Unity 6000.3.10f1 project substrate into CardGame and establish a repeatable EditMode verification host without compiling the legacy framework.",
  "dependencies": ["feat-001"],
  "status": "in-progress",
  "doneCriteria": [
    "CardGame contains the approved Assets, Packages, and ProjectSettings roots without Unity-generated caches",
    "Project identity and Bootstrap scene paths are normalized for CardGame",
    "A pure C# CardGame runtime assembly and EditMode smoke tests compile in Unity 6000.3.10f1",
    "Portable and full Harness verification pass from the CardGame repository root",
    "README, Harness documentation, progress, and handoff describe the Unity-host workflow in Chinese"
  ],
  "evidence": ""
}
```

- [ ] **Step 3：将会话状态改为中文并指向当前功能**

`progress.md` 的当前状态必须明确写出：

```markdown
**当前功能：** feat-005 CardGame Unity Project Host
**状态：** 进行中
```

同时记录 Step 1 的实际基线摘要。

`session-handoff.md` 必须将唯一下一步写成：

```markdown
创建 Unity 项目结构验证器，并先用 Node 测试固定迁移契约。
```

- [ ] **Step 4：验证状态文件**

运行：

```powershell
node scripts/harness/verify.mjs
```

预期：

- 输出包含 `Feature state parsed (5 features, 1 in progress)`。
- 总结仍为 0 个 failure。

- [ ] **Step 5：提交功能激活**

```powershell
git add feature_list.json progress.md session-handoff.md
git commit -m "chore: start Unity project host migration"
```

---

### Task 2：用测试固定 Unity 宿主结构契约

**文件：**

- 创建：`scripts/harness/unity-project.mjs`
- 创建：`scripts/harness/verify-unity-project.mjs`
- 创建：`scripts/harness/tests/unity-project.test.mjs`

**接口：**

- 提供：
  `inspectUnityProject(root: string): Promise<{ info: UnityProjectInfo, problems: string[] }>`
- `UnityProjectInfo` 形状：
  `{ editorVersion, companyName, productName, applicationIdentifier, firstScene }`
- 命令行入口：
  `node scripts/harness/verify-unity-project.mjs [--root <path>]`

- [ ] **Step 1：先写失败的 Node 测试**

创建 `scripts/harness/tests/unity-project.test.mjs`：

```javascript
import assert from "node:assert/strict";
import { mkdtemp, mkdir, writeFile } from "node:fs/promises";
import os from "node:os";
import path from "node:path";
import test from "node:test";

import { inspectUnityProject } from "../unity-project.mjs";

async function createFixture() {
  const root = await mkdtemp(path.join(os.tmpdir(), "cardgame-unity-host-"));
  await mkdir(path.join(root, "Assets", "CardGame", "Scenes"), { recursive: true });
  await mkdir(path.join(root, "Packages"), { recursive: true });
  await mkdir(path.join(root, "ProjectSettings"), { recursive: true });
  await writeFile(
    path.join(root, "Assets", "CardGame", "Scenes", "Bootstrap.unity"),
    "%YAML 1.1\n"
  );
  await writeFile(
    path.join(root, "Packages", "manifest.json"),
    JSON.stringify({
      dependencies: {
        "com.unity.inputsystem": "1.18.0",
        "com.unity.render-pipelines.universal": "17.3.0",
        "com.unity.test-framework": "1.6.0",
        "com.unity.ugui": "2.0.0"
      }
    })
  );
  await writeFile(
    path.join(root, "ProjectSettings", "ProjectVersion.txt"),
    "m_EditorVersion: 6000.3.10f1\n"
  );
  await writeFile(
    path.join(root, "ProjectSettings", "ProjectSettings.asset"),
    [
      "  companyName: E-Dawnize",
      "  productName: CardGame",
      "  applicationIdentifier:",
      "    Standalone: com.edawnize.cardgame"
    ].join("\n")
  );
  await writeFile(
    path.join(root, "ProjectSettings", "EditorBuildSettings.asset"),
    "    path: Assets/CardGame/Scenes/Bootstrap.unity\n"
  );
  return root;
}

test("accepts the approved CardGame Unity host", async () => {
  const root = await createFixture();
  const result = await inspectUnityProject(root);
  assert.deepEqual(result.problems, []);
  assert.equal(result.info.editorVersion, "6000.3.10f1");
  assert.equal(result.info.productName, "CardGame");
  assert.equal(
    result.info.firstScene,
    "Assets/CardGame/Scenes/Bootstrap.unity"
  );
});

test("reports an invalid template identity", async () => {
  const root = await createFixture();
  await writeFile(
    path.join(root, "ProjectSettings", "ProjectSettings.asset"),
    "  companyName: DefaultCompany\n  productName: Temp\n"
  );
  const result = await inspectUnityProject(root);
  assert.ok(result.problems.includes("companyName must be E-Dawnize"));
  assert.ok(result.problems.includes("productName must be CardGame"));
});
```

- [ ] **Step 2：运行测试并确认先失败**

运行：

```powershell
node --test scripts/harness/tests/unity-project.test.mjs
```

预期：失败，并包含
`ERR_MODULE_NOT_FOUND` 或缺少导出的 `inspectUnityProject`。

- [ ] **Step 3：实现最小结构检查库**

创建 `scripts/harness/unity-project.mjs`：

```javascript
import { readFile } from "node:fs/promises";
import path from "node:path";

const REQUIRED_PACKAGES = new Map([
  ["com.unity.inputsystem", "1.18.0"],
  ["com.unity.render-pipelines.universal", "17.3.0"],
  ["com.unity.test-framework", "1.6.0"],
  ["com.unity.ugui", "2.0.0"]
]);

async function read(root, relativePath, problems) {
  try {
    return await readFile(path.join(root, relativePath), "utf8");
  } catch {
    problems.push(relativePath + " is missing");
    return "";
  }
}

function capture(text, expression) {
  return text.match(expression)?.[1]?.trim() ?? "";
}

export async function inspectUnityProject(root) {
  const problems = [];
  const versionText = await read(
    root,
    "ProjectSettings/ProjectVersion.txt",
    problems
  );
  const settingsText = await read(
    root,
    "ProjectSettings/ProjectSettings.asset",
    problems
  );
  const buildText = await read(
    root,
    "ProjectSettings/EditorBuildSettings.asset",
    problems
  );
  const manifestText = await read(root, "Packages/manifest.json", problems);
  await read(root, "Assets/CardGame/Scenes/Bootstrap.unity", problems);

  const info = {
    editorVersion: capture(versionText, /^m_EditorVersion:\s*(.+)$/m),
    companyName: capture(settingsText, /^\s*companyName:\s*(.+)$/m),
    productName: capture(settingsText, /^\s*productName:\s*(.+)$/m),
    applicationIdentifier: capture(
      settingsText,
      /^\s*Standalone:\s*(com\.edawnize\.cardgame)$/m
    ),
    firstScene: capture(buildText, /^\s*path:\s*(.+)$/m)
  };

  if (info.editorVersion !== "6000.3.10f1") {
    problems.push("Unity editor version must be 6000.3.10f1");
  }
  if (info.companyName !== "E-Dawnize") {
    problems.push("companyName must be E-Dawnize");
  }
  if (info.productName !== "CardGame") {
    problems.push("productName must be CardGame");
  }
  if (info.applicationIdentifier !== "com.edawnize.cardgame") {
    problems.push("Standalone application identifier must be com.edawnize.cardgame");
  }
  if (info.firstScene !== "Assets/CardGame/Scenes/Bootstrap.unity") {
    problems.push("first enabled scene must be Assets/CardGame/Scenes/Bootstrap.unity");
  }

  if (manifestText) {
    let manifest;
    try {
      manifest = JSON.parse(manifestText);
    } catch {
      problems.push("Packages/manifest.json is invalid JSON");
    }
    for (const [name, version] of REQUIRED_PACKAGES) {
      if (manifest?.dependencies?.[name] !== version) {
        problems.push(name + " must be pinned to " + version);
      }
    }
  }

  return { info, problems };
}
```

- [ ] **Step 4：实现独立命令行入口**

创建 `scripts/harness/verify-unity-project.mjs`：

```javascript
#!/usr/bin/env node

import path from "node:path";
import process from "node:process";

import { inspectUnityProject } from "./unity-project.mjs";

const rootIndex = process.argv.indexOf("--root");
const root = path.resolve(
  rootIndex >= 0 ? process.argv[rootIndex + 1] : process.cwd()
);
const result = await inspectUnityProject(root);

if (result.problems.length > 0) {
  for (const problem of result.problems) {
    console.error("[FAIL] " + problem);
  }
  process.exitCode = 1;
} else {
  console.log("[PASS] CardGame Unity project host is valid");
  console.log(JSON.stringify(result.info, null, 2));
}
```

- [ ] **Step 5：运行测试并确认通过**

```powershell
node --test scripts/harness/tests/unity-project.test.mjs
```

预期：2 项测试通过。

- [ ] **Step 6：确认真实仓库仍处于预期红灯**

```powershell
node scripts/harness/verify-unity-project.mjs
```

预期：失败并报告缺少 `ProjectSettings`、`Packages` 和 Bootstrap 场景。
这是迁移前的预期失败，只提交已经通过单元测试的检查工具。

- [ ] **Step 7：提交结构检查工具**

```powershell
git add scripts/harness/unity-project.mjs `
  scripts/harness/verify-unity-project.mjs `
  scripts/harness/tests/unity-project.test.mjs
git commit -m "test: define Unity project host contract"
```

---

### Task 3：迁移并整理 Unity 项目文件

**文件：**

- 创建：`Assets/`
- 创建：`Packages/manifest.json`
- 创建：`Packages/packages-lock.json`
- 创建：`ProjectSettings/`
- 修改：`ProjectSettings/ProjectSettings.asset`
- 修改：`ProjectSettings/EditorBuildSettings.asset`

**接口：**

- 输入：`D:\Unity Project\Temp` 中 Unity `6000.3.10f1` 模板
- 输出：满足 `inspectUnityProject()` 的 CardGame Unity 宿主
- 保留：模板资源原 `.meta` GUID

- [ ] **Step 1：解析并核对迁移路径**

```powershell
$sourceRoot = (Resolve-Path -LiteralPath 'D:\Unity Project\Temp').Path
$targetRoot = (Resolve-Path -LiteralPath 'D:\Unity Project\CardGame').Path
if ($sourceRoot -ne 'D:\Unity Project\Temp') { throw "Unexpected source: $sourceRoot" }
if ($targetRoot -ne 'D:\Unity Project\CardGame') { throw "Unexpected target: $targetRoot" }
foreach ($name in @('Assets', 'Packages', 'ProjectSettings')) {
  if (Test-Path -LiteralPath (Join-Path $targetRoot $name)) {
    throw "Target already exists: $name"
  }
}
```

预期：命令无输出且不抛出异常。

- [ ] **Step 2：复制三个允许的 Unity 根目录**

```powershell
Copy-Item -LiteralPath 'D:\Unity Project\Temp\Assets' `
  -Destination 'D:\Unity Project\CardGame\Assets' -Recurse
Copy-Item -LiteralPath 'D:\Unity Project\Temp\Packages' `
  -Destination 'D:\Unity Project\CardGame\Packages' -Recurse
Copy-Item -LiteralPath 'D:\Unity Project\Temp\ProjectSettings' `
  -Destination 'D:\Unity Project\CardGame\ProjectSettings' -Recurse
```

不得执行针对 `Temp`、`Library` 或仓库根目录的递归删除。

- [ ] **Step 3：按 CardGame 布局移动模板资产**

```powershell
New-Item -ItemType Directory -Path 'Assets\CardGame'
Move-Item -LiteralPath 'Assets\Scenes' `
  -Destination 'Assets\CardGame\Scenes'
Move-Item -LiteralPath 'Assets\Scenes.meta' `
  -Destination 'Assets\CardGame\Scenes.meta'
Move-Item -LiteralPath 'Assets\Settings' `
  -Destination 'Assets\CardGame\Settings'
Move-Item -LiteralPath 'Assets\Settings.meta' `
  -Destination 'Assets\CardGame\Settings.meta'
Move-Item -LiteralPath 'Assets\CardGame\Scenes\SampleScene.unity' `
  -Destination 'Assets\CardGame\Scenes\Bootstrap.unity'
Move-Item -LiteralPath 'Assets\CardGame\Scenes\SampleScene.unity.meta' `
  -Destination 'Assets\CardGame\Scenes\Bootstrap.unity.meta'
```

将根目录三个模板配置资源及其 `.meta` 移入 `Settings`：

```powershell
foreach ($name in @(
  'DefaultVolumeProfile.asset',
  'InputSystem_Actions.inputactions',
  'UniversalRenderPipelineGlobalSettings.asset'
)) {
  Move-Item -LiteralPath (Join-Path 'Assets' $name) `
    -Destination (Join-Path 'Assets\CardGame\Settings' $name)
  Move-Item -LiteralPath (Join-Path 'Assets' ($name + '.meta')) `
    -Destination (Join-Path 'Assets\CardGame\Settings' ($name + '.meta'))
}
```

- [ ] **Step 4：创建 `Assets/CardGame.meta`**

创建 `Assets/CardGame.meta`：

```yaml
fileFormatVersion: 2
guid: 0c3fd7ed2ea54cc0a7c03f9b5182423
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData:
  assetBundleName:
  assetBundleVariant:
```

- [ ] **Step 5：修改项目身份**

在 `ProjectSettings/ProjectSettings.asset` 中做精确替换：

```diff
-  companyName: DefaultCompany
-  productName: Temp
+  companyName: E-Dawnize
+  productName: CardGame
```

以及：

```diff
-    Standalone: com.DefaultCompany.2D-URP
+    Standalone: com.edawnize.cardgame
```

- [ ] **Step 6：修改首个构建场景路径**

在 `ProjectSettings/EditorBuildSettings.asset` 中保持原 GUID
`8c9cfa26abfee488c85f1582747f6a02`，只修改路径：

```diff
-    path: Assets/Scenes/SampleScene.unity
+    path: Assets/CardGame/Scenes/Bootstrap.unity
```

- [ ] **Step 7：运行独立结构检查**

```powershell
node scripts/harness/verify-unity-project.mjs
```

预期：

```text
[PASS] CardGame Unity project host is valid
```

- [ ] **Step 8：检查没有迁移缓存**

```powershell
git status --short
git ls-files Library Temp Logs UserSettings '*.sln' '*.slnx' '*.csproj'
```

预期：

- `git status` 只显示允许的 Unity 项目文件。
- 第二条命令没有输出。

- [ ] **Step 9：提交 Unity 项目宿主**

```powershell
git add Assets Packages ProjectSettings
git commit -m "feat: migrate CardGame Unity project host"
```

---

### Task 4：建立最小纯 C# 程序集与 EditMode 冒烟测试

**文件：**

- 创建：`Assets/CardGame/Runtime.meta`
- 创建：`Assets/CardGame/Runtime/CardGame.Runtime.asmdef`
- 创建：`Assets/CardGame/Runtime/ProjectIdentity.cs`
- 创建：上述文件对应 `.meta`
- 创建：`Assets/CardGame/Tests.meta`
- 创建：`Assets/CardGame/Tests/EditMode.meta`
- 创建：`Assets/CardGame/Tests/EditMode/CardGame.Tests.EditMode.asmdef`
- 创建：`Assets/CardGame/Tests/EditMode/ProjectFoundationTests.cs`
- 创建：上述文件对应 `.meta`

**接口：**

- 提供：`CardGame.ProjectIdentity.ProductName`
- 提供：`CardGame.ProjectIdentity.CompanyName`
- 测试程序集引用：`CardGame.Runtime`

- [ ] **Step 1：创建测试程序集和失败测试**

创建 `Assets/CardGame/Tests/EditMode/CardGame.Tests.EditMode.asmdef`：

```json
{
  "name": "CardGame.Tests.EditMode",
  "rootNamespace": "CardGame.Tests.EditMode",
  "references": [
    "CardGame.Runtime"
  ],
  "includePlatforms": [
    "Editor"
  ],
  "optionalUnityReferences": [
    "TestAssemblies"
  ],
  "autoReferenced": false
}
```

创建 `Assets/CardGame/Tests/EditMode/ProjectFoundationTests.cs`：

```csharp
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace CardGame.Tests.EditMode
{
    public sealed class ProjectFoundationTests
    {
        [Test]
        public void ProjectIdentity_MatchesPlayerSettings()
        {
            Assert.That(ProjectIdentity.ProductName, Is.EqualTo("CardGame"));
            Assert.That(ProjectIdentity.CompanyName, Is.EqualTo("E-Dawnize"));
            Assert.That(PlayerSettings.productName, Is.EqualTo(ProjectIdentity.ProductName));
            Assert.That(PlayerSettings.companyName, Is.EqualTo(ProjectIdentity.CompanyName));
        }

        [Test]
        public void Bootstrap_IsFirstEnabledBuildScene()
        {
            var scene = EditorBuildSettings.scenes.First(item => item.enabled);
            Assert.That(
                scene.path,
                Is.EqualTo("Assets/CardGame/Scenes/Bootstrap.unity"));
        }
    }
}
```

- [ ] **Step 2：运行 Unity 并确认测试先失败**

```powershell
$env:UNITY_EDITOR='C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'
$redResult = Join-Path ([IO.Path]::GetTempPath()) `
  'CardGame-foundation-red.xml'
& $env:UNITY_EDITOR -batchmode -nographics -quit `
  -projectPath 'D:\Unity Project\CardGame' `
  -runTests -testPlatform EditMode `
  -testResults $redResult `
  -logFile 'D:\Unity Project\CardGame\Logs\foundation-red.log'
```

预期：Unity 返回非零退出码，日志指出缺少 `CardGame.Runtime` 或
`ProjectIdentity`。`Logs/` 不得加入 Git。

- [ ] **Step 3：实现最小运行时程序集**

创建 `Assets/CardGame/Runtime/CardGame.Runtime.asmdef`：

```json
{
  "name": "CardGame.Runtime",
  "rootNamespace": "CardGame",
  "references": [],
  "autoReferenced": true,
  "noEngineReferences": true
}
```

创建 `Assets/CardGame/Runtime/ProjectIdentity.cs`：

```csharp
namespace CardGame
{
    public static class ProjectIdentity
    {
        public const string ProductName = "CardGame";
        public const string CompanyName = "E-Dawnize";
    }
}
```

- [ ] **Step 4：运行 Unity EditMode 测试**

```powershell
$env:UNITY_EDITOR='C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'
$testResult = Join-Path ([IO.Path]::GetTempPath()) `
  'CardGame-foundation-editmode.xml'
& $env:UNITY_EDITOR -batchmode -nographics -quit `
  -projectPath 'D:\Unity Project\CardGame' `
  -runTests -testPlatform EditMode `
  -testResults $testResult `
  -logFile 'D:\Unity Project\CardGame\Logs\foundation-editmode.log'
if ($LASTEXITCODE -ne 0) {
  Get-Content 'D:\Unity Project\CardGame\Logs\foundation-editmode.log' -Tail 200
  exit $LASTEXITCODE
}
```

预期：2 项 EditMode 测试通过，Unity 返回退出码 0。

- [ ] **Step 5：检查 Unity 生成的新增资源 `.meta`**

Step 4 的 Unity 导入会为 Runtime、Tests、`.asmdef` 和 C# 文件生成 `.meta`。
运行：

```powershell
$requiredMeta = @(
  'Assets\CardGame\Runtime.meta',
  'Assets\CardGame\Runtime\CardGame.Runtime.asmdef.meta',
  'Assets\CardGame\Runtime\ProjectIdentity.cs.meta',
  'Assets\CardGame\Tests.meta',
  'Assets\CardGame\Tests\EditMode.meta',
  'Assets\CardGame\Tests\EditMode\CardGame.Tests.EditMode.asmdef.meta',
  'Assets\CardGame\Tests\EditMode\ProjectFoundationTests.cs.meta'
)
foreach ($path in $requiredMeta) {
  if (-not (Test-Path -LiteralPath $path)) {
    throw "Unity did not generate required meta file: $path"
  }
}
```

预期：所有路径存在且没有异常。生成的 `.meta` 必须与对应资源一起提交。

- [ ] **Step 6：确认 Unity 生成物未被跟踪**

```powershell
git status --short
git check-ignore Library Logs UserSettings
```

预期：第二条命令显示这些路径均被 `.gitignore` 覆盖。

- [ ] **Step 7：提交最小程序集和测试**

```powershell
git add Assets/CardGame/Runtime Assets/CardGame/Runtime.meta `
  Assets/CardGame/Tests Assets/CardGame/Tests.meta
git commit -m "test: add CardGame Unity foundation smoke tests"
```

---

### Task 5：把 Unity 宿主检查接入 Harness

**文件：**

- 创建：`scripts/harness/tests/verify-entrypoint.test.mjs`
- 修改：`scripts/harness/verify.mjs`
- 修改：`AGENTS.md`
- 修改：`README.md`
- 修改：`DESIGN-REVIEW.md`
- 修改：`docs/HARNESS.md`

**接口：**

- `node scripts/harness/verify.mjs` 包含 Unity 宿主结构检查。
- `node scripts/harness/verify.mjs --full` 默认以仓库根目录作为 Unity 项目路径。
- `UNITY_EDITOR` 是完整验证唯一必须设置的环境变量。
- `RAZOR_UNITY_PROJECT` 仍可作为可选覆盖路径，不再是必填项。

- [ ] **Step 1：先写失败的汇总入口测试**

创建 `scripts/harness/tests/verify-entrypoint.test.mjs`：

```javascript
import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "..",
  "..",
  ".."
);

test("portable Harness validates the Unity project host", () => {
  const result = spawnSync(
    process.execPath,
    ["scripts/harness/verify.mjs"],
    { cwd: root, encoding: "utf8" }
  );
  assert.equal(result.status, 0, result.stderr);
  assert.match(result.stdout, /\[PASS\] CardGame Unity project host validated/);
});
```

- [ ] **Step 2：运行测试并确认先失败**

```powershell
node --test scripts/harness/tests/verify-entrypoint.test.mjs
```

预期：断言失败，因为现有 `verify.mjs` 尚未输出 Unity 宿主通过信息。

- [ ] **Step 3：将结构检查接入 `verify.mjs`**

在 import 区加入：

```javascript
import { inspectUnityProject } from "./unity-project.mjs";
```

加入：

```javascript
async function checkUnityProjectHost() {
  const result = await inspectUnityProject(ROOT);
  if (result.problems.length > 0) {
    for (const problem of result.problems) {
      fail("Unity host: " + problem);
    }
    return;
  }
  pass("CardGame Unity project host validated");
}
```

在 `main()` 中 `checkSuperpowers()` 之后、C# 边界检查之前调用：

```javascript
await checkUnityProjectHost();
```

- [ ] **Step 4：让完整检查默认使用当前仓库**

将 `fullChecks()` 中：

```javascript
const projectPath = process.env.RAZOR_UNITY_PROJECT;
if (!editor || !projectPath) {
```

改为：

```javascript
const projectPath = process.env.RAZOR_UNITY_PROJECT || ROOT;
if (!editor) {
```

将警告改为：

```javascript
warn("UNITY_EDITOR is not set; Unity EditMode tests were not run");
```

删除根据根目录 `.sln` 或 `.csproj` 自动运行 `dotnet test` 的逻辑。
Unity 生成的解决方案不是独立 .NET 测试宿主，不能作为该检查的触发条件。

- [ ] **Step 5：运行 Node 测试和便携 Harness**

```powershell
node --test scripts/harness/tests/*.test.mjs
node scripts/harness/verify.mjs
```

预期：

- Node 测试全部通过。
- Harness 包含 `CardGame Unity project host validated`。
- Harness 没有 failure。

- [ ] **Step 6：将工作流文档改为中文 Unity 项目说明**

文档必须准确写明：

```text
便携验证：
node scripts/harness/verify.mjs

完整验证：
$env:UNITY_EDITOR='C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'
node scripts/harness/verify.mjs --full
```

具体修改：

- `README.md`：标题改为 CardGame，说明项目是 Unity 叙事肉鸽卡牌游戏，
  列出 `Assets/CardGame`、根目录旧框架和 Harness 的当前角色。
- `AGENTS.md`：删除“仓库不是完整 Unity 项目”的描述；启动流程增加 Unity
  项目版本检查；完整验证只要求 `UNITY_EDITOR`。
- `docs/HARNESS.md`：整体改为中文，说明便携和 Unity 两级验证。
- `DESIGN-REVIEW.md`：在顶部加入中文状态说明，明确旧审计结论仍作为框架
  重构输入，但“缺少 Unity 宿主”问题由 feat-005 解决。

- [ ] **Step 7：提交 Harness 集成**

```powershell
git add scripts/harness/verify.mjs `
  scripts/harness/tests/verify-entrypoint.test.mjs `
  AGENTS.md README.md DESIGN-REVIEW.md docs/HARNESS.md
git commit -m "chore: verify CardGame Unity host"
```

---

### Task 6：运行完整验证并关闭 feat-005

**文件：**

- 修改：`feature_list.json`
- 修改：`progress.md`
- 修改：`session-handoff.md`
- 可能修改：`Packages/packages-lock.json`，仅限 Unity `6000.3.10f1`
  首次解析产生的确定变更

**接口：**

- 输入：已迁移 Unity 宿主、EditMode 测试、Harness 集成
- 输出：状态为 `done` 且含实际证据的 `feat-005`
- 下一计划入口：RazorFramework 核心、DI 与生命周期重构

- [ ] **Step 1：使用完成前验证 skill**

读取并遵循：

```text
.agents/skills/verification-before-completion/SKILL.md
```

不得根据预期结果关闭功能。

- [ ] **Step 2：运行全部 Node 和便携检查**

```powershell
node --test scripts/harness/tests/*.test.mjs
node scripts/harness/verify.mjs
git diff --check
```

预期：所有命令退出码为 0。

- [ ] **Step 3：运行 Unity 完整检查**

```powershell
$env:UNITY_EDITOR='C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe'
node scripts/harness/verify.mjs --full
```

预期：

- Unity EditMode 测试通过。
- Harness 总结为 0 个 failure。
- Unity 相关检查不能仍以“未运行”形式存在。

- [ ] **Step 4：审查首次导入变化**

```powershell
git status --short
git diff -- Packages/packages-lock.json
git ls-files Library Temp Logs UserSettings '*.sln' '*.slnx' '*.csproj'
```

处理规则：

- `Library`、`Temp`、`Logs`、`UserSettings` 和生成工程文件不得被跟踪。
- `packages-lock.json` 只保留 Unity 解析当前 `manifest.json` 后的版本变化。
- 不在本功能中删除模板包；包精简在独立功能中用导入测试保护。

- [ ] **Step 5：将功能标记为完成**

在 `feature_list.json` 中将 `feat-005.status` 改为 `done`。

`evidence` 必须写入当前日期、以下实际命令以及它们的实际摘要：

```text
node --test scripts/harness/tests/*.test.mjs
node scripts/harness/verify.mjs
node scripts/harness/verify.mjs --full
git diff --check
```

不得复制计划中的预期数字；必须使用 Step 2 和 Step 3 的真实输出。

- [ ] **Step 6：更新中文进度与交接**

`progress.md` 记录：

- 迁移的允许目录。
- 排除的缓存目录。
- Unity Editor 版本。
- Node 与 Unity 测试结果。
- 旧框架仍在根目录、未进入 Unity 编译范围这一明确边界。
- 包精简尚未发生，不能描述为完成。

`session-handoff.md` 的唯一下一步写为：

```markdown
为 RazorFramework Core、DI 与 Lifecycle 编写独立重构规格和实施计划，
先消除 DI 对 UnityEngine 的编译时依赖。
```

- [ ] **Step 7：最终检查**

```powershell
node scripts/harness/verify.mjs
git diff --check
git status --short
```

预期：

- Harness 无 failure。
- `git diff --check` 无输出。
- 工作区只包含本任务的状态和证据文档修改。

- [ ] **Step 8：提交完成状态**

```powershell
git add feature_list.json progress.md session-handoff.md `
  Packages/packages-lock.json
git commit -m "docs: complete Unity project host migration"
```

若 `Packages/packages-lock.json` 没有变化，则不要将它加入命令。

---

## 计划边界与后续拆分

本计划只实现设计规格中的 Unity 项目宿主部分，并产生可独立验证的软件。
其余已确认设计按照以下顺序编写独立计划：

1. RazorFramework Core、DI 与 Lifecycle 全面重构。
2. Events、Presentation、Input 与 Unity Bootstrap 重构。
3. CardGame 战斗 Domain、命令/效果队列与伤害结算管线。
4. 地图生成、双层剧情状态与版本化存档。
5. 非程序文案内容编译器、中文协作规范与内容校验。
6. 无界面战斗模拟器、基础策略代理与 AI 辅助数值报告。

每份计划完成后都必须产生可以单独运行的测试和 Harness 证据，且同一时间只有
一个功能处于 `in-progress`。
