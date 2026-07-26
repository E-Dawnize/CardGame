# CardGame Project Foundation and Narrative Roguelike Design

**Status:** Approved in conversation; pending repository review  
**Date:** 2026-07-27  
**Target repository:** `D:\Unity Project\CardGame`  
**Reference framework:** Existing RazorFramework source and Harness configuration  

## 1. Purpose

This specification defines the maintainable foundation for CardGame:

- Turn `CardGame` into the real Unity project rather than keeping a separate
  framework-only checkout.
- Migrate only required Unity project files from `D:\Unity Project\Temp`.
- Fully review and refactor the existing RazorFramework before game systems
  depend on it.
- Establish a Slay the Spire-style map and deck-building loop with substantially
  deeper narrative progression.
- Provide non-programmer authoring contracts for narrative collaborators.
- Provide a deterministic simulation and AI-assisted balancing workflow for a
  team without a dedicated numerical designer.
- Persist decisions, evidence, and next steps according to the repository
  Harness rules.

Maintainability takes precedence over preserving existing framework APIs or
minimizing the initial refactor.

## 2. Confirmed Decisions

1. RazorFramework serves CardGame only; it is not maintained as an independent
   Unity package.
2. Framework source will live under `Assets/Plugins/RazorFramework/`.
3. Existing public APIs may be broken or removed.
4. The framework will be comprehensively audited and refactored before it
   becomes the base of game features.
5. The first delivery is a verified project foundation, not a complete combat
   or map implementation.
6. The campaign uses a two-layer narrative map: a branching roguelike route
   combined with persistent narrative state and guaranteed story anchors.
7. Combat uses one protagonist deck plus non-targetable companions.
8. All damage passes through a deterministic and inspectable settlement
   pipeline.
9. Narrative collaborators author structured tables and Markdown, not code or
   Unity object graphs.
10. Balance uses deterministic simulation, constrained optimization, and AI
    analysis. AI does not directly publish production values.
11. Design, implementation state, verification evidence, and handoff state must
    be stored in the repository.

## 3. Scope

### 3.1 Foundation milestone

The first milestone includes:

- Unity project migration and cleanup.
- Repository and assembly layout.
- Framework API redesign and automated tests.
- A minimal Bootstrap scene and application lifecycle.
- Content-source contracts and validation entrypoints.
- Pure C# foundations for deterministic rules, state, save versions, and
  reproducible random number generation.
- Harness updates for the new project layout.

### 3.2 Deferred work

The following are intentionally deferred to later vertical slices:

- A complete card set, enemy roster, act, or campaign.
- Multi-character combat with separate health and turns.
- Real-time combat, speed bars, multiplayer, or competitive modes.
- A custom visual story editor.
- Reinforcement-learning infrastructure.
- AI-generated content or values being published without validation.
- Final theme, cast, setting, plot, and production numerical values.

## 4. Unity Project Migration

`CardGame` becomes the Unity project root.

Migrate from `D:\Unity Project\Temp`:

- `Assets/` content that is required by the selected 2D URP template, preserving
  `.meta` files.
- `Packages/manifest.json`.
- `Packages/packages-lock.json`.
- `ProjectSettings/`.

Do not migrate:

- `Library/`
- `Temp/`
- `Logs/`
- `UserSettings/`
- `.vscode/`
- generated `.sln`, `.slnx`, or `.csproj` files

The source template is Unity `6000.3.10f1` with URP `17.3.0`, Input System
`1.18.0`, and Unity Test Framework `1.6.0`. Package dependencies must be reviewed
after migration; unused template packages are removed deliberately rather than
copied indefinitely.

The template `SampleScene` becomes a minimal `Bootstrap` scene. The template
first-person action map is not treated as the final card-game input contract.

## 5. Repository Layout

```text
CardGame/
├─ Assets/
│  ├─ CardGame/
│  │  ├─ Runtime/
│  │  │  ├─ Domain/
│  │  │  ├─ Application/
│  │  │  ├─ Infrastructure/
│  │  │  └─ Presentation/
│  │  ├─ Content/
│  │  │  └─ Generated/
│  │  ├─ Scenes/
│  │  └─ Tests/
│  └─ Plugins/
│     └─ RazorFramework/
│        ├─ Runtime/
│        ├─ Unity/
│        └─ Tests/
├─ ContentSource/
│  ├─ Tables/
│  ├─ Stories/
│  ├─ Glossary/
│  └─ Templates/
├─ Packages/
├─ ProjectSettings/
├─ docs/
│  ├─ decisions/
│  └─ superpowers/specs/
├─ reports/
│  └─ content-validation/
├─ scripts/
├─ feature_list.json
├─ progress.md
└─ session-handoff.md
```

