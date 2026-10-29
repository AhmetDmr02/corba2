# ÇORBA: Kepçe Savaşları — Game Design Document

*Working title: "Ladle Wars". 2-player online (Netcode for GameObjects), hand-painted stylized look, lane tower-defence with a physical "carry the soup" economy.*

---

## 1. Pitch

Two rival cooks fight over a winding country road. One defends the **Sofra** (the dinner table at the end of the road) by carrying soup out to build pads and pouring it into towers and traps. The other attacks by carrying soup back to their **Ocak** (camp hearth) and turning every ladle into hungry soldiers who march down the road to raid the table.

Money doesn't exist in this game. **Soup is money, and soup spills.** Run too fast, turn too sharply, trip on a root or get elbowed by your rival, and your budget ends up on the ground.

## 2. Design pillars

1. **The economy is a skill.** Every ladle you spend is one you physically carried. Careful players get more units and towers than sloppy ones.
2. **Soup identity matters.** Five Turkish soups, each with its own carrying feel and its own two defence and two attack units. Your soup choice is your strategy.
3. **Always two jobs.** Both players carry soup during the whole half, so there's never a moment where one player just watches.
4. **Fair, mirrored matches.** Both players attack and defend the same map in each course.

## 3. Match structure — Best of 3 Courses

| Level | Contents |
|---|---|
| **Match** | Best of 3 **Courses** (first to 2 wins). |
| **Course** | One new procedural map (new seed). Has two **Halves**: P1 defends / P2 attacks, then they swap on the same map. |
| **Half** | **Prep phase** (50 s) → **Raid phase** (80 s, or until the Sofra falls). |

**Course result:** each player's attack score (**Doyum**, satisfaction) = hunger damage dealt to the enemy Sofra in their attacking half, plus a speed bonus of **+1 per 2 s left** if the Sofra was emptied early. Higher Doyum wins the course.
**Tie-break:** the player who **spilled less soup** across the course wins (fits the theme and rewards careful play).

