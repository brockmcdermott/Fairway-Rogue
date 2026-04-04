# ArchitectureRules.md

# Fairway Rogue — Architecture Rules

This document defines the architectural rules for **Fairway Rogue**, a top-down 2D golf roguelite built in **Godot 4 with C#**.

These rules exist to keep the codebase:
- modular
- understandable
- easy to extend
- safer to integrate
- easier for multiple contributors and AI agents to work in without breaking core systems

This file is a strict implementation guide.  
When adding or modifying code, follow these rules unless the team explicitly decides to change them.

---

## 1. Architecture Goals

The architecture must support the following goals:

1. **A clean and playable golf gameplay loop**
2. **Procedural generation that stays separate from gameplay runtime**
3. **Simple and maintainable run progression**
4. **A UI layer that reflects state instead of owning rules**
5. **A centralized save/load system**
6. **Low integration friction between team members**
7. **Tunable systems for balancing and iteration**
8. **A project structure that AI agents can safely navigate**

---

## 2. Core Architectural Principle

Fairway Rogue must be built as a **modular scene-based game** with **single-responsibility systems**.

At a high level, the project is divided into these layers:

1. **Global Managers**
2. **Gameplay Runtime**
3. **Procedural Generation**
4. **Rules / Hazard Resolution**
5. **UI**
6. **Persistence**
7. **Audio / Visual Effects**
8. **Data / Configuration**

No single class or scene should try to own everything.

---

## 3. Source of Truth Hierarchy

When there is ambiguity, follow this priority order:

1. `Requirements.md`
2. `HLD.md`
3. `LLD.md`
4. `ArchitectureRules.md`
5. `AGENTS.md`

If code conflicts with these documents, update the code to match the docs unless the team explicitly revises the docs.

---

## 4. Allowed High-Level Modules

The project should be organized around these core modules.

## 4.1 Global Managers
Allowed responsibilities:
- high-level game state
- run state
- save/load
- scene routing
- centralized audio

Examples:
- `GameManager`
- `RunManager`
- `SaveManager`
- `SceneRouter`
- `AudioManager`

## 4.2 Gameplay Runtime
Allowed responsibilities:
- ball movement
- shot input
- current hole runtime logic
- club selection
- terrain interaction
- wind usage
- lie evaluation

Examples:
- `BallController`
- `ShotController`
- `HoleController`
- `ClubController`
- `WindController`
- `LieEvaluator`

## 4.3 Procedural Generation
Allowed responsibilities:
- hole layout generation
- path generation
- terrain generation
- hazard placement
- validation
- difficulty scaling inputs

Examples:
- `HoleGenerator`
- `FairwayPathBuilder`
- `HazardPlacer`
- `TerrainPainter`
- `HoleValidator`

## 4.4 Rules / Hazard Resolution
Allowed responsibilities:
- water penalty logic
- out-of-bounds penalty logic
- tree drop / play-from-lie logic
- drop position selection
- hole scoring calculations

Examples:
- `HazardResolver`
- `LieEvaluator`
- scoring helpers

## 4.5 UI
Allowed responsibilities:
- displaying current game state
- collecting player UI choices
- menu flow
- scorecard display
- shop interaction
- recovery dialog interaction

Examples:
- `HUDController`
- `RecoveryDialogController`
- `ShopController`
- `ScorecardController`
- `RunCompleteController`

## 4.6 Persistence
Allowed responsibilities:
- save data serialization
- save data loading
- high score persistence
- settings persistence

## 4.7 Audio / Visual Effects
Allowed responsibilities:
- sounds
- music
- ball shadow
- flight visual effects
- particle effects
- shader-based presentation

---

## 5. Forbidden Architectural Patterns

The following patterns are not allowed unless explicitly approved.

### 5.1 No giant god classes
Do not create a single class that owns:
- input
- ball movement
- terrain logic
- hazards
- scoring
- save/load
- UI
- generation

### 5.2 No duplicated authority
There must not be multiple competing sources of truth for:
- current run state
- current hole score
- player currency
- save data
- selected club
- active game state

### 5.3 No UI-owned rules
UI may present a choice, but gameplay/rules systems must decide outcomes.

Bad example:
- recovery dialog directly modifying strokes and repositioning without using gameplay/rule systems

Correct example:
- recovery dialog sends player choice to the hazard/rule system, which applies the actual game rule