`ContentSource` is the editable content source of truth.
`Assets/CardGame/Content/Generated` is generated and must not be edited by hand.

## 6. Architecture and Dependency Rules

### 6.1 Framework target modules

The refactored framework is reduced to explicit responsibilities:

- **Core:** identifiers, deterministic time/random abstractions, result types,
  and other minimal BCL-only primitives.
- **DI:** registration, scopes, construction, and disposal without a
  `UnityEngine` dependency.
- **Events:** strongly typed messages and lifecycle-safe subscriptions.
- **Lifecycle:** deterministic initialization, start, pause, resume, and
  shutdown ordering.
- **Presentation:** pure C# observable state and commands, separated from Unity
  binding code.
- **Unity Integration:** MonoBehaviour adapters, scene bootstrapping, Input
  System adapters, and resource-loading adapters.

Existing implementations are requirements evidence, not compatibility
constraints. Each module must justify its continued existence through an
immediate CardGame use case and tests.

### 6.2 Game layers

- **Domain:** pure C# card, battle, map, narrative, and progression rules.
- **Application:** use cases and orchestration over Domain and framework
  abstractions.
- **Infrastructure:** persistence, content loading, telemetry, and Unity-facing
  implementations.
- **Presentation:** Unity scenes, UI, animation, audio, and view binding.

### 6.3 Allowed dependency direction

```text
Unity and Input System
        |
        v
RazorFramework.Unity ------> RazorFramework pure modules
        ^                              ^
        |                              |
CardGame.Presentation ---> CardGame.Application ---> CardGame.Domain
        ^                         ^
        |                         |
CardGame.Infrastructure ----------+
```

Rules:

- Domain does not reference Unity, concrete UI, resource systems, or save
  implementations.
- Game assemblies may reference framework abstractions.
- Framework assemblies never reference CardGame.
- Assembly Definition files enforce the dependency graph.
- Unity object liveness checks belong in Unity adapters, never in pure DI.

## 7. Roguelike and Narrative Structure

### 7.1 Run loop

A run contains three acts. An initial target is approximately 12–15 floors per
act, subject to later simulation and playtesting.

Node categories:

- normal combat
- elite combat
- rest
- shop
- short event
- archive or fragment
- anomaly or condition node
- main-story anchor
- boss

Rewards modify the deck, relic inventory, resources, companion state, and
narrative state. The boss resolves the act conflict and advances the story
state.

### 7.2 Two-layer narrative map

The visible layer is a branching, directed route graph that supports risk and
reward planning. The narrative layer is a persistent state machine that
controls story anchors, conditional nodes, fragments, relationship changes,
and cross-run variations.

The generator places required main-story anchors before filling normal nodes.
Necessary main story must not be hidden behind random generation. Complex
conditions are reserved for optional branches, alternate resolutions, and
hidden endings.

### 7.3 Narrative content levels

1. **Fragment text:** short environmental echoes, records, item descriptions,
   or internal monologue attached to ordinary map interactions.
2. **Conditional events:** choices unlocked by deck, relic, health, companion,
   prior choice, fragment, relationship, or meta-progression state.
3. **Main-story scenes:** longer dialogue and staging at anchors, bosses, or
   strict condition nodes, with outcomes that affect the current run.

Previously read long scenes support summaries and skipping. All discovered
content enters an archive organized by timeline, character, and location.

### 7.4 Narrative state

Narrative state includes:

- chapter and main-story stage
- current-run and persistent choices
- discovered fragments
- typed condition tags and counters
- relationship states
- unlocked nodes and endings
- active world rules or anomalies

Map, battle, and narrative systems exchange explicit commands and results
instead of mutating one another's internal state.

## 8. Combat Model

### 8.1 Core loop

Combat is turn-based with visible enemy intent.

Initial configurable baselines:

- three energy per player turn
- five cards drawn per player turn
- block normally expires at the next player-turn start
- deterministic reshuffling using run random state

These are starting hypotheses, not production balance guarantees.

Combat phases:

1. setup
2. player-turn start
3. player actions
4. enemy turn
5. round end
6. victory or defeat

### 8.2 Cards and effects

Card categories:

- attack
- skill
- power
- status
- curse
- temporary narrative card

Cards compose reusable effects rather than requiring one C# class per card.
Initial effect vocabulary includes:

- deal damage
- gain block
- draw cards
- apply status
- change energy
- move cards between zones
- repeat conditionally
- select targets
- create temporary cards
- change companion charge

