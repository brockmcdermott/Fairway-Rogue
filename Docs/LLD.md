# Fairway Rogue — Low Level Design Document

## 1. Introduction

### 1.1 Project Name
**Fairway Rogue**

### 1.2 Purpose
This document describes the low-level design for Fairway Rogue, a top-down 2D golf roguelite built in **Godot 4 with C#**. It expands on the High Level Design by defining the internal systems, classes, data structures, scene responsibilities, runtime flow, and implementation details needed to build the game.

### 1.3 Scope
This document covers:
- scene structure
- core managers
- gameplay objects
- procedural generation systems
- terrain and hazard handling
- scoring and progression
- UI implementation
- save/load format
- audio and visual effect integration
- class responsibilities and relationships

This document is meant to help the team implement the game consistently and reduce confusion during integration.

---

## 2. Technical Stack

- **Engine:** Godot 4
- **Language:** C#
- **Rendering:** 2D
- **Persistence:** JSON or ConfigFile save system
- **Architecture Style:** scene-based with manager classes and event-driven communication
- **Target:** desktop first

---

## 3. Low-Level Architecture Overview

The game should be split into the following main implementation layers:

1. **Autoload / Global Managers**
2. **Scene Layer**
3. **Gameplay Entities**
4. **Data / Configuration Objects**
5. **UI Components**
6. **Persistence / Serialization**
7. **Audio / Visual Feedback Helpers**

### 3.1 Autoload Managers
Suggested autoload singletons:
- `GameManager`
- `SaveManager`
- `AudioManager`
- `RunManager`
- `SceneRouter`

### 3.2 Main Scene Types
Suggested scenes:
- `MainMenu.tscn`
- `GameplayScene.tscn`
- `HUD.tscn`
- `RecoveryDialog.tscn`
- `ShopScene.tscn`
- `ScorecardScene.tscn`
- `RunCompleteScene.tscn`
- `SettingsScene.tscn`

### 3.3 Main Runtime Systems
- shot input and execution
- ball movement and stopping
- procedural hole generation
- terrain detection
- hazard evaluation
- scoring and hole completion
- shop and upgrades
- save/load
- UI transitions

---

## 4. Project Folder Structure

A recommended folder structure:

```text
FairwayRogue/
├── Scenes/
│   ├── MainMenu.tscn
│   ├── Gameplay/
│   │   ├── GameplayScene.tscn
│   │   ├── Ball.tscn
│   │   ├── HoleRoot.tscn
│   │   ├── TerrainTileMap.tscn
│   │   ├── HUD.tscn
│   │   └── RecoveryDialog.tscn
│   ├── UI/
│   │   ├── ShopScene.tscn
│   │   ├── ScorecardScene.tscn
│   │   ├── RunCompleteScene.tscn
│   │   └── SettingsScene.tscn
│   └── Common/
│       ├── ButtonPanel.tscn
│       └── StatComparisonPanel.tscn
├── Scripts/
│   ├── Managers/
│   │   ├── GameManager.cs
│   │   ├── RunManager.cs
│   │   ├── SaveManager.cs
│   │   ├── AudioManager.cs
│   │   └── SceneRouter.cs
│   ├── Gameplay/
│   │   ├── BallController.cs
│   │   ├── ShotController.cs
│   │   ├── ClubController.cs
│   │   ├── LieEvaluator.cs
│   │   ├── HazardResolver.cs
│   │   ├── HoleController.cs
│   │   ├── WindController.cs
│   │   └── DistanceCalculator.cs
│   ├── Generation/
│   │   ├── HoleGenerator.cs
│   │   ├── HoleLayout.cs
│   │   ├── FairwayPathBuilder.cs
│   │   ├── HazardPlacer.cs
│   │   ├── TerrainPainter.cs
│   │   └── HoleValidator.cs
│   ├── UI/
│   │   ├── HUDController.cs
│   │   ├── RecoveryDialogController.cs
│   │   ├── ShopController.cs
│   │   ├── ScorecardController.cs
│   │   └── RunCompleteController.cs
│   ├── Data/
│   │   ├── ClubData.cs
│   │   ├── BallData.cs
│   │   ├── UpgradeData.cs
│   │   ├── HoleResultData.cs
│   │   ├── RunSaveData.cs
│   │   └── SettingsData.cs
│   └── Effects/
│       ├── BallShadowController.cs
│       ├── BallFlightEffectController.cs
│       └── TerrainSfxResolver.cs
├── Resources/
│   ├── Clubs/
│   ├── Balls/
│   ├── Upgrades/
│   └── Audio/
├── Saves/
└── Assets/
```

