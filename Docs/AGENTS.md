# AGENTS.md

# Fairway Rogue — Codex / AI Agent Instructions

This file contains the working rules, architecture rules, implementation expectations, and repo conventions for **Fairway Rogue**, a top-down 2D golf roguelite built in **Godot 4 with C#**.

All AI coding agents working in this repository must follow this file.

---

## 1. Project Summary

**Fairway Rogue** is a procedurally generated top-down 2D golf roguelite.

The player:
- plays through a run of generated golf holes
- aims and shoots using an arcade-style shot system
- deals with hazards like water, sand, rough, trees, and out-of-bounds
- can choose to **take a drop** or **play from lie** when stuck in the trees
- earns currency based on performance
- upgrades clubs and golf balls
- tries to complete a run with a strong score

This is **not** a full golf simulator. It is a **readable, arcade-style, skill-based golf game** with roguelite structure.

---

## 2. Source of Truth

When making changes, use the following priority order:

1. `Requirements.md`
2. `HLD.md`
3. `LLD.md`
4. `AGENTS.md`

If there is a conflict:
- follow `Requirements.md` first
- then `HLD.md`
- then `LLD.md`
- then this file

Do not invent gameplay rules that conflict with those docs.

---

## 3. Technology Requirements

- Engine: **Godot 4**
- Language: **C# only**
- Game type: **2D top-down**
- Target: desktop first
- Save system: local save using JSON or ConfigFile
- Architecture: modular scene-based structure with manager classes and event-driven communication

Do not convert the project to another engine, another language, or a different architectural style unless explicitly requested.

---

## 4. Core Design Goals

Every implementation decision should support these goals:

1. **Readable golf gameplay**
2. **Satisfying shot execution**
3. **Replayability through procedural generation**
4. **Meaningful hazards and recovery choices**
5. **Light roguelite progression**
6. **Clean, modular architecture**
7. **Minimal integration pain between systems**

The core fun loop is:

- aim
- choose club
- set power
- shoot
- ball moves
- evaluate lie/hazard
- recover if needed
- finish hole
- score
- upgrade
- continue run

Do not add systems that distract from this loop.

---

## 5. Hard Constraints

These are strict.

### 5.1 Do not break core game identity
Do not turn this into:
- a side-scroller
- a full golf sim
- a 3D game
- a physics sandbox
- a sports management game
- a puzzle-only game

It must remain a **top-down 2D golf roguelite**.

### 5.2 Do not overcomplicate physics
Ball movement should feel good and readable, not hyper-realistic.

Prefer:
- deterministic arcade-friendly movement
- tunable friction
- predictable bounce/roll behavior

Avoid:
- overly realistic simulation complexity
- fragile physics setups
- systems that make tuning much harder unless explicitly needed

### 5.3 Do not create duplicate systems
If a system already exists, extend or refactor it carefully instead of creating a second competing version.

Examples:
- do not create two save managers
- do not create two shot systems
- do not create two parallel hole generators
- do not create multiple truth sources for run state

### 5.4 Do not tightly couple unrelated systems
Keep these concerns separated:
- gameplay logic
- procedural generation
- UI
- save/load
- audio
- visual effects

### 5.5 Ball-in-air effect is visual-first
The “ball in the air” feature should primarily be a **presentation effect** unless explicitly requested otherwise.

Use:
- shadow offset
- visual height
- shader cue
- sprite offset
- subtle trail

Do not redesign the entire movement/collision model just to make the ball appear elevated unless explicitly asked.

### 5.6 Tree recovery must exist
The “in the trees” feature is required.

When the ball lands in trees or an obstructed lie, the player must be able to choose:
- **Take a drop**
- **Play from lie**

Do not remove or bypass this feature.

---

## 6. Gameplay Rules That Must Be Preserved

These rules define expected gameplay behavior.

### 6.1 Stroke rules
- each shot counts as 1 stroke
- penalties add strokes
- score is tracked per hole and per run

### 6.2 Hazard rules
#### Water
- add a penalty stroke
- reposition the ball based on the selected recovery rule

#### Out of Bounds
- add a penalty stroke
- reposition to previous legal or safe position