New code-level mechanics are added only when the effect vocabulary cannot
express a required rule safely.

### 8.3 Commands and execution

Player decisions become explicit commands:

- `PlayCard`
- `EndTurn`
- `UseCompanionAbility`
- `ChooseTarget`
- `ChooseGeneratedCard`

Commands produce ordered effects and immutable result records. Presentation
reads results and plays visuals; it does not determine game outcomes.

This model must support Unity presentation, headless simulation, deterministic
replay, save/resume, and inspectable error reports.

## 9. Companion Model

The protagonist owns the deck and is the only standard player combatant.
Companions are not independently targetable.

Each companion may provide:

- one persistent passive
- one charged assist ability
- narrative and map tags
- companion-specific cards, events, or card transformations
- relationship stages and corresponding variations

The architecture may support two slots, but the first playable vertical slice
implements one active companion slot to control balance complexity.

Companions connect story and construction without turning combat into a
multi-character health and turn-order system.

## 10. Damage Settlement Pipeline

No card, enemy, status, companion, or narrative rule may directly mutate
health. It submits a `DamageRequest` to the settlement pipeline.

### 10.1 Damage request

A request records:

- source and target
- base amount
- source card, skill, or status ID
- semantic tags such as `Attack`, `Skill`, `Status`, and `Companion`
- single-target, area, or multi-hit tags
- hit index
- causal chain ID
- allowed reaction categories

### 10.2 Ordered stages

1. Validate source, target, death state, immunity, and cancellation.
2. Apply outgoing source modifiers.
3. Convert or add semantic damage tags.
4. Apply incoming target modifiers.
5. Apply caps, reductions, barriers, and block in fixed order.
6. Apply the resulting health change.
7. create an immutable `DamageResult`.
8. enqueue lifesteal, retaliation, on-damage, companion charge, and death
   reactions.

The pipeline distinguishes:

- **Damage:** uses the full pipeline.
- **HealthLoss:** bypasses block and is not treated as damage for reactions.
- **SetHealth:** reserved for system-level operations.

### 10.3 Determinism and safety

- Use fixed-point integer multipliers, not floating point.
- Combine multipliers and round only at a documented stage.
- Sort rules by stage priority and stable ID.
- Resolve each multi-hit segment independently.
- Check death after every segment.
- Do not attack dead targets unless an explicit rule says otherwise.
- Queue reactions instead of recursively mutating health.
- Enforce causal-depth and effect-count limits.
- Record every modifier and absorption in the battle log.

## 11. Non-Programmer Narrative Authoring

Narrative collaborators do not edit C#, boolean expressions, Unity references,
or generated files.

### 11.1 Authoring sources

- CSV tables contain IDs, tags, conditions, relationships, choices, and
  references.
- Markdown files contain dialogue, narration, scene descriptions, and long
  fragments.
- Controlled glossaries define characters, terminology, and forbidden early
  reveals.

### 11.2 Scene template

Every story scene specifies:

- purpose and content category
- what the player already knows
- required information or emotional outcome
- cast and current relationship assumptions
- opening, development, turn, choice, and result
- apparent and mechanical meaning of each choice
- first-read version
- replay summary
- reusable fragments
- forbidden revelations
- recommended word count and reading time

### 11.3 Controlled conditions

Writers select conditions from a controlled vocabulary such as:

- owns relic
- discovered fragment
- chapter stage
- relationship threshold
- previous run choice

The content compiler, not the writer, converts these declarations into runtime
conditions.

### 11.4 Validation

The compiler reports:

- duplicate or missing IDs
- missing characters, nodes, or fragments
- contradictory conditions
- unreachable or non-terminating branches
- out-of-range state changes
- required plot information available only through hidden branches
- text-length violations
- missing replay summaries
- generated content that is stale relative to source

## 12. Numerical Design and AI Assistance

The project does not use unconstrained AI-generated production values.

### 12.1 Workflow

1. Humans define experience targets and hard design boundaries.
2. A baseline value model estimates damage, block, draw, energy, delayed value,
   and negative effects.
3. Multiple simulation agents represent random, aggressive, defensive,
   synergy-seeking, and near-optimal strategies.
4. Fixed and sampled seeds produce win rate, turn count, health loss, pick rate,
   combination, route, and failure reports.
5. Constrained parameter search proposes candidate values without crossing
   design boundaries.
6. Language models interpret reports, identify hypotheses, and propose
   experiments.
7. Human review approves changes.
8. Real-player telemetry later calibrates where simulation does not model fun
   or comprehension.