## 5. Global Managers

### 5.1 GameManager

**Purpose:** owns high-level game state and coordinates major transitions.

**Responsibilities**
- track current game state
- start a new run
- resume a saved run
- notify systems when state changes
- route flow between gameplay, shop, and score screens

**Suggested enum**
```csharp
public enum GameState
{
    MainMenu,
    LoadingHole,
    InHole,
    RecoveryPrompt,
    HoleComplete,
    Shop,
    Scorecard,
    RunComplete,
    Paused
}
```

**Key methods**
```csharp
void ChangeState(GameState newState);
void StartNewRun();
void ResumeRun();
void CompleteHole();
void CompleteRun();
void PauseGame();
void UnpauseGame();
```

### 5.2 RunManager

**Purpose:** owns run-specific gameplay progression.

**Responsibilities**
- track hole number
- track total strokes
- track currency
- store purchased upgrades
- store hole results
- supply current loadout data to gameplay systems

**Fields**
```csharp
int CurrentHoleIndex;
int TotalHoles;
int Currency;
List<HoleResultData> HoleResults;
PlayerLoadout CurrentLoadout;
int TotalStrokes;
int TotalPar;
int Seed;
```

**Key methods**
```csharp
void StartRun(int seed);
void AddStroke();
void AwardCurrency(int amount);
bool SpendCurrency(int amount);
void RecordHoleResult(HoleResultData result);
bool IsFinalHole();
int GetScoreRelativeToPar();
```

### 5.3 SaveManager

**Purpose:** serialize and deserialize persistent data.

**Responsibilities**
- save current run progress
- load saved run
- save high score and best run data
- save settings
- gracefully handle missing/corrupted save data

**Key methods**
```csharp
void SaveRun(RunSaveData data);
RunSaveData LoadRun();
void SaveHighScore(int score);
int LoadHighScore();
void SaveSettings(SettingsData settings);
SettingsData LoadSettings();
bool HasRunSave();
void DeleteRunSave();
```

### 5.4 AudioManager

**Purpose:** centralize sound effect and music playback.

**Responsibilities**
- play shot sounds
- play terrain interaction sounds
- switch menu/game music
- prevent duplicate audio spam
- expose simple event-based methods

**Key methods**
```csharp
void PlaySfx(string key);
void PlayMusic(string key);
void StopMusic();
void FadeToMusic(string key);
```

### 5.5 SceneRouter

**Purpose:** centralize scene changes and scene instantiation.

**Responsibilities**
- load scene by path
- switch between menu/gameplay/shop/scorecard scenes
- optionally preload common scenes

**Key methods**
```csharp
void GoToMainMenu();
void GoToGameplay();
void GoToShop();
void GoToScorecard();
void GoToRunComplete();
```

## 6. Scene-Level Design

### 6.1 MainMenu.tscn

**Node responsibilities**
- show New Run
- show Continue if save exists
- show High Scores
- show Settings
- show Quit

**Script**
`MainMenuController.cs`

**Interactions**
- calls `GameManager.StartNewRun()`
- calls `GameManager.ResumeRun()`

### 6.2 GameplayScene.tscn

This is the main runtime play scene.

**Suggested child nodes**
```text
GameplayScene
├── Camera2D
├── HoleRoot
├── Ball
├── ShotController
├── WindController
├── HUD
├── RecoveryDialog
├── EffectsRoot
└── AudioPlayers
```

**Responsibilities**
- host current hole terrain and hazard zones
- host ball entity
- connect hole generation output to gameplay objects
- route hazard events to recovery systems

**Script**
`GameplaySceneController.cs`

### 6.3 ShopScene.tscn

