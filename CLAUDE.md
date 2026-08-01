# CLAUDE.md — Sushi Rail Bite

This document defines the rules and context that Claude (and any other AI coding agent) must follow when working in this repository.
`AGENTS.md` mirrors this exact content and is kept in sync for other agent tooling (e.g. Codex).

---

## 1. Project Overview

### 1.1 Game Concept
- **Genre**: Tower Defense + Roguelite + Deckbuilding
- **Theme**: A conveyor-belt sushi restaurant run by a stoic sushi master and his daughter.
- **Visuals**: Japanese-style 2D pixel art, with frequent gag/parody moments.
- **Core loop**:
  1. Build a deck of Sushi (score source) and Customer (tower) cards.
  2. At stage start, sushi from the deck spawns randomly onto the rotating belt line.
  3. Customers are placed at fixed tables and each has the following stats:
     - **Reach**: the physical range on the belt where they can grab sushi
     - **Targeting**: the price band this customer prefers. **This is a priority hint, not a hard constraint** — see 3a
     - **Eating speed**: time taken to consume a piece of sushi
     - **Satiety**: total amount they can eat before becoming full
     - **Digestion time**: cooldown after becoming full before they can eat again
  3a. **How a sushi gets claimed** — the rule that Targeting participates in:
     - By default a customer takes whatever sushi enters its reach, **FIFO (first-come, first-served), regardless of price**. A customer never sits and watches sushi it could physically reach — that is a bad play experience and is treated as a bug.
     - Targeting only matters when **two or more customers can reach the same piece of sushi**. Among those contenders, Targeting decides who gets it.
     - If the resulting priority ties, the winner is broken arbitrarily (random or equivalent).
     - Consequence: a price band that a customer "does not prefer" still gets eaten by that customer when nobody else is contending for it.
  4. Score increases by the price of every sushi a customer eats.
  5. Reaching the target score within the time limit clears the stage; bonus objectives grant extra rewards.
  6. Clearing a stage grants roguelite meta-progression (new sushi/customer cards, etc.).

### 1.2 Development Environment
| Item | Value |
|---|---|
| Engine | Unity 6.5 |
| Render pipeline | URP (2D Renderer) |
| Language | C# |
| Deploy target | WebGL (demo web game) |

### 1.3 Project Goals (demo scope: 3 stages)
- [ ] Main screen: stage select + sushi/customer deckbuilding
- [ ] In-stage: place customers → deck-based sushi spawns onto the belt at start
- [ ] Customers: placed at designated tables, process sushi within reach
- [ ] Scoring: accumulate the price of consumed sushi, clear on reaching the target within the time limit
- [ ] Roguelite: post-clear rewards grant new sushi/customer cards and expand the deck

> Because the deploy target is WebGL, always keep initial load size, texture compression (ASTC/DXT), and GC pressure (assume mobile-tier performance) in mind.

---

## 2. Ubiquitous Language

Agents must not rename these terms arbitrarily anywhere in code, commits, or PRs.

| Term | Meaning | Expected code symbol |
|---|---|---|
| Sushi | Score unit flowing on the belt | `SushiData`, `SushiItem` |
| Customer | Tower-role customer | `CustomerData`, `Customer` |
| Targeting | A customer's preferred price band. Used **only** to rank contenders when several customers can reach the same sushi — never to forbid eating (§1.1-3a) | `TargetingRange`, `TargetingPriority` |
| Claim | Resolving which customer takes a contested piece of sushi | `SushiClaimResolver` |
| Table | Fixed slot where a customer is placed | `TableSlot` |
| Belt | The rotating line sushi flows on | `SushiBelt` |
| Deck | The set of sushi/customer cards assembled before a stage | `SushiDeck`, `CustomerDeck` |
| Stage | A single play session unit | `StageConfig`, `StageController` |
| Run | The full roguelite progression unit (a 3-stage set) | `RunState` |

---

## 3. Architecture Principles

### 3.1 Data-Driven Design via ScriptableObject
- Balance data — sushi, customers, stage objectives, etc. — **must be defined as `ScriptableObject`** (`SushiData`, `CustomerData`, `StageConfig`).
- Never hardcode balance numbers (price, speed, satiety, etc.) in code. Design changes must be possible without touching code.
- Always separate runtime state (current satiety, remaining digestion time, etc.) from static data (SO) — the SO is a read-only template; runtime state lives in a separate class/struct.

### 3.2 Composition First, Thin MonoBehaviours
- Use `MonoBehaviour` only as the minimal shell required for Unity's lifecycle and scene placement.
- Actual game logic (price checks, score calculation, digestion timers, etc.) must live in **plain C# classes (POCOs)** so it can be unit-tested without a `MonoBehaviour`.
  - Example: `Customer : MonoBehaviour` delegates real logic to a plain class `CustomerLogic`; `Customer` merely calls into it.
