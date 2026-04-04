# Fairway Rogue — Requirements Document

## 1. Project Title
**Fairway Rogue**  
A procedurally generated top-down 2D golf roguelite built in **Godot 4 with C#**.

---

## 2. Project Summary
Fairway Rogue is a replayable arcade-style golf game where the player completes a run of procedurally generated holes while managing club choice, shot power, aim, hazards, and upgrades. The game combines the satisfying mechanics of golf with a roguelite progression loop. Each run should feel different because the course layouts, hazard placement, and hole difficulty are generated dynamically.

The player’s objective is to complete a full run of holes with the lowest score possible while earning currency and unlocking or upgrading better equipment. The game should be easy to understand, quick to play, and deep enough to reward skill and strategy.

---

## 3. High-Level Goals

### 3.1 Core Design Goals
- Create a fun and readable **top-down golf experience**
- Make each run feel fresh through **procedural generation**
- Keep the controls simple but allow for meaningful skill
- Add progression through **club and ball upgrades**
- Support short sessions while still feeling rewarding over time
- Use visuals, HUD, and feedback to make every shot satisfying

### 3.2 Player Experience Goals
The game should feel:
- quick to pick up
- satisfying to play
- slightly challenging but fair
- replayable
- rewarding when the player improves or upgrades equipment

---

## 4. Platform and Technology Requirements
- Engine: **Godot 4**
- Language: **C#**
- Genre: **2D top-down golf roguelite**
- Target platform: desktop first
- Resolution target: should support a clean fixed game resolution and scale properly
- Save system: local file save using Godot-supported save approach such as JSON or ConfigFile

---

## 5. Core Game Pillars
The project is centered around the following major systems:

1. **Golf Shot Gameplay**
2. **Procedural Course Generation**
3. **Hazards and Terrain**
4. **Scoring and Run Progression**
5. **Upgrade / Shop System**
6. **HUD, Menus, and Scorecard**
7. **Save / Load**
8. **Audio and Visual Feedback**

---

## 6. Game Structure

### 6.1 Main Flow
The player should move through the following loop:

1. Launch game
2. Open main menu
3. Start a new run or continue
4. Play generated holes one at a time
5. Complete each hole
6. View score / rewards
7. Visit upgrade/shop screen between holes or between rounds
8. Continue until the run is complete
9. See final scorecard and rewards
10. Save progress / high scores

### 6.2 Run Structure
- A run consists of a sequence of generated golf holes
- Default run length should be **9 holes**
- Each hole should have:
  - par value
  - tee position
  - cup / hole position
  - fairway path
  - rough
  - hazards
- Hole difficulty should increase over the course of a run

---

## 7. Core Gameplay Requirements

## 7.1 Player Shot System
The player must be able to take golf shots using an aim-and-power system.

### Required functionality
- The player can aim before each shot
- The player can control shot power
- The player commits to the shot using an input action
- The ball moves based on the chosen aim and power
- The game should support different shot feel depending on club and terrain

### Shot controls must support
- aiming left/right
- adjusting shot direction visually
- adjusting power with either:
  - hold-and-release, or
  - power meter timing system
- confirming the shot

### Shot behavior requirements
- Ball should move in a believable arcade-golf way
- Ball should slow down over time due to friction
- Ball should react to surface type
- Ball should stop completely before the next shot begins
- The player cannot take another shot while the ball is still moving

---

## 7.2 Aiming Requirements
- Aim direction must be clearly visible before the shot
- Aim should be anchored to the ball position
- The player must always know the current intended shot direction
- A shot preview line, arrow, or indicator should be shown
- The UI should make it clear which direction the ball will launch

---

## 7.3 Power Requirements
- Power must be readable and predictable
- Lower power should produce a shorter shot
- Higher power should produce a longer shot
- Power system should feel consistent across shots
- Different clubs should affect the effective distance and control

---

## 7.4 Club System
The player must have multiple club types.

### Minimum required clubs
- Driver
- Iron
- Wedge
- Putter

### Club requirements
Each club should affect at least some of the following:
- max power
- shot distance
- accuracy or forgiveness
- loft / arc feel if implemented
- roll after landing
- special trait or modifier

### Club selection requirements
- Player can switch clubs before taking a shot
- Current club must be shown in the HUD
- Club choice should matter depending on distance and lie

---

## 7.5 Golf Ball System
The player must be able to use golf balls with different stats or behaviors.

### Ball variants should affect one or more of:
- distance
- control
- bounce
- friction interaction
- terrain resistance
- spin potential if implemented

