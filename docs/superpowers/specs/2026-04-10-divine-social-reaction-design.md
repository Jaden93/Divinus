# Design Spec: Divine Interaction & Social Reaction (MVP 2)

Implementing a reactive social ecosystem where villagers observe, discuss, and remember divine actions.

## 1. Vision
Transform the village from a simple economic simulation into a "living organism" that reacts morally to the player's actions. Divine acts (Smite, Repair, Revive) will trigger immediate social responses, group gatherings, and the spread of information through "messengers."

## 2. Core Components

### A. Divine Event System
- **DivineEvent (Struct)**: Contains `Type` (Smite, Repair, Revive), `Position` (Vector3), `Initiator` (God), and `ImpactRadius`.
- **DivineEventManager (Static/Singleton)**: A lightweight dispatcher.
    - `Broadcast(DivineEvent e)`: Notifies all active villagers of a divine occurrence.

### B. Villager Individual Morality
New parameters in `VillagerController`:
- **Loyalty (0-100)**: 
    - *Increases*: Having a house, resting, witnessing `Repair/Revive`.
    - *Decreases*: Excessive work, low energy, witnessing "unjustified" `Smite`.
- **Habituation (Memory)**:
    - Track "Divine Shock" level. High frequency of `Smite` leads to **Apathy** (😶) or **Permanent Terror** (💀).
    - Long periods without divine intervention lead to "Cooling," returning reactions to baseline.

### C. The Social Loop (Reaction & Propagation)
1. **Perception**: Villagers within 20m of a `DivineEvent` stop their current task.
2. **The Gathering (Il Capannello)**:
    - Witnesses walk towards the event point.
    - They face each other and exchange emojis based on `Loyalty` (🙏/😢 vs 🤔/🧐).
    - Loyalty values are averaged/influenced within the group during this phase.
3. **The Messenger (Il Passaparola)**:
    - After 5-10s of gathering, the system selects 1-2 "Messengers."
    - Messengers run to the nearest villager *outside* the event radius to "tell the news" (💬).
    - The "news" (loyalty impact) spreads through the village physically.

## 3. Visual Feedback (Android Optimized)
- **Emoji System**: Simple floating 2D sprites/icons above villager heads.
- **Console Logs**: Detailed internal thoughts for debugging/flavor.
- **Animations**: Use existing `Idle/Walk` but add head-tracking (LookAt) towards the event or conversation partner.

## 4. Technical Implementation Steps
1. Create `DivineEventManager` and `DivineEvent` data structure.
2. Update `DivineActionSystem` to broadcast events on Smite/Repair.
3. Add `Loyalty` logic to `VillagerController`.
4. Create `VillagerSocialReaction` component to handle state transitions (Working -> Investigating -> Gathering -> Messenger).
5. Implement `EmojiUI` for floating feedback.

## 5. Success Criteria
- [ ] Villagers stop working when a nearby Smite occurs.
- [ ] Witnesses gather around the impact point and show emojis.
- [ ] A messenger leaves the group to notify others.
- [ ] Loyalty levels change dynamically based on the event type and villager status.
