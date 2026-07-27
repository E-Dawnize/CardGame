import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { mkdtemp, mkdir, rm, writeFile } from "node:fs/promises";
import os from "node:os";
import path from "node:path";
import test from "node:test";

import { inspectUnityProject } from "../unity-project.mjs";

const packages = {
  "com.unity.inputsystem": "1.18.0",
  "com.unity.render-pipelines.universal": "17.3.0",
  "com.unity.test-framework": "1.6.0",
  "com.unity.ugui": "2.0.0"
};

async function createFixture() {
  const root = await mkdtemp(path.join(os.tmpdir(), "cardgame-unity-host-"));
  await mkdir(path.join(root, "Assets", "CardGame", "Scenes"), { recursive: true });
  await mkdir(path.join(root, "Packages"), { recursive: true });
  await mkdir(path.join(root, "ProjectSettings"), { recursive: true });
  await writeFile(path.join(root, "Assets", "CardGame", "Scenes", "Bootstrap.unity"), "%YAML 1.1\n");
  await writeManifest(root);
  await writeFile(path.join(root, "ProjectSettings", "ProjectVersion.txt"), "m_EditorVersion: 6000.3.10f1\n");
  await writeSettings(root, "com.edawnize.cardgame");
  await writeScenes(root, [
    { enabled: 1, path: "Assets/CardGame/Scenes/Bootstrap.unity" }
  ]);
  return root;
}

async function writeManifest(root, changedPackage) {
  const dependencies = { ...packages };
  if (changedPackage) dependencies[changedPackage] = "0.0.0";
  await writeFile(path.join(root, "Packages", "manifest.json"), JSON.stringify({ dependencies }));
}

async function writeSettings(root, applicationIdentifier) {
  await writeFile(path.join(root, "ProjectSettings", "ProjectSettings.asset"), [
    "  companyName: E-Dawnize",
    "  productName: CardGame",
    "  applicationIdentifier:",
    `    Standalone: ${applicationIdentifier}`
  ].join("\n"));
}

async function writeScenes(root, scenes) {
  const text = scenes.flatMap((scene) => [
    `- enabled: ${scene.enabled}`,
    `  path: ${scene.path}`
  ]).join("\n");
  await writeFile(path.join(root, "ProjectSettings", "EditorBuildSettings.asset"), text);
}

test("accepts the approved CardGame Unity host", async () => {
  const root = await createFixture();
  const result = await inspectUnityProject(root);
  assert.deepEqual(result.problems, []);
  assert.equal(result.info.editorVersion, "6000.3.10f1");
  assert.equal(result.info.productName, "CardGame");
  assert.equal(result.info.firstScene, "Assets/CardGame/Scenes/Bootstrap.unity");
});

test("reports an invalid template identity", async () => {
  const root = await createFixture();
  await writeFile(path.join(root, "ProjectSettings", "ProjectSettings.asset"), "  companyName: DefaultCompany\n  productName: Temp\n");
  const result = await inspectUnityProject(root);
  assert.ok(result.problems.includes("companyName must be E-Dawnize"));
  assert.ok(result.problems.includes("productName must be CardGame"));
});

test("reports every pinned Unity host contract value when it changes", async () => {
  const cases = [
    { name: "editor version", apply: (root) => writeFile(path.join(root, "ProjectSettings", "ProjectVersion.txt"), "m_EditorVersion: 6000.3.9f1\n"), problem: "Unity editor version must be 6000.3.10f1" },
    ...Object.entries(packages).map(([name, version]) => ({ name, apply: (root) => writeManifest(root, name), problem: `${name} must be pinned to ${version}` })),
    { name: "application identifier", apply: (root) => writeSettings(root, "com.example.cardgame"), problem: "Standalone application identifier must be com.edawnize.cardgame" },
    { name: "Bootstrap scene file", apply: (root) => rm(path.join(root, "Assets", "CardGame", "Scenes", "Bootstrap.unity")), problem: "Assets/CardGame/Scenes/Bootstrap.unity is missing" }
  ];
  for (const contractCase of cases) {
    const root = await createFixture();
    await contractCase.apply(root);
    const result = await inspectUnityProject(root);
    assert.ok(result.problems.includes(contractCase.problem), contractCase.name);
  }
});

test("uses the first enabled build scene instead of the first listed scene", async () => {
  const root = await createFixture();
  await writeScenes(root, [
    { enabled: 0, path: "Assets/CardGame/Scenes/Disabled.unity" },
    { enabled: 1, path: "Assets/CardGame/Scenes/Bootstrap.unity" }
  ]);
  const result = await inspectUnityProject(root);
  assert.deepEqual(result.problems, []);
  assert.equal(result.info.firstScene, "Assets/CardGame/Scenes/Bootstrap.unity");
});

test("rejects Bootstrap when it is disabled before another enabled scene", async () => {
  const root = await createFixture();
  await writeScenes(root, [
    { enabled: 0, path: "Assets/CardGame/Scenes/Bootstrap.unity" },
    { enabled: 1, path: "Assets/CardGame/Scenes/Gameplay.unity" }
  ]);
  const result = await inspectUnityProject(root);
  assert.ok(result.problems.includes("first enabled scene must be Assets/CardGame/Scenes/Bootstrap.unity"));
});

test("CLI validates the fixture passed through --root", async () => {
  const root = await createFixture();
  const cliPath = path.resolve("scripts/harness/verify-unity-project.mjs");
  const result = spawnSync(process.execPath, [cliPath, "--root", root], { encoding: "utf8", cwd: path.resolve() });
  assert.equal(result.status, 0);
  assert.match(result.stdout, /CardGame Unity project host is valid/);
  assert.equal(result.stderr, "");
});
