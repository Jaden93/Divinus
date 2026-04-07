# Journey Into Divinus

*A narrative history of "Black & White 3 Android" — April 3–7, 2026*

---

## 1. Project Genesis

The project was born from a clear creative vision: a mobile god game for Android, inspired by Lionhead Studios' *Black & White*, but restructured around the slower, more contemplative rhythm of an idle city builder. The player is not a hero, not a unit commander — the player is a god. An unseen hand that shapes the world indirectly.

The founding design insight, documented from day one in `CLAUDE.md`, is *influence without direct control*. Villagers do not know they are NPCs. They wander aimlessly until the divine hand gives them purpose — an axe, a house, a bench. Only then do they develop routines, identities, and needs. The player's power is not combat but context: what you build, where you place it, and how you treat the people within it.

The earliest recorded session (April 3, 2026, 1:16 PM) shows this philosophy translated immediately into code. The very first C# feature was a **sleep workflow** for villagers — `VillagerController.cs` implementing a state machine with sleep, exhaustion, and rest recovery. Before trees were even choppable, the design already insisted: villagers must need to sleep. The loop from need → action → relief was architectural, not cosmetic.

The technical stack was Unity for Android, with a semi-isometric camera, NavMesh for pathfinding, and touch input via Unity's new Input System (not the legacy `UnityEngine.Input` — a rule enforced firmly throughout the project and documented in persistent memory). Assets were authored in Blender and imported as FBX. The project used a clean folder hierarchy under `Assets/_Project/` following conventions laid out in `CLAUDE.md` from the start.

---

## 2. Architectural Evolution

### Phase 1: State Machine Foundation (April 3)

