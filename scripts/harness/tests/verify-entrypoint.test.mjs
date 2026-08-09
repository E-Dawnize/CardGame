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

function runHarness(args = [], environment = {}) {
  const env = { ...process.env, ...environment };
  for (const [name, value] of Object.entries(env)) {
    if (value === undefined) delete env[name];
  }
  return spawnSync(process.execPath, ["scripts/harness/verify.mjs", ...args], {
    cwd: root,
    encoding: "utf8",
    env
  });
}

test("portable Harness validates the Unity project host", () => {
  const result = runHarness();
  assert.equal(result.status, 0, result.stderr);
  assert.match(result.stdout, /\[PASS\] CardGame Unity project host validated/);
});

test("portable Harness validates the migrated pure-C# DI boundary", () => {
  const result = runHarness();
  assert.equal(result.status, 0, result.stderr);
  assert.match(
    result.stdout,
    /\[PASS\] RazorFramework\.DI pure-C# boundary validated/
  );
  assert.doesNotMatch(result.stdout, /Known DI Unity null-guard debt/);
});

test("full Harness invokes the configured editor without requiring an override project", () => {
  const result = runHarness(["--full"], { UNITY_EDITOR: process.execPath });
  assert.equal(result.status, 1);
  assert.doesNotMatch(result.stdout, /RAZOR_UNITY_PROJECT/);
  assert.match(result.stderr, /Unity EditMode runner failed with exit code/);
});

test("full Harness fails when UNITY_EDITOR is missing", () => {
  const result = runHarness(["--full"], {
    UNITY_EDITOR: undefined,
    RAZOR_UNITY_PROJECT: undefined
  });
  assert.notEqual(result.status, 0);
  assert.match(result.stderr, /\[FAIL\] UNITY_EDITOR is required for --full/);
  assert.match(result.stderr, /set UNITY_EDITOR to the Unity 6000\.3\.10f1 executable/);
});
