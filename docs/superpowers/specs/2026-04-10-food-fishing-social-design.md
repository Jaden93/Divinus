# Design Spec: Food, Fishing, and Social Personalities (MVP 2)

## Goal
Implement a food-based survival loop and individual villager personalities. This adds depth to the simulation, moving villagers from simple resource-gatherers to individuals with needs, social preferences, and varying levels of obedience.

## Core Mechanics

### 1. Resource: Food
- **Storage**: Managed by `ResourceManager.cs`.
- **Source**: Fishing at the **Port**.
- **Logistics**: Caught fish is delivered to the **Generic Depot**.
- **Consumption**: Eaten at the **Market**.

### 2. Villager Stats & Traits
- **Hunger (0-100)**: Increases over time.
    - `0-50`: Satiated. Normal performance.
    - `51-80`: Hungry. Work and movement speed reduced by 30%. Energy drain increased.
    - `81-100`: Starving. Stops working. Energy drains rapidly. If Energy reaches 0 while starving, the villager dies.
- **Traits (Individual)**:
    - `Sociability`: Propensity to seek others at the Market or in the street.
    - `Laziness`: Chance to abandon work tasks prematurely.
    - `Solitude`: Preference for sitting on benches alone rather than socializing.
- **Faith & Obedience**: Current `VillageFaithSystem` influences how often a villager ignores orders or "takes a break" based on their `Laziness`.

### 3. New Buildings
- **Fishing Port**: A work node placed near water. Villagers are assigned by dragging them here.
- **Market**: A social hub and food distribution point. 
    - Function 1: Villagers must go here to eat (consumes Food from `ResourceManager`).
    - Function 2: Serves as a meeting point for social interactions.

## Implementation Plan

### Phase 1: Infrastructure & Production
- Update `ResourceManager` to include `Food`.
- Update `DebugHUD` to show `FOOD` and `HUNGER` (for the monitored villager).
- Create `FishingPort.cs` (inherits from or mimics `ResourceNode`).
- Add `Fishing` state to `VillagerController`.

### Phase 2: Hunger & Consumption
- Implement `Hunger` logic in `VillagerController`.
- Create `MarketController.cs` as a destination for eating.
- Add `GoingToMarket` and `Eating` states to `VillagerController`.

### Phase 3: Personalities & Social Life
- Add `VillagerPersonality` struct to `VillagerController`.
- Implement "Work Interruption" logic: checking `Laziness` and `Faith` during work.
- Update `VillagerSocialReaction` to trigger based on `Sociability` when idle or near the Market.

## Data Flow
1. **Fishing**: `Villager` (Fishing) -> `Port` -> `Resource` (Fish) -> `Generic Depot` -> `ResourceManager.food.count++`.
2. **Eating**: `Villager` (Hungry) -> `Market` -> `ResourceManager.SpendResource("Food", 1)` -> `Villager.hunger = 0`.

## Success Criteria
- Villagers can be assigned to the Port and generate Food.
- Food is consumed at the Market when villagers are hungry.
- Hungry villagers work slower.
- Starving villagers eventually die.
- Villagers occasionally abandon work to socialize or sit, influenced by their traits.