**Responsibilities**
- display current currency
- show upgrade options
- show current club/ball stats
- process purchases
- return to next hole

**Script**
`ShopController.cs`

### 6.4 ScorecardScene.tscn

**Responsibilities**
- show per-hole results
- show total run score
- show score relative to par
- allow transition onward

**Script**
`ScorecardController.cs`

### 6.5 RunCompleteScene.tscn

**Responsibilities**
- show final score
- show rewards
- show high score / best run update
- offer restart or menu option

**Script**
`RunCompleteController.cs`

## 7. Gameplay Class Design

### 7.1 BallController

**Purpose:** controls ball movement, terrain evaluation hooks, and stop detection.

**Likely Godot base**
`CharacterBody2D` or `RigidBody2D`

For this game, `CharacterBody2D` may be simpler and more deterministic for arcade tuning. `RigidBody2D` is possible, but tuning may be harder.

**Responsibilities**
- receive launch vector from shot system
- move ball frame-by-frame
- apply friction
- detect stopping
- detect terrain beneath ball
- signal hazard entry
- signal cup entry

**Fields**
```csharp
Vector2 Velocity;
bool IsMoving;
bool IsAirborne;
float CurrentHeightVisual;
TerrainType CurrentTerrain;
Vector2 LastSafePosition;
Vector2 PreviousShotPosition;
```

**Signals / events**
```csharp
event Action BallStopped;
event Action<TerrainType> TerrainChanged;
event Action<HazardType> HazardEntered;
event Action CupReached;
```

**Key methods**
```csharp
void Launch(Vector2 direction, float power, ClubData club, BallData ballData);
void ApplyFriction(float delta);
void EvaluateTerrain();
void HandleCollision();
void StopBall();
void SetPositionToDrop(Vector2 newPosition);
void SetPreviousShotPosition(Vector2 pos);
```

### 7.2 ShotController

**Purpose:** manages pre-shot aiming, power input, and shot confirmation.

**Responsibilities**
- read input only when a shot is allowed
- rotate or move aim direction
- manage power meter
- request current club and ball modifiers
- launch ball
- disable input during motion

**Fields**
```csharp
bool CanShoot;
float CurrentPower;
float MinPower;
float MaxPower;
float AimAngleDegrees;
bool IsCharging;
ClubData SelectedClub;
```

**Key methods**
```csharp
void BeginCharge();
void EndChargeAndShoot();
void RotateAim(float amount);
void CycleClub(int direction);
Vector2 GetAimDirection();
float GetEffectivePower();
```

**Dependencies**
- `BallController`
- `ClubController`
- `HUDController`
- `RunManager`

### 7.3 ClubController

**Purpose:** manages club selection and resolves current club stats.

**Responsibilities**
- hold currently available clubs
- expose selected club
- support cycling through clubs
- apply upgrade changes

**Fields**
```csharp
List<ClubData> Clubs;
int SelectedClubIndex;
```

**Key methods**
```csharp
ClubData GetCurrentClub();
void SelectNextClub();
void SelectPreviousClub();
void ApplyUpgrade(string clubId, UpgradeData upgrade);
```

### 7.4 WindController

**Purpose:** stores and provides wind force for the current hole.

**Fields**
```csharp
Vector2 WindDirection;
float WindStrength;
```

**Key methods**
```csharp
void GenerateWind(HoleDifficulty difficulty);
Vector2 GetWindForce();
string GetHudLabel();
```

**Notes**
If the shot supports visible airborne travel, wind should only meaningfully affect the ball while in an airborne phase or during initial launch frames.

### 7.5 LieEvaluator

**Purpose:** determine what lie the player currently has.

**Possible lie types**
```csharp
public enum LieType
{
    Tee,
    Fairway,
    Rough,
    Sand,
    Green,
    Trees,
    Water,
    OutOfBounds
}
```

**Responsibilities**
- inspect terrain under/around the ball
- determine if lie is obstructed by trees
- decide whether recovery prompt is needed

**Key methods**
```csharp
LieType EvaluateLie(BallController ball);
bool IsObstructedLie(BallController ball);
Vector2 FindNearestDropPosition(Vector2 ballPosition);
```

### 7.6 HazardResolver

