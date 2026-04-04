# TESTING.md

# Fairway Rogue — Testing Guide

This document defines the testing strategy, acceptance checks, smoke tests, regression checks, and manual test cases for **Fairway Rogue**, a top-down 2D golf roguelite built in **Godot 4 with C#**.

The goal of this file is to make it easy for:
- developers
- teammates
- AI coding agents
- reviewers

to know when a feature is actually working and when a change is safe to merge.

This file should be used together with:
- `Requirements.md`
- `HLD.md`
- `LLD.md`
- `AGENTS.md`
- `ArchitectureRules.md`

---

## 1. Testing Philosophy

Fairway Rogue should be tested in layers:

1. **Core gameplay correctness**
2. **Rules and hazard correctness**
3. **Procedural generation correctness**
4. **Run progression correctness**
5. **UI correctness**
6. **Persistence correctness**
7. **Polish / presentation correctness**
8. **Regression testing after changes**

The most important thing to protect is the main gameplay loop:

- aim
- choose club
- set power
- shoot
- ball moves
- ball stops
- evaluate lie/hazard
- resolve penalties or recovery
- finish hole
- score
- continue run

If this loop breaks, the build is not acceptable.

---

## 2. Definition of Done

A feature is only considered done if:

1. it compiles
2. it works in the game scene/runtime
3. it follows the design docs
4. it does not obviously break existing core systems
5. it handles major expected edge cases
6. it is readable and maintainable
7. it passes relevant manual tests
8. it passes relevant logic/unit tests if those exist

A feature is **not done** just because:
- the code was written
- the scene loads
- the UI appears
- one ideal path works

---

## 3. Testing Layers

## 3.1 Unit / Logic Testing
Use unit-style or isolated logic tests for systems that do not require full scene interaction.

Good candidates:
- score calculations
- currency reward calculations
- upgrade purchase logic
- save serialization
- drop point selection
- hole validation logic
- deterministic generator seed behavior
- terrain property lookups

## 3.2 Integration Testing
Use integration tests or controlled runtime tests for systems that work together.

Good candidates:
- shot system + ball movement
- ball stop + lie evaluation
- ball hazard entry + hazard resolver
- hole completion + score tracking
- shop purchase + run state update
- save/load + run restoration
- generator output + terrain painter + playable runtime

## 3.3 Manual Gameplay Testing
Manual testing is required for:
- game feel
- usability
- readability
- procedural variety
- hazard fairness
- tree recovery flow
- wind readability
- visual polish
- audio timing

---

## 4. Minimum Testing Rules for Every Change

Any meaningful change should be tested at least at the level it affects.

### If you change gameplay code:
test:
- shot still works
- ball still stops correctly
- score still tracks correctly
- hazards still work if related systems were touched

### If you change hazard code:
test:
- water
- sand
- trees
- out-of-bounds
- stroke penalties
- repositioning

### If you change procedural generation:
test:
- tee placement
- cup placement
- fairway readability
- at least one valid route
- no obviously impossible holes
- multiple seeds

### If you change UI:
test:
- correct data shown
- buttons call the correct systems
- no duplicated or stale state
- inputs still work in the intended scene flow

### If you change save/load:
test:
- save is created
- save is loaded
- current run restores correctly
- bad or missing save fails safely

### If you change visual effects:
test:
- gameplay is still readable
- ball position is still understandable
- no logic depends on the effect unless intended

---

## 5. Smoke Test Checklist

This is the minimum smoke test list for a playable build.

A build should pass all of these before being considered healthy.

### 5.1 Main menu smoke test
- game launches successfully
- main menu appears
- New Run works
- Continue works if save exists
- Settings opens if implemented
- High Scores opens if implemented
- Quit works

### 5.2 Core hole smoke test
- a hole loads
- ball appears on tee
- player can aim
- player can set power
- player can shoot
- ball moves
- ball stops
- player can take another shot only after the ball stops
- ball can enter the cup
- hole completes correctly

### 5.3 Hazard smoke test
- water applies penalty and repositions ball
- sand changes shot feel
- rough changes rollout
- tree lie can trigger recovery choice
- out-of-bounds applies penalty and recovery

### 5.4 Run progression smoke test
- hole result is recorded
- currency is awarded
- scorecard shows correct data
- next hole loads
- run can complete
- run complete screen appears