### Ball requirements
- Ball type should matter enough to change gameplay
- Ball type should be visible in menus/shop
- Ball upgrades should carry through a run according to progression rules

---

## 8. Golf Rules for This Game
This game is inspired by golf rules but should use **simplified, game-friendly arcade rules** rather than strict full simulation rules.

## 8.1 Basic Scoring Rules
- Every shot counts as **1 stroke**
- Each hole has a **par value**
- The player’s hole score is the total number of strokes taken to sink the ball
- The game should compare strokes against par
- The scorecard should show:
  - strokes
  - par
  - score relative to par

### Relative score terms
- 1 under par = Birdie
- 2 under par = Eagle
- Even par = Par
- 1 over par = Bogey
- 2 over par = Double Bogey
- More than that may be shown numerically or by standard golf naming if desired

---

## 8.2 Hole Completion Rule
- A hole is complete when the ball enters the cup
- The game should clearly indicate hole completion
- Hole completion should stop gameplay and transition to results / next step

---

## 8.3 Ball at Rest Rule
- The ball is considered playable only after it has fully stopped moving
- Inputs for the next shot should be disabled while the ball is moving

---

## 8.4 Lie Rule
The player’s next shot is taken from wherever the ball comes to rest, unless a penalty or special rule applies.

Possible lies include:
- fairway
- rough
- sand
- green
- trees / obstructed area
- hazard recovery position

Each lie should affect gameplay.

---

## 8.5 Water Hazard Rule
If the ball enters water:
- the player receives a penalty stroke
- the ball is repositioned according to the game’s water recovery rule
- the player then continues play

### Water recovery rule
The game must choose one of the following and use it consistently:
- return the ball to the previous shot position, or
- place the ball at a predefined drop point near the hazard, or
- nearest safe playable position with penalty

The chosen rule must be explained in the UI or help text.

---

## 8.6 Out of Bounds Rule
If the ball goes out of bounds:
- the player receives a penalty stroke
- the ball is reset according to the game’s OOB rule

### Recommended OOB rule
- add 1 penalty stroke
- reset to previous legal position, or nearest valid playable location

---

## 8.7 Trees / Unplayable Lie Rule
If the ball lands in trees or a blocked position, the player should be given a choice, matching the mockup concept:

### Required tree recovery feature
When the ball is in the trees:
- display a prompt
- let the player choose between:
  - **Take a drop**
  - **Play from lie**

### Play from lie
If the player chooses **Play from lie**:
- they shoot from the current location
- obstruction remains part of the challenge
- shot may be harder due to limited line, lower accuracy, blocked path, or terrain penalty

### Take a drop
If the player chooses **Take a drop**:
- the player receives a penalty stroke
- the ball is moved to a nearby valid playable location
- the drop position must be fair and clearly chosen

### Drop requirements
- drop location must not be inside collision or impossible terrain
- drop should be visually clear
- the player should understand that taking a drop costs a stroke

---

## 8.8 Sand Rule
If the ball is in a bunker:
- the next shot should be harder or behave differently
- sand should reduce roll and/or reduce power efficiency
- sand should visually read as a hazard

---

## 8.9 Rough Rule
If the ball is in rough:
- friction should be higher than fairway
- shots should have less ideal rollout and/or slightly lower power efficiency

---

## 8.10 Green Rule
If the ball is on the green:
- putting should feel more precise
- friction should support controlled short shots
- the player should ideally use the putter

---

## 9. Terrain and Surface Requirements

## 9.1 Required Terrain Types
At minimum, the game must support:
- Tee box
- Fairway
- Rough
- Green
- Sand bunker
- Water
- Trees / obstructed zone
- Out-of-bounds area or boundary logic

## 9.2 Terrain Behavior Requirements
Each terrain type should affect one or more of:
- friction
- bounce
- roll
- shot difficulty
- recovery rules
- visual feedback

## 9.3 Terrain Detection
The game must detect what terrain the ball is currently on so that:
- shot behavior can change
- penalties can be applied
- UI prompts can appear
- correct sounds and particles can play

---

## 10. Hazard and Obstacle Requirements

## 10.1 Required Hazards
- Water hazards
- Sand bunkers
- Tree clusters / obstructed lies
- Tight fairways / rough punishment

## 10.2 Optional Advanced Hazards
If time allows:
- moving obstacles
- rotating barriers
- windmills
- elevated hazard difficulty
- hazard-based hole themes

---

## 11. Wind System Requirements
The game should include a wind system.

### Wind requirements
- Wind direction must be displayed in the HUD
- Wind strength must be displayed in the HUD
- Wind should affect the ball during flight if airborne behavior is used
- Wind should matter enough to influence shot decisions
- Wind may vary by hole or difficulty level

