import assert from "node:assert/strict";
import { mkdtemp, writeFile } from "node:fs/promises";
import { writeFileSync } from "node:fs";
import os from "node:os";
import path from "node:path";
import test from "node:test";

import { runUnityEditMode, validateUnityTestResults } from "../verify.mjs";

function unityResults({
  result = "Passed",
  total = 3,
  passed = 3,
  failed = 0,
  inconclusive = 0,
  skipped = 0,
  extraRootAttributes = "",
  body = [
    '  <test-suite type="TestSuite" name="CardGame">',
    '    <test-case id="1001" name="Foundation" result="Passed" />',
    "  </test-suite>"
  ],
  close = true
} = {}) {
  const opening = [
    '<?xml version="1.0" encoding="utf-8"?>',
    `<test-run id="2" testcasecount="${total}" result="${result}"${extraRootAttributes} total="${total}" passed="${passed}" failed="${failed}" inconclusive="${inconclusive}" skipped="${skipped}" asserts="0" engine-version="3.5.0.0" duration="0.02">`,
    ...body
  ].join("\n");
  return close ? `${opening}\n</test-run>\n` : opening;
}

async function resultPathFor(name) {
  const directory = await mkdtemp(path.join(os.tmpdir(), "cardgame-unity-runner-"));
  return path.join(directory, `${name}.xml`);
}

test("runs EditMode against the requested project and accepts a complete passing result", async () => {
  const resultPath = await resultPathFor("passing");
  let invocation;
  const outcome = await runUnityEditMode({
    editor: "fake-unity",
    projectPath: "D:/CardGame/host",
    resultPath,
    runner: (command, args) => {
      invocation = { command, args };
      writeFileSync(args[args.indexOf("-testResults") + 1], unityResults());
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

test("accepts the nested structure used by a real Unity result", () => {
  const xml = [
    '<?xml version="1.0" encoding="utf-8"?>',
    '<test-run id="2" testcasecount="3" result="Passed" total="3" passed="3" failed="0" inconclusive="0" skipped="0" asserts="0" duration="0.02">',
    '  <!-- Unity Test Framework result -->',
    '  <test-suite type="TestSuite" name="CardGame">',
    '    <?unity-test result="complete"?>',
    '    <test-case id="1001" name="Foundation"><output><![CDATA[ok <done>]]></output></test-case>',
    '    <test-case id="1002" name="Scene" />',
    '  </test-suite>',
    '</test-run>'
  ].join("\n");
  const outcome = validateUnityTestResults(xml);
  assert.equal(outcome.ok, true, outcome.message);
  assert.equal(outcome.total, 3);
  assert.equal(outcome.passed, 3);
});

test("rejects exit zero when the runner leaves no fresh result XML", async () => {
  const resultPath = await resultPathFor("missing");
  await writeFile(resultPath, unityResults());
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
        unityResults({ result: "Failed", passed: 2, failed: 1 })
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
      writeFileSync(args[args.indexOf("-testResults") + 1], unityResults({ total: 0, passed: 0 }));
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

test("rejects truncated or malformed test-run roots", () => {
  const cases = [
    { xml: unityResults({ close: false }), expected: /well-formed|unclosed/i },
    { xml: unityResults().replace("</test-run>", "</test-ran>"), expected: /well-formed|mismatched/i }
  ];
  for (const { xml, expected } of cases) {
    const outcome = validateUnityTestResults(xml);
    assert.equal(outcome.ok, false);
    assert.match(outcome.message, expected);
  }
});

test("rejects passed zero and contradictory result counts", () => {
  const cases = [
    { xml: unityResults({ total: 3, passed: 0, skipped: 3 }), expected: /reported zero passed tests/ },
    { xml: unityResults({ total: 3, passed: 2 }), expected: /counts are inconsistent/ }
  ];
  for (const { xml, expected } of cases) {
    const outcome = validateUnityTestResults(xml);
    assert.equal(outcome.ok, false);
    assert.match(outcome.message, expected);
  }
});
test("rejects an unclosed nested test-suite", () => {
  const outcome = validateUnityTestResults(unityResults({ body: ["  <test-suite>"] }));
  assert.equal(outcome.ok, false);
  assert.match(outcome.message, /well-formed|mismatched|unclosed/i);
});

test("rejects mismatched nested closing tags", () => {
  const outcome = validateUnityTestResults(unityResults({
    body: ["  <test-suite>", "  </test-case>"]
  }));
  assert.equal(outcome.ok, false);
  assert.match(outcome.message, /well-formed|mismatched/i);
});

test("rejects two concatenated test-run roots", () => {
  const secondRoot = unityResults().replace(/^<\?xml[^>]+>\n/, "");
  const outcome = validateUnityTestResults(unityResults() + secondRoot);
  assert.equal(outcome.ok, false);
  assert.match(outcome.message, /one root|multiple root|outside the root/i);
});

test("rejects duplicate result attributes", () => {
  const outcome = validateUnityTestResults(unityResults({ extraRootAttributes: ' result="Passed"' }));
  assert.equal(outcome.ok, false);
  assert.match(outcome.message, /duplicate attribute/i);
});

test("rejects adjacent counts above Number.MAX_SAFE_INTEGER", () => {
  const outcome = validateUnityTestResults(unityResults({
    total: "9007199254740993",
    passed: "9007199254740992",
    skipped: 1
  }));
  assert.equal(outcome.ok, false);
  assert.match(outcome.message, /safe integer|too large/i);
});

test("rejects a 400 digit count before numeric conversion", () => {
  const huge = "9".repeat(400);
  const outcome = validateUnityTestResults(unityResults({ total: huge, passed: huge }));
  assert.equal(outcome.ok, false);
  assert.match(outcome.message, /safe integer|too large/i);
});

test("rejects Infinity as a result count", () => {
  const outcome = validateUnityTestResults(unityResults({ total: "Infinity", passed: "Infinity" }));
  assert.equal(outcome.ok, false);
  assert.match(outcome.message, /non-negative decimal integer/i);
});
