#!/usr/bin/env node

import { spawnSync } from "node:child_process";
import { randomUUID } from "node:crypto";
import { readFile, readdir, rm } from "node:fs/promises";
import os from "node:os";
import path from "node:path";
import process from "node:process";
import { fileURLToPath } from "node:url";

import { inspectUnityProject } from "./unity-project.mjs";
import { parseXmlDocument } from "./xml.mjs";

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..", "..");
const FULL = process.argv.includes("--full");
const failures = [];
const warnings = [];
let passed = 0;

function pass(message) {
  passed += 1;
  console.log("[PASS] " + message);
}

function warn(message) {
  warnings.push(message);
  console.log("[WARN] " + message);
}

function fail(message) {
  failures.push(message);
  console.error("[FAIL] " + message);
}

async function text(relativePath) {
  return readFile(path.join(ROOT, relativePath), "utf8");
}

async function requireFile(relativePath) {
  try {
    const value = await text(relativePath);
    if (!value.trim()) {
      fail(relativePath + " is empty");
      return;
    }
    pass(relativePath + " exists");
  } catch {
    fail(relativePath + " is missing");
  }
}

async function walk(directory, suffix) {
  const files = [];
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    const absolute = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      files.push(...await walk(absolute, suffix));
    } else if (entry.isFile() && absolute.endsWith(suffix)) {
      files.push(absolute);
    }
  }
  return files;
}

function maskedCharacter(character) {
  return character === "\r" || character === "\n" ? character : " ";
}

function maskCSharpCommentsAndStrings(source) {
  let masked = "";
  let index = 0;

  while (index < source.length) {
    const current = source[index];
    const next = source[index + 1];
    if (current === "/" && next === "/") {
      masked += "  ";
      index += 2;
      while (index < source.length && source[index] !== "\r" && source[index] !== "\n") {
        masked += " ";
        index += 1;
      }
      continue;
    }
    if (current === "/" && next === "*") {
      masked += "  ";
      index += 2;
      while (index < source.length) {
        if (source[index] === "*" && source[index + 1] === "/") {
          masked += "  ";
          index += 2;
          break;
        }
        masked += maskedCharacter(source[index]);
        index += 1;
      }
      continue;
    }

    const verbatimString = current === "@" && next === "\"";
    if (current === "\"" || verbatimString) {
      const openingLength = verbatimString ? 2 : 1;
      for (let offset = 0; offset < openingLength; offset += 1) {
        masked += " ";
      }
      index += openingLength;
      while (index < source.length) {
        if (verbatimString && source[index] === "\"" && source[index + 1] === "\"") {
          masked += "  ";
          index += 2;
          continue;
        }
        if (source[index] === "\"") {
          masked += " ";
          index += 1;
          break;
        }
        if (!verbatimString && source[index] === "\\" && index + 1 < source.length) {
          masked += "  ";
          index += 2;
          continue;
        }
        masked += maskedCharacter(source[index]);
        index += 1;
      }
      continue;
    }

    if (current === "'") {
      masked += " ";
      index += 1;
      while (index < source.length) {
        if (source[index] === "\\" && index + 1 < source.length) {
          masked += "  ";
          index += 2;
          continue;
        }
        masked += maskedCharacter(source[index]);
        if (source[index] === "'") {
          index += 1;
          break;
        }
        index += 1;
      }
      continue;
    }

    masked += current;
    index += 1;
  }

  return masked;
}

