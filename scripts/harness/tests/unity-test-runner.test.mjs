import assert from "node:assert/strict";
import { mkdtemp, writeFile } from "node:fs/promises";
import { writeFileSync } from "node:fs";
import os from "node:os";
import path from "node:path";
import test from "node:test";

import { runUnityEditMode } from "../verify.mjs";

function passedResults(total = 3) {
  return `<test-run result="Passed" total="${total}" failed="0" />`;
}

async function resultPathFor(name) {
  const directory = await mkdtemp(path.join(os.tmpdir(), "cardgame-unity-runner-"));
  return path.join(directory, `${name}.xml`);
}

test("runs EditMode against the requested project and accepts a populated passing result", async () => {
  const resultPath = await resultPathFor("passing");
  let invocation;
  const outcome = await runUnityEditMode({
    editor: "fake-unity",
    projectPath: "D:/CardGame/host",
    resultPath,
    runner: (command, args) => {
      invocation = { command, args };
      writeFileSync(args[args.indexOf("-testResults") + 1], passedResults());
      return { status: 0 };
    }
  });

  assert.equal(outcome.ok, true, outcome.message);
  assert.equal(invocation.command, "fake-unity");
  assert.deepEqual(
    invocation.args.slice(invocation.args.indexOf("-projectPath"), invocation.args.indexOf("-projectPath") + 2),
    ["-projectPath", "D:/CardGame/host"]
  );
  assert.ok(invocation.args.includes("-runTests"));
  assert.ok(invocation.args.includes("-testResults"));
  assert.equal(invocation.args.includes("-quit"), false);
});

test("rejects exit zero when the runner leaves no fresh result XML", async () => {
  const resultPath = await resultPathFor("missing");
  await writeFile(resultPath, passedResults());
  const outcome = await runUnityEditMode({
    editor: "fake-unity",
    projectPath: "D:/CardGame/host",
    resultPath,
    runner: () => ({ status: 0 })
  });

  assert.equal(outcome.ok, false);
  assert.match(outcome.message, /result XML is missing/);
});

test("rejects a failing Unity result XML even when the process exits zero", async () => {
  const resultPath = await resultPathFor("failing");
  const outcome = await runUnityEditMode({
    editor: "fake-unity",
    projectPath: "D:/CardGame/host",
    resultPath,
    runner: (_command, args) => {
      writeFileSync(
        args[args.indexOf("-testResults") + 1],
        '<test-run result="Failed" total="3" failed="1" />'
      );
      return { status: 0 };
    }
  });

  assert.equal(outcome.ok, false);
  assert.match(outcome.message, /reported 1 failed test/);
});

test("rejects a passing Unity result XML that contains zero tests", async () => {
  const resultPath = await resultPathFor("zero-tests");
  const outcome = await runUnityEditMode({
    editor: "fake-unity",
    projectPath: "D:/CardGame/host",
    resultPath,
    runner: (_command, args) => {
      writeFileSync(args[args.indexOf("-testResults") + 1], passedResults(0));
      return { status: 0 };
    }
  });

  assert.equal(outcome.ok, false);
  assert.match(outcome.message, /reported zero tests/);
});

test("reports actionable runner failures before attempting to read XML", async () => {
  const resultPath = await resultPathFor("runner-failures");
  const cases = [
    { result: { error: new Error("not found") }, expected: /could not start/ },
    { result: { signal: "SIGTERM" }, expected: /ended from signal SIGTERM/ },
    { result: { status: 7 }, expected: /exit code 7/ }
  ];

  for (const { result, expected } of cases) {
    const outcome = await runUnityEditMode({
      editor: "fake-unity",
      projectPath: "D:/CardGame/host",
      resultPath,
      runner: () => result
    });
    assert.equal(outcome.ok, false);
    assert.match(outcome.message, expected);
  }
});
