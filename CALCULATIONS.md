# Werewolves — Game Design Calculations & Balance Reference

This document serves as the formal repository for all mathematical models, combat formulas, drop tables, entity stats, and game balance calculations in **Werewolves (Godot 4 C# Edition)**.

---

## 1. Combat Mathematics & Formulas

### 1.1 Hit & Evasion Resolution
Combat hits are rolled on each attack attempt using the attacker's accuracy and the defender's evasion:
$$\text{Hit Chance} = \text{Clamp}(\text{Attacker.Accuracy} - \text{Defender.Evasion}, 0.05, 0.95)$$

- If $\text{Randf}() < \text{Hit Chance}$, the strike lands successfully.
- Otherwise, the attack misses and displays floating text `"Miss"`.

### 1.2 Physical Damage Mitigation Formula
Every physical attack mitigates damage based on half of the defender's base defense value:
$$\text{Damage Dealt} = \max\left(1.0, \text{Attacker.BaseDamage} - \frac{\text{Defender.BaseDefense}}{2.0} + \text{SkillBonus}\right)$$

### 1.3 Skill Damage & Costs
The Werewolf player has 4 active skills mapped to hotkeys <kbd>1</kbd>–<kbd>4</kbd>:

| Skill Name | Hotkey | Power Cost | Cooldown | Skill Bonus | Formula | Target Restriction |
| :--- | :---: | :---: | :---: | :---: | :--- | :--- |
| **Basic Attack** | <kbd>L-Click</kbd> | $0$ | $0.85\text{s}$ | $+0$ | $\max(1, \text{Atk} - \frac{\text{Def}}{2})$ | In melee range ($75\text{px}$) |
| **Scratch Hit** | <kbd>1</kbd> | $15$ | $5.0\text{s}$ | $+12$ | $\max(1, \text{Atk} - \frac{\text{Def}}{2} + 12)$ | In melee range ($90\text{px}$) |
| **Charge Attack** | <kbd>2</kbd> | $25$ | $8.0\text{s}$ | $+20$ | $\max(1, \text{Atk} - \frac{\text{Def}}{2} + 20)$ | Dashes to target within $220\text{px}$ |
| **Execute Bite** | <kbd>3</kbd> | $20$ | $10.0\text{s}$ | $+15$ | $\max(1, \text{Atk} - \frac{\text{Def}}{2} + 15)$ | Target HP $\le 25\%$ |
| **Blood Howling** | <kbd>4</kbd> | $40$ | $30.0\text{s}$ | N/A | $+30\%$ to Damage, Defense, Speed, Accuracy, Evasion | $10\text{s}$ self-buff duration |

### 1.4 Power Regeneration Economy
- **Passive Regeneration Rate**: $4.0\text{ units/sec}$.
- **Max Power Pool**: $100\text{ units}$.
- **Time to full recharge from empty**: $100 / 4.0 = 25.0\text{ seconds}$.
- **Total burst rotation cost** (Scratch + Charge + Howl): $15 + 25 + 40 = 80\text{ Power}$.
- Attacks consume power on cast; basic melee attacks require zero power.

---

## 2. Drop Rates & Loot Tables (Patch 2.0)

### 2.1 Villager Loot System
Villagers drop resources upon defeat based on random principles. They can drop **Meat**, **Gold Coins**, or **Both**, categorized into **Small Loot** and **More / Large Loot** quantity tiers.

#### Drop Category Distribution
$$\begin{aligned}
P(\text{Gold Coins Only}) &= 0.40 \quad (40\%) \\
P(\text{Meat Only}) &= 0.30 \quad (30\%) \\
P(\text{Both Meat \& Gold}) &= 0.30 \quad (30\%)
\end{aligned}$$

#### Quantity Tiers
$$\begin{aligned}
P(\text{Small Loot}) &= 0.65 \quad (65\%) \\
P(\text{More Loot}) &= 0.35 \quad (35\%)
\end{aligned}$$

| Resource | Small Loot Tier ($65\%$) | More Loot Tier ($35\%$) | Overall Mean Per Drop Event |
| :--- | :---: | :---: | :---: |
| **Gold Coins** | $2 - 5\text{ coins}$ (mean: $3.5$) | $6 - 12\text{ coins}$ (mean: $9.0$) | $0.65 \times 3.5 + 0.35 \times 9.0 = 5.425$ |
| **Meat** | $1\text{ piece}$ (mean: $1.0$) | $2 - 3\text{ pieces}$ (mean: $2.5$) | $0.65 \times 1.0 + 0.35 \times 2.5 = 1.525$ |

#### Expected Value (EV) per Villager Defeated
- **Expected Gold Coins**:
  $$\mathbb{E}[\text{Gold}] = (P(\text{Gold Only}) + P(\text{Both})) \times \text{Mean}_{\text{Gold}} = (0.40 + 0.30) \times 5.425 = 0.70 \times 5.425 \approx \mathbf{3.80\text{ coins}}$$
- **Expected Meat Pieces**:
  $$\mathbb{E}[\text{Meat}] = (P(\text{Meat Only}) + P(\text{Both})) \times \text{Mean}_{\text{Meat}} = (0.30 + 0.30) \times 1.525 = 0.60 \times 1.525 \approx \mathbf{0.915\text{ meat}}$$

---

### 2.2 Wildlife & Environmental Gathering Drop Tables

| Source Entity / Node | Resource | Chance | Quantity Range | Expected Value (EV) | Interaction / Trigger |
| :--- | :--- | :---: | :---: | :---: | :--- |
| **Deer** | Meat | $100\%$ | $1 - 2$ | $1.5$ | Defeat in combat |
| **Pine Tree** | Wood Logs | $100\%$ | $1 - 3$ | $2.0$ | Chop (3 strikes) |
| **Quarry Boulder** | Stones | $100\%$ | $1 - 3$ | $2.0$ | Mine (3 strikes) |

---

## 3. Entity & NPC Stat Sheets

| Attribute | Werewolf (Player) | Werewolf (Howl Buffed) | Villager (NPC) | Deer (Fauna) |
| :--- | :---: | :---: | :---: | :---: |
| **Max Health** | $100$ | $100$ | $80$ | $80$ |
| **Base Damage** | $25$ | $32.5$ | $15$ | $15$ |
| **Base Defense** | $10$ | $13$ | $5$ | $5$ |
| **Move Speed** | $260$ (sprint $416$) | $338$ (sprint $540.8$) | $65$ (chase $87.75$) | $70$ (chase $98$) |
| **Accuracy** | $0.80$ | $1.04$ | $0.75$ | $0.75$ |
| **Evasion** | $0.10$ | $0.13$ | $0.15$ | $0.15$ |
| **Attack Range** | $75\text{px}$ | $75\text{px}$ | $75\text{px}$ | $90\text{px}$ |
| **Attack Cooldown** | Player input ($0.85\text{s}$) | Player input ($0.85\text{s}$) | $2.5\text{s}$ | $3.0\text{s}$ |

---

## 4. Game Balance & Time-to-Kill (TTK) Metrics

### 4.1 Werewolf vs. Villager (Target HP: 80, Def: 5)
- Defender Defense Halved: $5 / 2 = 2.5$.
- Basic Melee Hit: $25 - 2.5 = 22.5\text{ dmg}$.
- Scratch Hit: $25 - 2.5 + 12 = 34.5\text{ dmg}$.
- Charge Attack: $25 - 2.5 + 20 = 42.5\text{ dmg}$.
- Execute Bite (Target HP $\le 20$): $25 - 2.5 + 15 = 37.5\text{ dmg}$.

#### Optimal Burst Rotation:
1. **Charge Attack** (<kbd>2</kbd>): $42.5\text{ dmg} \implies \text{Remaining HP} = 37.5$.
2. **Scratch Hit** (<kbd>1</kbd>): $34.5\text{ dmg} \implies \text{Remaining HP} = 3.0$ (triggers execute threshold $\le 20$).
3. **Execute Bite** (<kbd>3</kbd>) or **Basic Melee Hit**: Target eliminated.
- **Time to Kill**: $\approx 1.5 - 2.0\text{ seconds}$.
- **Hit Reliability**: $P(\text{Hit}) = 0.80 - 0.15 = 0.65\text{ (65\% chance without Howl)}$; $0.89\text{ (89\% with Howl)}$.

### 4.2 Villager vs. Werewolf (Target HP: 100, Def: 10)
- Werewolf Defense Halved: $10 / 2 = 5.0$.
- Villager Hit Damage: $15 - 5.0 = 10.0\text{ dmg}$.
- Hit Reliability: $P(\text{Hit}) = 0.75 - 0.10 = 0.65\text{ (65\%)}$.
- Expected DPS from 1 Villager: $\frac{10.0 \times 0.65}{2.5\text{s}} = 2.6\text{ dmg/sec}$.
- Safe combat threshold: A single Werewolf can comfortably withstand $3$ Villagers for $\sim 12\text{ seconds}$ without healing.

---

## 5. Economy & Pacing Optimization

- **Village Clear Yield**:
  - Village contains $12$ roaming Villagers.
  - Expected total clear yield: $\approx 45.6\text{ Gold Coins}$ and $\approx 11.0\text{ Meat Pieces}$.
- **Pouch Storage Space**:
  - Pouch UI supports uniform $44\times 44\text{px}$ item stack slots.
  - Stacks aggregate seamlessly in `GameState.PouchItems` without upper bounds.
- **Safe Haven Lair Recovery**:
  - Inside the Werewolf Lair hideout, safe haven resting restores $10\text{ HP/sec}$ and $10\text{ Power/sec}$.
  - Complete recovery from $0$ to $100\%$ occurs in $10\text{ seconds}$ within the lair.
