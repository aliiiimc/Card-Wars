# Fort & Cards

A local turn-based strategy card game where each player defends a **Fort** on a hexagonal map. You deploy characters, cast powerful spells, and construct world effects to destroy your opponent's Fort before they destroy yours.

## Core Mechanics

### The Fort
- Each player has a Fort on the map.
- If a Fort is destroyed (reaches 0 HP), that player loses the game.
- The game ends immediately when a Fort falls.

### The Game Board
- The game is played on a hexagonal grid.
- Each tile may contain **one unit maximum** (no stacking).
- **Deployment Zones:** Player 1 deploys units in the first 2 columns (blue side); Player 2 deploys in the last 2 columns (red side). World Effect structures can be placed anywhere in the player's own half of the board.

### Card Types
1. **Character Cards (e.g., Knight, Archer, Dragon):** Spawn physical units onto the board. Units have Health, Attack Damage, and Movement Capacity. Some units (like the Dragon) have flying capabilities!
2. **World Effect Cards (e.g., Wall, Wheat Field, Anti-Air Tower):** Build structures or fields on the board. These provide defensive cover, generate extra income per turn, or actively attack enemies.
3. **Spell Cards (e.g., Lightning Strike, Revival, Tax Collection):** One-time magical effects that can heal allies, damage enemies, or provide economic utility. Spells target specific units or Forts directly.

## Match Flow

The match follows a strict phase order every turn:

1. **Income Phase:** The current player gains a guaranteed amount of money based on their base income and any active resource fields (like Wheat Fields) on the board.
2. **Buy Phase (Card Economy):** The player may purchase **1 card maximum**. The economy uses a **Cost Tier system**: the player selects an affordable cost amount (e.g., $3, $4, $5, $9) and receives a random card from the active deck matching that exact cost. Players can also choose to discard a card to gain extra money.
3. **Play Phase:** The player may use cards from their hand (spending their remaining money) to spawn units, cast spells, or place buildings. All unit movements and combat attacks happen freely during this phase.
4. **End Turn:** The turn is passed to the opponent or the AI.

## The Computer Opponent (AI)
The game features an intelligent computer opponent that understands the full scope of the game's mechanics. The AI:
- Aggressively defends its Fort when HP is low.
- Prioritizes lethal damage against the player's Fort.
- Uses a smart economy: it dynamically saves money to purchase expensive late-game cards (like the Dragon) and discards weak cards to build its bank.
- Supports its own board by healing units (Priest) and repairing structures (Engineer).

## How to Play
1. Open the project in Unity (built for `6000.3.10f1`).
2. Load `Assets/Scenes/SampleScene.unity`.
3. Press Play and enjoy the match!