**Purpose:** apply rules when hazards occur.

**Responsibilities**
- add penalty strokes
- reposition ball
- trigger recovery prompt
- coordinate with UI

**Key methods**
```csharp
void ResolveWaterHazard(BallController ball);
void ResolveOutOfBounds(BallController ball);
void ResolveTreeLie(BallController ball);
void ApplyDrop(BallController ball, Vector2 dropPosition);
void ResumeFromLie(BallController ball);
```

**Example rule behavior**
- water: +1 stroke, move to previous safe or nearest drop zone
- OOB: +1 stroke, move to previous legal position
- trees: prompt player to choose drop or play from lie

### 7.7 HoleController

**Purpose:** owns current hole runtime state.

**Responsibilities**
- store hole metadata
- track strokes for this hole
- determine completion
- expose par and distance info
- own terrain references and cup reference

**Fields**
```csharp
int HoleNumber;
int Par;
int StrokeCount;
Vector2 TeePosition;
Vector2 CupPosition;
HoleLayout Layout;
```

**Key methods**
```csharp
void StartHole(HoleLayout layout, int holeNumber);
void AddStroke();
void CompleteHole();
float GetDistanceToCup(Vector2 ballPosition);
HoleResultData BuildHoleResult();
```

## 8. Procedural Generation Design

### 8.1 HoleLayout Data Structure

This is the generated runtime output used by gameplay.

```csharp
public class HoleLayout
{
    public int HoleNumber;
    public int Par;
    public int Seed;
    public Vector2 TeePosition;
    public Vector2 CupPosition;
    public List<Vector2> FairwayPathPoints;
    public List<HazardRegionData> Hazards;
    public List<TreeClusterData> TreeClusters;
    public Vector2 WindDirection;
    public float WindStrength;
    public Rect2 Bounds;
}
```

### 8.2 HoleGenerator

**Purpose:** main generation coordinator.

**Responsibilities**
- create hole layout from seed and difficulty
- call sub-builders
- validate layout
- regenerate if invalid

**Key methods**
```csharp
HoleLayout GenerateHole(int holeNumber, int seed, HoleDifficulty difficulty);
```

**Generation flow**
- create tee and cup anchors
- generate fairway spline/path
- determine fairway width per segment
- assign par based on length and complexity
- place hazards and trees
- generate wind
- validate
- return `HoleLayout`

### 8.3 FairwayPathBuilder

**Purpose:** generate path from tee to cup.

**Possible implementation**
- use control points
- apply noise or offset curvature
- generate smooth spline
- ensure path stays inside bounds

**Key methods**
```csharp
List<Vector2> BuildPath(Vector2 tee, Vector2 cup, int seed, HoleDifficulty difficulty);
```

### 8.4 HazardPlacer

**Purpose:** place hazards using layout rules.

**Responsibilities**
- place bunkers near landing zones or green
- place water as risk/reward obstacle
- place trees to shape shot routes
- avoid impossible total blockage

**Key methods**
```csharp
List<HazardRegionData> PlaceHazards(HoleLayout baseLayout, int seed);
List<TreeClusterData> PlaceTrees(HoleLayout baseLayout, int seed);
```

### 8.5 TerrainPainter

**Purpose:** paint generated layout into TileMap or region data.

**Responsibilities**
- create tee area
- paint fairway
- paint rough around fairway
- paint green around cup
- fill hazard zones
- place tree colliders/zones

**Key methods**
```csharp
void PaintHole(TileMapLayer terrainLayer, HoleLayout layout);
```

### 8.6 HoleValidator

**Purpose:** ensure generated holes are playable.

**Validation checks**
- tee and cup not overlapping hazards
- fairway path exists
- width never collapses below minimum
- cup reachable
- water does not fully block route
- tree clusters do not create impossible shot progression

**Key methods**
```csharp
bool Validate(HoleLayout layout);
List<string> GetValidationErrors(HoleLayout layout);
```

## 9. Terrain and Hazard Data

### 9.1 TerrainType
```csharp
public enum TerrainType
{
    Tee,
    Fairway,
    Rough,
    Sand,
    Green,
    Water,
    Trees,
    OutOfBounds
}
```

