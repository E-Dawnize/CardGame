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
  return spawnSync(process.execPath, ["scripts/harness/verify.mjs", ...args], {
    cwd: root,
    encoding: "utf8",
    env: { ...process.env, ...environment }
  });
}

test("portable Harness validates the Unity project host", () => {
  const result = runHarness();
  assert.equal(result.status, 0, result.stderr);
  assert.match(result.stdout, /\[PASS\] CardGame Unity project host validated/);
});

test("full Harness invokes the configured editor without requiring an override project", () => {
  const result = runHarness(["--full"], { UNITY_EDITOR: process.execPath });
  assert.equal(result.status, 1);
  assert.doesNotMatch(result.stdout, /RAZOR_UNITY_PROJECT/);
  assert.match(result.stderr, /Unity EditMode runner failed with exit code/);
});
