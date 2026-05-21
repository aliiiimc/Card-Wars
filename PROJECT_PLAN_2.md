# Project Plan 2 - Remaining Work

Date: 2026-05-21

This plan only lists work that still remains after the current checks.

Verified baseline:

- Unity 6000.3.10f1 opens the project in batchmode and script compilation succeeds.
- No Unity compile errors were found in `Temp/codex-unity-batch.log`.
- Ali confirmed the final v1 match flow works: `Income -> Buy/Discard -> Play -> EndTurn`.
- Game Over panel and Restart button work in `SampleScene`.

New buy/economy directive:

- Buying should stay random, but not from the full card library.
- During Buy phase, the player chooses a cost tier/amount to spend, for example `2`, `3`, `4`, or `5`.
- The game then gives one random card whose `CardData.cost` exactly matches the chosen amount.
- The player pays the chosen amount, not one global `buyCost`.
- The one-buy-per-turn rule stays.
- If the player does not have enough money, the buy is refused.
- If no active card exists for that chosen cost, the buy is refused.
- This keeps randomness while making economy cards such as Wheat Field useful, because more money unlocks higher-cost random pools.
- The computer player must use the same rule by choosing an affordable non-empty cost tier.

## Ali - Game Logic, Rules, and Balance

- Finalize v1 balance values.
  - Tune `MainGameConfig.asset`: starting money, Fort HP, income, discard reward, hand limit, and any remaining global economy values.
  - Replace the old single global buy-cost balance with cost-tier buying.
  - Tune card costs and stats with Fatine so each buy tier has useful cards and no placeholder/free cards unless intentionally free.

- Lock the final v1 rules in documentation.
  - Flow is now final: `Income -> Buy/Discard -> Play -> EndTurn`.
  - Attack is part of Play.
  - Buy remains random, but the random pool is filtered by the cost tier chosen by the player.
  - Update any old docs that still mention a separate normal Attack phase or undecided buy behavior.

## Abdo - Hex Board and Combat

- Finish board mechanics for unfinished World Effects.
  - `Wall`: block movement and/or attacks according to the final rule.
  - `Watch tower`: attack or damage enemy units in range.
  - `Hospital`: heal nearby allied units.
  - `Fog`: apply the movement/visibility penalty rule.
  - `Anti-air tower`: counter flying units.

- Finish flying and anti-air combat rules.
  - `Dragon` needs board/combat behavior for flying.
  - Melee units such as Knight/Spearman should not hit flying units if that remains the final rule.
  - Ranged/projectile or anti-air sources should be able to hit flying units.

- Finish structure combat behavior.
  - World Effects with HP should be damageable and removable consistently.
  - Structure ownership changes and destruction should update the board tile state without leaving stale objects.

- Clean main-scene board setup for the final demo.
  - Main gameplay should use `SampleScene`.
  - Debug-only board behavior should stay out of the final playable scene.

## Rabie - Computer Opponent and UI

- Complete the AI turn behavior.
  - The computer currently plays during Play phase; it still needs final v1 behavior for buy/discard if the enemy is expected to use economy like the player.
  - AI buy logic should choose an affordable non-empty cost tier, then receive a random card from that tier.
  - The AI should not blindly spend all useful-looking actions just because ending turn scores very low.
  - Add a practical stopping rule so the AI can end turn after good actions are exhausted.

- Improve AI priorities.
  - Prefer lethal Fort damage.
  - Defend its Fort when low.
  - Avoid wasting strong cards on weak targets.
  - Prefer income/board setup early when useful.

- Finish player-facing UI polish.
  - Add a clear way to choose the buy cost tier, either buy buttons per tier or a cost selector plus Buy button.
  - Clear invalid action feedback.
  - Clear selected-card and selected-target feedback.
  - Make turn owner, money, Fort HP, hand count, and phase readable in the final scene.

## Fatine - Card System, Card Data, and Effects

- Complete unfinished Character card data.
  - `Spearman`, `Priest`, `Dragon`, and `Engineer` still have `maxHp: 0` and/or `attackDamage: 0`.
  - Several Character cards still have `cost: 0`; keep only the cards that are intentionally free.
  - Assign meaningful costs so the tiered random buy pools are balanced.

- Complete unfinished Spell cards.
  - `Freeze`: needs real effect mapping, power/duration, and movement-lock behavior.
  - `Lightning strike`: needs real damage value and effect mapping.
  - `Revival`: needs revive behavior or removal from v1 scope.
  - `Sabotage`: needs building-disable behavior or removal from v1 scope.
  - `Tax collection`: needs field/resource-steal behavior or removal from v1 scope.

- Complete unfinished World Effect card data and effect mapping.
  - `Wall`, `Watch tower`, `Hospital`, `Fog`, and `Anti-air tower` currently exist as assets but still need real gameplay values/effects.
  - Any World Effect with no real v1 behavior should be removed from the active random pool until implemented.

- Clean the random card library for v1.
  - `MainCardLibrary.asset` should only include cards that are playable with real stats/effects.
  - Random buy should not give placeholder cards with zero stats or missing effects.
  - Each active buy tier should contain enough valid cards to make random buying feel fair.

- Align card effect IDs with actual effects.
  - Cards that need `effect.damage`, `effect.heal`, `effect.buff`, `effect.debuff`, `effect.utility`, `effect.income_boost`, or `effect.summon` must have the correct `effectId`.
  - Cards with empty `effectId` should only stay empty if they intentionally have no runtime effect.

## Final Demo Checklist

- Main playable scene is `Assets/Scenes/SampleScene.unity`.
- Project compiles in Unity without script errors.
- Player can complete full turns with the final flow.
- Computer can complete turns without breaking the match.
- Random buying only gives usable v1 cards from the chosen cost tier.
- Game ends when a Fort reaches 0 HP.
- Game Over screen appears with the winner.
- Restart starts a clean new match.
- No placeholder zero-stat cards appear in the final random pool.