- Keep inheritance hierarchies shallow. Prefer interfaces (`ISushiConsumer`, `IScorable`) + composition over polymorphic inheritance.

### 3.3 Event-Driven Decoupling
- Minimize direct references between belt ↔ customer ↔ score ↔ UI; communicate via C# `event`/`Action` or a custom event bus (SO-based `GameEventChannel`).
- Prefer the SO-based event channel pattern (e.g. `SushiEatenEventChannelSO`) to reduce coupling between scenes/systems.

### 3.4 Object Pooling
- Sushi items on the belt must always be created/returned through an object pool (`SushiPool`). Direct `Instantiate`/`Destroy` calls are forbidden in production code (editor tooling/tests are the exception).

### 3.5 State Machines
- Customer state (`Idle → Eating → Full/Digesting → Idle`) must be implemented as an explicit state machine (a simple `enum` + `switch` is acceptable, but the transition logic must live in a testable, pure class).

### 3.6 UI Architecture (MVP)
- The deckbuilding and stage-select screens follow MVP: `View` (UI component, input/rendering only) / `Presenter` (logic, plain C#, test target) / `Model` (deck/run state data).
- `View` communicates with `Presenter` only through an interface (`IDeckBuildingView`); `Presenter` must not depend directly on Unity APIs.

### 3.7 Assembly Definition Separation
Split assemblies as below to reduce compile time and isolate tests.

```
Runtime.asmdef          # Game logic (minimal Unity API dependency)
Runtime.Data.asmdef      # ScriptableObject definitions (minimal dependencies)
Presentation.asmdef      # MonoBehaviours, UI, Views
Editor.asmdef            # Editor-only tooling
Tests.EditMode.asmdef    # EditMode unit tests (Assets/Tests/EditMode)
Tests.PlayMode.asmdef    # PlayMode integration tests (Assets/Tests/PlayMode)
```
- `Tests.*` assemblies may reference `Runtime` and `Runtime.Data`, but never the reverse.

---

## 4. C# / Unity Coding Conventions

### 4.1 Naming
| Target | Rule | Example |
|---|---|---|
| Public members/types | PascalCase | `MaxSaturation`, `CustomerData` |
| Private fields | `_camelCase` | `_currentSaturation` |
| Local variables/parameters | camelCase | `sushiPrice` |
| Interfaces | `I` prefix | `ISushiConsumer` |
| SO data classes | `~Data` / `~Config` suffix | `SushiData`, `StageConfig` |
| Event channel SOs | `~EventChannelSO` | `SushiEatenEventChannelSO` |
| Constants | PascalCase (per Unity convention) | `DefaultDigestSeconds` |

### 4.2 File/Class Structure
- One file = one public class/interface. File name matches the type name.
- Default to `[SerializeField] private`; do not expose public fields to the inspector.
- Order `using` directives: system → Unity → third-party → project namespaces.
- Namespaces follow `SushiDefense.<Domain>` (e.g. `SushiDefense.Customers`, `SushiDefense.Belt`, `SushiDefense.UI`).

### 4.3 Unity-Specific Rules
- Cache `GetComponent` results in `Awake`/`Start`; never call it inside `Update`.
- `Find`/`FindObjectOfType` are forbidden in production code (bootstrap/editor tooling is the exception) — inject references via the inspector or a DI container instead.
- Prefer `TryGetComponent`; when null-checking, use explicit `!= null` (be mindful of Unity's overloaded `==` on `UnityEngine.Object`).
- No magic numbers — every balance value belongs in an SO field.
- Minimize allocations inside `Update` — GC spikes matter especially on the WebGL target.

### 4.4 Comments/Documentation
- Public APIs require `///` XML doc comments, especially in the `Runtime` and `Runtime.Data` assemblies.
- Keep only comments that explain "why"; omit comments that restate obvious "what".

---

## 5. TDD / Testing Strategy

**TDD is the default development approach: write a failing test → implement the minimum → refactor.**

### 5.1 Framework
- **Unity Test Framework (UTF)**, built on NUnit.
- **EditMode tests**: pure logic (`CustomerLogic`, price checks, score calculation, state machines, Presenters) — these should make up the majority of the test suite.
- **PlayMode tests**: scene integration, coroutine/timing-dependent logic (belt movement, object pool behavior) — use only when frame-accurate verification is required (these are slow, so minimize them).
- Both live under `Assets/Tests/EditMode` and `Assets/Tests/PlayMode` respectively — see §10 for why Unity tests must stay inside `Assets`.

### 5.2 Test Priority
1. Pure game logic (price/satiety/digestion timers/score calculation) — **must be decoupled from `MonoBehaviour` so it's EditMode-testable**
2. State machine transition conditions
3. Presenters (UI logic, with the View replaced by a mock/stub)
4. SO data validation (e.g. rejecting negative prices)
5. PlayMode: belt spawning/movement, object pool reuse, scene-to-scene flow

### 5.3 Naming/Structure
- Test method naming: `MethodName_StateUnderTest_ExpectedBehavior`
  - e.g. `TryTake_SushiInReach_TakesRegardlessOfPrice`, `ResolveClaim_TwoCustomersInRange_HigherTargetingPriorityWins`, `Digest_AfterCooldown_ResetsSaturation`
- Separate Arrange-Act-Assert with blank lines even without comments.
- Prefer hand-written stubs/fakes for test doubles; evaluate NSubstitute only if needed (adding a new package requires team agreement — Claude does not add packages unilaterally, see §7).

### 5.4 Commit Gate
- Do not commit new logic without a corresponding test (see §8 pre-commit checklist).

---

## 6. Git Branching Strategy (Git Flow + 3-Way Handshake)

"3-way handshake" is defined here — analogous to TCP's 3-step handshake — as: **propose (branch) → confirm (PR/review) → finalize (merge approval)**. Nothing lands on `dev`/`main` until all three steps complete.

### 6.1 Branch Structure
```
main        # Deployable stable version (only release tags merge here)
dev         # Integration branch
feat/*      # Per-feature work (e.g. feat/customer-digestion)
release/*   # Demo release preparation
hotfix/*    # Urgent fixes on main
```

### 6.2 The 3-Step Handshake Procedure
1. **Propose (SYN)**: Branch `feat/*` off `dev` and work there. Freely deletable/restartable at this point — nothing is committed to shared history yet.
2. **Confirm (SYN-ACK)**: Open a PR once work is done; it must pass the §8 checklist (lint/type-check/tests). Review is performed by a human.
3. **Finalize (ACK)**: Merge only proceeds once a human explicitly approves it.

### 6.3 Rollback-Friendliness
- Merges are **always `--no-ff`** (a merge commit is created), so each merge is a single node in history.
  - To roll back: `git revert -m 1 <merge-commit-sha>` reverts the entire feature in one step.
  - Fast-forward merges and squash-merges are avoided by default (exceptions require team agreement).
- Tag every release point (e.g. `v0.1.0-demo`) so any point in time can always be restored.

### 6.4 Branch Cleanup
- Merged `feat/*` and `hotfix/*` branches are deleted in principle.
- **Deletion still requires final human approval before execution** (see §7). Claude may propose the deletion (present the `git branch -d` command) but never runs it directly.

### 6.5 Commit Messages
- Follow Conventional Commits: `feat:`, `fix:`, `test:`, `refactor:`, `chore:`, `docs:`
  - e.g. `feat(customer): add digestion cooldown state`

---

## 7. Areas Requiring Human Judgment (Agent Constraints)

Claude/AI agents must **never execute the following on their own.** Always propose/draft and wait for explicit human confirmation.

- `git merge`, `git push --force`, clicking the PR merge button
- Deleting branches (`git branch -d/-D`, `git push origin --delete`)
- Direct commits to `main`/`release/*`
- Modifying project settings (`ProjectSettings/*`, `Packages/manifest.json`), the build pipeline, or CI configuration
- Adding new third-party packages/assets
- Deleting, overwriting, or uploading original asset files (sprites, music) externally
- Bulk-editing balance SO data without confirming design intent

When any of these are needed, Claude presents a change plan and diff, then requests human approval.

---

## 8. Pre-Commit Checklist

**All of the following must pass before a commit is made.** If any step fails, do not commit — report the failure instead.

1. **Format/lint**: `dotnet format` (or the project's `.editorconfig`-based formatter) passes
2. **Type check/compile**: no Unity compile errors (or newly introduced warnings)
3. **Tests**: the full Unity Test Runner suite passes (EditMode first, plus relevant PlayMode)
   - CLI example: `Unity -batchmode -runTests -testPlatform EditMode -projectPath . -testResults results.xml -quit`
4. **Asset leak check** (§9)
5. Confirm new logic has a corresponding test (§5.4)

> Lint/formatting configs and CI pipeline scripts for the feedback loop live in the root-level `tests/` directory (see §10) — distinct from the gameplay unit tests under `Assets/Tests`.

---

## 9. Protecting Personal Assets (Preventing Sprite/Music Leaks)

- Before every commit, check `git status` / `git diff --stat` for **unintended inclusion of original assets** (png, psd, wav, mp3, aseprite, etc.).
- Manage large/original assets via Git LFS, and declare the relevant extensions in `.gitattributes`.
- Register temporary render files, unprocessed originals (`*_raw.*`), and personal reference folders in `.gitignore`.
- When attaching original assets or screenshots externally (issue trackers, chat, PR descriptions), prefer watermarked/low-resolution versions; avoid uploading raw originals externally.
- Claude always confirms with a human whether a given asset commit is meant for final/public release before proceeding.

---

## 10. Directory Structure

Agent-facing material (`docs/`, `.claude/`, `tests/`) lives at the **project root, outside `Assets/`**, with one exception: `Assets/Tests` remains in place because Unity Test Framework requires test assemblies to live under `Assets` (or `Packages/`) to be discovered by the Test Runner. The root-level `tests/` directory is unrelated to gameplay tests — it holds the CI/CD feedback loop: pipeline scripts, linter configs, and other non-Unity tooling.

Agent skills live in **`.claude/skills/`**, not a root-level `skills/`. That path is fixed by Claude Code — it is where the tooling actually discovers them, so it is not a free choice. Everything else the agents read (`INDEX.md`, `knowledge/`, `rules/`, `domain/`, `agents/`) sits alongside them under `.claude/`.

```
ProjectRoot
├── docs/                       # GDD, architecture notes, agent-facing documentation
├── .claude/                    # Agent layer — loaded by Claude Code
│   ├── INDEX.md                # Selective-load routing index (read this first)
│   ├── knowledge/              # Engine/language knowledge (project-agnostic)
│   ├── rules/                  # Path-scoped rules (scripts, asmdef, tests, SO, worktree)
│   ├── domain/                 # This project's design/system knowledge
│   ├── agents/                 # Sub-agent definitions (researcher / engineer)
│   └── skills/                 # Agent skills — /task-start, /run, /qa, /debug, ...
├── tests/                      # CI/CD feedback loop: pipeline scripts, linter configs, non-Unity tooling
├── Assets/
│   ├── Art/
│   │   ├── Sprites/
│   │   │   ├── Sushi/          # Sushi item sprites
│   │   │   ├── Customers/      # Customer character sprites
│   │   │   └── UI/
│   │   └── Animations/
│   ├── Audio/
│   │   ├── Music/
│   │   └── Sound/
│   ├── Code/
│   │   └── Scripts/
│   │       ├── Runtime/          # Runtime.asmdef — pure game logic
│   │       │   ├── Belt/
│   │       │   ├── Customers/
│   │       │   ├── Scoring/
│   │       │   └── StateMachines/
│   │       ├── Runtime.Data/     # Runtime.Data.asmdef — ScriptableObject definitions
│   │       │   ├── SushiData/
│   │       │   ├── CustomerData/
│   │       │   └── StageConfig/
│   │       ├── Presentation/     # Presentation.asmdef — MonoBehaviours, UI bindings
│   │       │   ├── Views/
│   │       │   └── Presenters/
│   │       └── Editor/           # Editor.asmdef — editor tooling
│   ├── Editor/                   # Agent harness tooling — ClaudeBridge, RunBuildCommand
│   │   └── ClaudeBridge/         #   namespace `Editor.ClaudeBridge` (NOT SushiDefense.*)
│   ├── Level/
│   │   ├── Prefabs/
│   │   ├── Scenes/
│   │   └── UI/
│   ├── Settings/                 # URP render pipeline assets
│   └── Tests/                    # Unity Test Framework only — must stay inside Assets
│       ├── EditMode/              # Tests.EditMode.asmdef
│       └── PlayMode/              # Tests.PlayMode.asmdef
├── scripts/                      # Harness scripts — run.sh, bridge-run.sh, worktree setup
├── Packages/
├── ProjectSettings/
├── CLAUDE.md
└── AGENTS.md
```

`Assets/Editor/` is **agent harness infrastructure**, distinct from `Assets/Code/Scripts/Editor/` (game-facing editor tooling). It keeps its own `Editor` / `Editor.ClaudeBridge` namespaces; game code must not depend on it.

---

## 11. Default Agent Workflow Summary

1. Before starting: check whether relevant SOs/interfaces already exist; if not, propose a design first.
2. TDD: write a failing test → implement the minimum → refactor → confirm it passes.
3. Never generate a commit message or run a commit without confirming the §8 checklist passed.
4. Anything covered by §7 must wait for explicit human confirmation before proceeding.
5. When summarizing changes, state exactly which assembly/directory was added to or modified.