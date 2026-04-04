# Fairway Rogue — High Level Design Document

## 1. Introduction

### 1.1 Project Name
**Fairway Rogue**

### 1.2 Project Description
Fairway Rogue is a top-down 2D golf roguelite built in **Godot 4 using C#**. The game combines arcade-style golf mechanics with procedural generation and roguelite progression. In each run, the player plays through a sequence of procedurally generated holes, takes shots using aim and power controls, navigates hazards such as water, sand, and trees, and earns currency to upgrade clubs and golf balls.

The game is designed to be easy to understand, quick to pick up, and replayable over many runs. It emphasizes satisfying golf shots, meaningful recovery decisions, and variety through procedural course layouts.

### 1.3 Purpose of This Document
This document provides a high-level design for Fairway Rogue. It describes the overall architecture of the game, the major systems, the interactions between those systems, and the intended gameplay flow. The purpose of this document is to guide development, establish a shared technical direction for the team, and ensure the project remains consistent as implementation progresses.

### 1.4 Intended Audience
This document is intended for:
- team members implementing the game
- instructors evaluating the project design
- future contributors who need to understand the system at a high level

---

## 2. System Overview

Fairway Rogue is a single-player, session-based 2D game. The player starts a run, plays through a series of generated holes, earns rewards based on performance, upgrades equipment, and attempts to achieve the best possible score.

The game is composed of several major systems:
- **Game State / Run Management**
- **Shot Mechanics**
- **Ball Physics and Terrain Interaction**
- **Procedural Course Generation**
- **Hazards and Recovery Rules**
- **Club and Ball Upgrade System**
- **HUD, Menus, and Shop UI**
- **Scoring and Scorecard**
- **Save / Load**
- **Audio and Visual Feedback**

At a high level, the architecture is event-driven. The player interacts with the current hole through gameplay input, which affects the ball and game state. Hole completion, hazards, purchases, and run progression all trigger transitions handled by the game state manager and UI systems.

---

## 3. Design Goals

### 3.1 Gameplay Goals
The game should:
- deliver satisfying aim-and-shoot golf gameplay
- reward skillful shot planning and execution
- create variety through procedural generation
- make hazards and recovery choices meaningful
- encourage replay through roguelite progression

### 3.2 Technical Goals
The implementation should:
- separate gameplay logic from UI logic
- use modular systems that are easy to test independently
- support procedural generation without tightly coupling it to the shot system
- keep upgrade data and terrain data extensible
- support save/load with minimal complexity

### 3.3 User Experience Goals
The game should feel:
- readable
- responsive
- fair
- replayable
- polished enough to clearly communicate all major game rules

---

## 4. High Level Architecture

The game will be organized into several high-level modules. Each module is responsible for a distinct area of functionality.

### 4.1 Main Architectural Modules
1. **Core Game Management**
2. **Gameplay / Hole Module**
3. **Procedural Generation Module**
4. **Progression and Upgrade Module**
5. **UI / HUD Module**
6. **Persistence Module**
7. **Audio / Visual Feedback Module**

### 4.2 Module Relationships
- The **Core Game Management** module coordinates overall flow.
- The **Gameplay / Hole Module** manages active play on a hole.
- The **Procedural Generation Module** creates hole layouts and terrain data.
- The **Progression and Upgrade Module** handles currency, equipment, and purchases.
- The **UI / HUD Module** displays gameplay state and accepts menu/shop input.
- The **Persistence Module** stores run data, best scores, and settings.
- The **Audio / Visual Feedback Module** reacts to gameplay events and enhances presentation.

---

## 5. Core Gameplay Architecture

## 5.1 Game State Management
The game should use a high-level state-driven structure.

### Primary game states
- **Main Menu**
- **Loading / Generation**
- **In Hole Gameplay**
- **Hazard Recovery Prompt**
- **Hole Complete**
- **Upgrade / Shop**
- **Scorecard**
- **Run Complete**
- **Paused**
- **Loading Save / Resume**

The state manager controls transitions between these states and ensures that only the correct systems are active at any given time.

### Example transitions
- Main Menu → New Run
- New Run → Generate Hole 1
- Generate Hole → In Hole Gameplay
- Ball enters trees → Hazard Recovery Prompt
- Ball enters cup → Hole Complete
- Hole Complete → Scorecard / Shop / Next Hole
- Final Hole Complete → Run Complete
- Resume Saved Run → Load current hole and state

---