function extractCSharpNamespaceDeclarations(source) {
  const namespacePattern =
    /\bnamespace\s+([A-Za-z_]\w*(?:\s*\.\s*[A-Za-z_]\w*)*)\s*(?=\{|;)/g;
  return [
    ...maskCSharpCommentsAndStrings(source).matchAll(namespacePattern)
  ].map((match) => match[1].replace(/\s+/g, ""));
}

function extractCSharpInterpolationExpressions(source) {
  const expressions = [];
  for (let index = 0; index < source.length; index += 1) {
    let cursor = index;
    let dollars = 0;
    let verbatim = false;
    if (source[cursor] === "@") {
      verbatim = true;
      cursor += 1;
    }
    while (source[cursor] === "$") {
      dollars += 1;
      cursor += 1;
    }
    if (dollars === 0) continue;
    if (source[cursor] === "@") {
      verbatim = true;
      cursor += 1;
    }
    if (source[cursor] !== "\"") continue;

    let quotes = 0;
    while (source[cursor + quotes] === "\"") quotes += 1;
    const raw = quotes >= 3;
    const brace = "{".repeat(dollars);
    const closeBrace = "}".repeat(dollars);
    const closeQuote = "\"".repeat(raw ? quotes : 1);
    cursor += quotes;

    while (cursor < source.length && !source.startsWith(closeQuote, cursor)) {
      if (source.startsWith(brace, cursor)) {
        const expressionStart = cursor + brace.length;
        let depth = 0;
        cursor = expressionStart;
        while (cursor < source.length) {
          if (source[cursor] === "{") depth += 1;
          if (source[cursor] === "}") {
            if (depth === 0 && source.startsWith(closeBrace, cursor)) break;
            if (depth > 0) depth -= 1;
          }
          cursor += 1;
        }
        if (!source.startsWith(closeBrace, cursor)) break;
        expressions.push(source.slice(expressionStart, cursor));
        cursor += closeBrace.length;
      } else if (!raw && !verbatim && source[cursor] === "\\") {
        cursor += 2;
      } else if (!raw && source[cursor] === "\"" && source[cursor + 1] === "\"") {
        cursor += 2;
      } else {
        cursor += 1;
      }
    }
    index = cursor + closeQuote.length - 1;
  }
  return expressions;
}

function containsQualifiedUnityReference(source) {
  const qualifiedUnity = /\bUnityEngine\s*\.\s*[A-Za-z_]\w*/;
  if (qualifiedUnity.test(maskCSharpCommentsAndStrings(source))) return true;
  return extractCSharpInterpolationExpressions(source).some((expression) =>
    qualifiedUnity.test(maskCSharpCommentsAndStrings(expression))
  );
}

async function checkDurableDiDocuments() {
  const documents = [
    "docs/superpowers/specs/2026-08-04-di-v2-design.md",
    "docs/superpowers/plans/2026-08-04-di-v2.md"
  ];
  let valid = 0;
  for (const relative of documents) {
    let value;
    try {
      value = await text(relative);
    } catch {
      fail(relative + " is missing");
      continue;
    }

    if (/\?{12,}/.test(value) || /\uFFFD{3,}/.test(value)) {
      fail(relative + " contains a suspicious run of replacement question marks");
      continue;
    }

    const chineseCharacters = value.match(/[\u3400-\u4DBF\u4E00-\u9FFF]/g) ?? [];
    if (chineseCharacters.length < 20) {
      fail(relative + " contains too little Chinese text for a durable Chinese design document");
      continue;
    }
    valid += 1;
  }

  if (valid === documents.length) {
    pass("Durable DI design documents passed corruption checks");
  }
}

function checkFeatureState(state) {
  if (!state || !Array.isArray(state.features)) {
    fail("feature_list.json must contain a features array");
    return;
  }

  const allowed = new Set(["not-started", "in-progress", "blocked", "done"]);
  const ids = new Set();
  const byId = new Map();

  for (const feature of state.features) {
    if (!/^feat-\d{3}$/.test(feature.id || "")) {
      fail("Invalid feature id: " + (feature.id || "<missing>"));
      continue;
    }
    if (ids.has(feature.id)) {
      fail("Duplicate feature id: " + feature.id);
    }
    ids.add(feature.id);
    byId.set(feature.id, feature);

    if (!feature.name || !feature.description) {
      fail(feature.id + " needs a name and description");
    }
    if (!Array.isArray(feature.dependencies)) {
      fail(feature.id + " needs a dependencies array");
    }
    if (!allowed.has(feature.status)) {
      fail(feature.id + " has invalid status " + feature.status);
    }
    if (!Array.isArray(feature.doneCriteria) || feature.doneCriteria.length === 0) {
      fail(feature.id + " needs explicit doneCriteria");
    }
    if (feature.status === "done" && !(feature.evidence || "").trim()) {
      fail(feature.id + " is done without evidence");
    }
  }

  const active = state.features.filter((feature) => feature.status === "in-progress");
  if (active.length > 1) {
    fail("Only one feature may be in-progress; found " + active.length);
  }

  for (const feature of state.features) {
    for (const dependency of feature.dependencies || []) {
      if (!ids.has(dependency)) {
        fail(feature.id + " depends on missing feature " + dependency);
      }
      if (dependency === feature.id) {
        fail(feature.id + " cannot depend on itself");
      }
    }
  }

  const visiting = new Set();
  const visited = new Set();

  function visit(id) {
    if (visiting.has(id)) {
      fail("Feature dependency cycle detected at " + id);
      return;
    }
    if (visited.has(id) || !byId.has(id)) {
      return;
    }
    visiting.add(id);
    for (const dependency of byId.get(id).dependencies || []) {
      visit(dependency);
    }
    visiting.delete(id);
    visited.add(id);
  }

  for (const id of ids) {
    visit(id);
  }

  pass(
    "Feature state parsed (" + state.features.length +
    " features, " + active.length + " in progress)"
  );
}

async function checkSuperpowers() {
  const skills = [
    "brainstorming",
    "dispatching-parallel-agents",
    "executing-plans",
    "finishing-a-development-branch",
    "receiving-code-review",
    "requesting-code-review",
    "subagent-driven-development",
    "systematic-debugging",
    "test-driven-development",
    "using-git-worktrees",
    "using-superpowers",
    "verification-before-completion",
    "writing-plans",
    "writing-skills"
  ];

  let valid = 0;
  for (const skill of skills) {
    const relative = ".agents/skills/" + skill + "/SKILL.md";
    try {
      const value = await text(relative);
      const frontmatter = value.match(/^---\s*\r?\n([\s\S]*?)\r?\n---/);
      const namePattern = new RegExp(
        "^name:\\s*[\"']?" + skill + "[\"']?\\s*$",
        "m"
      );
      if (!frontmatter || !namePattern.test(frontmatter[1])) {
        fail(relative + " has invalid frontmatter or name");
      } else if (!/^description:\s*.+$/m.test(frontmatter[1])) {
        fail(relative + " has no trigger description");
      } else {
        valid += 1;
      }
    } catch {
      fail(relative + " is missing");
    }
  }

  if (valid === skills.length) {
    pass("Superpowers skill library is complete (" + valid + " skills)");
  }

  try {
    const source = JSON.parse(await text("third_party/superpowers/SOURCE.json"));
    const pinned =
      source.version === "6.2.0" &&
      source.commit === "3dcbd5c4b48e02263fbf4a3c01e3fe4f81d584d9";
    if (pinned) {
      pass("Superpowers source metadata is pinned to v6.2.0");
    } else {
      fail("Superpowers source metadata does not match the vendored version");
    }
  } catch {
    fail("Superpowers source metadata is missing or invalid");
  }
}

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

async function checkCSharpBoundaries() {
  const modules = new Map([
    ["Lifecycle", "RazorFramework.Lifecycle"],
    ["Events", "RazorFramework.Events"],
    ["MVVM", "RazorFramework.MVVM"],
    ["Input", "RazorFramework.Input"],
    ["Boot", "RazorFramework.Boot"]
  ]);
  let count = 0;

  for (const [module, namespaceName] of modules) {
    let files;
    try {
      files = await walk(path.join(ROOT, module), ".cs");
    } catch {
      fail("Source module is missing: " + module + "/");
      continue;
    }

    for (const file of files) {
      count += 1;
      const relative = path.relative(ROOT, file).replaceAll("\\", "/");
      const value = await readFile(file, "utf8");
      const code = value
        .replace(/\/\*[\s\S]*?\*\//g, "")
        .replace(/^\s*\/\/.*$/gm, "");
      const namespacePattern = new RegExp(
        "\\bnamespace\\s+" + namespaceName.replaceAll(".", "\\.") + "(?:\\.|\\s*\\{)"
      );
      if (!namespacePattern.test(code)) {
        fail(relative + " is outside namespace " + namespaceName);
      }

      const importsUnity = /^\s*using\s+UnityEngine(?:\.|;)/m.test(code);
      const qualifiedUnity = /\bUnityEngine\.[A-Za-z_]\w*/.test(code);
      const pureMvvm =
        relative.startsWith("MVVM/Commands/") ||
        relative.startsWith("MVVM/ViewModel/");
      if (pureMvvm && (importsUnity || qualifiedUnity)) {
        fail(relative + " violates the pure-C# MVVM boundary");
      }
    }
  }

  if (count > 0) {
    pass("C# module boundaries checked (" + count + " files)");
  }
}

async function checkPureCSharpDiBoundary() {
  const sourceDirectory = path.join(ROOT, "Assets", "Plugins", "RazorFramework", "DI");
  let files;
  try {
    files = await walk(sourceDirectory, ".cs");
  } catch {
    fail("RazorFramework.DI source module is missing: Assets/Plugins/RazorFramework/DI/");
    return;
  }

  if (files.length === 0) {
    fail("RazorFramework.DI source module contains no C# files");
  }

  for (const file of files) {
    const relative = path.relative(ROOT, file).replaceAll("\\", "/");
    const value = await readFile(file, "utf8");
    const code = maskCSharpCommentsAndStrings(value);
    const namespaceDeclarations = extractCSharpNamespaceDeclarations(value);
    if (namespaceDeclarations.length === 0) {
      fail(relative + " is outside namespace RazorFramework.DI");
    }
    for (const namespaceName of namespaceDeclarations) {
      if (
        namespaceName !== "RazorFramework.DI" &&
        !namespaceName.startsWith("RazorFramework.DI.")
      ) {
        fail(relative + " is outside namespace RazorFramework.DI");
      }
    }

    const importsUnity =
      /^\s*(?:global\s+)?using\s+(?:(?:static\s+)?|(?:[A-Za-z_]\w*\s*=\s*))?(?:global::)?UnityEngine(?:\.[A-Za-z_]\w*)*\s*;/m.test(code);
    const qualifiedUnity = containsQualifiedUnityReference(value);
    if (importsUnity || qualifiedUnity) {
      fail(relative + " violates the RazorFramework.DI BCL-only boundary");
    }
  }

  try {
    const assembly = JSON.parse(
      await text("Assets/Plugins/RazorFramework/DI/RazorFramework.DI.asmdef")
    );
    if (assembly.name !== "RazorFramework.DI") {
      fail("RazorFramework.DI.asmdef must name RazorFramework.DI");
    }
    if (assembly.rootNamespace !== "RazorFramework.DI") {
      fail("RazorFramework.DI.asmdef must set rootNamespace to RazorFramework.DI");
    }
    if (assembly.autoReferenced !== true) {
      fail("RazorFramework.DI.asmdef must set autoReferenced to true");
    }
    if (assembly.noEngineReferences !== true) {
      fail("RazorFramework.DI.asmdef must set noEngineReferences to true");
    }
    if (!Array.isArray(assembly.references) || assembly.references.length !== 0) {
      fail("RazorFramework.DI.asmdef must declare no assembly references");
    }
  } catch {
    fail("RazorFramework.DI.asmdef is missing or invalid JSON");
  }

  let legacyFiles = [];
  try {
    legacyFiles = await walk(path.join(ROOT, "DI"), ".cs");
  } catch (error) {
    if (error.code !== "ENOENT") {
      fail("Unable to inspect legacy root DI/: " + error.message);
    }
  }
  for (const file of legacyFiles) {
    const relative = path.relative(ROOT, file).replaceAll("\\", "/");
    fail("Legacy root DI source must be absent: " + relative);
  }
  if (legacyFiles.length === 0) {
    pass("Legacy root DI implementation is absent");
  }

  if (failures.length === 0) {
    pass("RazorFramework.DI pure-C# boundary validated");
  }
}

function run(command, args, capture, timeout) {
  return spawnSync(command, args, {
    cwd: ROOT,
    encoding: "utf8",
    stdio: capture ? "pipe" : "inherit",
    timeout
  });
}

export function buildUnityEditModeArgs(projectPath, resultPath) {
  return [
    "-batchmode",
    "-nographics",
    "-projectPath",
    projectPath,
    "-runTests",
    "-testPlatform",
    "EditMode",
    "-testResults",
    resultPath,
    "-logFile",
    "-"
  ];
}

export function validateUnityTestResults(xml) {
  const document = parseXmlDocument(xml);
  if (!document.ok) {
    return {
      ok: false,
      message: "Unity EditMode result XML is not well-formed: " + document.error
    };
  }
  if (document.root.name !== "test-run") {
    return {
      ok: false,
      message: "Unity EditMode result XML root must be <test-run>, found <" + document.root.name + ">"
    };
  }

  const attributes = document.root.attributes;
  const counts = {};
  const maximum = BigInt(Number.MAX_SAFE_INTEGER);
  for (const name of ["total", "passed", "failed", "inconclusive", "skipped"]) {
    const raw = attributes.get(name);
    if (raw === undefined && (name === "inconclusive" || name === "skipped")) {
      counts[name] = 0n;
      continue;
    }
    if (!/^\d+$/.test(raw ?? "")) {
      return {
        ok: false,
        message: "Unity EditMode result XML attribute " + name +
          " must be a non-negative decimal integer"
      };
    }
    const value = BigInt(raw);
    if (value > maximum) {
      return {
        ok: false,
        message: "Unity EditMode result XML attribute " + name +
          " exceeds Number.MAX_SAFE_INTEGER and is too large to report safely"
      };
    }
    counts[name] = value;
  }

  const result = attributes.get("result") ?? "";
  if (counts.total === 0n) {
    return { ok: false, message: "Unity EditMode reported zero tests; verify the EditMode test assembly is included" };
  }
  if (counts.passed === 0n) {
    return { ok: false, message: "Unity EditMode reported zero passed tests; inspect the result XML" };
  }
  if (counts.failed > 0n) {
    return { ok: false, message: "Unity EditMode reported " + counts.failed + " failed test(s); inspect the result XML" };
  }
  if (result.toLowerCase() !== "passed") {
    return { ok: false, message: "Unity EditMode result is " + (result || "missing") + "; inspect the result XML" };
  }

  const categorized = counts.passed + counts.failed + counts.inconclusive + counts.skipped;
  if (categorized !== counts.total) {
    return {
      ok: false,
      message: "Unity EditMode result counts are inconsistent: total=" + counts.total +
        ", categorized=" + categorized + "; inspect the result XML"
    };
  }

  return {
    ok: true,
    total: Number(counts.total),
    passed: Number(counts.passed),
    failed: Number(counts.failed),
    inconclusive: Number(counts.inconclusive),
    skipped: Number(counts.skipped)
  };
}

export async function runUnityEditMode({
  editor,
  projectPath,
  resultPath = path.join(os.tmpdir(), "CardGame-EditMode-" + process.pid + "-" + randomUUID() + ".xml"),
  runner = run
}) {
  try {
    await rm(resultPath, { force: true });
  } catch (error) {
    return {
      ok: false,
      message: "Could not prepare Unity EditMode result path " + resultPath + ": " + error.message
    };
  }

  const result = runner(
    editor,
    buildUnityEditModeArgs(projectPath, resultPath),
    false,
    30 * 60 * 1000
  );

  if (result.error) {
    return {
      ok: false,
      message: "Unity EditMode runner could not start: " + result.error.message
    };
  }
  if (result.signal) {
    return {
      ok: false,
      message: "Unity EditMode runner ended from signal " + result.signal
    };
  }
  if (result.status !== 0) {
    return {
      ok: false,
      message: "Unity EditMode runner failed with exit code " + (result.status ?? "unknown")
    };
  }

  let xml;
  try {
    xml = await readFile(resultPath, "utf8");
  } catch {
    return {
      ok: false,
      message: "Unity EditMode result XML is missing at " + resultPath + "; inspect Unity output"
    };
  }

  const verdict = validateUnityTestResults(xml);
  return verdict.ok
    ? { ...verdict, resultPath }
    : { ...verdict, resultPath, message: verdict.message + " (" + resultPath + ")" };
}

function checkGitDiff() {
  const result = run("git", ["diff", "--check"], true);
  if (result.error) {
    fail("Unable to run git diff --check: " + result.error.message);
  } else if (result.status !== 0) {
    const details = ((result.stdout || "") + (result.stderr || "")).trim();
    fail("git diff --check failed" + (details ? ":\n" + details : ""));
  } else {
    pass("git diff --check is clean");
  }
}

async function fullChecks() {
  const editor = process.env.UNITY_EDITOR;
  const projectPath = process.env.RAZOR_UNITY_PROJECT || ROOT;
  if (!editor) {
    fail(
      "UNITY_EDITOR is required for --full; " +
      "set UNITY_EDITOR to the Unity 6000.3.10f1 executable"
    );
    return;
  }

  const outcome = await runUnityEditMode({ editor, projectPath });
  if (outcome.ok) {
    pass("Unity EditMode tests passed (" + outcome.total + " tests, " + outcome.resultPath + ")");
  } else {
    fail(outcome.message);
  }
}

async function main() {
  process.chdir(ROOT);
  console.log("CardGame harness verification: " + ROOT);
  console.log("Mode: " + (FULL ? "full" : "portable"));
  console.log("");

  const nodeMajor = Number.parseInt(process.versions.node.split(".")[0], 10);
  nodeMajor >= 18
    ? pass("Node.js " + process.versions.node + " is supported")
    : fail("Node.js 18 or newer is required");

  const required = [
    "AGENTS.md",
    "README.md",
    "DESIGN-REVIEW.md",
    "docs/CONTRACT.md",
    "docs/HARNESS.md",
    "feature_list.json",
    "feature_list.schema.json",
    "progress.md",
    "session-handoff.md",
    "init.sh",
    "init.ps1",
    ".codex/config.toml",
    "third_party/superpowers/LICENSE",
    "third_party/superpowers/SOURCE.json"
  ];
  for (const relative of required) {
    await requireFile(relative);
  }

  try {
    const schema = JSON.parse(await text("feature_list.schema.json"));
    schema?.properties?.features
      ? pass("feature_list.schema.json is valid JSON")
      : fail("feature_list.schema.json has no features definition");
  } catch {
    fail("feature_list.schema.json is not valid JSON");
  }

  try {
    checkFeatureState(JSON.parse(await text("feature_list.json")));
  } catch {
    fail("feature_list.json is not valid JSON");
  }

  await checkSuperpowers();
  await checkDurableDiDocuments();
  await checkUnityProjectHost();
  await checkPureCSharpDiBoundary();
  await checkCSharpBoundaries();
  checkGitDiff();

  if (FULL) {
    await fullChecks();
  } else {
    warn(
      "Unity compilation/tests were not run in portable mode; " +
      "use --full after setting UNITY_EDITOR"
    );
  }

  console.log("");
  console.log(
    "Summary: " + passed + " passed, " + warnings.length +
    " warning(s), " + failures.length + " failure(s)"
  );
  if (failures.length > 0) {
    process.exitCode = 1;
  }
}

const invokedPath = process.argv[1] ? path.resolve(process.argv[1]) : "";
if (invokedPath === fileURLToPath(import.meta.url)) {
  await main();
}