### 5.5 Save/load smoke test
- save can be created
- saved run can be resumed
- important run state restores correctly
- invalid save does not crash the game

---

## 6. Core Gameplay Test Cases

## 6.1 Shot System Tests

### Test: player can aim before a shot
**Setup**
- load a playable hole
- place ball on tee
- ensure shot input is enabled

**Steps**
1. rotate aim left
2. rotate aim right

**Expected**
- aim indicator updates correctly
- indicator remains anchored to ball
- aim is readable
- no shot occurs until confirmed

---

### Test: player can charge or set shot power
**Setup**
- ball at rest
- shot available

**Steps**
1. begin power input
2. hold or adjust power
3. confirm shot

**Expected**
- power meter or charge state updates
- final shot power matches chosen value
- no invalid values outside allowed min/max range

---

### Test: shot launches ball once per input
**Setup**
- ball at rest
- shot available

**Steps**
1. confirm shot once

**Expected**
- exactly one stroke is added
- exactly one launch occurs
- ball enters movement state
- no duplicate launches from one input

---

### Test: cannot shoot while ball is moving
**Setup**
- launch a shot

**Steps**
1. try to aim or shoot again while the ball is rolling

**Expected**
- new shot input is blocked
- no extra stroke is added
- no duplicate launch happens

---

### Test: ball comes to complete rest
**Setup**
- launch moderate shot

**Steps**
1. wait for ball to slow down

**Expected**
- ball eventually stops
- stop event/state is reached
- next shot becomes available
- ball does not jitter forever

---

## 6.2 Club System Tests

### Test: player can switch clubs
**Setup**
- ball at rest
- multiple clubs unlocked/available

**Steps**
1. switch to next club
2. switch to previous club

**Expected**
- selected club changes
- HUD updates
- shot behavior uses selected club stats

---

### Test: different clubs affect shot behavior
**Setup**
- use the same ball position on same terrain

**Steps**
1. shoot with Driver at a given power
2. reset
3. shoot with Wedge at similar power
4. reset
5. shoot with Putter at similar power

**Expected**
- shot distances/behavior differ meaningfully
- club identity is clear
- results are consistent with club design

---

## 6.3 Ball Variant Tests

### Test: equipped ball changes behavior
**Setup**
- same hole
- same shot setup
- different ball types available

**Steps**
1. hit same shot with control ball
2. repeat with distance ball

**Expected**
- differences in distance/control/bounce are visible
- effect is understandable and not random

---

## 7. Terrain Test Cases

## 7.1 Fairway Tests

### Test: fairway provides normal rollout
**Setup**
- place ball on fairway

**Steps**
1. take medium-power shot

**Expected**
- ball moves with baseline friction
- rollout feels normal
- no penalty applied

---

## 7.2 Rough Tests

### Test: rough slows the ball more than fairway
**Setup**
- identical shot setup on fairway and rough

**Steps**
1. shoot from fairway
2. reset
3. shoot from rough

**Expected**
- rough result stops sooner and/or loses more effective power
- difference is noticeable but not extreme unless designed that way

---

## 7.3 Sand Tests

### Test: bunker shot feels more punishing
**Setup**
- place ball in bunker

**Steps**
1. attempt shot with normal power

**Expected**
- reduced rollout and/or power efficiency
- bunker behavior is distinct from rough/fairway
- no incorrect hazard penalty just for being in sand

---

## 7.4 Green Tests

### Test: green supports controlled short shots
**Setup**
- place ball on green

**Steps**
1. use putter for short shot

**Expected**
- precise movement
- controlled short roll
- putting is easier to read than non-green surfaces

---

## 7.5 Terrain Detection Tests

### Test: ball correctly identifies terrain transitions
**Setup**
- create a hole with clearly adjacent terrain zones

**Steps**
1. roll ball from fairway to rough
2. roll ball from rough to green
3. roll ball into bunker

**Expected**
- current terrain updates correctly
- physics behavior updates accordingly
- HUD/debug indicators update if available

---

## 8. Hazard Test Cases

## 8.1 Water Hazard Tests

### Test: water applies one penalty stroke
**Setup**
- position ball so next shot enters water

**Steps**
1. hit ball into water

**Expected**
- 1 penalty stroke is added
- water sound/effect plays if implemented
- ball is repositioned according to rule
- player can continue from valid position

---

### Test: water recovery uses valid safe position
**Setup**
- hit ball into water near edge cases