## 5.2 Hole Gameplay Loop
Each hole follows a simple gameplay loop:

1. Load or generate the hole
2. Place the ball on the tee
3. Display hole information and HUD
4. Allow player to aim and select club
5. Player takes a shot
6. Ball travels and interacts with terrain/hazards
7. Ball comes to rest
8. Evaluate ball position
9. If hazard or tree recovery is needed, show the appropriate recovery flow
10. Repeat until the ball is sunk
11. Record score and reward the player
12. Transition to the next screen

This loop must remain readable and consistent on every hole.

---

## 6. Gameplay Systems

## 6.1 Shot System
The shot system is the main player interaction system. It is responsible for translating player input into a golf shot.

### Responsibilities
- display aim direction
- display or manage shot power
- read player input
- launch the ball with the selected angle and power
- apply club and ball modifiers
- disable new shots while the ball is moving

### Key design principle
The shot system should feel arcade-readable rather than simulation-heavy. It must be predictable enough that players can improve through practice.

---

## 6.2 Ball Movement and Terrain Interaction
The ball system manages movement, stopping behavior, collision handling, and surface interaction.

### Responsibilities
- move the ball after a shot
- detect when the ball is rolling, bouncing, or stopped
- apply friction based on terrain
- interact with world boundaries and hazards
- notify other systems when the ball enters special zones

### Terrain interaction examples
- **Fairway**: normal friction, good rollout
- **Rough**: increased friction, reduced rollout
- **Sand**: high friction, difficult recovery
- **Green**: low-speed controlled movement for putting
- **Water**: penalty and reposition
- **Trees**: obstructed lie or recovery prompt
- **Out of Bounds**: penalty and reset or reposition

The ball system should expose events so that hazards, UI, and audio can respond appropriately.

---

## 6.3 Club System
The club system manages available clubs and their gameplay effects.

### Initial club types
- Driver
- Iron
- Wedge
- Putter

### Responsibilities
- define club stats
- determine power ranges
- influence forgiveness / control
- allow player selection before the shot
- expose the current club to HUD and shot systems

This system should be data-driven so club values can be tuned without rewriting core shot logic.

---

## 6.4 Ball Equipment System
The equipment system should support different golf ball variants.

### Responsibilities
- define ball stat profiles
- affect gameplay attributes such as control, bounce, or distance
- integrate with upgrade progression
- expose equipment data to shop and HUD systems

Ball type should matter enough to support player choice but not so much that one option always dominates.

---

## 7. Hazard and Rule Systems

## 7.1 Hazard System
The hazard system detects penalty situations and enforces corresponding rules.

### Required hazards
- water
- sand
- tree / obstructed lie
- out of bounds

### Responsibilities
- identify when the ball enters a hazard
- apply stroke penalties where needed
- determine valid recovery positions
- communicate with the UI to present player choices
- trigger effects and sounds

---

## 7.2 Tree Recovery / Unplayable Lie System
One important game-specific rule is the tree recovery feature shown in the mockup.

### Required behavior
When the ball lands in trees or a blocked lie:
- the game displays a prompt
- the player chooses:
  - **Take a drop**
  - **Play from lie**

### System responsibilities
- decide whether the lie qualifies as obstructed
- pause regular shot input
- show a recovery dialog
- apply a penalty if a drop is chosen
- find a valid drop location
- return control to standard gameplay

This system gives the player a meaningful risk/reward choice and supports the game’s arcade-golf identity.

---

## 7.3 Golf Scoring Rules System
The scoring system manages strokes, par comparisons, and run totals.

### Responsibilities
- track stroke count per hole
- compare hole result to par
- compute run totals
- support labels such as birdie, par, bogey, etc.
- reward or penalize performance for progression purposes

The scoring system should remain simple and readable rather than try to implement the entire official golf ruleset.

---

## 8. Procedural Generation Architecture

## 8.1 Generator Overview
The procedural generation module is responsible for creating playable golf holes.

### Generator outputs
Each generated hole should include:
- tee location
- cup location
- fairway shape
- green area
- rough regions
- hazards
- obstacle placement
- metadata such as par, difficulty, and wind

### Design requirement
Every generated hole must be completable and visually understandable.

---

## 8.2 Generation Approach
The exact algorithm may change during development, but the intended high-level structure is:

1. Generate a base hole path from tee to green
2. Shape fairway width and curvature
3. Create terrain zones around that path
4. Place hazards using rule-based constraints
5. Validate playability
6. Adjust or regenerate if invalid
7. Package the hole data for gameplay use