The villager AI began as a simple state machine with states: `Idle`, `Walking`, `ChoppingWood`, `GoingToSleep`, `Sleeping`, `Resting`. Energy was the core currency — working drained it, sleeping restored it. This was deliberately minimal. The first session (#4) implemented the complete sleep workflow. The second meaningful observation (#17) added exhaustion thresholds and rest detection.

`HouseController.cs` started life as an **event-driven** system but was simplified the same day (observation #7) into an **instantiation-driven** design — a telling early example of the project's discipline about not over-engineering for an MVP.

The bench was introduced as a secondary rest point: lower cost, shorter rest, no full sleep cycle. This distinction between "rest briefly on a bench" and "sleep deeply in a house" gave the game two meaningful build choices from very early on.

### Phase 2: NavMesh Integration and Breaking (April 4, morning)

As the project grew, the NavMesh became both the backbone and the primary source of pain. Villagers needed to path around houses (which are NavMesh obstacles), through doors, and to trees. The baking workflow for Unity's new AI Navigation package caused repeated confusion:

- Observation #112: NavMesh baking failed despite apparently correct configuration
- Observation #123: Attempted obstacle optimization blocked by Unity play mode
- Observation #124–#126: Multiple approaches tried — static vs dynamic obstacles, `NavMeshModifier` with `applyToChildren`, switching area types

The resolution was not a single breakthrough but a slow accumulation of working configurations. By observation #169, `NavMeshObstacle` carving was enabled for dynamically spawned houses — the key insight that blocking should happen at obstacle level, not by re-baking the whole mesh.

### Phase 3: UI Systems and the Divine Menu (April 4, afternoon–evening)

The UI layer grew rapidly once the core loop was stable. A **circular context menu** (`CircularMenuUI.cs`) became the primary interaction pattern — tap a zone, get a radial wheel of options. The `BottomBarUI.cs` handled persistent navigation. The `DivineActionSystem.cs` managed the god's powers: spawn a villager, grant an axe, smite, repair.

A significant architectural decision in this phase: villager spawning was **decoupled from faith cost** (observation #65). Early designs required spending "mana" to create villagers, but this was removed to lower friction during the MVP loop. The system was replaced later by a faith-based economy redesigned at the macro level (observation #418, April 7: "Faith Replaces Mana").

The drag-and-drop interaction pattern emerged as the signature UX: drag an axe icon onto a villager → villager becomes a lumberjack. Drag a house icon onto terrain → place a construction site. This was implemented across `AxeActionUI.cs`, `HouseActionUI.cs`, and `BenchActionUI.cs`, all sharing a similar ghost-preview → collision check → confirm placement pattern.

### Phase 4: House Entry — The Wall Slab Problem (April 5, morning)

The most architecturally interesting challenge of the project was making villagers enter houses properly. The naive solution — teleport the villager to a sleep position near the house — was rejected. The correct solution required physical navigation through a door.

The problem: houses are NavMesh obstacles. If you mark the entire house as an obstacle, villagers cannot path inside at all. The solution evolved through multiple stages:

1. **Threshold approach** (observation #S148): sleep position placed at house entrance, not inside. Acceptable for MVP but visually wrong.
2. **Door position targeting** (observation #165): sleep target moved to door position specifically.
3. **Wall slab collider system** (observation #334): walls were decomposed into separate "slab" objects, each with individual colliders. The door opening was left without a slab, creating a physical gap that NavMesh could path through.
4. **NavMesh rebake** (observation #337): the NavMesh was rebaked to recognize the new geometry, creating a walkable channel through the doorway.

This evolution — from hack to proper spatial reasoning — is characteristic of how the project handled complexity: accept the MVP workaround first, then replace it with the real solution once the system was understood.

### Phase 5: Church Integration and Blender Pipeline (April 5, evening)

The church building introduced a new challenge: a multi-mesh asset with separate exterior and interior components. The initial import created collider conflicts between the two meshes, causing villagers to be blocked unexpectedly.

The fix (observation #358) was to merge all church meshes into a single unified FBX in Blender, using Python scripting via the Blender MCP integration. Gothic door panels with pointed arches were constructed (observations #373–#378) as independent GameObjects, preserving the ability to animate or replace the door separately while keeping the main building as a single coherent mesh.

This Blender → FBX → Unity pipeline was used throughout: model in Blender, export as FBX, refresh Unity assets, adjust prefab configuration. The workflow was reliable but manual, requiring careful attention to pivot points, scale normalization, and normal orientation.

---

## 3. Key Breakthroughs

**The NavMeshObstacle carving insight** (April 4, observation #169): After hours of failed baking attempts and modifier configuration, the realization that dynamic obstacle carving — rather than static area masking — was the correct primitive for runtime building placement. This unblocked the entire house placement system.

**Door-based sleep targeting** (April 4, observation #165): Understanding that "go sleep" should navigate to the *door position* rather than a point inside or behind the house. This single change eliminated the most visible villager pathfinding failure mode: villagers circling the back of the house looking for their bed.

**Wall slab collider system** (April 5, observation #334): The architectural insight that a house with a proper door opening is not "a NavMesh obstacle with a hole" but rather "a set of wall segments with a gap between them." Representing it as individual collider slabs rather than a single blocker was the unlock.

**WoodDepot singleton bug** (April 5, observation #332): A subtle failure where villagers would chop wood successfully but accumulation never registered. The root cause was a missing WoodDepot singleton reference — villagers were delivering to a null target. Once identified, the fix was trivial, but finding it required mapping the full resource delivery chain.

**Faith replaces Mana** (April 7, observation #418): A design breakthrough rather than a technical one. The economy was simplified: mana (a separate creation resource) was eliminated entirely. Faith — the village's collective belief in the god — became the single currency for both creation and progression. Simpler, more thematic, and better suited to the mobile session length.

---

## 4. Work Patterns

The development rhythm across five days shows distinct modes:

**Day 1 (April 3) — Foundation Sprint.** Pure feature velocity. Sleep system, energy system, energy UI bar, bench placement UI all landed in the same afternoon session. Work was additive, not corrective. No bug fixes recorded.

**Day 2 (April 4) — Integration Struggle.** The longest and most complex day. NavMesh baking failures, multiple UI systems built, the faith economy removed, Input System migration executed. The ratio of investigation (blue observations) to implementation (purple) was high — this was a day of figuring things out as much as building them. Multiple debugging sessions stacked: NavMesh, then placement collision, then bench drag-and-drop blocking.

**Day 3 (April 5, morning) — Precision Fixes.** Targeted bug work. The wall slab system, sleep position bugfixes, WoodDepot singleton. Each session had a clear issue going in and a working system coming out. The git commits from this day (`fix: villager sleep trigger`, `fix: NavMeshObstacle carving`) reflect disciplined, small-scope changes.

**Day 3 (April 5, evening) — Asset Pipeline.** A shift to Blender work. Church mesh merging, door construction, villager character model creation and export. A completely different modality — Python scripting in Blender, inspecting geometry via MCP tools, then refreshing Unity to verify the result.

**Day 4 (April 6) — Light Debugging.** One compilation error (`CS1061` in `VillagerActionUI`) and a broken "create" button. Short sessions, targeted fixes.

**Day 5 (April 7) — Design and Documentation.** No new code. Comprehensive post-MVP design documentation written and committed to `CLAUDE.md`. Future systems planned: faith economy, creature system, spherical world via vertex shader, furnishing requirements for houses.

---

## 5. Debugging Sagas

### The NavMesh Labyrinth (April 4, ~5:18–5:29 PM)

Four consecutive sessions focused on a single problem: why wouldn't the NavMesh bake correctly? The investigation covered:

- Whether the new AI Navigation package requires different baking steps than the legacy NavMesh
- How `NavMeshModifier` interacts with child objects
- Whether switching from dynamic to static obstacles changes area types
- Whether play mode prevents baking from taking effect

Each attempt produced a commit but no working result. The resolution came indirectly — by shifting to a different approach (dynamic obstacle carving) rather than fixing the original baking configuration. This is a common debugging arc: the solution is abandoning the frame, not fixing the bug within it.

### The Villager Sleep Loop (April 5, 8:04–9:39 PM)

A cluster of bug fixes late on April 5 targeted the sleep behavior specifically. The issues were interconnected:

1. Villagers stuck in `GoingToSleep` state unable to reach exact house position (#351)
2. Villagers stuck at the door without entering (#357)
3. Infinite sleep loop after wood delivery (#380)
4. NavMesh pathfinding failures to sleep position (#381)

Each bug had a different root cause, but all pointed at the same design tension: the sleep position was calculated from mesh bounds, but those bounds are in world space relative to a transform that had been moved and rotated. Getting the math right required switching from naive position offsets to explicit `Renderer.bounds` calculations, then combining that with door-relative waypoints.

### The Missing Singleton (April 5, 10:02 AM, #332)

A particularly sneaky bug: villagers were chopping wood visually, delivering to the depot apparently, but the wood count never increased. The code was correct. The prefab was configured correctly. The issue was that the `WoodDepot` singleton instance was not present in the scene at runtime — the script existed but the GameObject hosting it had been inadvertently removed. The fix: one line restoring the scene reference. The investigation: mapping the entire resource delivery chain from `VillagerController` through `ForestManager` to `WoodDepot`.

---

## 6. Technical Debt

The project was disciplined about not accumulating hidden debt, but some patterns emerged:

**Color-tinting for gender.** Initially, the game used a single villager prefab with color tinting to distinguish male/female characters. This was a known placeholder, documented, and replaced in observation #392–#393 with proper gender-specific prefabs after the 3D character models were created in Blender.

**Threshold-based house entry.** The MVP sleep position at the door threshold was explicitly temporary. It was committed with the understanding that it would be replaced by proper interior navigation — which it was, three sessions later.

**NavMesh obstacle carving.** The dynamic obstacle approach works but has performance implications at scale. For an MVP with a handful of buildings it is acceptable. The observation notes acknowledge this and defer the static-bake optimization to a future milestone.

**Faith/mana ambiguity.** The economy went through at least three states: mana-based creation, no cost at all, then a faith-based system redesigned at the architectural level. The absence of a single coherent resource model during active development created some friction — removal of the faith cost (#65) simplified testing, but re-introducing it later as a fundamentally different mechanic (faith as the sole progression currency) required updating documentation, design docs, and `CLAUDE.md`.

---

## 7. Timeline Statistics

**Date range:** April 3, 2026 (1:16 PM) — April 7, 2026 (2:47 PM)
**Total duration:** 5 days, 1 hour, 31 minutes

**Observations by type:**
- Purple (new features / implementations): ~45
- Blue (discoveries / investigations): ~35
- Red (bug fixes): ~20
- Green check (commits / version control): ~25
- Blue diamond (decisions / architecture): ~5
- Session boundaries: ~50+ sessions recorded

**Most active day:** April 4 (observations #38–#298, spanning 10:10 AM to 11:52 PM)

**Longest debugging session:** NavMesh pathfinding, April 4, 5:18–6:48 PM (6 consecutive sessions, multiple failed approaches before resolution)

**Files most frequently modified:**
1. `VillagerController.cs` — villager AI state machine, energy, sleep, navigation
2. `HouseController.cs` — sleep position, door targeting, wall slab system
3. `DivineActionSystem.cs` — spawn, smite, repair, faith economy
4. `HouseActionUI.cs` / `BenchActionUI.cs` — placement UX, drag-and-drop, collision detection
5. `CLAUDE.md` — design documentation, updated 3 times over the project

**Git commits (from log):**
- `fix: wood collection cap, house button state, duplicate construction`
- `fix: NavMeshObstacle carving sulle WallSlab per bloccare percorsi attraverso le pareti`
- `fix: restore original church interior meshes with DamageableObject components`
- `fix: villager sleep trigger and stuck-at-door`
- `feat: merge church exterior+interior into single mesh with door opening`
- `docs: add future design document to CLAUDE.md`
- `docs: add house furnishing requirement design (post-MVP 0)`

---

## 8. Memory and Continuity

The claude-mem persistent memory system provided measurable continuity across the 50+ discrete sessions spanning five days. Several observations explicitly reference context from prior sessions:

- Observation #346: "Project History Shows Church Interior Already Refactored" — preventing a redundant re-refactor
- Observation #249: project state review that correctly summarized what had been built, unblocking the next session immediately
- Observation #379: "Villager AI System Architecture Fully Mapped" — a complete re-derivation of the state machine before fixing bugs, reflecting that cross-session architectural context was not fully available and had to be reconstructed

The Input System rule (never use `UnityEngine.Input` legacy) was documented in `memory/feedback_input_system.md` (observation #142) and successfully prevented re-introduction of legacy input code in all subsequent sessions. This is exactly the kind of cross-session enforcement that persistent memory enables — a rule learned from a mistake, codified, and applied automatically going forward.

The project state file `memory/project_state.md` was read at the start of several sessions (#143, #249, #417) to re-establish context. Its value was highest at session boundaries: rather than spending 15 minutes re-reading code, the developer could spend 3 minutes reading the memory summary and be immediately productive.

---

## 9. Lessons and Meta-Observations

**The MVP discipline held.** The CLAUDE.md rules ("modify only necessary files," "no enterprise architecture," "3-7 step plans before implementing") were followed consistently. The codebase remained readable and small. No feature was added without justification against the MVP definition. When scope crept (spherical world via vertex shader was proposed), it was parked in a future milestone document, not implemented.

**Blender as a coding environment.** The project used the Blender MCP integration for Python scripting — not just for modeling but for geometry analysis, mesh merging, and programmatic construction of door frames. This is an unusual workflow that proved effective: code in Blender to create assets, then import those assets into Unity as data. The church door construction (observations #370–#378) is a clear example where this was faster than manual mesh editing.

**The NavMesh is a first-class gameplay element.** In a game about villagers navigating a world, the pathfinding mesh is not infrastructure — it is game design. Every building placed changes what is reachable. Every door created is a decision about navigation. The project spent significant time on NavMesh configuration because it had to: getting it wrong means villagers walk through walls or can't find their beds.

**Design and code are interleaved.** The shift from mana to faith (April 7) was not a code change — it was a design document update. But it had direct implications for future code: the villager spawn system, the UI economy display, and the tutorial flow all need to express this change. In a small MVP project like this, the CLAUDE.md design doc is as important as any script — it is the source of truth that keeps sessions coherent across days and developers.

**Small, reversible steps compound.** The sleep system went from "teleport to a position near the house" to "walk to door position" to "walk through a physical doorway in a wall slab system with carved NavMesh." Each step was a small, committed, verified improvement. No step required throwing away the previous one. This is the project's most important operational lesson: the right architecture is found through iteration, not upfront design.

---

## 10. What Comes Next

As of April 7, 2026, the MVP 0 loop is complete: god spawns villager → grants axe → villager chops wood → wood accumulates at depot → player builds house → villager sleeps in house → cycle repeats.

The post-MVP roadmap, now documented in CLAUDE.md, identifies:

- **House furnishing** (bed + wardrobe + light source required for habitation) — houses as empty shells until furnished
- **Spherical world** via vertex shader — the entire island rendered on a curved surface for the god's-eye perspective
- **Faith economy** — replacing mana, making the village's collective belief the sole resource for divine powers
- **Creature system** — a large creature aligned with the player's moral alignment (benevolent or malevolent)
- **Multiple professions** — miners for stone, farmers for food, extending beyond the lumberjack archetype

The project is positioned at exactly the right place for an MVP at day five: the core loop works, the architecture is clean, and the roadmap is specific. The village is small, but the world it lives in is ready to grow.

---

*Report covers observations #1–#430, sessions spanning April 3–7, 2026. Generated April 2026.*