**Steps**
1. repeat water entry near different edges or corners

**Expected**
- recovery location is legal
- ball is not placed inside collision
- ball is not placed inside another invalid hazard
- recovery position is understandable/fair

---

## 8.2 Out-of-Bounds Tests

### Test: OOB applies penalty and reposition
**Setup**
- hit toward out-of-bounds boundary

**Steps**
1. send ball OOB

**Expected**
- penalty stroke is added
- ball repositions to previous legal or safe position
- next shot can continue normally

---

## 8.3 Tree / Obstructed Lie Tests

### Test: obstructed tree lie triggers recovery prompt
**Setup**
- land ball in marked tree area or obstructed lie

**Steps**
1. let ball stop in trees

**Expected**
- game detects obstructed lie
- normal shot flow pauses
- recovery dialog appears
- dialog clearly offers:
  - Take a drop
  - Play from lie

---

### Test: choosing “Play from lie” keeps current position
**Setup**
- recovery prompt active

**Steps**
1. choose “Play from lie”

**Expected**
- no reposition occurs
- no drop penalty is added unless design says otherwise
- player resumes shot flow from current location

---

### Test: choosing “Take a drop” adds penalty and repositions
**Setup**
- recovery prompt active

**Steps**
1. choose “Take a drop”

**Expected**
- 1 penalty stroke is added
- ball moves to valid drop location
- location is playable
- UI closes and gameplay resumes

---

### Test: drop location is not invalid
**Setup**
- repeat drop scenarios in difficult tree layouts

**Steps**
1. trigger multiple tree drops in different areas

**Expected**
- drop point is not inside:
  - tree collider
  - water
  - wall
  - out-of-bounds
- drop remains near enough to feel fair

---

## 9. Scoring Test Cases

## 9.1 Stroke Count Tests

### Test: regular shots add one stroke
**Setup**
- start hole at 0 strokes

**Steps**
1. take one shot
2. take second shot

**Expected**
- stroke count becomes 1, then 2
- no duplicate increments

---

### Test: penalties add strokes correctly
**Setup**
- hit ball into water or OOB

**Steps**
1. take shot
2. trigger penalty

**Expected**
- total strokes reflect both shot and penalty
- displayed value matches internal result

---

## 9.2 Hole Result Tests

### Test: hole score relative to par is correct
**Setup**
- hole par known, such as par 4

**Steps**
1. finish in 3 strokes
2. finish in 4 strokes
3. finish in 5 strokes

**Expected**
- 3 = Birdie / -1
- 4 = Par / 0
- 5 = Bogey / +1

---

### Test: hole completion records result once
**Setup**
- ball near cup

**Steps**
1. sink ball

**Expected**
- hole complete triggers once
- score recorded once
- no duplicate currency or duplicate transitions occur

---

## 9.3 Run Total Tests

### Test: total run score updates after each hole
**Setup**
- complete multiple holes

**Steps**
1. finish hole 1
2. finish hole 2
3. inspect scorecard

**Expected**
- hole results appear in order
- total strokes and relative score are correct
- no hole result is missing or duplicated

---

## 10. Currency and Upgrade Test Cases

## 10.1 Currency Reward Tests

### Test: currency awarded after hole completion
**Setup**
- complete a hole

**Steps**
1. finish at par
2. finish under par if supported
3. finish over par if supported

**Expected**
- currency is awarded according to rule
- amount is consistent with design
- UI updates correctly

---

## 10.2 Shop Purchase Tests

### Test: purchase succeeds when player has enough currency
**Setup**
- open shop with sufficient funds

**Steps**
1. buy one upgrade

**Expected**
- currency decreases correctly
- upgrade becomes owned/applied
- stat preview and current state update

---

### Test: purchase fails when player lacks funds
**Setup**
- open shop with insufficient currency

**Steps**
1. attempt upgrade purchase

**Expected**
- purchase does not apply
- currency does not go negative
- UI communicates unavailable state

---

### Test: purchased upgrades affect gameplay
**Setup**
- buy club or ball upgrade

**Steps**
1. compare before/after behavior

**Expected**
- relevant stat improvement is visible
- new behavior matches upgrade description

---

## 11. Procedural Generation Test Cases

## 11.1 General Generator Validity

### Test: generated hole contains tee and cup
**Setup**
- generate multiple holes with multiple seeds