### 5.4 No generator-owned runtime flow
The procedural generation system must not directly:
- switch scenes
- drive UI
- award currency
- save the game
- control the shop

The generator produces data.  
Gameplay consumes that data.

### 5.5 No scattered save logic
Saving must not be spread randomly across unrelated scripts.

All save/load behavior should flow through a centralized save manager.

### 5.6 No hidden coupling through random node lookups
Avoid fragile architecture based on:
- deep `GetNode()` chains everywhere
- hardcoded scene tree assumptions across unrelated systems
- hidden dependencies that are not obvious from the class design

Prefer:
- explicit references
- dependency injection through setup methods
- events/signals
- clearly documented node ownership

---

## 6. Scene Architecture Rules

## 6.1 Scene ownership must be clear
Each scene should have a single main purpose.

### Examples
- `MainMenu.tscn` → main menu only
- `GameplayScene.tscn` → active gameplay for current hole
- `ShopScene.tscn` → upgrade purchases
- `ScorecardScene.tscn` → score summary
- `RecoveryDialog.tscn` → obstructed lie choice UI

A scene may contain child components, but its purpose must stay focused.

## 6.2 GameplayScene is the active runtime host
`GameplayScene.tscn` is responsible for hosting:
- current hole visuals
- current ball instance
- gameplay controllers
- current HUD
- runtime hazard/recovery UI

It should not permanently embed unrelated menu systems inside the same logic flow.

## 6.3 Reusable UI components should remain reusable
Reusable UI pieces like:
- stat rows
- button panels
- score rows
- prompt panels

should be separate components instead of hardcoded repeatedly.

---

## 7. Script Responsibility Rules

## 7.1 One main responsibility per script
Each major script should have one core responsibility.

### Good examples
- `BallController` → ball movement and ball state
- `ShotController` → aim/power/shot input
- `HoleGenerator` → generate hole layout
- `SaveManager` → save/load data
- `HUDController` → display runtime HUD

### Bad examples
- `BallController` also saving files
- `HUDController` calculating penalties
- `HoleGenerator` also deciding menu transitions

## 7.2 Controllers should coordinate, not do everything
A controller may coordinate related behavior, but if it becomes too broad, split responsibilities.

Example:
- `ShotController` may read input and send launch requests
- actual movement remains in `BallController`
- actual data for clubs remains in `ClubData`

## 7.3 Data should not be embedded everywhere
Stats and tunable values should not be hardcoded across many gameplay scripts.

Use:
- data classes
- resources
- structured config objects

for things like:
- club stats
- ball stats
- upgrade values
- terrain properties
- difficulty scaling values

---

## 8. Global Manager Rules

## 8.1 Use autoloads only for true global concerns
Allowed autoload/global manager responsibilities:
- high-level game state
- run progression state
- save/load
- scene switching
- global audio

Do not use autoloads for normal local gameplay concerns unless there is a strong reason.

## 8.2 GameManager is the owner of high-level game state
`GameManager` should own:
- current top-level state
- transitions like menu → gameplay → shop → scorecard → run complete

It should not own detailed ball movement or hole generation internals.

## 8.3 RunManager is the owner of run state
`RunManager` should own:
- current hole index
- currency
- hole results
- loadout/upgrades
- total score state

Do not duplicate run state inside UI scripts.

## 8.4 SaveManager is the only save authority
All save/load operations must route through `SaveManager`.

No other class should become a second save system.

## 8.5 AudioManager is the preferred audio entry point
Gameplay and UI systems should request audio through `AudioManager`.

Do not hardcode raw file playback in many different places.

---

## 9. Gameplay Runtime Rules

## 9.1 BallController owns ball movement
`BallController` should own:
- current velocity
- movement updates
- stopping logic
- terrain contact checks
- hazard detection hooks
- visual flight state inputs

It should not own:
- shop logic
- scorecard logic
- scene routing
- save file writing

## 9.2 ShotController owns shot input
`ShotController` should own:
- aim direction
- power charge logic
- input collection for taking a shot
- shot launch request

It should not directly own:
- save/load
- hole generation
- reward calculation

## 9.3 HoleController owns current hole runtime state
`HoleController` should own:
- hole number
- par
- strokes on this hole
- tee/cup metadata
- hole completion state

It should not own persistent cross-run save logic.

## 9.4 LieEvaluator owns lie classification
`LieEvaluator` should determine:
- what terrain the ball is on
- whether the ball is obstructed in trees
- whether a drop location is needed