### 9.2 TerrainProperties
```csharp
public class TerrainProperties
{
    public TerrainType Type;
    public float Friction;
    public float PowerMultiplier;
    public float AccuracyPenalty;
    public bool IsHazard;
    public bool RequiresRecoveryPrompt;
}
```

**Example values**
- Fairway: balanced friction, no penalty
- Rough: higher friction, slight power penalty
- Sand: very high friction, greater power penalty
- Green: low friction, precise short-roll feel
- Trees: may trigger obstructed-lie logic
- Water: immediate penalty trigger

## 10. Golf Rules Implementation Detail

### 10.1 Stroke Counting

A stroke is added:
- whenever the player executes a shot
- whenever a penalty stroke is applied

**Implementation detail**
- regular shot: `HoleController.AddStroke()`
- penalty event: `RunManager.AddStroke()` and/or `HoleController.AddStroke()`

**Best practice:** the `HoleController` owns hole-local strokes, while `RunManager` aggregates total results.

### 10.2 Water Hazard Rule

**Recommended implementation**
On water entry:
- ball stops
- +1 penalty
- move to `PreviousShotPosition` or nearest safe drop point
- allow new shot

**Event flow**
- `BallController` detects water region
- raises `HazardEntered(Water)`
- `HazardResolver.ResolveWaterHazard()`
- update strokes
- reposition ball
- refresh HUD

### 10.3 Out of Bounds Rule

**Recommended implementation**
- +1 penalty
- reposition to previous legal position

### 10.4 Trees / Take a Drop Rule

This is one of the more specific systems.

**Detection**

The lie qualifies as “in the trees” when:
- ball is inside a tree zone, or
- line-of-shot clearance is sufficiently obstructed, or
- terrain metadata marks the lie as tree-covered

**Player choices**
- Play from lie
- Take a drop

**Play from lie**
- no reposition
- no penalty unless rule says otherwise
- shot continues using current lie penalties

**Take a drop**
- +1 penalty stroke
- move ball to valid drop point returned by `LieEvaluator`

**Drop point selection**

Drop point should:
- be close to current lie
- be outside blocking terrain
- be on valid playable terrain
- not place the ball inside another hazard

**Event flow**
- `BallController` stops
- `LieEvaluator.IsObstructedLie()` returns true
- `GameManager.ChangeState(RecoveryPrompt)`
- `RecoveryDialog` shown
- player picks one option
- `HazardResolver` resolves choice
- state returns to `InHole`

## 11. UI Low-Level Design

### 11.1 HUDController

**Responsibilities**
- show hole number
- show par
- show strokes
- show current club
- show wind
- optionally show distance to cup
- show power meter
- show currency if desired

**Methods**
```csharp
void SetHoleInfo(int holeNumber, int par);
void SetStrokeCount(int strokes);
void SetClubName(string clubName);
void SetWind(Vector2 direction, float strength);
void SetDistance(float yards);
void SetPowerMeter(float normalizedPower);
```

### 11.2 RecoveryDialogController

**Responsibilities**
- display “In the trees” message
- display buttons for:
  - take a drop
  - play from lie
- route selection to resolver

**Methods**
```csharp
void Show();
void Hide();
void OnTakeDropPressed();
void OnPlayFromLiePressed();
```

### 11.3 ShopController

**Responsibilities**
- render available upgrades
- show costs
- compare current and upgraded stats
- gray out unavailable options
- handle purchase confirmation

**Data it needs**
- current currency
- club list
- ball list
- upgrade definitions
- purchased state

**Methods**
```csharp
void PopulateShop();
void RefreshCurrency();
void AttemptPurchase(string upgradeId);
void UpdateStatPreview();
```

### 11.4 ScorecardController

**Responsibilities**
- create one row per hole
- show par, strokes, relative score
- show totals
- highlight strong results if desired

**Methods**
```csharp
void LoadResults(List<HoleResultData> results);
void ShowTotals(int totalStrokes, int totalPar);
```

### 11.5 RunCompleteController

**Responsibilities**
- show final summary
- compare against high score
- present buttons for replay/menu

## 12. Data Models

### 12.1 ClubData

