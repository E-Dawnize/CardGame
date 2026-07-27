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

function firstEnabledScene(text) {
  const scenes = [];
  let scene;

  for (const line of text.split(/\r?\n/)) {
    const enabled = line.match(/^\s*-\s+enabled:\s*(\d+)\s*$/);
    if (enabled) {
      if (scene) scenes.push(scene);
      scene = { enabled: enabled[1] === "1", path: "" };
      continue;
    }

    const scenePath = line.match(/^\s*path:\s*(.+)$/);
    if (scene && scenePath) scene.path = scenePath[1].trim();
  }

  if (scene) scenes.push(scene);
  return scenes.find((candidate) => candidate.enabled)?.path ?? "";
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
    firstScene: firstEnabledScene(buildText)
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
