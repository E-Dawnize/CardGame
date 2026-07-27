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