Use `Resource` files or plain classes.

```csharp
public class ClubData
{
    public string Id;
    public string Name;
    public float BasePower;
    public float Accuracy;
    public float LoftFactor;
    public float RollModifier;
    public List<string> UpgradeIds;
}
```

### 12.2 BallData
```csharp
public class BallData
{
    public string Id;
    public string Name;
    public float DistanceModifier;
    public float ControlModifier;
    public float BounceModifier;
    public float TerrainResistanceModifier;
}
```

### 12.3 UpgradeData
```csharp
public class UpgradeData
{
    public string Id;
    public string TargetType;
    public string TargetId;
    public string Name;
    public string Description;
    public int Cost;
    public float PowerDelta;
    public float AccuracyDelta;
    public float ControlDelta;
    public float BounceDelta;
}
```

### 12.4 HoleResultData
```csharp
public class HoleResultData
{
    public int HoleNumber;
    public int Par;
    public int Strokes;
    public int ScoreRelativeToPar;
    public string Label;
}
```

### 12.5 PlayerLoadout
```csharp
public class PlayerLoadout
{
    public List<ClubData> Clubs;
    public BallData EquippedBall;
    public List<string> PurchasedUpgradeIds;
}
```

### 12.6 RunSaveData
```csharp
public class RunSaveData
{
    public int Seed;
    public int CurrentHoleIndex;
    public int Currency;
    public int TotalStrokes;
    public List<HoleResultData> HoleResults;
    public PlayerLoadout CurrentLoadout;
    public Vector2 BallPosition;
    public int CurrentHolePar;
    public int CurrentHoleStrokes;
}
```

### 12.7 SettingsData
```csharp
public class SettingsData
{
    public float MasterVolume;
    public float MusicVolume;
    public float SfxVolume;
    public bool Fullscreen;
}
```

## 13. Save/Load Format

### 13.1 Recommended JSON shape
```json
{
  "seed": 12345,
  "currentHoleIndex": 3,
  "currency": 1200,
  "totalStrokes": 14,
  "holeResults": [
    {
      "holeNumber": 1,
      "par": 4,
      "strokes": 3,
      "scoreRelativeToPar": -1,
      "label": "Birdie"
    }
  ],
  "currentLoadout": {
    "purchasedUpgradeIds": ["driver_power_1", "ball_control_1"]
  },
  "ballPosition": {
    "x": 512.0,
    "y": 384.0
  }
}
```

### 13.2 Save timing

Save should happen:
- between holes
- after purchases
- when pausing/quitting mid-run
- optionally after every shot stop for safety

## 14. Audio Event Mapping

### 14.1 Required sound events
- `shot_driver`
- `shot_iron`
- `shot_wedge`
- `shot_putter`
- `ball_land`
- `ball_in_cup`
- `water_splash`
- `sand_hit`
- `ui_click`
- `purchase_success`
- `recovery_prompt_open`

**Example integration**

`BallController` and `HazardResolver` should not play raw files directly. They should call:

```csharp
AudioManager.PlaySfx("water_splash");
```

## 15. Ball-in-Air Shader / Visual Effect Design

### 15.1 Goal

Make the golf ball visually feel elevated during flight without making gameplay confusing.

### 15.2 Recommended implementation

Use a presentation-only effect system.

**Likely components**
- `BallShadowController`
- `BallFlightEffectController`

**Visual behavior**

When shot power exceeds a loft threshold or when club loft is high:
- ball sprite rises visually using `CurrentHeightVisual`
- shadow offsets away from the ball
- shadow becomes softer or smaller/larger based on style
- ball may brighten slightly or trail slightly

**Important rule**

This is mainly visual and should not complicate core collision logic unless the team intentionally adds true projectile phases.

**Possible runtime fields**
```csharp
float VisualHeight;
float VisualHeightVelocity;
bool UseFlightEffect;
```

**Update idea**
- set initial `VisualHeight` from club loft and shot power
- decay height over time
- map height to shadow offset and ball sprite Y offset

## 16. Gameplay Flow in Detail