It should not directly own UI presentation.

## 9.5 HazardResolver owns penalty application
`HazardResolver` should apply:
- water penalties
- OOB penalties
- tree drop logic
- repositioning after penalties

This keeps gameplay rules centralized and avoids duplicated hazard behavior.

---

## 10. Procedural Generation Rules

## 10.1 Generation must output structured data
The generator should output a structured layout object such as `HoleLayout`.

This object should contain enough information for gameplay scenes to instantiate and paint the hole.

Do not make generation depend on direct UI state or menu state.

## 10.2 Generator output should be deterministic per seed
Given the same seed and same parameters, the generator should produce the same result whenever practical.

This helps with:
- debugging
- reproducibility
- daily challenge support later
- testing

## 10.3 Generation is separate from rendering
The system should conceptually follow:

1. generate layout data
2. validate layout data
3. paint/render layout into scene objects or tiles
4. run gameplay on that result

Do not merge all of this into one giant method if it can be kept separate.

## 10.4 Validation is required
Every generated hole must be validated before use.

Validation should confirm:
- tee is safe
- cup is safe
- route exists
- hazards do not fully block play
- hole scale matches difficulty expectations
- recovery cases are not broken

## 10.5 Regeneration is preferred over accepting broken content
If validation fails:
- regenerate
- adjust seed
- retry within a reasonable attempt limit
- fall back to a safe template if needed

Do not knowingly pass obviously broken holes into gameplay.

---

## 11. Golf Rule Architecture Rules

## 11.1 Rules must be centralized and consistent
Golf-like gameplay rules such as:
- stroke addition
- hazard penalties
- drop logic
- hole completion
- score labels

must be handled consistently in rule-owning systems.

Do not reimplement these rules in multiple unrelated places.

## 11.2 Tree drop / play-from-lie feature is mandatory
The architecture must support this feature directly.

Required behavior:
- the lie is evaluated
- if obstructed, the player gets a choice
- the rules system applies either:
  - drop with penalty
  - continue from current lie

This flow must not be bypassed by ad hoc one-off implementations.

## 11.3 Hazard events should flow through clear channels
Preferred flow:
- ball enters hazard / stops in obstructed state
- gameplay system raises event or calls resolver
- resolver applies penalty/rule
- UI updates based on new state

Avoid hidden hazard behavior buried in random code paths.

---

## 12. UI Architecture Rules

## 12.1 UI reflects the current state
UI should display:
- hole number
- par
- strokes
- selected club
- wind
- prompt choices
- upgrade info
- run summary

UI should not independently decide actual golf rules.

## 12.2 Dialogs collect input, not own game logic
Example:
- `RecoveryDialog` shows choices
- actual penalty and reposition are resolved elsewhere

## 12.3 UI should not directly mutate deep gameplay state unless coordinated
Direct state mutation from UI should be minimized.

Preferred:
- UI fires an event or calls a public gameplay/rule method
- gameplay system updates state
- UI refreshes from authoritative state

## 12.4 HUD should remain lean
The runtime HUD should focus on gameplay-relevant information only.

Do not crowd it with unrelated debug info unless in debug mode.

---

## 13. Persistence Rules

## 13.1 Save format must be explicit
Save data must be represented using clearly defined data models.

Good examples:
- `RunSaveData`
- `HoleResultData`
- `SettingsData`

## 13.2 Save boundaries must be intentional
Save should happen at clear moments such as:
- between holes
- after purchases
- on pause/quit
- optionally after stable shot resolution if needed

Avoid chaotic or inconsistent save timing.

## 13.3 Save data should not depend on fragile scene tree state
Do not serialize random node references or unstable scene tree paths.

Persist meaningful data only:
- seed
- hole index
- scores
- currency
- purchased upgrades
- settings
- essential ball/hole state if needed

## 13.4 Corrupt saves must fail safely
If save data is invalid:
- do not crash
- do not soft-lock the run
- recover to a safe state like menu or clean new run

---

## 14. Audio and Effects Architecture Rules

## 14.1 Audio must stay centralized
Gameplay and UI should ask the audio system to play sounds instead of managing lots of local audio file logic.

## 14.2 Ball flight visuals are presentation-only by default
The “ball in the air” effect should be implemented as a visual layer on top of gameplay motion unless a different design is explicitly approved.

Examples:
- shadow offset
- sprite Y-offset
- shader cue
- subtle trail

