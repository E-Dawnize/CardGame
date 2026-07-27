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