This design allows generation logic to stay separate from rendering and play systems.

---

## 8.3 Playability Validation
The generator must validate that holes are not impossible or unfair.

### Validation goals
- there is at least one playable route from tee to cup
- hazards do not fully block progress
- recovery areas exist where needed
- terrain transitions are readable
- hole scale matches intended difficulty

Validation is critical because the generator is one of the biggest technical risks in the project.

---

## 8.4 Difficulty Scaling
The generation system should support difficulty increases as the run continues.

### Difficulty can scale through
- longer holes
- narrower fairways
- more trees
- more water
- more sand
- stronger wind
- riskier angles into the green

Difficulty should increase without making the game feel random or unfair.

---

## 9. Progression and Upgrade Architecture

## 9.1 Currency System
The currency system tracks the player’s earnings and spending.

### Currency sources
- hole completion
- under-par performance
- bonuses for strong play
- run completion rewards

### Currency usage
- purchase club upgrades
- purchase ball upgrades
- possibly unlock variants or improvements

The currency system should connect scoring to progression so good performance feels meaningful.

---

## 9.2 Upgrade Shop System
The shop system is responsible for offering upgrades and applying them.

### Responsibilities
- display current funds
- present available upgrade options
- show stat comparisons
- allow purchases
- update the player loadout or stats

The shop should be visually readable and should support quick between-hole decisions.

---

## 9.3 Progression Rules
The game is intended to function as a light roguelite, not a full permanent progression RPG.

### Recommended progression behavior
- purchased upgrades persist during the current run
- high score and key records persist between sessions
- some meta unlocks may persist if time allows
- saved runs can be resumed later

This keeps the project manageable while still delivering a roguelite feel.

---

## 10. User Interface Architecture

## 10.1 UI Overview
The UI system is responsible for all player-facing interface elements outside of direct terrain rendering.

### Major UI screens
- Main Menu
- Settings
- High Scores
- In-Game HUD
- Hazard Recovery Prompt
- Upgrade Shop
- Scorecard
- Run Complete Screen
- Pause Menu

---

## 10.2 In-Game HUD
The HUD should support quick decision-making during active play.

### Required HUD data
- current hole number
- par
- stroke count
- selected club
- wind direction and strength
- possibly remaining distance
- relevant prompts when needed

The HUD should stay readable without cluttering the screen.

---

## 10.3 Scorecard and Summary Screens
These screens present run performance in a digestible way.

### Scorecard responsibilities
- show hole-by-hole results
- show par and strokes
- show totals
- show score relative to par

### Run summary responsibilities
- show final result
- show rewards
- show high score updates
- present replay or return-to-menu options

---

## 10.4 Prompt and Recovery UI
Special interaction screens are needed for situations like tree recovery.

### Responsibilities
- interrupt normal gameplay when necessary
- present clearly labeled options
- confirm penalties where relevant
- return to gameplay cleanly after selection

---

## 11. Audio and Visual Feedback Architecture

## 11.1 Audio System
The audio system enhances player feedback and readability.

### Required audio categories
- shot sounds
- terrain impact sounds
- water splash
- cup completion sound
- menu sounds
- upgrade/shop sounds
- background music

The audio system should respond to gameplay events and scene transitions.

---

## 11.2 Visual Feedback System
Visual feedback is important for making golf feel satisfying.

### Responsibilities
- show shot direction and power clearly
- emphasize cup completion
- distinguish terrain and hazards
- provide responsive UI animations
- improve the sense of ball movement and height

---

## 11.3 Ball-in-Air Visual Effect
The team wants to use shaders or similar effects to make the golf ball feel like it is elevated during flight.

### High-level design for this feature
This should be treated as a visual presentation system layered on top of core ball logic.

Possible cues include:
- moving or offset ball shadow
- scale/brightness changes
- arc or trail effect
- simple shader-based flight highlight

This effect should not control gameplay physics directly. It should visually communicate height while the underlying gameplay system tracks position and movement.

---

## 12. Persistence Architecture

## 12.1 Save / Load System
The persistence module handles all long-term stored data.

### Data to persist
- current run state
- current hole
- current score
- player currency
- purchased upgrades
- high scores
- settings

### Save system goals
- save reliably on quit or transition points
- resume a run correctly
- handle missing save data safely
- keep stored data simple and readable

A lightweight file-based approach such as JSON or ConfigFile is appropriate for this project.

---