**Comeback mechanic, "Annenin Eli" (Mom's Touch):** the player who lost the previous course gets **+15 % pot capacity** in the next course. It's small enough not to erase a skill gap, but it makes the decider tense.

Max match length: 6 halves × ~130 s ≈ **13 min**.

## 4. The map: terraced tile grid

Generated deterministically from a seed shared over the network. The grid is 22 × 14 tiles of 3.2 m, with **three terrace heights**.

- **Terraces:** noise is rounded to height levels 0/1/2 and smoothed so there are no one-tile spikes. Height differences become **cliffs** that block walking.
- **Road:** A* from the **Ocak** (west edge) through two waypoints to the **Sofra** (east edge). When it changes level it gets a **ramp** on a straight stretch, and it crosses the river on a **wooden bridge**. Painted arrows on the road point toward the Sofra.
- **River:** most maps have a river running north–south. Cooks can't wade across, so bridges and the road become **chokepoints** for carrying soup.
- **Stairs:** extra stone/wood stairs are added until every walkable area connects back to the main one, so no spot is a dead end.
- **Ledges:** cooks can **jump down** a cliff as a shortcut, but the landing adds a slosh impulse that scales with the fall.
- **Plazas:** cobbled 3×3 squares for the **Kitchen** (defender, next to the Sofra) and the **Market** (attacker, a third of the way along the road). Each holds five big boiling cauldrons in an arc and has a floating sign.
- **Pads:** 10 tiles beside the road, spread along its length. A pad that sits **higher than the road next to it** is high ground, marked with a gold ▲ flag, and gives towers **+25 % range**. Traps go on the road tile next to the pad.
- **Rough ground:** rocks and roots jolt the pot, and mud slows you down. Trees line the border and dot the open ground.

### Readability rules
- Only pads can be built on. They glow and show a `+` (or a gold ▲) for the defender only.
- A floating arrow and an off-screen edge arrow always point at your current objective, and the objective card in the top-left says what to do next.
- Every key location has a world label in its role colour (blue = defender, red = attacker).

## 4b. Modes
- **Play vs AI** (first time: guided tutorial; the prep timer pauses until each step is done)
- **Host / Join** (direct IP, port 7777)
- **Practice** (solo, no opponent)
- **Watch AI vs AI**

## 5. Carrying soup (the core mechanic)

### 5.1 Controls
| Input | Action |
|---|---|
| WASD | Move (camera-relative) |
| Shift | Sprint: ×1.45 speed, but acceleration sloshes a lot more |
| E | Interact: fill pot at a cauldron / pour at Ocak / build or upgrade at a pad |
| 1 / 2 | Pick the build option at a pad (each soup has 2 defence builds) |
| Q | **Dirsek** (elbow shove) |
| Mouse (attacker) | Recruit units from the recruit bar (hotkeys F1–F10 too) |

### 5.2 The pot
- Capacity **100 ladles** (115 with Annenin Eli). Filling at a cauldron is instant, and you pick a soup by walking to its cauldron.
- You can only carry **one soup type** at a time. Filling another type throws the rest away (and counts as spill).
- **Slosh model:** the soup surface is a damped 2D spring driven by your horizontal acceleration. When slosh goes past the rim threshold, soup spills at a rate proportional to how far over it is. The HUD shows the soup surface tilting in the bowl and red droplets when you spill.
- Bumps (rocks, roots, being shoved) add instant slosh impulses.

### 5.3 Soup carry traits
| Soup | Liquid | Spill × | Move × | Carry quirk |
|---|---|---|---|---|
| **Mercimek** (red lentil) | Thick | 0.70 | 0.92 | Heavy and steady, the beginner's soup |
| **Ezogelin** (bulgur-lentil, pul biber) | Medium | 1.00 | 1.00 | Baseline |
| **Yayla** (yoghurt-mint) | Thin | 1.30 | 1.10 | Fast but runny |
| **Tarhana** (fermented) | Thick | 0.85 | 0.97 | Forgiving |
| **İşkembe** (tripe, garlic-vinegar) | Medium | 1.10 | 1.00 | **Pungent aura**: enemy cooks within 4 m move 15 % slower |

### 5.4 Dirsek (elbow shove)
Cone of 1.8 m in front, **6 s cooldown**. The victim gets a **large slosh impulse** (typically 15–25 ladles lost) and is knocked back 2 m. The shover also gets a small slosh (0.5 impulse), so shoving with a full pot is risky.

### 5.5 Economy targets (tuning baseline)
A clean round trip is ~16–20 s. A typical player keeps **75–85 %** of each pot.
- Prep phase: ≈ 3 trips → **~230 ladles**
- Raid phase: ≈ 4 trips → **~300 ladles**
- **Total budget per half: ~500–550 ladles** for each side. All costs below are built around that.

## 6. Defence — 10 builds (2 per soup)

> **Numbers in this table are the original design targets.** Live costs and stats come from the Monte Carlo balancer (section 13) and are listed in [Balance_Report.md](Balance_Report.md).

The defender walks to an empty pad with a full-enough pot, presses **E**, and picks **1** or **2**. The soup in the pot decides which two builds are offered. Pouring the same soup into your own build upgrades it (**Lv2: 80 % of base cost, ×1.4 stats; Lv3: 100 % of base cost, ×1.9 stats**).

Towers have **300 HP** and can be damaged by siege units. Traps sit on the road next to their pad, **can't be damaged**, and can only be disabled.

| # | Soup | Name | Kind | Cost | Stats | Role |
|---|---|---|---|---|---|---|
| D1 | Mercimek | **Kepçe Mancınığı** (Ladle Catapult) | Tower | 70 | 40 dmg, splash r1.8, 1.6 s, range 9 | Anti-swarm all-rounder |
| D2 | Mercimek | **Mercimek Seddi** (Lentil Barricade) | Blocker | 50 | 600 HP wall across the road | Stalls a wave inside tower range |
| D3 | Ezogelin | **Pul Biber Kulesi** (Chili Spire) | Tower | 80 | 12 dmg every 0.5 s, range 8, burn 6 dps for 3 s | High single-target DPS |
| D4 | Ezogelin | **Kızgın Sac** (Hot Griddle) | Trap | 45 | 60 dps to everything standing on it (3 m strip) | Punishes swarms, combos with slows |
| D5 | Yayla | **Nane Esintisi** (Mint Breeze) | Tower | 70 | Pulse every 2 s, r6: 10 dmg + 40 % slow for 2.5 s | Support, multiplies other towers' time-on-target |
| D6 | Yayla | **Yoğurt Gölü** (Yoghurt Puddle) | Trap | 40 | 3 m zone, 55 % slow (+1 s after leaving) | Cheap slow, pair with Griddle |
| D7 | Tarhana | **Ekşi Maya Kulesi** (Sour Ferment Tower) | Tower | 85 | 20 dmg / 1.0 s, range 10, **Ekşime** stack: +8 % dmg taken per stack (max 5), 5 s | Amplifier that beats tanks |
| D8 | Tarhana | **Tarhana Fıçısı** (Ferment Barrel) | Trap | 55 | Triggers within 2 m: poison cloud r4, 25 dps for 6 s, re-arms in 15 s | Burst zone |
| D9 | İşkembe | **Sarımsak Kulesi** (Garlic Tower) | Tower | 90 | Pulse every 4 s, r5: 15 dmg + **Fear**: pushed 4 m back along the road | Time thief, resets runners |
| D10 | İşkembe | **Sirke Kapanı** (Vinegar Snare) | Trap | 50 | Roots up to 3 units for 2.5 s + 40 dmg, re-arms in 8 s | Pick / stall |

**Efficiency check (DPS per 100 ladles, single target):** Catapult 36 (+splash), Chili 38, Ferment 24 (×1.4 team amp at 5 stacks), Garlic 6 (+~2 s stolen per unit per pulse), Mint 7 (+40 % time-on-target for every tower). Supports trade raw DPS for multipliers, and a mix always beats spamming one tower.

## 7. Attack — 10 units (2 per soup)

> **Numbers in this table are the original design targets.** Live costs and stats come from the Monte Carlo balancer (section 13) and are listed in [Balance_Report.md](Balance_Report.md).

The attacker pours soup at the **Ocak** to bank it (by soup type), then recruits from the recruit bar. Units queue and leave the Ocak 0.6 s apart. During prep the queue fills, and everything marches when the raid starts.

Units follow the road. When one reaches the Sofra it deals its **Bite** (hunger damage) and leaves. **Sofra hunger: 120.**

| # | Soup | Name | Cost | HP | Speed | Bite | Special |
|---|---|---|---|---|---|---|---|
| A1 | Mercimek | **Kaşıkçı Er** (Spoon Grunt) | 15 | 90 | 2.6 | 2 | Cheap mass unit. Smashes barricades for 15 dps |
| A2 | Mercimek | **Kazan Kalkanlı** (Cauldron Shieldbearer) | 45 | 380 | 1.8 | 4 | 25 % armour. **Taunt**: single-target towers in range must shoot him |
| A3 | Ezogelin | **Biber Koşucusu** (Pepper Runner) | 20 | 60 | 4.4 | 2 | +30 % speed for 3 s after taking damage |
| A4 | Ezogelin | **Közcü Aşçı** (Ember Cook) | 40 | 160 | 2.4 | 3 | **Siege**: throws embers at towers within 6 m, 18 dps |
| A5 | Yayla | **Nane Şifacısı** (Mint Healer) | 35 | 110 | 2.4 | 1 | Heals allies within 4 m for 12 hp/s |
| A6 | Yayla | **Ayran Golemi** (Ayran Golem) | 50 | 300 | 2.0 | 4 | **Slow-immune**. Splits into 2 mini-golems (80 HP, Bite 1) on death |
| A7 | Tarhana | **Maya Sabotajcısı** (Ferment Saboteur) | 30 | 100 | 2.9 | 2 | Disarms the first 2 traps it passes for 10 s. Immune to traps |
| A8 | Tarhana | **Ekşi Bulut** (Sour Cloud) | 40 | 130 | 2.2 | 2 | Aura r6: enemy towers attack **35 % slower** |
| A9 | İşkembe | **Sarımsak Devi** (Garlic Brute) | 60 | 520 | 1.7 | 6 | **Fear & root immune**. Smashes towers within 4 m for 30 dps |
| A10 | İşkembe | **Sirke Bombacısı** (Vinegar Bomber) | 35 | 120 | 3.0 | 3 | Charges the first tower within 5 m: 120 dmg + disables towers within 4 m for 5 s, then dies |

**Value check: exposure = effective HP × speed ÷ cost** (how much tower time a ladle buys)

| Unit | Exposure / ladle | Bite / 10 ladles | Utility |
|---|---|---|---|
| Grunt | 15.6 | 1.3 | — |
| Shieldbearer | 20.3 | 0.9 | Taunt |
| Runner | 13.2 | 1.0 | Burst speed |
| Golem | 18.4 | 1.2 | Slow immune, split |
| Brute | 14.7 | 1.0 | Siege + immunities |
| Bomber | 10.3 | 0.9 | Tower burst/disable |
| Saboteur | 9.7 | 0.7 | Trap counter |
| Ember Cook | 9.6 | 0.75 | Ranged siege |
| Healer | 7.5 | 0.3 | Sustain |
| Sour Cloud | 7.2 | 0.5 | Tower debuff |

Raw-stat units sit at 13–20 and utility units at 7–10. Paying for utility should be a choice, never the default.

## 8. Counter web

```
Pepper Runner ──beaten by──▶ Mint Breeze / Yoghurt Puddle (slow)
Ayran Golem   ──ignores────▶ slows      ──beaten by──▶ Sour Ferment amp + Chili
Garlic Brute  ──ignores────▶ Fear/Root  ──beaten by──▶ Ekşime stacks, Barricade stall
Shieldbearer  ──soaks──────▶ single-target towers ──beaten by──▶ Catapult splash hits the escorts
Saboteur      ──beats──────▶ traps      ──beaten by──▶ towers (only 100 HP)
Grunt swarm   ──beaten by──▶ Griddle, Catapult, Barrel
Sour Cloud / Bomber / Ember ──beat──▶ tower clumps ──beaten by──▶ spreading pads, Garlic pushback
```

There's no dominant soup. Mono-soup defences have a hard counter on the attack side, and mono-soup attacks have one on the defence side.

## 9. Sample half (what a good game looks like)

1. **Prep 0–15 s:** the defender carries Mercimek (safe) to a mid pad: Catapult. The attacker sprints Ezogelin through the rocks to the Ocak and loses 30 % of it.
2. **Prep 15–50 s:** the defender reads the attacker's bank (it's public on the HUD) and sees lots of Ezogelin, so they carry Yayla to the front: Mint Breeze + Yoghurt Puddle. The attacker queues 8 runners + 2 Ember Cooks and switches to Tarhana for Sour Clouds.
3. **Raid:** runners get slowed and griddled. The attacker answers mid-raid with a Garlic Brute + Bomber push. The defender runs a full pot of Tarhana to upgrade the Ferment Tower to Lv2. Both cooks meet on the open field, and an elbow costs the attacker 25 ladles, which is exactly one Bomber.