Suitable optimization methods include bounded parameter sweeps, Bayesian
optimization, and evolutionary search. Reinforcement learning is deferred until
the rule system is stable and there is evidence that simpler agents cannot
cover important strategies.

### 12.2 Required foundation

- pure C# battle rules
- headless runner
- deterministic seeds
- configurable values
- baseline agents
- batch reports
- infinite-loop and bounds checks
- reproducible command logs

## 13. Runtime State and Persistence

State is divided into:

- `StaticContent`: immutable compiled definitions
- `MetaProgress`: unlocks, archive, historical endings, and persistent choices
- `RunState`: deck, health, resources, map position, and random state
- `NarrativeState`: story, relationships, fragments, conditions, and anomalies
- `BattleState`: entities, zones, statuses, queue, and battle random state

Serialized state stores stable IDs and pure data, never scene objects.

Save requirements:

- explicit save and content versions
- atomic checkpoint writes
- one known-good backup
- explicit migrations
- checkpoint at node entry and battle completion
- immediate persistence of critical choices
- battle random state and command sequence

A corrupt save must not be silently replaced. The game attempts backup and
known migrations, then reports a recoverable failure.

## 14. Map Generation

The generator accepts an act definition, seed, and narrative requirements:

1. create start, floor bands, and boss
2. place required reachable story anchors
3. generate branches and merges
4. assign ordinary node categories
5. place conditional and hidden nodes
6. validate reachability, spacing, and risk distribution
7. retry using a deterministically derived seed
8. use a safe fallback map after a bounded number of attempts

`MapNodeInstance` stores definition ID, position, connections, and run-local
state. It does not duplicate full content definitions.

## 15. Failure Handling

- Duplicate IDs, missing references, or unreachable required story fail content
  validation before a build.
- Missing optional presentation assets are prominent errors in development and
  explicit placeholders in release.
- Map generation uses a validated fallback rather than blocking play.
- Rule failures preserve seed, command log, effect chain, and damage trace.
- Excessive effect depth or count aborts the current chain and produces a
  reproducible diagnostic.
- Unity presentation may fail gracefully without changing the already resolved
  domain result.

## 16. Verification Strategy

### 16.1 Pure C# tests

- DI construction, scopes, and disposal
- event subscription lifecycle and ordering
- lifecycle order and failure handling
- card effects and zones
- damage pipeline stages and rounding
- narrative conditions
- save migration and round-trip
- deterministic replay

### 16.2 Property and simulation tests

- generated maps always provide a valid start-to-boss route
- required story anchors remain reachable
- health, energy, and stacks remain within valid bounds
- arbitrary save round-trips preserve state
- effect queues terminate within configured limits
- identical seed and command log produce identical results

### 16.3 Unity tests

- EditMode: assembly graph, content import, and Unity adapter integration
- PlayMode: Bootstrap, scene transition, input, save checkpoint, and minimal
  combat presentation

### 16.4 Harness evidence

The portable Harness remains the common entrypoint. It validates repository
state, content-source/generated synchronization, framework boundaries, and
available pure tests. Unity checks are run when the editor is configured; an
unavailable Unity check is recorded as a limitation, never presented as a
passing result.

## 17. Foundation Acceptance Criteria

The foundation milestone is complete only when:

- `CardGame` opens as a Unity `6000.3.10f1` project.
- Unity-generated caches are not tracked.
- the repository layout matches this specification or an approved ADR explains
  a change
- Assembly Definitions enforce the intended dependency graph.
- refactored framework modules have focused responsibilities and tests.
- no pure framework or Domain assembly references Unity.
- Bootstrap initializes and shuts down a minimal application deterministically.
- content-source templates and validation entrypoints exist.
- save, random, command, effect, and damage contracts are represented in pure
  C# and tested where included in the milestone plan.
- portable Harness verification passes.
- Unity compilation and applicable tests pass.
- `git diff --check` is clean.
- `feature_list.json`, `progress.md`, and `session-handoff.md` contain current
  evidence and the next executable step.

## 18. Durable Documentation

This specification is the high-level approved design. Implementation will also
create and maintain:

- `docs/NARRATIVE-AUTHORING.md`
- `docs/BALANCING.md`
- focused ADRs under `docs/decisions/`
- feature records with dependencies and acceptance criteria
- command evidence in `progress.md`
- an executable next step in `session-handoff.md`

Chat history is not treated as the source of truth.

## 19. Implementation Planning Boundary

No implementation begins from this document alone. After repository review, a
separate implementation plan will:

- inventory and disposition every existing framework type
- split work into one active feature at a time
- specify tests before implementation changes
- name exact files and verification commands
- define safe migration and rollback steps