### 16.1 Starting a New Run
- player presses New Run
- `GameManager.StartNewRun()`
- `RunManager.StartRun(seed)`
- scene changes to gameplay
- `HoleGenerator.GenerateHole(...)`
- `TerrainPainter.PaintHole(...)`
- `HoleController.StartHole(...)`
- `BallController` placed at tee
- HUD initialized
- state changes to `InHole`

### 16.2 Taking a Shot
- player aims
- player adjusts power
- player confirms shot
- `ShotController.EndChargeAndShoot()`
- stroke added
- `BallController.Launch(...)`
- HUD disables shot input
- ball moves
- terrain and hazard checks run
- ball stops
- evaluate lie
- either recovery prompt or next shot phase begins

### 16.3 Completing a Hole
- ball reaches cup
- `HoleController.CompleteHole()`
- build `HoleResultData`
- `RunManager.RecordHoleResult(...)`
- award currency
- save run
- go to scorecard or shop
- load next hole or finish run

### 16.4 Tree Recovery Prompt
- ball stops in trees
- `LieEvaluator` flags obstructed lie
- game enters `RecoveryPrompt`
- dialog shown
- player picks:
  - Take a drop
  - Play from lie
- resolver applies choice
- game returns to `InHole`

## 17. Error Handling / Edge Cases

### 17.1 Invalid Generated Hole

If validation fails:
- regenerate using modified seed
- repeat until max attempts reached
- fall back to safe template hole if needed

### 17.2 Ball Stuck in Collision

If ball is stuck:
- detect low-speed/no-progress state
- snap to nearest safe playable cell
- log warning in debug build

### 17.3 Invalid Drop Location

If drop search fails:
- use previous safe position
- as final fallback, use previous shot position

### 17.4 Corrupt Save

If save parsing fails:
- ignore run save
- return to menu
- preserve settings/high score if separate and valid

## 18. Integration Plan

### 18.1 Recommended build order

**Step 1**
Implement:
- ball movement
- shot system
- one static hole
- hole completion

**Step 2**
Add:
- terrain friction
- water
- sand
- trees
- recovery prompt

**Step 3**
Add:
- scoring
- HUD
- club selection
- wind

**Step 4**
Add:
- hole generation
- validation
- multiple holes
- run tracking

**Step 5**
Add:
- shop
- upgrades
- save/load
- audio
- visual effects

## 19. Testing Strategy

### 19.1 Unit / Logic Tests

Good candidates:
- score relative to par calculation
- currency award calculation
- upgrade purchase logic
- save serialization
- drop point validity
- hole validation rules

### 19.2 Manual Gameplay Tests
- take shots from every terrain type
- verify penalty handling
- verify tree prompt appears correctly
- verify scorecard totals
- verify run resume
- verify wind is shown and affects play
- verify no impossible generated holes

### 19.3 Tuning Tests
- ball friction values
- club power values
- bunker punishment
- rough punishment
- drop fairness
- procedural hole readability

## 20. Team Mapping to LLD

### Brock McDermott

**Likely ownership:**
- `HUDController`
- `RecoveryDialogController`
- `ShopController`
- `ScorecardController`
- `RunCompleteController`
- `SaveManager`
- audio hookups
- menu scenes

### Isaac Burton

**Likely ownership:**
- `BallController`
- `ShotController`
- `ClubController`
- `WindController`
- gameplay state transitions
- ball feel tuning

### Ben Hickenlooper

**Likely ownership:**
- `HoleGenerator`
- `FairwayPathBuilder`
- `HazardPlacer`
- `TerrainPainter`
- `HoleValidator`
- difficulty scaling

### Shared
- integration in `GameplayScene`
- rules tuning
- testing and bug fixing
- polish

## 21. Final Notes

The low-level design for Fairway Rogue is built around a modular, scene-driven structure that separates:
- gameplay logic
- procedural generation
- rules/hazard handling
- UI
- persistence

This is important because the biggest technical risk is integration between generation, hazard logic, and the shot system. By keeping those systems isolated and communicating through clean events and shared data models, the team should be able to build features in parallel and integrate them more safely.

The most important thing to get right first is the playable golf loop:
- aim
- shoot
- roll
- hazard
- recover
- score
- continue

Everything else should support that loop.