### Wind HUD requirements
- A visible arrow or icon
- Numeric or simple strength indicator
- Easy to read at all times

---

## 12. Procedural Course Generation Requirements

## 12.1 General Generation Requirements
- Each run must generate new holes
- No two runs should be guaranteed to be identical
- Generation should create readable, playable golf holes
- Every generated hole must be completable

## 12.2 Hole Components
Each hole must contain:
- a start / tee area
- a destination / cup area
- one or more playable routes
- fairway space
- rough space
- hazards
- boundaries or natural limits

## 12.3 Playability Rules
The generator must avoid creating holes that are:
- impossible to complete
- fully blocked
- unfairly cramped
- visually unreadable
- immediately punishing without player choice

## 12.4 Difficulty Scaling
As the run continues, generated holes may become harder by increasing:
- length
- curvature
- hazard density
- narrow fairways
- awkward landing zones
- wind strength
- recovery difficulty

## 12.5 Variety Requirements
Generated holes should vary in:
- shape
- length
- fairway width
- hazard placement
- tree layout
- risk/reward opportunities

## 12.6 Seeded Generation
If possible, the generator should support a seed system so that:
- runs can be reproduced
- debugging is easier
- special daily or challenge runs could exist later

---

## 13. Roguelite Progression Requirements

## 13.1 Currency System
The player must earn currency during a run.

### Currency can be awarded for:
- completing a hole
- finishing under par
- birdies / eagles
- streaks or bonus performance
- completing a full run

### Currency usage
Currency must be spendable in a shop or upgrade screen.

---

## 13.2 Upgrade System
The game must include an upgrade system for clubs and/or balls.

### Upgrade requirements
- upgrades must have visible stat changes
- upgrades must feel meaningful
- upgrade options must be readable
- player should understand cost and benefit before purchasing

### Upgrade examples
Clubs may improve:
- power
- accuracy
- forgiveness
- special effect

Balls may improve:
- control
- distance
- bounce handling
- terrain handling

---

## 13.3 Upgrade Persistence Rules
The game must clearly define what persists:
- during a hole
- across holes in a run
- across full runs
- across sessions

### Recommended rule set
- Upgrades purchased during a run persist for that run
- High score and some meta progression persist across sessions
- Current run state can be saved and resumed

---

## 14. Scoring and End-of-Run Requirements

## 14.1 Hole Score Tracking
For every hole, the game must track:
- hole number
- par
- stroke count
- result relative to par

## 14.2 Run Score Tracking
At the run level, the game must track:
- total strokes
- total score relative to par
- rewards earned
- best run data if relevant

## 14.3 Scorecard Screen
The game must include a scorecard that shows hole-by-hole results.

### Scorecard should include
- hole number
- par
- strokes
- score relative to par
- total score summary

## 14.4 End-of-Run Screen
At the end of a run, show:
- final total score
- performance summary
- rewards earned
- new high score if achieved
- option to return to menu or start again

---

## 15. User Interface Requirements

## 15.1 Main Menu
The main menu must include:
- New Run
- Continue (if save exists)
- Settings
- High Scores
- Quit

## 15.2 In-Game HUD
The in-game HUD must display, at minimum:
- current hole number
- par
- current stroke count
- active club
- wind direction and strength
- shot / aim related controls or indicators as needed

## 15.3 Shot Interface
The shot interface must make it clear:
- where the ball is
- what club is selected
- which way the player is aiming
- how much power is being used
- when the player can shoot

## 15.4 Tree / Recovery Prompt
The game must show a recovery dialog when the ball is in the trees if that rule is triggered.

The prompt must:
- explain the situation clearly
- present both choices
- indicate penalty if taking a drop
- be easy to dismiss by making a valid choice

## 15.5 Upgrade Shop UI
The upgrade/shop screen must:
- show current currency
- show equipment options
- show cost
- show stat comparison
- clearly confirm purchases

## 15.6 Scorecard UI
The scorecard must be readable and easy to understand at a glance.

---

## 16. Visual Requirements

## 16.1 Art Style
- The game should use a readable 2D pixel-art style or similar stylized arcade presentation
- Terrain types should be easy to distinguish
- Hazards should be visually obvious
- UI should be clean and readable

## 16.2 Ball Visibility
- The ball must always be easy to locate
- Camera and terrain contrast should support visibility
- Shot result should be readable from a distance

## 16.3 Air / Flight Presentation
The team wants to use shaders or visual effects to help the golf ball feel like it is in the air.