#### Trees / obstructed lie
- present recovery prompt
- allow **Take a drop** or **Play from lie**

#### Sand
- reduce rollout and/or shot effectiveness
- sand should feel punishing but fair

#### Rough
- more friction than fairway
- slightly weaker or less forgiving shot outcome

#### Green
- supports precise short shots and putting

### 6.3 Ball at rest rule
The player cannot take another shot while the ball is still moving.

### 6.4 Hole completion rule
A hole is complete when the ball enters the cup.

### 6.5 Procedural hole rule
Generated holes must always be playable.

Never knowingly accept a generator output that is:
- impossible
- unfairly blocked
- visually unreadable
- broken by hazards fully cutting off progress

---

## 7. Development Priorities

When building or modifying the game, prioritize in this order:

### Phase 1 — Playable vertical slice
- one playable hole
- aim
- power
- shoot
- ball movement
- hole completion
- basic scoring

### Phase 2 — Terrain and hazard rules
- fairway
- rough
- sand
- water
- trees
- out-of-bounds
- recovery prompt

### Phase 3 — Core run structure
- multiple holes
- run progression
- hole results
- scorecard
- currency rewards

### Phase 4 — Procedural generation
- tee and cup placement
- fairway generation
- hazard placement
- playability validation
- difficulty scaling

### Phase 5 — Progression systems
- clubs
- golf ball variants
- upgrades
- shop
- save/load

### Phase 6 — Polish
- audio
- shader effects
- ball-in-air visuals
- UI improvements
- balancing
- bug fixes

Do not spend major effort on polish before the core loop works.

---

## 8. Repo Layout Expectations

Use or preserve a structure close to this unless the repo already has a clearly established equivalent:

```text
Scenes/
  MainMenu.tscn
  Gameplay/
    GameplayScene.tscn
    Ball.tscn
    HoleRoot.tscn
    HUD.tscn
    RecoveryDialog.tscn
  UI/
    ShopScene.tscn
    ScorecardScene.tscn
    RunCompleteScene.tscn
    SettingsScene.tscn

Scripts/
  Managers/
    GameManager.cs
    RunManager.cs
    SaveManager.cs
    AudioManager.cs
    SceneRouter.cs
  Gameplay/
    BallController.cs
    ShotController.cs
    ClubController.cs
    LieEvaluator.cs
    HazardResolver.cs
    HoleController.cs
    WindController.cs
  Generation/
    HoleGenerator.cs
    HoleLayout.cs
    FairwayPathBuilder.cs
    HazardPlacer.cs
    TerrainPainter.cs
    HoleValidator.cs
  UI/
    HUDController.cs
    RecoveryDialogController.cs
    ShopController.cs
    ScorecardController.cs
    RunCompleteController.cs
  Data/
    ClubData.cs
    BallData.cs
    UpgradeData.cs
    HoleResultData.cs
    RunSaveData.cs
    SettingsData.cs
  Effects/
    BallShadowController.cs
    BallFlightEffectController.cs

Resources/
  Clubs/
  Balls/
  Upgrades/
  Audio/
```

Do not scatter unrelated scripts across random folders.

## 9. Scene and Script Rules

### 9.1 Scene ownership

Each scene should have a clear job.

Examples:

- `MainMenu.tscn` = menu flow only
- `GameplayScene.tscn` = current active hole and gameplay runtime
- `ShopScene.tscn` = upgrade purchasing
- `ScorecardScene.tscn` = result summary
- `RecoveryDialog.tscn` = tree drop / lie decision UI

### 9.2 Script naming

Use clear names ending in the role of the script:

- `GameManager`
- `ShotController`
- `HoleGenerator`
- `RecoveryDialogController`

Avoid vague names like:

- `Handler`
- `Helper2`
- `Thing`
- `ManagerNew`
- `SystemFinal`

### 9.3 One responsibility per main script

Each main script should have one clear reason to change.

Examples:

- `ShotController` should not also save files
- `SaveManager` should not also generate holes
- `HUDController` should not decide golf rules

## 10. Architecture Rules

### 10.1 Global managers

Use global managers only for true global concerns:

- game state
- run state
- save/load
- scene routing
- audio

Do not move normal gameplay logic into autoloads unless there is a real reason.

### 10.2 Event-driven communication

Prefer signals/events or well-defined method calls instead of deep hard-coded dependencies.

Examples:

- ball enters water → raise event → hazard system resolves
- ball stops → lie evaluator checks position
- cup reached → hole controller finalizes result

### 10.3 Procedural generation must output data, not directly own the whole game

The generator should produce a layout/data structure such as `HoleLayout`.
Gameplay scenes should consume that output.

Do not make the generator directly control:

- UI transitions
- shop logic
- scoring screens
- save logic

### 10.4 UI must reflect state, not own the rules

UI components display and collect input.
They should not be the final source of truth for golf rules.

Example:

- recovery dialog can present “Take a drop”
- hazard/gameplay logic decides penalty and position

### 10.5 Save logic must be centralized

All save and load behavior should go through one save system.

Do not serialize gameplay state in many disconnected places.

## 11. Procedural Generation Rules

These are very important.

### 11.1 Generated holes must be playable

Every generated hole must:

- have a tee
- have a cup
- have at least one valid route
- have readable terrain
- avoid impossible hazard placement

### 11.2 Difficulty should scale gradually

Later holes may become harder by adjusting:

- length
- fairway width
- hazard density
- wind strength
- tree placement
- approach difficulty

Do not create sudden unfair difficulty spikes unless explicitly intended and documented.

### 11.3 Validation is required

A generated hole should be validated before it is accepted.

Validation should check:

- tee not inside hazard
- cup not inside hazard
- playable route exists
- fairway width not too narrow
- hazards do not fully block all progression
- recovery spaces exist where needed

### 11.4 Regeneration is acceptable

If a hole is invalid, regenerate it.

It is better to regenerate than to ship an obviously broken hole.

## 12. Golf Systems Rules

### 12.1 Shot system

The shot system must support:

- visible aim direction
- visible power control
- club-based behavior
- no shooting while ball is moving

### 12.2 Club system

At minimum support:

- Driver
- Iron
- Wedge
- Putter

Club data should be data-driven where practical.

### 12.3 Ball variants

Ball types should affect gameplay in understandable ways such as:

- distance
- control
- bounce
- terrain resistance

Do not add variants that are impossible for the player to understand.

### 12.4 Wind system

Wind should:

- be visible in HUD
- affect shot behavior enough to matter
- not feel random or unreadable

### 12.5 Terrain system

Terrain types must be clearly defined and distinguishable:

- Tee
- Fairway
- Rough
- Sand
- Green
- Water
- Trees
- OutOfBounds

Each terrain should affect gameplay in a clear way.

## 13. UI / UX Rules

### 13.1 Readability first

The player must always understand:

- current hole
- par
- strokes
- active club
- wind
- current lie or hazard situation

### 13.2 HUD should stay focused

Do not overload the HUD with unnecessary information.

### 13.3 Recovery prompt must be obvious

When the ball is in the trees or obstructed:

- pause normal shot flow
- clearly show available choices
- clearly show penalty for drop if applicable

### 13.4 Menus should support the run loop

At minimum there should be:

- Main Menu
- Continue
- Settings
- High Scores
- Shop
- Scorecard
- Run Complete

## 14. Save / Load Rules

Save data must include the information needed to resume a run correctly.

At minimum include:

- current run seed
- current hole index
- current currency
- hole results so far
- current loadout/upgrades
- current score data
- relevant current hole state as needed

Save/load behavior should be:

- simple
- reliable
- centralized
- resilient to missing data

If a save is invalid:

- fail safely
- do not crash the game
- return the player to a safe state like the main menu

## 15. Audio Rules

Use centralized audio playback through `AudioManager`.

Include sounds for:

- shots
- water splash
- bunker/sand impact
- hole completion
- UI clicks
- upgrade purchase
- recovery prompt opening

Do not hardcode lots of raw audio file paths inside unrelated gameplay scripts.

## 16. Visual Effect Rules

### 16.1 Ball-in-air effect

The airborne ball effect should communicate height visually.

Good tools:

- shadow offset
- shadow scale
- sprite offset
- subtle shader cue
- subtle trail

Bad outcomes:

- unreadable ball position
- collisions that no longer make sense
- effect-driven bugs in the core shot system

### 16.2 Terrain readability

Terrain colors, tiles, and edges should make it easy to tell what surface the ball is on.

### 16.3 Hazard readability

Water, sand, trees, and OOB boundaries should be visually clear.

## 17. Coding Rules

### 17.1 General code style

Write code that is:

- clear
- modular
- easy to tune
- easy to debug

Prefer:

- small focused methods
- descriptive names
- constants/config values for tunable data
- comments only where they help clarify intent

Avoid:

- giant god classes
- hardcoded magic values everywhere
- hidden dependencies
- unclear abbreviations

### 17.2 C# conventions

Prefer:

- PascalCase for classes, methods, properties
- camelCase for local variables and parameters
- enums for clear rule categories like terrain and hazards
- plain data classes or Resources for tunable game content

### 17.3 Tunable data

Keep values tunable without requiring deep code changes.

Examples:

- friction values
- power multipliers
- upgrade costs
- wind ranges
- hole difficulty values

### 17.4 Minimal diff mindset

When editing existing code:

- change only what is needed
- preserve working logic when possible
- do not refactor unrelated systems in the same change unless necessary

## 18. Testing and Definition of Done

A feature is not done just because it compiles.

A feature should be considered done only if:

- it works in the intended scene/runtime flow
- it does not obviously break existing features
- it handles the main edge cases
- it follows the project docs
- it is readable and maintainable

### 18.1 Minimum manual checks

Before considering a change done, verify relevant behavior such as:

- can the player still take a shot
- does the ball stop correctly
- does water apply a penalty
- does sand change feel
- does tree recovery prompt appear
- does take-a-drop reposition correctly
- does score still track correctly
- does save/load still work if touched
- are generated holes still playable if generation changed

### 18.2 For procedural generation changes

Always verify:

- tee placement
- cup placement
- hazard readability
- at least one playable path
- no obviously impossible holes

## 19. Safe Change Strategy for AI Agents

Before making large changes:

- read relevant docs
- inspect existing files
- identify the smallest correct change
- preserve working systems
- keep architecture consistent

For large features, implement in slices:

- data/model first
- runtime logic second
- UI integration third
- polish last

Do not attempt huge repo-wide rewrites unless explicitly requested.

## 20. Things AI Agents Should Not Do

Do not:

- rewrite working systems just because you prefer another style
- move many files around without a strong reason
- invent undocumented gameplay modes
- silently delete features from requirements
- add large dependencies without need
- add overly clever abstractions for a student game
- prioritize polish over core playability
- create hidden behavior not documented in code or data
- remove the tree drop feature
- skip generator validation
- change save format carelessly once save/load exists

## 21. Preferred Implementation Order for Future Tasks

When asked to build the game from scratch or continue major work, prefer this order:

- single playable hole
- ball movement and stopping
- aim and power
- cup/hole completion
- scoring
- terrain friction
- hazards
- tree drop prompt
- run structure
- procedural generation
- upgrade shop
- save/load
- audio
- shader/ball-in-air polish

## 22. Team Ownership Reference

Use this as a guide when organizing work.

### Brock McDermott

Primary likely ownership:

- HUD
- recovery dialog UI
- shop UI
- scorecard
- run complete UI
- save/load
- audio integration
- menu flow

### Isaac Burton

Primary likely ownership:

- shot mechanics
- ball behavior
- club and ball gameplay logic
- game state transitions during play

### Ben Hickenlooper

Primary likely ownership:

- procedural generation
- terrain layout
- hazard placement
- playability validation
- difficulty scaling

### Shared
- integration
- testing
- balance
- polish
- bug fixing

## 23. Final Instruction to AI Coding Agents

When in doubt, optimize for:

- a working playable golf loop
- readable architecture
- minimal breakage
- faithful adherence to project docs
- simple, tunable systems

The game should feel like a polished student-built arcade golf roguelite, not an overengineered framework.

Build the simplest correct version first, then improve it.