## 10. Netcode architecture (Netcode for GameObjects 2.x)

- **Topology:** Host + 1 client (Unity Transport, direct IP). Server-authoritative simulation for units, towers, economy and phases.
- **Cook movement:** owner-authoritative (`ClientNetworkTransform`) for responsive carrying. The **pot fill** is an owner-write NetworkVariable, and every spend is validated on the server (distance to station, fill ≥ cost).
- **Units:** don't use NetworkTransform. They sync `pathDistance`, `lateral`, `hp` and a status bitmask, and clients rebuild positions from the shared road spline. Thirty units only cost a few floats each.
- **Towers:** sync type, level, pad, HP, disabled flag. Shots are cosmetic `ClientRpc`s.
- **Map:** only the **seed** is synced. Each peer generates the identical map.

## 11. Art direction — hand-painted

- **Texturing:** all colour lives in the albedo. Light is painted in (top-down warm gradient, cool shadow side), with darkened cavities, brush-stroke noise and pale edge highlights. Textures are baked in Blender.
- **Shader:** a custom URP "HandPainted" shader that's mostly unlit (soft wrap-light + painted rim) so the painted texture reads the way it was painted.
- **Shapes:** chunky, slightly crooked proportions, big readable silhouettes, soup-identity colours on every unit and tower:
  Mercimek **gold**, Ezogelin **red**, Yayla **mint**, Tarhana **amber**, İşkembe **lavender**.
