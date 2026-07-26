# RazorFramework Harness

This repository uses the five-subsystem model from
`walkinglabs/learn-harness-engineering`: instructions, state, verification,
scope, and lifecycle. The goal is repeatable agent work, not a larger prompt.

## Artifact Map

| Subsystem | Repository artifact | Responsibility |
|---|---|---|
| Instructions | `AGENTS.md` | Startup route, architecture invariants, done gate |
| State | `feature_list.json`, `progress.md` | Durable scope, status, evidence, next action |
| Verification | `scripts/harness/verify.mjs`, `init.ps1`, `init.sh` | Portable baseline and optional Unity checks |
| Scope | Feature dependencies and `doneCriteria` | One active feature and explicit closure |
| Lifecycle | `session-handoff.md` | Restartable end-of-session state |

`feature_list.schema.json` documents the state-file shape. The verifier also
checks dependency references, cycles, statuses, evidence for completed work,
and the one-active-feature rule.

## Normal Startup

From the repository root:

```text
node scripts/harness/verify.mjs
```

Then read the state artifacts and select at most one feature with status
`in-progress`. On Windows, `.\init.ps1` is a convenience wrapper. On POSIX
or Git Bash, use `./init.sh`.

The scripts install nothing, change no project files, and use only Node.js
built-in modules.

## Verification Levels

### Portable baseline

`node scripts/harness/verify.mjs` checks:

- required Harness artifacts;
- valid and coherent feature state;
- the complete pinned Superpowers skill set;
- module namespaces and the pure-C# DI/ViewModel boundaries;
- `git diff --check`.

This is a repository and policy check. It cannot prove that Unity-dependent
code compiles.


### Known baseline warning

`DI/DIContainer.cs` currently uses one direct `UnityEngine.Object` check to
handle Unity's destroyed-object null semantics. This conflicts with the target
BCL-only DI boundary and is tracked by feat-002. The verifier allows only that
exact known guard as a warning; any additional Unity reference in `DI/` fails.

### Full Unity verification

This checkout is source-only. A consuming Unity project must contain the
current framework source (normally at `Assets/Plugins/RazorFramework/`) and its
tests. Set:

- `UNITY_EDITOR` to the Unity executable;
- `RAZOR_UNITY_PROJECT` to that consuming project.

Then run:

```text
node scripts/harness/verify.mjs --full
```

The command runs available `.NET` tests and Unity EditMode tests in batch mode.
Until feat-004 establishes that host, Unity-dependent changes must be handed
off with the missing compile/test evidence stated explicitly.

## Superpowers Integration

The project vendors the official `obra/superpowers` skill library under
`.agents/skills/`:

- release: `v6.2.0`;
- commit: `3dcbd5c4b48e02263fbf4a3c01e3fe4f81d584d9`;
- license: MIT, copied to `third_party/superpowers/LICENSE`.

Project-local vendoring makes skill behavior reproducible across contributors.
Codex discovers new project skills when a new task starts. The official Codex
plugin remains an optional personal installation; it is not required by this
repository.

To update the library:

1. Pick and record an upstream tag and commit.
2. Install all skill directories into a temporary destination with the Codex
   skill installer.
3. Compare the temporary tree with `.agents/skills/`.
4. Review upstream release notes and any changed safety or tool assumptions.
5. Replace only the explicit skill directories after review.
6. Update `third_party/superpowers/SOURCE.json`, keep the upstream license, and
   run the portable verifier.

Do not track an unpinned branch or overwrite the library without reviewing the
diff.

## State Transitions

Allowed statuses are `not-started`, `in-progress`, `blocked`, and `done`.

- Start only when dependencies are done or the exception is recorded.
- Keep at most one feature `in-progress`.
- Use `blocked` only with a concrete blocker and next unblocking action.
- Use `done` only with exact commands/results in `evidence`.

At session end, update `progress.md` for historical continuity and overwrite
`session-handoff.md` with the latest concise restart instructions.

## Security and Tooling

- No project hook executes automatically.
- `.codex/config.toml` enables multi-agent capability for Superpowers workflows,
  but `AGENTS.md`, runtime policy, and user authorization still govern use.
- Never store credentials in state, instructions, skills, or command examples.
- Destructive operations and external side effects remain explicit user
  decisions.