## 13. Scene and System Organization

At a high level, the project should separate reusable managers from scene-specific content.

### Suggested high-level organization
- **Autoload / global managers**
  - GameManager
  - SaveManager
  - AudioManager
- **Scenes**
  - MainMenu
  - GameplayScene / HoleScene
  - ShopScene
  - ScorecardScene
  - SettingsScene
- **Gameplay objects**
  - Ball
  - PlayerShotController
  - ClubData
  - BallData
  - Terrain regions
  - Hazard triggers
- **Procedural systems**
  - HoleGenerator
  - HazardPlacer
  - Validation utilities
- **UI**
  - HUD
  - RecoveryDialog
  - ScorecardPanel
  - ShopPanel

This structure should make it easier to divide work across team members.

---

## 14. Data Design at a High Level

The project should use simple structured data for content and progression.

### High-level data categories
- club definitions
- golf ball definitions
- hole metadata
- terrain metadata
- save data
- score data
- shop / upgrade entries

### Data design goal
Values should be easy to tune without rewriting gameplay systems. This is especially important for:
- club stats
- terrain friction values
- reward amounts
- upgrade costs
- difficulty scaling values

---

## 15. Risks and Mitigation

## 15.1 Procedural Generation Risk
The largest technical risk is generating holes that are both varied and playable.

### Mitigation
- prototype generator early
- validate hole layouts
- keep generator rules constrained
- fall back to regeneration when necessary

---

## 15.2 Physics / Feel Risk
Ball movement may feel wrong if friction, power, or collision values are not tuned well.

### Mitigation
- isolate tuning values
- test on simple practice holes first
- focus on readable arcade feel rather than perfect realism

---

## 15.3 Scope Risk
It is easy for upgrades, special hazards, and generation complexity to grow beyond the schedule.

### Mitigation
- prioritize a polished core loop first
- keep advanced hazard types as stretch goals
- avoid building unnecessary systems before the main loop works

---

## 15.4 UI Clarity Risk
Because this is a top-down golf game, players must always understand the hole, lie, and shot state.

### Mitigation
- keep HUD simple
- use clear prompts
- visually distinguish terrain strongly
- test readability frequently

---

## 16. Team Responsibility Mapping

Based on the current proposed division of work, the high-level system ownership is:

### Brock McDermott
- UI and HUD
- upgrade shop interface
- scorecard and summary screens
- audio integration
- save/load system

### Isaac Burton
- shot mechanics
- ball movement / physics behavior
- club and ball gameplay logic
- game loop state behavior

### Ben Hickenlooper
- procedural hole generation
- terrain layout system
- hazard placement
- difficulty scaling for generated holes

### Shared responsibilities
- integration
- tuning
- playtesting
- bug fixing
- polish

---

## 17. Development Priorities

A recommended high-level implementation order is:

### Phase 1: Core Playable Slice
- ball movement
- aiming and shot power
- one simple hole
- cup completion
- basic scoring

### Phase 2: Terrain and Hazards
- fairway, rough, sand, water
- tree lie detection
- recovery prompt
- club switching
- HUD basics

### Phase 3: Procedural Generation
- tee to cup layout generation
- fairway shaping
- hazard placement
- validation

### Phase 4: Progression and Menus
- run structure
- shop
- currency
- scorecard
- main menu
- save/load

### Phase 5: Polish
- audio
- shader/ball-in-air effect
- better UI feedback
- tuning
- balancing
- bug fixes

---

## 18. Success Criteria

The high-level design will be considered successfully implemented if:
- the player can start and complete a run
- holes are procedurally generated and playable
- the player can aim, choose a club, and take shots
- terrain and hazards affect gameplay
- the player can encounter a tree lie and choose recovery behavior
- scoring works across multiple holes
- upgrades and currency meaningfully affect runs
- save/load works
- the UI clearly communicates gameplay state
- the game feels polished enough to demonstrate a complete gameplay loop

---

## 19. Conclusion

Fairway Rogue is designed as a replayable arcade golf experience with procedural generation and roguelite progression. Its architecture separates the game into clear systems for gameplay, generation, progression, UI, and persistence, allowing the team to divide work effectively while keeping the project cohesive.

The most important design priority is to create a fun, readable, and satisfying golf loop first. Procedural generation, hazards, upgrades, and visual polish should all strengthen that core experience rather than distract from it. If implemented successfully, Fairway Rogue will provide both strong technical depth and a clear, enjoyable player experience.