- **UI:** parchment panels with painted brown borders, wooden plank buttons, painted soup badges, and unit/tower portraits rendered from the actual models.

## 12. The attacker's raid: more than carrying

Carrying soup to the Ocak buys raiders, but the raid itself is where the attacker plays:

- **Waves.** Recruited raiders wait at the Ocak. Press **G** to send them all at once, or they leave by themselves at 10 raiders or after 14 s. Big waves overwhelm single-target towers and set up ladle combos. Anything queued during prep marches when the raid starts.
- **Ladle toss (R / right-click).** Carry a pot onto the battlefield and throw ladles of it (cheap per toss, 13 m range, 3.6 m splash). Each soup does something different:
  - **Mercimek, Hearty:** raiders in the splash take far less damage.
  - **Ezogelin, Spicy:** raiders sprint through tower range.
  - **Yayla, Soothing:** heals raiders and clears slows, burns and roots.
  - **Tarhana, Sour fog:** towers in the fog fire much slower.
  - **İşkembe, Stench:** stuns towers briefly, disarms traps, and raiders ignore fear and roots.
- **Rally.** Raiders within 5 m of the attacking cook march 20% faster and take 15% less damage, so escorting a wave matters.
- **Counterplay.** The defender can elbow the escorting attacker to make them spill, and the attacker's pot is what's being spent.

