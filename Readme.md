# Fort & Cards - Turn-Based Strategy Game

## Game Concept Summary

A local turn-based strategy card game where each player defends a **Fort** on the map.
The match is played on one machine first, with gameplay quality taking priority over networking.

---

## Core Mechanics

### The Fort
- Each player has a Fort on the map
- If a Fort is destroyed, that player loses
- Fort HP is part of the main game rules and balance

### Card System
We start the game like this:

| Category | Starting Hand | Purpose |
|----------|---------------|---------|
| **World Effect** (2) | Build structures, plant money-generating fields, deploy hazards and weather |
| **Character** (2) | Spawn units for offense and defense |
| **Spell** (1) | Heal, direct damage, buffs, debuffs, utility effects |

- **Starting hand**: 5 cards total
- Suggested first hand: `2 World Effect + 2 Character + 1 Spell`
- After the starting hand, players do **not** draw automatically each turn
- Each turn gives guaranteed income
- During the **Buy** phase, the player may buy **1 card maximum**
- In v1, a bought card goes directly into the player's hand

### Economy Rules
- Every turn, the player gets a minimum amount of money
- Card prices should stay low enough for early turns to feel active
- Economy cards should help, but not snowball too fast
- Limiting purchases to 1 per turn keeps the pace more fair and controlled

### Win Condition
Destroy the opponent's Fort.

---

## Match Flow

The match follows this turn order:

1. **Setup**
   - Create both players
   - Assign starting money
   - Assign Fort HP
   - Give the starting hand
   - Set the current player
2. **Income**
   - The current player gains guaranteed money
3. **Buy**
   - The player may buy up to 1 card
   - Bought cards go directly to hand in v1
4. **Play**
   - The player uses cards from hand
5. **Attack**
   - Units attack enemy units or the enemy Fort
6. **End**
   - Check win or lose
   - Pass the turn to the other player

Short version:

`Setup -> Income -> Buy -> Play -> Attack -> End -> Next Player`

Design note:

- This system is less random than a classic draw-every-turn card game
- The player chooses cards through spending, so economy becomes part of the strategy
- The first version should focus on a clear and reliable flow before extra complexity

---

## How to Build This in Unity - Step by Step

### Phase 0: Learn the Basics

Since this is a first Unity/C# project, start here:

1. Install Unity Hub and use **Unity 6 LTS, version 6000.3.10f1**
2. Keep the project version locked to `ProjectSettings/ProjectVersion.txt`
3. Learn basic C#:
   - variables
   - functions
   - classes
   - lists
   - enums
4. Learn the main Unity concepts:
   - GameObjects
   - Components
   - MonoBehaviour
   - ScriptableObjects
   - Scene management
   - Canvas and UI basics

### Phase 1: Card Data and Economy System

**Goal**: Define cards as data, create the starting hand, and support the buy flow.

```text
Assets/
  ScriptableObjects/
    Cards/
      WorldEffectCards/
      CharacterCards/
      SpellCards/
    GameConfig/
  Scripts/
    Cards/
      Card.cs
      WorldEffectCard.cs
      CharacterCard.cs
      SpellCard.cs
    Deck/
      StartingHand.cs
      BuyStack.cs
      DiscardPile.cs
    Player/
      PlayerHand.cs
      PlayerData.cs
    Config/
      GameConfig.cs
```

Suggested data in `GameConfig`:

- starting money
- starting Fort HP
- starting hand size
- max hand size
- money per turn
- buy cost

### Phase 2: Game Board and Fort

**Goal**: Create the map, place Forts, and build a basic tile system.

- Use a Tilemap or simple grid for the board
- Each player's Fort is placed on the board
- Define legal placement zones

```text
Scripts/
  Board/
    GameBoard.cs
    Tile.cs
  Fort/
    Fort.cs
```

### Phase 3: Turn System

**Goal**: Alternate turns and enforce the game phases.

```csharp
public class TurnManager : MonoBehaviour
{
    public enum Phase { Income, Buy, Play, Attack, End, GameOver }

    public void StartTurn(Player player) { }
    public void EndTurn() { }
    public void NextPhase() { }
}
```

Each turn:

1. **Income phase** - gain guaranteed money
2. **Buy phase** - buy up to 1 card
3. **Play phase** - play cards from hand
4. **Attack phase** - attack with units
5. **End phase** - check victory and pass the turn

### Phase 4: Card Effects and Combat

**Goal**: Make cards and units affect the board.

- **World Effect**: buildings, income fields, hazards, weather
- **Character**: units with HP, ATK, movement, range
- **Spell**: heal, damage, buff, debuff, utility

Suggested effect pipeline:

1. Select a card
2. Validate the target
3. Spend the cost if needed
4. Apply the effect
5. Move the card to the correct pile if needed

### Phase 5: Local Play First

**Goal**: Finish a clean local version before networking.

Options:

| Option | Difficulty | Description |
|--------|------------|-------------|
| **Hot-seat** (same PC) | Easy | Both players take turns on one machine |
| **Unity Netcode for GameObjects** | Medium | Unity networking solution |
| **Mirror Networking** | Medium | Popular free networking library |

Recommendation:

- Finish the local version first
- Add networking only after the match flow is reliable

### Phase 6: UI and Polish

- Hand UI
- Money display
- Turn and phase display
- Fort HP display
- Valid target highlights
- Error feedback
- Game over screen

---

## Recommended Project Structure

```text
Assets/
  Art/
    Cards/
    Board/
    UI/
  Audio/
  Prefabs/
    Cards/
    Units/
    Buildings/
  Scenes/
    MainMenu.unity
    GameScene.unity
  ScriptableObjects/
    Cards/
    GameConfig/
  Scripts/
    Cards/
    Board/
    Fort/
    Player/
    Deck/
    Combat/
    TurnSystem/
    Networking/
    UI/
    Config/
```

---

## How to Work as a Team

### Use Git from Day 1
- Create a GitHub or GitLab repo
- Use Unity's `.gitignore` template
- Each person works on a branch
- Merge through pull requests
- Never commit the `Library/` folder

### Development Rules
1. Prototype ugly first - gameplay before visuals
2. Test constantly after each feature
3. Keep a shared balance sheet for card costs and stats
4. Use ScriptableObjects for data
5. Commit often with clear messages

---

## Milestone Checklist

- [ ] Everyone installs Unity and opens the project
- [ ] Card ScriptableObjects created
- [ ] Starting hand setup working
- [ ] Income phase working
- [ ] Buy phase working with 1 card maximum per turn
- [ ] Cards appear in player's hand
- [ ] Game board with tiles rendered
- [ ] Fort placed, has HP, and can take damage
- [ ] Turn system working in local play
- [ ] Cards can be played onto the board
- [ ] Character units move and attack
- [ ] World effect cards work
- [ ] Spell effects work
- [ ] Money system feels balanced
- [ ] Win condition works
- [ ] Game over screen works
- [ ] Networking is added only after the local version is stable
- [ ] UI polish, animations, and sound
