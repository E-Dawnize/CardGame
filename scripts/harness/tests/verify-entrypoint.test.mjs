import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { rm, writeFile } from "node:fs/promises";
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

async function withTemporaryDiSource(fileName, source, action) {
  const filePath = path.join(
    root,
    "Assets",
    "Plugins",
    "RazorFramework",
    "DI",
    fileName
  );
  await writeFile(filePath, source, "utf8");
  try {
    return await action();
  } finally {
    await rm(filePath, { force: true });
  }
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

test("portable Harness rejects global and alias UnityEngine imports in DI core", async () => {
  await withTemporaryDiSource(
    "Task7GlobalUnityFixture.cs",
    "global using UnityEngine;\nnamespace RazorFramework.DI { internal sealed class Task7GlobalUnityFixture {} }\n",
    async () => {
      await withTemporaryDiSource(
        "Task7AliasUnityFixture.cs",
        "using UE = UnityEngine;\nnamespace RazorFramework.DI { internal sealed class Task7AliasUnityFixture {} }\n",
        () => {
          return withTemporaryDiSource(
            "Task7DirectUnityFixture.cs",
            "using UnityEngine;\nnamespace RazorFramework.DI { internal sealed class Task7DirectUnityFixture {} }\n",
            () => {
              const result = runHarness();
              assert.notEqual(result.status, 0);
              assert.match(
                result.stderr,
                /Task7GlobalUnityFixture\.cs violates the RazorFramework\.DI BCL-only boundary/
              );
              assert.match(
                result.stderr,
                /Task7AliasUnityFixture\.cs violates the RazorFramework\.DI BCL-only boundary/
              );
              assert.match(
                result.stderr,
                /Task7DirectUnityFixture\.cs violates the RazorFramework\.DI BCL-only boundary/
              );
            }
          );
        }
      );
    }
  );
});

test("portable Harness rejects a foreign namespace in DI core", async () => {
  await withTemporaryDiSource(
    "Task7ForeignNamespaceFixture.cs",
    "namespace RazorFramework.Foreign;\ninternal sealed class Task7ForeignNamespaceFixture {}\n",
    () => {
      const result = runHarness();
      assert.notEqual(result.status, 0);
      assert.match(
        result.stderr,
        /Assets\/Plugins\/RazorFramework\/DI\/Task7ForeignNamespaceFixture\.cs is outside namespace RazorFramework\.DI/
      );
    }
  );
});

test("portable Harness rejects a DI source file with a mixed foreign namespace", async () => {
  await withTemporaryDiSource(
    "Task7MixedNamespaceFixture.cs",
    "namespace RazorFramework.DI.Internal { internal sealed class Task7AllowedNamespaceFixture {} }\nnamespace RazorFramework . Foreign { internal sealed class Task7ForeignNamespaceFixture {} }\n",
    () => {
      const result = runHarness();
      assert.notEqual(result.status, 0);
      assert.match(
        result.stderr,
        /Task7MixedNamespaceFixture\.cs is outside namespace RazorFramework\.DI/
      );
    }
  );
});

test("portable Harness ignores namespace-looking comments and strings in DI core", async () => {
  await withTemporaryDiSource(
    "Task7NamespaceTriviaFixture.cs",
    "namespace RazorFramework.DI;\n// namespace RazorFramework.Foreign;\ninternal sealed class Task7NamespaceTriviaFixture { private const string Text = \"namespace RazorFramework.Foreign;\"; }\n",
    () => {
      const result = runHarness();
      assert.equal(result.status, 0, result.stderr);
      assert.match(
        result.stdout,
        /\[PASS\] RazorFramework\.DI pure-C# boundary validated/
      );
    }
  );
});

test("portable Harness ignores Unity tokens in DI comments and ordinary strings", async () => {
  await withTemporaryDiSource(
    "Task7UnityTriviaFixture.cs",
    "namespace RazorFramework.DI;\n// UnityEngine.Object\ninternal sealed class Task7UnityTriviaFixture { private const string Text = \"UnityEngine.Object\"; }\n",
    () => {
      const result = runHarness();
      assert.equal(result.status, 0, result.stderr);
    }
  );
});

test("portable Harness rejects Unity tokens in every supported interpolation form", async () => {
  await withTemporaryDiSource(
    "Task7InterpolatedUnityFixture.cs",
    "namespace RazorFramework.DI;\ninternal sealed class Task7InterpolatedUnityFixture {\n  private string Regular => $\"{typeof(UnityEngine.Object)}\";\n  private string VerbatimDollarFirst => $@\"{typeof(UnityEngine.Object)}\";\n  private string VerbatimAtFirst => @$\"{typeof(UnityEngine.Object)}\";\n  private string Raw => $\"\"\"{typeof(UnityEngine.Object)}\"\"\";\n}\n",
    () => {
      const result = runHarness();
      assert.notEqual(result.status, 0);
      assert.match(
        result.stderr,
        /Task7InterpolatedUnityFixture\.cs violates the RazorFramework\.DI BCL-only boundary/
      );
    }
  );
});

test("portable Harness accepts a file-scoped RazorFramework.DI namespace", async () => {
  await withTemporaryDiSource(
    "Task7FileScopedNamespaceFixture.cs",
    "namespace RazorFramework.DI;\ninternal sealed class Task7FileScopedNamespaceFixture {}\n",
    () => {
      const result = runHarness();
      assert.equal(result.status, 0, result.stderr);
      assert.match(
        result.stdout,
        /\[PASS\] RazorFramework\.DI pure-C# boundary validated/
      );
    }
  );
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