So the raid becomes a loop: bank soup and recruit, hold and send a wave, grab a support soup, escort the wave and throw the right ladle at the right tower.

## 13. Balance: headless Monte Carlo

All combat rules live in one engine-free C# core, `Assets/Corba/Scripts/Sim`. The Unity server ticks it at 20 Hz, and units and towers are network views of it. `Tools/CorbaSim` is a .NET console app that compiles the *same* files and runs about 11,000 simulated halves per second on 16 threads, using cook travel times taken from real exported maps (NavGrid walking distances).

How the tuner works (`dotnet run -c Release -- tune`):

1. **Co-evolution.** Populations of attacker and defender strategies (unit mix, build preferences, wave size, support share, ladle choice, adaptivity) evolve against each other. An archive of past elites is kept (fictitious play), so prices follow the whole meta instead of chasing its latest swing.
2. **Paired A/B measurement.** The same matchup and seed is replayed with one extra unit, defence or ladle pot, to measure what each is worth per ladle of soup.
3. **Adjustment.** Costs move until value per ladle is even. Anything above twice its fair share of elite spend gets a small dominance penalty. Options stuck at a cost bound get their stats changed instead. The Sofra is sized for a 55% average raid score.
4. **Output.** `BalanceTuning.g.cs` (compiled by Unity and the tool), the bots' value weights, and `Docs/Balance_Report.md`.

**Anti-monoculture rules found by the simulator:** each repeat of the same defence costs +30%, and each repeat of the same unit in a half costs +5% (capped at +50%). Without these, the evolved meta collapsed into "spam Griddles" against "spam Saboteurs".

## 14. Tuning knobs

Everything numeric is in `Assets/Corba/Scripts/Sim/Balance.cs`; the tuner overrides go in `BalanceTuning.g.cs`. Cook feel (slosh spring, sprint, shove) stays in `GameData.cs`.