The effect should improve readability, not complicate the rules system.

## 14.3 Effects must not become a hidden gameplay dependency
Do not make the core game rely on a shader or effect script just to function.

Gameplay should still be understandable and functional even if visual polish is disabled.

---

## 15. Data Architecture Rules

## 15.1 Data classes should be used for tunable systems
Use structured data for:
- clubs
- balls
- upgrades
- terrain properties
- hole layout
- save data

## 15.2 Data should be reusable and inspectable
A gameplay system should be able to consume data without requiring deep knowledge of unrelated systems.

## 15.3 Avoid magic constants scattered in code
Put tunable values in:
- config fields
- data objects
- exported properties
- clearly named constants

Examples:
- friction
- power scaling
- wind strength ranges
- upgrade costs
- bunker penalty multipliers

---

## 16. Dependency Rules

## 16.1 Prefer one-way dependencies
Preferred relationship direction:

- data/config → consumed by systems
- generation → produces layout for gameplay
- gameplay → raises events for UI/audio
- UI → displays state and forwards player intent
- persistence → stores/restores authoritative state

Avoid cyclical dependencies where two systems deeply depend on each other’s implementation details.

## 16.2 Avoid deep scene-tree coupling
A script should not need to know the exact full nested path of distant nodes unless absolutely necessary.

## 16.3 Prefer clear setup contracts
When a scene/controller needs references, provide them:
- via exported references
- via initialization methods
- via parent-owned setup
- via autoload access for true global managers

---

## 17. Refactoring Rules

## 17.1 Preserve working behavior unless the task requires behavior change
When refactoring:
- preserve public behavior
- preserve save compatibility when practical
- avoid mixing large functional changes with style-only cleanup

## 17.2 Do not refactor unrelated systems in the same change
Keep changes focused.

Example:
- do not rewrite audio and procedural generation while adding a bunker rule

## 17.3 Make smallest safe structural improvement
Prefer a small safe architecture improvement over a giant rewrite.

---

## 18. Integration Rules

## 18.1 Features must integrate through defined seams
Examples of safe seams:
- generator outputs `HoleLayout`
- shot system launches `BallController`
- ball stop triggers lie evaluation
- hazard resolver applies penalties
- UI observes and reflects state
- save manager serializes authoritative data

## 18.2 New systems must declare ownership
Any new system added to the repo should have a clearly stated responsibility.

Before adding a new class, ask:
- what does this own?
- why does this not belong in an existing class?
- what systems depend on it?

## 18.3 Integration should happen incrementally
Build and integrate in slices:
1. simple logic
2. scene hookup
3. UI feedback
4. save implications
5. polish

Avoid giant “everything at once” merges.

---

## 19. Testing-Oriented Architecture Rules

## 19.1 Systems should be testable in isolation where practical
Good candidates:
- score calculations
- upgrade application
- drop point selection
- hole validation
- save serialization

## 19.2 Generation should be testable from seed input
It should be possible to generate a hole from a seed and inspect/validate the result.

## 19.3 Rule systems should be deterministic where possible
Hazard and scoring rules should behave predictably.

---

## 20. AI Agent Rules

Any AI coding agent working in this repo must follow these architectural rules.

### AI agents must:
- preserve module boundaries
- avoid inventing alternate architectures
- avoid duplicate systems
- avoid moving rule logic into UI
- avoid spreading save logic
- keep generation separated from runtime/UI
- keep visual effects separate from game rules where practical

### AI agents must not:
- rewrite the codebase around a new framework style
- collapse many modules into one
- bypass the tree recovery system
- remove validation from generation
- invent hidden state owners
- create fragile scene coupling

When uncertain, choose:
- simpler
- more modular
- more explicit
- easier to integrate

---

## 21. Final Architecture Summary

Fairway Rogue must follow this core structure:

- **GameManager** owns top-level state
- **RunManager** owns run state
- **SaveManager** owns save/load
- **Gameplay systems** own active hole play
- **Generation systems** create hole layout data
- **Hazard/rule systems** apply penalties and recovery rules
- **UI systems** display state and collect player choices
- **Audio/effects systems** provide presentation only
- **Data models** hold tunable values and serialized data

The architecture should always protect the main gameplay loop:

- aim
- choose club
- set power
- shoot
- evaluate result
- resolve hazards/recovery
- finish hole
- score
- upgrade
- continue run

Everything in the architecture should support that loop cleanly.