**Steps**
1. inspect outputs

**Expected**
- every hole has valid tee position
- every hole has valid cup position

---

### Test: generated hole has at least one playable route
**Setup**
- generate multiple holes

**Steps**
1. inspect fairway and hazard layout
2. perform manual play check on sample set

**Expected**
- player has at least one reasonable path from tee to cup
- hazards do not fully block progress

---

### Test: tee and cup are not placed in hazards
**Setup**
- generate many holes

**Steps**
1. inspect tee and cup placement

**Expected**
- tee not in water
- cup not in water
- tee/cup not buried in tree colliders or OOB

---

### Test: difficulty scales across run
**Setup**
- generate a full run

**Steps**
1. compare early holes to later holes

**Expected**
- later holes may be longer, narrower, or more hazardous
- scaling feels gradual rather than random

---

## 11.2 Seeded Determinism Tests

### Test: same seed generates same hole
**Setup**
- pick seed value

**Steps**
1. generate hole using seed X
2. generate again using seed X

**Expected**
- same major layout is produced
- deterministic data matches within intended limits

---

### Test: different seeds produce variety
**Setup**
- generate a set of holes with different seeds

**Steps**
1. compare layouts

**Expected**
- holes are not all near-identical
- there is visible variation in path, hazards, or shape

---

## 11.3 Generator Failure Handling

### Test: invalid hole is rejected or regenerated
**Setup**
- force or simulate invalid layout if possible

**Steps**
1. run generation/validation

**Expected**
- invalid layout is not passed into gameplay
- generator retries or falls back safely

---

## 12. Save / Load Test Cases

## 12.1 Save Creation Tests

### Test: save file created during valid save event
**Setup**
- start run and make progress

**Steps**
1. trigger save point
2. inspect save existence

**Expected**
- save file exists
- save format is readable/valid

---

## 12.2 Resume Tests

### Test: resume continues current run correctly
**Setup**
- progress into a run
- save
- close/reload game

**Steps**
1. choose Continue

**Expected**
- current hole index restores correctly
- currency restores correctly
- upgrades restore correctly
- score data restores correctly
- player resumes valid state

---

### Test: load handles missing save safely
**Setup**
- no save file present

**Steps**
1. open game
2. attempt Continue if shown

**Expected**
- no crash
- Continue hidden or safely disabled
- player can still start New Run

---

### Test: corrupt save fails safely
**Setup**
- corrupt or modify save file manually if possible

**Steps**
1. load game

**Expected**
- no crash
- invalid run save is ignored or reported safely
- player is returned to safe state

---

## 13. UI Test Cases

## 13.1 HUD Tests

### Test: HUD displays correct runtime data
**Setup**
- active hole gameplay

**Steps**
1. inspect HUD after spawn
2. take shots
3. switch clubs
4. change wind if available

**Expected**
- hole number is correct
- par is correct
- stroke count updates
- selected club updates
- wind display matches runtime state

---

## 13.2 Recovery Dialog Tests

### Test: recovery dialog appears only when appropriate
**Setup**
- multiple terrain outcomes

**Steps**
1. stop in fairway
2. stop in rough
3. stop in trees

**Expected**
- no dialog on normal lies
- dialog appears on obstructed tree lies only

---

## 13.3 Scorecard Tests

### Test: scorecard rows are accurate
**Setup**
- complete multiple holes

**Steps**
1. open scorecard

**Expected**
- each hole has correct row
- par and strokes are correct
- totals are correct
- no duplicated or missing rows

---

## 13.4 Shop UI Tests

### Test: shop reflects current run state
**Setup**
- earn currency
- open shop

**Steps**
1. inspect price/status of upgrades
2. buy one upgrade

**Expected**
- current currency is correct
- owned/unowned state is correct
- stats update after purchase

---

## 14. Audio Test Cases

## 14.1 Core Audio Events
Verify that sounds play at the correct time for:
- shot
- splash
- sand hit
- ball in cup
- UI click
- shop purchase
- recovery prompt open

### Expected
- correct sound plays
- sound is not duplicated excessively
- sound is not missing in major gameplay events

---

## 15. Ball-in-Air / Visual Effect Tests

## 15.1 Visual Height Readability Test

### Test: airborne effect improves readability
**Setup**
- hit higher-loft or stronger shot that triggers effect