### Airborne ball effect requirements
If a ball is considered airborne:
- the player should be able to tell it is elevated
- a shadow or shader-based depth cue should be used
- the effect should improve readability, not reduce it

### Recommended visual methods
- drop shadow that separates from the ball as height increases
- shader-based brightness / scale / shadow offset
- arc trail or subtle effect indicating flight path

---

## 17. Audio Requirements

## 17.1 Required Sound Effects
The game should include sound effects for:
- hitting the ball
- different shot types if possible
- ball landing
- ball entering cup
- water splash
- bunker / sand impact
- UI button press
- purchase / upgrade confirmation

## 17.2 Music Requirements
- Background music should support play without being distracting
- Menus and gameplay may use different music
- Audio should feel cohesive with the arcade golf tone

---

## 18. Save and Persistence Requirements

## 18.1 Required Saved Data
The game must save and load:
- current run progress
- current hole in run
- player score / strokes
- purchased upgrades for current run
- persistent high score
- settings if implemented

## 18.2 Save Behavior
- Player should be able to quit and resume later
- Save data should be loaded correctly on startup
- Corrupt or missing save data should fail gracefully

---

## 19. Controls Requirements
Controls must be simple and responsive.

### Required actions
- move aim left/right
- increase / decrease power or control power timing
- confirm shot
- switch club
- navigate menus
- confirm shop purchases
- select recovery options

### Control principles
- easy to learn
- responsive
- consistent across menus and gameplay

---

## 20. Difficulty and Balance Requirements

## 20.1 Difficulty Curve
The game should start approachable and become harder over time.

### Difficulty can increase through:
- longer holes
- tighter fairways
- more hazards
- stronger wind
- trickier green approaches
- more punishing recovery choices

## 20.2 Fairness Rule
Difficulty should come from challenge, not confusion.  
The game should never feel unfair because of:
- unreadable hazards
- impossible generation
- unclear controls
- invisible penalties
- hidden state

---

## 21. Functional Requirements Summary

The final game must include all of the following:
- playable top-down golf gameplay
- shot aiming system
- power system
- club selection
- terrain interaction
- water hazards
- sand hazards
- trees / obstructed lie system
- take-a-drop vs play-from-lie feature
- scoring by strokes and par
- multiple generated holes per run
- procedural generation
- upgrade shop
- currency rewards
- HUD
- scorecard
- main menu
- save/load
- audio feedback
- high score tracking

---

## 22. Non-Functional Requirements

## 22.1 Performance
- The game should run smoothly during normal play
- Generation should complete quickly enough not to feel stalled
- Inputs should remain responsive

## 22.2 Readability
- UI text must be readable
- Hazards and terrain types must be visually distinct
- Important game information must not be hidden

## 22.3 Maintainability
- Systems should be modular
- Club, ball, and upgrade data should be data-driven where possible
- Procedural generation should be separated from gameplay logic
- Save data should be clearly structured

## 22.4 Robustness
- Edge cases should be handled safely
- Invalid generated holes should be prevented or regenerated
- Ball recovery should not place the player in impossible locations

---

## 23. Stretch Goals
These are optional and should only be attempted after the core game is working well.

- moving hazards
- special themed holes
- seeded daily challenge mode
- online leaderboard
- advanced ball spin / shot shaping
- more club types
- environmental animation
- replay viewer
- better camera transitions
- richer meta progression between runs

---

## 24. Out of Scope
The following are not required unless time allows:
- full realistic golf simulation
- multiplayer
- large open-world hub
- online matchmaking
- full official PGA rulebook implementation
- advanced character customization
- voice acting

---

## 25. Acceptance Criteria

The project will be considered successful if:
1. A player can start a run from the main menu
2. The game generates playable golf holes
3. The player can aim and take shots
4. The ball reacts correctly to terrain and hazards
5. The player can complete holes and receive a score
6. The game tracks score across a run
7. The player can encounter trees and choose to take a drop or play from the lie
8. The player can earn currency and buy upgrades
9. The HUD displays essential gameplay information
10. The game can save and load progress
11. The game ends with a clear score summary and supports replayability

---

## 26. Recommended System Ownership
Suggested team ownership based on current project direction:

- **Procedural generation**: hole layout, terrain logic, hazard placement, difficulty scaling
- **Core gameplay**: ball behavior, shot system, club logic, terrain interactions
- **UI / progression**: HUD, menus, shop, scorecard, audio, save/load

---

## 27. Final Design Principle
Fairway Rogue should feel like a polished arcade golf game first and a technical showcase second. Procedural generation, upgrades, and effects should support the fun of planning a shot, taking the swing, recovering from mistakes, and finishing a run with a score the player wants to beat.