**Steps**
1. observe ball during travel

**Expected**
- ball appears visually elevated
- shadow or shader cue helps communicate height
- player can still tell where ball actually is

---

### Test: visual effect does not break gameplay
**Setup**
- enable ball flight effect

**Steps**
1. take repeated shots
2. interact with hazards
3. complete hole

**Expected**
- scoring still works
- hazard detection still works
- cup detection still works
- effect is presentation, not source of gameplay bugs

---

## 16. Regression Checklists by System

## 16.1 If ShotController changes
Re-test:
- aim
- power
- club switching
- one-stroke-per-shot
- no shooting while moving

## 16.2 If BallController changes
Re-test:
- motion
- stop behavior
- terrain transitions
- hazard entry
- cup detection
- flight effect compatibility

## 16.3 If HazardResolver or LieEvaluator changes
Re-test:
- water
- OOB
- tree prompt
- take-a-drop
- play-from-lie
- penalty strokes
- safe repositioning

## 16.4 If HoleGenerator or validation changes
Re-test:
- tee/cup placement
- playability
- variety
- deterministic seeds
- no impossible holes

## 16.5 If RunManager changes
Re-test:
- hole results
- currency
- run totals
- end-of-run flow
- scorecard

## 16.6 If SaveManager changes
Re-test:
- create save
- resume save
- corrupt save handling
- continue button behavior

## 16.7 If UI changes
Re-test:
- displayed data accuracy
- button routing
- menu transitions
- recovery dialog behavior
- scorecard consistency

---

## 17. Manual Playtest Sessions

In addition to targeted tests, the team should regularly do real play sessions.

## 17.1 Short play session
Play 1–2 holes and check:
- core feel
- readability
- hazard fairness
- pacing

## 17.2 Full run play session
Play a full run and check:
- difficulty curve
- procedural variety
- currency pacing
- shop usefulness
- fatigue/frustration points
- final score summary

## 17.3 Focused hazard session
Deliberately test:
- water entries
- bunker lies
- rough recovery
- tree drops
- OOB edge cases

## 17.4 Save/load session
Test:
- save mid-run
- exit game
- reload
- continue
- finish run

---

## 18. Recommended Debug Tools

The following debug tools are helpful and acceptable during development:

- show current terrain under ball
- show current lie type
- show current club stats
- show ball velocity
- show selected seed
- show validation failures
- show chosen drop position
- show hazard trigger outlines in debug mode

These should be removable or hidden in non-debug builds.

---

## 19. Test Data / Test Environments

To make testing easier, the repo should ideally support a few controlled test scenarios:

1. **StaticPracticeHole**
   - simple fairway and green
   - used for aim, power, and cup testing

2. **HazardTestHole**
   - water, sand, rough, trees, OOB all close together
   - used for hazard testing

3. **GeneratorSandbox**
   - repeated seed generation
   - used for inspection and validation

4. **SaveLoadTestRun**
   - predictable state for persistence testing

These do not need to ship as player-facing content, but they are useful for development and AI-assisted testing.

---

## 20. AI Agent Testing Rules

Any AI coding agent working in this repo must treat testing as part of the task.

When making a change, an AI agent should:
1. identify what systems were touched
2. identify likely regressions
3. run or describe relevant checks
4. avoid claiming success without verifying likely break points

AI agents must not:
- assume a feature works because the code looks correct
- skip hazard testing after changing hazard logic
- skip playability checks after changing generation
- skip save/load checks after changing persistence

---

## 21. Minimum Acceptance Checklist for Final Project Builds

Before considering a build “presentation ready,” verify all of the following:

- game launches
- menu works
- new run works
- continue works if save exists
- at least one full run is playable
- aim and power feel consistent
- clubs behave differently
- hazards work
- tree recovery prompt works
- take-a-drop works
- play-from-lie works
- scoring is correct
- scorecard is correct
- currency and upgrades work
- generated holes are playable
- save/load works
- audio works for major actions
- ball-in-air effect is readable and does not break gameplay
- no major soft-locks or obvious crashes occur in normal play

---

## 22. Final Testing Principle

The most important testing question for Fairway Rogue is:

**Can a player reliably start a run, play readable golf, recover from hazards, finish holes, progress through the run, and feel that the systems are fair and working together?**

If the answer is no, keep testing and fixing.

If the answer is yes, then polish and balance can continue from a stable base.