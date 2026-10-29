# Corba balance report

Generated 2026-10-29 15:40 by `Tools/CorbaSim` (1,643,704 simulated halves in 113s on 16 threads, 40 real exported maps).

**Method.** Attacker and defender strategy populations co-evolve against each other, so the numbers describe what *good* play does, not random play. Each value is a paired A/B test on the same matchup and seed: give one side a little extra of one thing and measure the change in raid score. Costs are then nudged until every option is worth about the same per ladle of soup.

- Mean raid score: **56 %** of the Sofra (target 55%); Sofra emptied in 13 % of halves; Sofra hunger 60; raider HP x5.00
- Per half: 26.9 raiders sent, 6.4 defences built; ladle toss costs 5
- Value spread (coefficient of variation, lower is more even): units 0.24, defences 0.54, ladles 1.14

## Attack units

| Unit | Soup | Cost | Hunger per 100 ladles | Share of winning attackers' spend |
|---|---|---|---|---|
| Spoon Grunt | Mercimek | 31 | 6.4 | 8 % |
| Cauldron Shieldbearer | Mercimek | 72 | 5.1 | 3 % |
| Pepper Runner | Ezogelin | 25 | 7.0 | 6 % |
| Ember Cook | Ezogelin | 59 | 5.6 | 13 % |
| Mint Healer | Yayla | 19 | 5.4 | 3 % |
| Ayran Golem | Yayla | 69 | 5.4 | 5 % |
| Ferment Saboteur | Tarhana | 32 | 6.1 | 7 % |
| Sour Cloud | Tarhana | 32 | 5.9 | 10 % |
| Garlic Brute | Iskembe | 106 | 5.0 | 1 % |
| Vinegar Bomber | Iskembe | 19 | 10.3 | 45 % |

## Defences

| Defence | Soup | Kind | Cost | Hunger saved per 100 ladles | Share of winning defenders' spend |
|---|---|---|---|---|---|
| Ladle Catapult | Mercimek | Tower | 87 | 7.5 | 25 % |
| Lentil Barricade | Mercimek | Blocker | 79 | 1.0 | 47 % |
| Chili Spire | Ezogelin | Tower | 83 | 7.6 | 17 % |
| Hot Griddle | Ezogelin | Trap | 56 | 11.6 | 2 % |
| Mint Breeze | Yayla | Tower | 89 | 6.7 | 0 % |
| Yoghurt Puddle | Yayla | Trap | 36 | 2.2 | 2 % |
| Sour Ferment Tower | Tarhana | Tower | 86 | 7.2 | 0 % |
| Ferment Barrel | Tarhana | Trap | 27 | 4.3 | 1 % |
| Garlic Tower | Iskembe | Tower | 66 | 9.2 | 4 % |
| Vinegar Snare | Iskembe | Trap | 34 | 2.4 | 2 % |

## Ladle tosses

| Soup | Effect | Magnitude | Duration | Hunger per 100 ladles thrown | Share of tosses |
|---|---|---|---|---|---|
| Mercimek | Hearty | 0.48 | 6s | 0.7 | 37 % |
| Ezogelin | Spicy | 1.17 | 5s | -0.6 | 19 % |
| Yayla | Soothing | 25 | 0s | 1.0 | 23 % |
| Tarhana | Sour fog | 0.52 | 7s | 1.0 | 15 % |
| Iskembe | Stench | 3 | 9s | 0.4 | 5 % |

## Tuning log

```
Corba Monte Carlo tuner: 40 maps, 900 samples/iteration, 16 threads
exploit gaps: attacker +10 pts, defender +2 pts

== iteration 1/48  (36,792 sims, 3.0s)
raid fraction 0.130  sofra fell 0 %  units/half 18.9  builds/half 4.8  raider hp x1.00
value spread: units CV 0.43  defences CV 0.36  ladles CV 0.84
units:    Grunt 15 v4.5 s19 %  Shieldbearer 45 v6.3 s23 %  Runner 20 v0.6 s5 %  Cook 40 v4.3 s9 %  Healer 35 v2.6 s5 %  Golem 50 v5.5 s6 %  Saboteur 30 v2.9 s10 %  Cloud 40 v2.6 s6 %  Brute 60 v2.8 s7 %  Bomber 35 v4.0 s11 %
defences: Catapult 70 v3.4 s23 %  Barricade 50 v5.4 s3 %  Spire 80 v3.0 s26 %  Griddle 45 v3.6 s6 %  Breeze 70 v4.4 s16 %  Puddle 40 v2.7 s1 %  Tower 85 v3.4 s20 %  Barrel 55 v1.7 s2 %  Tower 90 v2.0 s0 %  Snare 50 v1.6 s3 %
ladles:   Hearty v3.4 s28 %  Spicy v1.5 s10 %  Soothing v11.6 s15 %  Sour fog v4.4 s41 %  Stench v1.4 s5 %
exploit gaps: attacker +3 pts, defender +2 pts

== iteration 2/48  (69,488 sims, 5.2s)
raid fraction 0.175  sofra fell 0 %  units/half 17.8  builds/half 4.9  raider hp x1.14
value spread: units CV 0.35  defences CV 0.27  ladles CV 0.77
units:    Grunt 17 v5.2 s21 %  Shieldbearer 53 v6.9 s33 %  Runner 17 v1.1 s5 %  Cook 45 v4.3 s5 %  Healer 30 v3.3 s4 %  Golem 59 v5.8 s4 %  Saboteur 28 v3.7 s8 %  Cloud 35 v4.0 s5 %  Brute 54 v5.4 s8 %  Bomber 38 v3.8 s7 %
defences: Catapult 73 v3.5 s21 %  Barricade 59 v5.4 s3 %  Spire 81 v3.4 s27 %  Griddle 48 v3.5 s2 %  Breeze 83 v4.6 s14 %  Puddle 37 v3.1 s6 %  Tower 88 v4.1 s16 %  Barrel 47 v2.4 s8 %  Tower 76 v2.8 s1 %  Snare 42 v2.1 s2 %
ladles:   Hearty v3.7 s24 %  Spicy v2.1 s6 %  Soothing v10.6 s19 %  Sour fog v3.9 s47 %  Stench v1.2 s5 %
exploit gaps: attacker +3 pts, defender +7 pts

== iteration 3/48  (102,184 sims, 7.2s)
raid fraction 0.265  sofra fell 0 %  units/half 15.2  builds/half 5.1  raider hp x1.29
value spread: units CV 0.34  defences CV 0.24  ladles CV 0.65
units:    Grunt 19 v5.1 s20 %  Shieldbearer 61 v6.7 s41 %  Runner 15 v1.5 s5 %  Cook 46 v4.6 s4 %  Healer 27 v3.5 s4 %  Golem 68 v5.1 s2 %  Saboteur 27 v5.1 s8 %  Cloud 34 v3.6 s3 %  Brute 61 v7.4 s9 %  Bomber 37 v3.7 s5 %
defences: Catapult 74 v4.1 s22 %  Barricade 68 v5.2 s2 %  Spire 84 v3.9 s30 %  Griddle 48 v3.4 s3 %  Breeze 94 v5.3 s0 %  Puddle 35 v5.1 s7 %  Tower 95 v5.0 s1 %  Barrel 41 v2.3 s34 %  Tower 70 v3.4 s0 %  Snare 37 v3.3 s2 %
ladles:   Hearty v3.5 s29 %  Spicy v2.2 s4 %  Soothing v9.9 s25 %  Sour fog v3.2 s29 %  Stench v3.0 s12 %
exploit gaps: attacker +4 pts, defender +3 pts

== iteration 4/48  (134,880 sims, 9.0s)
raid fraction 0.273  sofra fell 0 %  units/half 13.6  builds/half 5.3  raider hp x1.47
value spread: units CV 0.24  defences CV 0.19  ladles CV 0.66
units:    Grunt 19 v5.3 s15 %  Shieldbearer 70 v6.1 s41 %  Runner 13 v2.7 s4 %  Cook 45 v4.8 s4 %  Healer 24 v4.3 s5 %  Golem 69 v5.6 s2 %  Saboteur 27 v6.0 s8 %  Cloud 30 v4.4 s3 %  Brute 70 v7.2 s14 %  Bomber 33 v4.0 s5 %
defences: Catapult 76 v3.8 s22 %  Barricade 76 v4.8 s2 %  Spire 87 v3.5 s30 %  Griddle 45 v3.3 s7 %  Breeze 95 v4.9 s0 %  Puddle 39 v4.6 s3 %  Tower 95 v4.7 s1 %  Barrel 36 v2.6 s31 %  Tower 66 v3.3 s0 %  Snare 34 v3.8 s4 %
ladles:   Hearty v3.4 s26 %  Spicy v2.6 s4 %  Soothing v10.8 s28 %  Sour fog v3.3 s32 %  Stench v3.2 s11 %
exploit gaps: attacker +3 pts, defender +1 pts

== iteration 5/48  (167,576 sims, 10.8s)
raid fraction 0.299  sofra fell 0 %  units/half 12.8  builds/half 5.3  raider hp x1.67
value spread: units CV 0.16  defences CV 0.16  ladles CV 0.63
units:    Grunt 19 v5.9 s14 %  Shieldbearer 79 v5.1 s41 %  Runner 12 v4.1 s4 %  Cook 44 v5.2 s4 %  Healer 23 v4.8 s4 %  Golem 71 v5.3 s2 %  Saboteur 29 v6.3 s9 %  Cloud 29 v4.6 s4 %  Brute 79 v6.7 s15 %  Bomber 30 v4.2 s4 %
defences: Catapult 77 v3.5 s22 %  Barricade 83 v4.8 s3 %  Spire 88 v3.4 s29 %  Griddle 43 v3.3 s14 %  Breeze 95 v4.5 s0 %  Puddle 42 v4.2 s4 %  Tower 95 v4.4 s1 %  Barrel 33 v2.9 s22 %  Tower 63 v3.4 s0 %  Snare 34 v4.5 s6 %
ladles:   Hearty v3.5 s22 %  Spicy v3.1 s5 %  Soothing v11.0 s30 %  Sour fog v3.1 s32 %  Stench v3.6 s11 %
exploit gaps: attacker +6 pts, defender +1 pts

== iteration 6/48  (200,272 sims, 12.8s)
raid fraction 0.295  sofra fell 0 %  units/half 15.6  builds/half 5.5  raider hp x1.89
value spread: units CV 0.11  defences CV 0.07  ladles CV 0.55
units:    Grunt 20 v6.4 s14 %  Shieldbearer 84 v4.8 s27 %  Runner 11 v6.6 s7 %  Cook 44 v6.5 s10 %  Healer 22 v5.3 s6 %  Golem 71 v5.4 s4 %  Saboteur 31 v6.3 s9 %  Cloud 28 v5.4 s3 %  Brute 86 v6.2 s11 %  Bomber 28 v5.0 s10 %
defences: Catapult 75 v3.7 s23 %  Barricade 89 v4.4 s5 %  Spire 88 v3.8 s31 %  Griddle 41 v4.2 s12 %  Breeze 95 v4.7 s0 %  Puddle 43 v4.5 s4 %  Tower 95 v4.4 s0 %  Barrel 30 v4.0 s21 %  Tower 60 v4.4 s0 %  Snare 36 v4.3 s2 %
ladles:   Hearty v4.6 s25 %  Spicy v5.3 s4 %  Soothing v13.3 s27 %  Sour fog v3.7 s30 %  Stench v4.9 s15 %
exploit gaps: attacker +15 pts, defender +1 pts

== iteration 7/48  (232,968 sims, 14.8s)
raid fraction 0.344  sofra fell 1 %  units/half 17.9  builds/half 5.6  raider hp x2.15
value spread: units CV 0.21  defences CV 0.13  ladles CV 0.44
units:    Grunt 21 v6.8 s12 %  Shieldbearer 81 v5.6 s23 %  Runner 11 v10.6 s13 %  Cook 46 v7.0 s15 %  Healer 21 v5.7 s6 %  Golem 69 v5.8 s3 %  Saboteur 32 v6.6 s8 %  Cloud 27 v6.0 s3 %  Brute 88 v6.5 s7 %  Bomber 27 v5.6 s10 %
defences: Catapult 72 v4.4 s25 %  Barricade 89 v6.2 s4 %  Spire 88 v5.3 s31 %  Griddle 41 v6.0 s14 %  Breeze 95 v6.4 s0 %  Puddle 43 v7.4 s4 %  Tower 95 v5.4 s1 %  Barrel 29 v5.7 s19 %  Tower 60 v6.2 s0 %  Snare 36 v5.3 s2 %
ladles:   Hearty v5.7 s25 %  Spicy v5.4 s6 %  Soothing v12.2 s25 %  Sour fog v4.6 s24 %  Stench v4.9 s19 %
exploit gaps: attacker +3 pts, defender +0 pts

== iteration 8/48  (265,664 sims, 16.9s)
raid fraction 0.397  sofra fell 3 %  units/half 19.8  builds/half 5.7  raider hp x2.45
value spread: units CV 0.22  defences CV 0.20  ladles CV 0.35
units:    Grunt 21 v6.7 s10 %  Shieldbearer 79 v5.8 s20 %  Runner 12 v11.3 s19 %  Cook 48 v6.5 s18 %  Healer 20 v5.9 s5 %  Golem 68 v6.1 s3 %  Saboteur 33 v6.9 s6 %  Cloud 27 v6.3 s3 %  Brute 89 v6.9 s5 %  Bomber 26 v6.3 s10 %
defences: Catapult 67 v5.1 s24 %  Barricade 90 v8.8 s0 %  Spire 89 v6.4 s30 %  Griddle 41 v7.9 s21 %  Breeze 95 v7.6 s0 %  Puddle 46 v10.8 s2 %  Tower 93 v6.2 s3 %  Barrel 29 v7.7 s18 %  Tower 61 v7.2 s1 %  Snare 35 v6.9 s2 %
ladles:   Hearty v4.9 s30 %  Spicy v5.0 s4 %  Soothing v10.3 s26 %  Sour fog v4.7 s23 %  Stench v5.4 s18 %
exploit gaps: attacker +15 pts, defender +0 pts

== iteration 9/48  (298,360 sims, 19.1s)
raid fraction 0.457  sofra fell 5 %  units/half 21.3  builds/half 5.5  raider hp x2.72
value spread: units CV 0.31  defences CV 0.24  ladles CV 0.38
units:    Grunt 21 v6.8 s7 %  Shieldbearer 77 v5.3 s18 %  Runner 13 v12.4 s29 %  Cook 48 v6.2 s17 %  Healer 20 v5.4 s5 %  Golem 67 v5.5 s3 %  Saboteur 34 v6.1 s6 %  Cloud 27 v6.2 s3 %  Brute 91 v5.7 s4 %  Bomber 26 v5.1 s10 %
defences: Catapult 61 v5.5 s24 %  Barricade 94 v11.1 s0 %  Spire 89 v6.6 s29 %  Griddle 42 v7.5 s19 %  Breeze 95 v9.0 s0 %  Puddle 51 v11.9 s3 %  Tower 89 v6.5 s10 %  Barrel 29 v7.4 s12 %  Tower 61 v7.6 s1 %  Snare 34 v7.7 s2 %
ladles:   Hearty v4.9 s28 %  Spicy v4.2 s5 %  Soothing v9.1 s22 %  Sour fog v4.3 s28 %  Stench v3.5 s17 %
exploit gaps: attacker +13 pts, defender +1 pts

== iteration 10/48  (331,056 sims, 21.2s)
raid fraction 0.500  sofra fell 7 %  units/half 22.1  builds/half 5.3  raider hp x2.89
value spread: units CV 0.28  defences CV 0.20  ladles CV 0.30
units:    Grunt 22 v6.9 s9 %  Shieldbearer 75 v5.8 s16 %  Runner 14 v12.1 s33 %  Cook 49 v6.1 s15 %  Healer 20 v5.4 s5 %  Golem 66 v6.0 s3 %  Saboteur 34 v6.4 s5 %  Cloud 27 v6.5 s2 %  Brute 90 v6.0 s4 %  Bomber 25 v5.6 s9 %
defences: Catapult 57 v6.2 s21 %  Barricade 95 v11.9 s0 %  Spire 89 v7.0 s28 %  Griddle 42 v8.1 s23 %  Breeze 95 v9.0 s0 %  Puddle 56 v11.3 s2 %  Tower 85 v6.9 s14 %  Barrel 29 v8.4 s10 %  Tower 61 v8.0 s1 %  Snare 34 v8.2 s1 %
ladles:   Hearty v4.4 s24 %  Spicy v4.1 s3 %  Soothing v7.4 s28 %  Sour fog v3.8 s30 %  Stench v3.6 s15 %
exploit gaps: attacker +15 pts, defender +0 pts

== iteration 11/48  (363,752 sims, 23.4s)
raid fraction 0.515  sofra fell 7 %  units/half 24.0  builds/half 5.3  raider hp x2.97
value spread: units CV 0.28  defences CV 0.16  ladles CV 0.30
units:    Grunt 23 v6.4 s6 %  Shieldbearer 74 v6.0 s12 %  Runner 15 v12.2 s46 %  Cook 49 v5.9 s13 %  Healer 19 v6.1 s5 %  Golem 66 v6.0 s2 %  Saboteur 34 v6.3 s4 %  Cloud 27 v6.6 s2 %  Brute 90 v6.0 s3 %  Bomber 25 v5.4 s8 %
defences: Catapult 54 v6.8 s20 %  Barricade 95 v11.6 s0 %  Spire 88 v7.3 s29 %  Griddle 42 v8.3 s21 %  Breeze 95 v9.8 s0 %  Puddle 61 v10.3 s2 %  Tower 81 v7.7 s16 %  Barrel 29 v9.0 s12 %  Tower 61 v8.1 s0 %  Snare 34 v8.1 s1 %
ladles:   Hearty v5.9 s33 %  Spicy v7.1 s3 %  Soothing v10.1 s21 %  Sour fog v4.9 s31 %  Stench v4.7 s12 %
exploit gaps: attacker +3 pts, defender +1 pts

== iteration 12/48  (396,448 sims, 25.6s)
raid fraction 0.533  sofra fell 8 %  units/half 25.3  builds/half 5.4  raider hp x3.03
value spread: units CV 0.24  defences CV 0.14  ladles CV 0.27
units:    Grunt 23 v5.9 s4 %  Shieldbearer 74 v6.3 s9 %  Runner 16 v11.3 s54 %  Cook 49 v5.9 s13 %  Healer 19 v5.9 s4 %  Golem 66 v6.0 s2 %  Saboteur 34 v6.5 s4 %  Cloud 28 v6.0 s2 %  Brute 90 v6.0 s3 %  Bomber 24 v5.8 s6 %
defences: Catapult 52 v7.6 s18 %  Barricade 95 v11.4 s0 %  Spire 88 v7.5 s31 %  Griddle 42 v10.2 s21 %  Breeze 95 v10.4 s0 %  Puddle 64 v10.2 s1 %  Tower 80 v8.2 s17 %  Barrel 30 v10.6 s11 %  Tower 61 v8.7 s1 %  Snare 34 v8.4 s1 %
ladles:   Hearty v6.2 s24 %  Spicy v6.2 s4 %  Soothing v9.8 s27 %  Sour fog v5.7 s30 %  Stench v4.6 s15 %
exploit gaps: attacker +3 pts, defender +2 pts

== iteration 13/48  (429,144 sims, 27.9s)
raid fraction 0.489  sofra fell 5 %  units/half 25.8  builds/half 5.5  raider hp x3.05
value spread: units CV 0.24  defences CV 0.11  ladles CV 0.20
units:    Grunt 23 v5.9 s4 %  Shieldbearer 75 v6.8 s6 %  Runner 17 v11.5 s60 %  Cook 49 v5.7 s12 %  Healer 19 v6.6 s4 %  Golem 66 v6.3 s2 %  Saboteur 35 v6.2 s3 %  Cloud 28 v6.0 s1 %  Brute 90 v6.1 s2 %  Bomber 24 v6.0 s5 %
defences: Catapult 50 v7.6 s16 %  Barricade 95 v9.7 s0 %  Spire 86 v7.6 s32 %  Griddle 43 v10.3 s14 %  Breeze 95 v9.9 s0 %  Puddle 65 v9.3 s1 %  Tower 77 v8.9 s19 %  Barrel 31 v10.9 s16 %  Tower 60 v9.2 s1 %  Snare 33 v9.0 s1 %
ladles:   Hearty v7.1 s21 %  Spicy v6.6 s6 %  Soothing v9.7 s30 %  Sour fog v7.2 s32 %  Stench v5.4 s11 %
exploit gaps: attacker +2 pts, defender +3 pts

== iteration 14/48  (461,840 sims, 30.4s)
raid fraction 0.525  sofra fell 7 %  units/half 27.8  builds/half 5.5  raider hp x3.15
value spread: units CV 0.24  defences CV 0.09  ladles CV 0.23
units:    Grunt 23 v5.7 s2 %  Shieldbearer 76 v7.1 s3 %  Runner 18 v11.3 s69 %  Cook 48 v5.8 s12 %  Healer 19 v6.8 s4 %  Golem 66 v6.4 s2 %  Saboteur 35 v6.0 s2 %  Cloud 28 v5.7 s1 %  Brute 90 v6.2 s1 %  Bomber 24 v5.7 s4 %
defences: Catapult 48 v9.0 s14 %  Barricade 95 v9.9 s0 %  Spire 85 v8.1 s33 %  Griddle 44 v10.8 s13 %  Breeze 95 v10.7 s0 %  Puddle 65 v9.2 s1 %  Tower 76 v9.6 s19 %  Barrel 32 v11.2 s19 %  Tower 60 v9.7 s0 %  Snare 33 v9.7 s1 %
ladles:   Hearty v7.6 s24 %  Spicy v7.7 s4 %  Soothing v10.6 s29 %  Sour fog v7.0 s16 %  Stench v5.2 s28 %
exploit gaps: attacker +1 pts, defender +3 pts

== iteration 15/48  (494,536 sims, 32.6s)
raid fraction 0.529  sofra fell 8 %  units/half 28.6  builds/half 5.4  raider hp x3.19
value spread: units CV 0.21  defences CV 0.10  ladles CV 0.15
units:    Grunt 23 v5.7 s2 %  Shieldbearer 78 v7.3 s2 %  Runner 19 v11.1 s73 %  Cook 47 v6.2 s11 %  Healer 19 v7.0 s3 %  Golem 67 v6.5 s1 %  Saboteur 35 v6.5 s2 %  Cloud 28 v6.3 s1 %  Brute 90 v6.7 s1 %  Bomber 24 v6.3 s3 %
defences: Catapult 47 v10.0 s10 %  Barricade 95 v9.2 s0 %  Spire 84 v8.5 s32 %  Griddle 45 v11.3 s15 %  Breeze 95 v11.5 s0 %  Puddle 64 v9.9 s1 %  Tower 76 v10.3 s21 %  Barrel 33 v11.8 s20 %  Tower 60 v10.1 s1 %  Snare 33 v9.8 s1 %
ladles:   Hearty v8.2 s15 %  Spicy v8.7 s4 %  Soothing v9.8 s37 %  Sour fog v8.0 s16 %  Stench v6.0 s27 %
exploit gaps: attacker +1 pts, defender +3 pts

== iteration 16/48  (527,232 sims, 34.9s)
raid fraction 0.498  sofra fell 5 %  units/half 28.5  builds/half 5.4  raider hp x3.22
value spread: units CV 0.19  defences CV 0.07  ladles CV 0.15
units:    Grunt 22 v5.8 s2 %  Shieldbearer 80 v7.4 s1 %  Runner 20 v10.5 s79 %  Cook 46 v6.1 s10 %  Healer 19 v7.2 s3 %  Golem 67 v6.9 s1 %  Saboteur 35 v6.2 s1 %  Cloud 28 v6.2 s0 %  Brute 91 v6.4 s0 %  Bomber 24 v6.6 s2 %
defences: Catapult 47 v9.3 s8 %  Barricade 93 v9.1 s0 %  Spire 84 v8.4 s31 %  Griddle 46 v10.6 s15 %  Breeze 95 v10.6 s0 %  Puddle 64 v9.7 s1 %  Tower 77 v9.6 s22 %  Barrel 34 v10.7 s22 %  Tower 60 v9.8 s1 %  Snare 33 v9.6 s1 %
ladles:   Hearty v7.9 s8 %  Spicy v7.9 s0 %  Soothing v10.0 s44 %  Sour fog v8.8 s13 %  Stench v6.3 s36 %
exploit gaps: attacker +3 pts, defender +0 pts

== iteration 17/48  (559,928 sims, 37.1s)
raid fraction 0.483  sofra fell 4 %  units/half 28.1  builds/half 5.3  raider hp x3.30
value spread: units CV 0.15  defences CV 0.08  ladles CV 0.14
units:    Grunt 21 v6.3 s2 %  Shieldbearer 82 v7.2 s1 %  Runner 21 v10.2 s82 %  Cook 45 v6.6 s9 %  Healer 19 v7.5 s2 %  Golem 68 v6.8 s1 %  Saboteur 35 v6.3 s0 %  Cloud 28 v6.7 s0 %  Brute 91 v6.7 s0 %  Bomber 24 v7.1 s2 %
defences: Catapult 47 v8.8 s6 %  Barricade 92 v8.3 s0 %  Spire 84 v7.7 s31 %  Griddle 47 v9.7 s17 %  Breeze 95 v10.1 s0 %  Puddle 64 v8.9 s1 %  Tower 77 v8.9 s24 %  Barrel 35 v9.9 s20 %  Tower 60 v9.4 s1 %  Snare 33 v8.9 s0 %
ladles:   Hearty v8.0 s7 %  Spicy v7.5 s0 %  Soothing v9.4 s45 %  Sour fog v8.5 s11 %  Stench v6.0 s37 %
exploit gaps: attacker +0 pts, defender +1 pts

== iteration 18/48  (592,624 sims, 39.0s)
raid fraction 0.461  sofra fell 2 %  units/half 27.6  builds/half 5.1  raider hp x3.40
value spread: units CV 0.11  defences CV 0.06  ladles CV 0.15
units:    Grunt 21 v6.5 s1 %  Shieldbearer 83 v8.0 s1 %  Runner 23 v9.4 s86 %  Cook 45 v6.7 s7 %  Healer 19 v7.6 s2 %  Golem 68 v7.3 s1 %  Saboteur 35 v6.5 s0 %  Cloud 28 v7.0 s0 %  Brute 91 v7.7 s0 %  Bomber 24 v7.1 s2 %
defences: Catapult 47 v8.7 s7 %  Barricade 91 v8.4 s0 %  Spire 84 v7.2 s31 %  Griddle 48 v7.9 s21 %  Breeze 95 v9.2 s0 %  Puddle 64 v8.4 s2 %  Tower 78 v8.2 s25 %  Barrel 36 v8.1 s14 %  Tower 61 v8.4 s1 %  Snare 33 v8.4 s0 %
ladles:   Hearty v8.2 s28 %  Spicy v8.8 s0 %  Soothing v9.0 s25 %  Sour fog v8.1 s10 %  Stench v5.7 s38 %
exploit gaps: attacker +4 pts, defender +0 pts

== iteration 19/48  (625,320 sims, 40.9s)
raid fraction 0.442  sofra fell 2 %  units/half 26.1  builds/half 5.1  raider hp x3.55
value spread: units CV 0.11  defences CV 0.08  ladles CV 0.13
units:    Grunt 21 v6.1 s1 %  Shieldbearer 85 v7.4 s1 %  Runner 25 v8.6 s87 %  Cook 44 v6.4 s5 %  Healer 19 v7.0 s2 %  Golem 68 v6.9 s1 %  Saboteur 34 v6.2 s0 %  Cloud 28 v6.6 s0 %  Brute 92 v7.5 s0 %  Bomber 24 v6.1 s1 %
defences: Catapult 47 v7.2 s6 %  Barricade 91 v7.8 s0 %  Spire 84 v6.6 s30 %  Griddle 48 v7.2 s21 %  Breeze 95 v8.4 s0 %  Puddle 64 v8.0 s2 %  Tower 79 v7.7 s25 %  Barrel 36 v7.0 s15 %  Tower 61 v8.0 s1 %  Snare 33 v8.7 s0 %
ladles:   Hearty v7.5 s6 %  Spicy v8.1 s2 %  Soothing v7.6 s52 %  Sour fog v7.9 s16 %  Stench v5.5 s23 %
exploit gaps: attacker +0 pts, defender +0 pts

== iteration 20/48  (658,016 sims, 42.6s)
raid fraction 0.417  sofra fell 1 %  units/half 24.6  builds/half 5.2  raider hp x3.72
value spread: units CV 0.07  defences CV 0.11  ladles CV 0.17
units:    Grunt 21 v6.9 s2 %  Shieldbearer 86 v7.1 s2 %  Runner 27 v7.5 s84 %  Cook 44 v6.2 s5 %  Healer 19 v6.7 s3 %  Golem 68 v7.0 s1 %  Saboteur 33 v6.5 s0 %  Cloud 28 v6.7 s0 %  Brute 94 v7.0 s0 %  Bomber 24 v5.9 s2 %
defences: Catapult 46 v6.0 s7 %  Barricade 91 v7.4 s0 %  Spire 83 v6.4 s30 %  Griddle 47 v6.4 s21 %  Breeze 95 v7.9 s0 %  Puddle 64 v7.5 s1 %  Tower 80 v7.3 s26 %  Barrel 35 v6.2 s14 %  Tower 61 v7.2 s1 %  Snare 34 v8.6 s0 %
ladles:   Hearty v6.7 s0 %  Spicy v8.4 s0 %  Soothing v6.8 s38 %  Sour fog v7.0 s21 %  Stench v4.8 s41 %
exploit gaps: attacker +2 pts, defender +0 pts

== iteration 21/48  (690,712 sims, 44.5s)
raid fraction 0.411  sofra fell 1 %  units/half 23.8  builds/half 5.3  raider hp x3.96
value spread: units CV 0.06  defences CV 0.14  ladles CV 0.15
units:    Grunt 21 v7.3 s3 %  Shieldbearer 87 v7.0 s2 %  Runner 29 v7.2 s82 %  Cook 43 v7.2 s5 %  Healer 19 v6.9 s4 %  Golem 68 v7.2 s1 %  Saboteur 33 v6.6 s1 %  Cloud 28 v7.0 s0 %  Brute 95 v7.5 s0 %  Bomber 23 v6.0 s2 %
defences: Catapult 44 v5.3 s9 %  Barricade 91 v7.0 s0 %  Spire 83 v6.0 s30 %  Griddle 46 v6.3 s22 %  Breeze 95 v7.3 s0 %  Puddle 64 v7.3 s1 %  Tower 81 v7.3 s27 %  Barrel 34 v5.5 s11 %  Tower 61 v6.7 s0 %  Snare 35 v8.7 s0 %
ladles:   Hearty v5.8 s18 %  Spicy v7.2 s1 %  Soothing v6.3 s28 %  Sour fog v6.4 s30 %  Stench v4.4 s23 %
exploit gaps: attacker +3 pts, defender +1 pts

== iteration 22/48  (723,408 sims, 46.5s)
raid fraction 0.409  sofra fell 0 %  units/half 23.1  builds/half 5.2  raider hp x4.22
value spread: units CV 0.08  defences CV 0.19  ladles CV 0.17
units:    Grunt 21 v7.4 s4 %  Shieldbearer 87 v6.2 s2 %  Runner 31 v6.3 s78 %  Cook 43 v6.8 s5 %  Healer 19 v6.5 s5 %  Golem 68 v6.6 s1 %  Saboteur 33 v6.0 s1 %  Cloud 28 v7.1 s1 %  Brute 96 v6.7 s0 %  Bomber 22 v5.5 s2 %
defences: Catapult 42 v4.6 s11 %  Barricade 91 v7.3 s0 %  Spire 83 v5.8 s30 %  Griddle 46 v5.0 s20 %  Breeze 95 v6.6 s0 %  Puddle 65 v7.3 s1 %  Tower 83 v6.8 s28 %  Barrel 33 v4.6 s10 %  Tower 61 v5.9 s0 %  Snare 37 v8.1 s0 %
ladles:   Hearty v5.3 s11 %  Spicy v6.8 s0 %  Soothing v5.9 s24 %  Sour fog v5.8 s20 %  Stench v4.0 s44 %
exploit gaps: attacker +3 pts, defender +1 pts

== iteration 23/48  (756,104 sims, 48.5s)
raid fraction 0.444  sofra fell 2 %  units/half 23.3  builds/half 5.5  raider hp x4.49
value spread: units CV 0.10  defences CV 0.19  ladles CV 0.19
units:    Grunt 21 v8.0 s6 %  Shieldbearer 86 v6.6 s4 %  Runner 33 v6.1 s66 %  Cook 43 v7.5 s9 %  Healer 19 v6.2 s6 %  Golem 68 v6.8 s2 %  Saboteur 32 v6.4 s2 %  Cloud 28 v7.5 s3 %  Brute 97 v7.1 s0 %  Bomber 21 v5.8 s4 %
defences: Catapult 40 v4.6 s12 %  Barricade 94 v6.9 s0 %  Spire 84 v5.3 s31 %  Griddle 44 v4.6 s18 %  Breeze 95 v6.5 s0 %  Puddle 67 v6.6 s1 %  Tower 86 v6.5 s25 %  Barrel 31 v4.2 s11 %  Tower 60 v5.9 s0 %  Snare 39 v7.9 s0 %
ladles:   Hearty v4.2 s21 %  Spicy v5.6 s17 %  Soothing v4.8 s28 %  Sour fog v4.9 s22 %  Stench v3.1 s12 %
exploit gaps: attacker +1 pts, defender +0 pts

== iteration 24/48  (788,800 sims, 50.6s)
raid fraction 0.445  sofra fell 1 %  units/half 23.2  builds/half 5.8  raider hp x4.70
value spread: units CV 0.12  defences CV 0.20  ladles CV 0.21
units:    Grunt 22 v8.1 s7 %  Shieldbearer 86 v6.3 s4 %  Runner 35 v5.7 s60 %  Cook 44 v7.7 s10 %  Healer 19 v6.2 s7 %  Golem 68 v6.8 s2 %  Saboteur 32 v6.2 s3 %  Cloud 29 v7.3 s3 %  Brute 98 v7.3 s0 %  Bomber 20 v5.7 s5 %
defences: Catapult 38 v4.8 s16 %  Barricade 95 v6.5 s0 %  Spire 84 v5.0 s31 %  Griddle 42 v4.1 s16 %  Breeze 95 v6.6 s0 %  Puddle 68 v5.9 s1 %  Tower 88 v6.2 s22 %  Barrel 29 v3.7 s13 %  Tower 59 v6.0 s0 %  Snare 41 v7.3 s0 %
ladles:   Hearty v3.6 s13 %  Spicy v4.7 s2 %  Soothing v4.5 s23 %  Sour fog v4.6 s42 %  Stench v2.5 s20 %
exploit gaps: attacker +4 pts, defender +1 pts

== iteration 25/48  (821,496 sims, 53.1s)
raid fraction 0.445  sofra fell 1 %  units/half 22.8  builds/half 5.9  raider hp x4.91
value spread: units CV 0.12  defences CV 0.22  ladles CV 0.21
units:    Grunt 23 v7.6 s8 %  Shieldbearer 85 v5.6 s5 %  Runner 36 v5.5 s53 %  Cook 45 v7.2 s11 %  Healer 19 v5.8 s8 %  Golem 68 v6.3 s2 %  Saboteur 32 v6.0 s4 %  Cloud 30 v6.9 s4 %  Brute 100 v6.4 s0 %  Bomber 20 v5.2 s5 %
defences: Catapult 37 v4.5 s19 %  Barricade 95 v6.1 s0 %  Spire 84 v4.5 s32 %  Griddle 40 v3.8 s14 %  Breeze 95 v6.3 s0 %  Puddle 68 v5.6 s1 %  Tower 89 v5.7 s21 %  Barrel 28 v3.3 s14 %  Tower 59 v6.1 s0 %  Snare 43 v7.5 s0 %
ladles:   Hearty v3.1 s20 %  Spicy v4.2 s9 %  Soothing v4.3 s35 %  Sour fog v3.8 s25 %  Stench v2.3 s11 %
exploit gaps: attacker +0 pts, defender +0 pts

== iteration 26/48  (854,192 sims, 55.6s)
raid fraction 0.444  sofra fell 1 %  units/half 23.3  builds/half 6.0  raider hp x5.00
value spread: units CV 0.11  defences CV 0.20  ladles CV 0.24
units:    Grunt 24 v7.4 s9 %  Shieldbearer 84 v5.7 s4 %  Runner 37 v5.0 s48 %  Cook 46 v7.2 s11 %  Healer 19 v5.8 s8 %  Golem 68 v6.6 s2 %  Saboteur 32 v6.0 s5 %  Cloud 31 v6.7 s5 %  Brute 101 v6.4 s0 %  Bomber 19 v5.7 s7 %
defences: Catapult 36 v5.1 s20 %  Barricade 95 v5.9 s0 %  Spire 83 v4.7 s32 %  Griddle 38 v3.7 s14 %  Breeze 95 v6.1 s0 %  Puddle 68 v4.9 s0 %  Tower 89 v5.5 s21 %  Barrel 27 v3.0 s12 %  Tower 60 v5.7 s0 %  Snare 45 v6.5 s0 %
ladles:   Hearty v2.9 s8 %  Spicy v4.0 s16 %  Soothing v4.0 s36 %  Sour fog v3.4 s16 %  Stench v1.9 s24 %
exploit gaps: attacker +0 pts, defender +2 pts

== iteration 27/48  (886,888 sims, 58.0s)
raid fraction 0.457  sofra fell 2 %  units/half 24.0  builds/half 6.2  raider hp x5.00
value spread: units CV 0.10  defences CV 0.22  ladles CV 0.25
units:    Grunt 25 v7.1 s10 %  Shieldbearer 83 v5.6 s4 %  Runner 37 v5.1 s39 %  Cook 47 v7.2 s11 %  Healer 19 v5.8 s7 %  Golem 69 v6.3 s3 %  Saboteur 32 v6.1 s8 %  Cloud 31 v6.7 s7 %  Brute 102 v6.7 s0 %  Bomber 19 v6.2 s10 %
defences: Catapult 36 v6.0 s21 %  Barricade 95 v5.3 s1 %  Spire 83 v4.7 s33 %  Griddle 36 v3.4 s12 %  Breeze 95 v6.0 s0 %  Puddle 67 v4.3 s0 %  Tower 90 v5.4 s18 %  Barrel 26 v2.9 s15 %  Tower 61 v5.9 s0 %  Snare 46 v5.7 s0 %
ladles:   Hearty v2.4 s27 %  Spicy v3.7 s1 %  Soothing v3.5 s25 %  Sour fog v3.1 s24 %  Stench v1.8 s24 %
exploit gaps: attacker +3 pts, defender +1 pts

== iteration 28/48  (919,584 sims, 60.4s)
raid fraction 0.463  sofra fell 1 %  units/half 24.2  builds/half 6.2  raider hp x5.00
value spread: units CV 0.10  defences CV 0.19  ladles CV 0.25
units:    Grunt 26 v6.9 s11 %  Shieldbearer 82 v5.6 s4 %  Runner 37 v4.9 s32 %  Cook 48 v7.0 s12 %  Healer 19 v5.7 s7 %  Golem 69 v6.1 s4 %  Saboteur 32 v6.1 s10 %  Cloud 31 v6.6 s10 %  Brute 103 v6.4 s0 %  Bomber 19 v6.1 s9 %
defences: Catapult 37 v5.5 s20 %  Barricade 95 v4.4 s1 %  Spire 83 v4.6 s32 %  Griddle 34 v3.3 s11 %  Breeze 95 v5.7 s0 %  Puddle 65 v4.0 s1 %  Tower 90 v5.1 s19 %  Barrel 25 v2.9 s15 %  Tower 62 v5.2 s0 %  Snare 47 v5.0 s0 %
ladles:   Hearty v2.3 s15 %  Spicy v2.8 s12 %  Soothing v3.5 s25 %  Sour fog v3.5 s26 %  Stench v1.7 s22 %
exploit gaps: attacker +0 pts, defender +4 pts

== iteration 29/48  (952,280 sims, 62.8s)
raid fraction 0.471  sofra fell 2 %  units/half 24.4  builds/half 6.3  raider hp x5.00
value spread: units CV 0.09  defences CV 0.21  ladles CV 0.24
units:    Grunt 27 v6.8 s11 %  Shieldbearer 81 v5.8 s5 %  Runner 37 v5.0 s25 %  Cook 49 v6.8 s13 %  Healer 19 v5.9 s8 %  Golem 69 v6.1 s4 %  Saboteur 32 v6.3 s12 %  Cloud 31 v6.5 s12 %  Brute 104 v6.4 s0 %  Bomber 19 v6.8 s10 %
defences: Catapult 38 v6.3 s23 %  Barricade 94 v4.6 s3 %  Spire 84 v4.1 s28 %  Griddle 32 v3.4 s11 %  Breeze 95 v5.3 s0 %  Puddle 63 v3.3 s2 %  Tower 91 v4.6 s15 %  Barrel 25 v3.1 s18 %  Tower 63 v4.6 s0 %  Snare 47 v4.2 s0 %
ladles:   Hearty v2.1 s15 %  Spicy v2.7 s15 %  Soothing v3.0 s13 %  Sour fog v3.0 s23 %  Stench v1.4 s33 %
exploit gaps: attacker +8 pts, defender +3 pts

== iteration 30/48  (984,976 sims, 65.3s)
raid fraction 0.476  sofra fell 3 %  units/half 24.1  builds/half 6.3  raider hp x5.00
value spread: units CV 0.08  defences CV 0.16  ladles CV 0.19
units:    Grunt 27 v7.0 s11 %  Shieldbearer 80 v6.2 s6 %  Runner 36 v5.2 s20 %  Cook 50 v6.9 s16 %  Healer 19 v6.0 s7 %  Golem 69 v6.3 s4 %  Saboteur 32 v6.3 s13 %  Cloud 31 v6.7 s13 %  Brute 104 v6.8 s0 %  Bomber 19 v6.3 s10 %
defences: Catapult 40 v5.5 s32 %  Barricade 95 v4.5 s5 %  Spire 85 v3.6 s23 %  Griddle 31 v3.5 s7 %  Breeze 95 v5.1 s0 %  Puddle 60 v3.5 s2 %  Tower 92 v3.9 s10 %  Barrel 25 v3.4 s20 %  Tower 64 v4.1 s0 %  Snare 47 v3.9 s0 %
ladles:   Hearty v2.2 s10 %  Spicy v2.2 s18 %  Soothing v2.8 s29 %  Sour fog v2.5 s14 %  Stench v1.5 s29 %
exploit gaps: attacker +0 pts, defender +1 pts

== iteration 31/48  (1,017,672 sims, 67.9s)
raid fraction 0.477  sofra fell 3 %  units/half 24.2  builds/half 6.2  raider hp x5.00
value spread: units CV 0.09  defences CV 0.21  ladles CV 0.23
units:    Grunt 27 v7.0 s10 %  Shieldbearer 80 v6.0 s6 %  Runner 35 v4.9 s16 %  Cook 51 v6.6 s16 %  Healer 19 v6.1 s8 %  Golem 69 v6.2 s6 %  Saboteur 32 v6.1 s14 %  Cloud 31 v6.6 s15 %  Brute 105 v6.2 s0 %  Bomber 19 v5.8 s9 %
defences: Catapult 42 v6.0 s37 %  Barricade 95 v4.6 s7 %  Spire 85 v3.4 s19 %  Griddle 31 v4.3 s6 %  Breeze 95 v4.9 s0 %  Puddle 59 v3.1 s2 %  Tower 92 v3.7 s9 %  Barrel 25 v3.5 s18 %  Tower 65 v3.9 s0 %  Snare 47 v3.4 s1 %
ladles:   Hearty v2.0 s14 %  Spicy v2.4 s41 %  Soothing v2.7 s22 %  Sour fog v2.3 s10 %  Stench v1.2 s12 %
exploit gaps: attacker +1 pts, defender +1 pts

== iteration 32/48  (1,050,368 sims, 70.6s)
raid fraction 0.471  sofra fell 3 %  units/half 24.2  builds/half 6.0  raider hp x5.00
value spread: units CV 0.10  defences CV 0.21  ladles CV 0.25
units:    Grunt 28 v6.7 s10 %  Shieldbearer 80 v5.9 s6 %  Runner 34 v4.9 s13 %  Cook 52 v6.7 s15 %  Healer 19 v6.2 s7 %  Golem 69 v6.1 s8 %  Saboteur 32 v6.0 s15 %  Cloud 31 v6.8 s16 %  Brute 105 v6.4 s0 %  Bomber 19 v5.1 s9 %
defences: Catapult 44 v5.5 s40 %  Barricade 95 v4.0 s10 %  Spire 84 v3.6 s17 %  Griddle 32 v3.5 s4 %  Breeze 95 v5.1 s0 %  Puddle 57 v3.0 s2 %  Tower 92 v3.7 s9 %  Barrel 25 v3.0 s16 %  Tower 65 v3.6 s1 %  Snare 46 v3.4 s1 %
ladles:   Hearty v2.1 s23 %  Spicy v1.8 s14 %  Soothing v2.8 s15 %  Sour fog v2.6 s40 %  Stench v1.3 s8 %
exploit gaps: attacker +0 pts, defender +1 pts

== iteration 33/48  (1,083,064 sims, 73.2s)
raid fraction 0.475  sofra fell 2 %  units/half 24.6  builds/half 6.0  raider hp x5.00
value spread: units CV 0.09  defences CV 0.22  ladles CV 0.27
units:    Grunt 28 v6.8 s9 %  Shieldbearer 79 v6.0 s5 %  Runner 33 v4.9 s12 %  Cook 53 v6.6 s15 %  Healer 19 v6.6 s8 %  Golem 69 v6.0 s10 %  Saboteur 32 v6.0 s16 %  Cloud 31 v6.8 s17 %  Brute 106 v6.1 s0 %  Bomber 18 v5.5 s8 %
defences: Catapult 46 v4.9 s47 %  Barricade 95 v4.0 s12 %  Spire 84 v2.9 s14 %  Griddle 32 v3.3 s3 %  Breeze 95 v4.5 s0 %  Puddle 55 v2.4 s2 %  Tower 92 v3.0 s7 %  Barrel 25 v2.8 s13 %  Tower 65 v3.0 s1 %  Snare 46 v2.9 s1 %
ladles:   Hearty v2.1 s0 %  Spicy v1.8 s32 %  Soothing v2.6 s40 %  Sour fog v2.3 s6 %  Stench v1.0 s21 %
exploit gaps: attacker +7 pts, defender +1 pts

== iteration 34/48  (1,115,760 sims, 75.7s)
raid fraction 0.454  sofra fell 1 %  units/half 23.7  builds/half 5.8  raider hp x5.00
value spread: units CV 0.09  defences CV 0.26  ladles CV 0.27
units:    Grunt 28 v6.8 s9 %  Shieldbearer 79 v6.2 s6 %  Runner 32 v5.1 s11 %  Cook 54 v6.7 s15 %  Healer 19 v7.0 s7 %  Golem 69 v6.2 s11 %  Saboteur 32 v6.1 s15 %  Cloud 32 v6.8 s18 %  Brute 106 v6.5 s0 %  Bomber 18 v5.8 s8 %
defences: Catapult 48 v4.8 s53 %  Barricade 95 v3.7 s15 %  Spire 83 v2.6 s11 %  Griddle 32 v3.4 s2 %  Breeze 95 v4.3 s0 %  Puddle 53 v2.0 s2 %  Tower 92 v2.7 s5 %  Barrel 25 v2.6 s10 %  Tower 65 v2.8 s1 %  Snare 46 v2.7 s1 %
ladles:   Hearty v2.0 s19 %  Spicy v1.9 s21 %  Soothing v2.4 s15 %  Sour fog v2.3 s35 %  Stench v0.9 s10 %
exploit gaps: attacker +1 pts, defender +0 pts

== iteration 35/48  (1,148,456 sims, 78.2s)
raid fraction 0.458  sofra fell 2 %  units/half 23.7  builds/half 5.4  raider hp x5.00
value spread: units CV 0.08  defences CV 0.30  ladles CV 0.28
units:    Grunt 28 v6.8 s9 %  Shieldbearer 79 v6.2 s6 %  Runner 31 v5.1 s10 %  Cook 54 v6.7 s16 %  Healer 19 v6.7 s6 %  Golem 69 v6.0 s11 %  Saboteur 32 v6.1 s15 %  Cloud 32 v6.5 s19 %  Brute 106 v6.6 s0 %  Bomber 18 v5.5 s7 %
defences: Catapult 50 v4.7 s53 %  Barricade 95 v3.3 s23 %  Spire 83 v2.5 s8 %  Griddle 33 v3.3 s1 %  Breeze 95 v3.7 s0 %  Puddle 51 v2.0 s2 %  Tower 92 v2.4 s4 %  Barrel 25 v2.3 s7 %  Tower 65 v2.1 s1 %  Snare 46 v2.0 s1 %
ladles:   Hearty v2.1 s17 %  Spicy v1.7 s32 %  Soothing v2.4 s17 %  Sour fog v2.3 s26 %  Stench v0.9 s7 %
exploit gaps: attacker +2 pts, defender +1 pts

== iteration 36/48  (1,181,152 sims, 80.6s)
raid fraction 0.465  sofra fell 2 %  units/half 22.6  builds/half 5.1  raider hp x5.00
value spread: units CV 0.08  defences CV 0.31  ladles CV 0.29
units:    Grunt 28 v6.8 s8 %  Shieldbearer 79 v5.8 s7 %  Runner 30 v5.3 s11 %  Cook 54 v6.4 s20 %  Healer 19 v6.4 s6 %  Golem 69 v6.0 s10 %  Saboteur 32 v5.9 s14 %  Cloud 32 v6.4 s17 %  Brute 106 v6.3 s0 %  Bomber 18 v5.2 s6 %
defences: Catapult 53 v4.2 s58 %  Barricade 95 v3.0 s25 %  Spire 83 v2.2 s5 %  Griddle 34 v3.7 s1 %  Breeze 95 v3.2 s0 %  Puddle 50 v1.9 s2 %  Tower 92 v2.1 s2 %  Barrel 25 v2.5 s5 %  Tower 64 v1.7 s1 %  Snare 45 v1.7 s1 %
ladles:   Hearty v1.8 s20 %  Spicy v1.7 s27 %  Soothing v2.1 s38 %  Sour fog v1.9 s4 %  Stench v0.7 s11 %
exploit gaps: attacker +2 pts, defender +1 pts

== iteration 37/48  (1,213,848 sims, 83.0s)
raid fraction 0.474  sofra fell 2 %  units/half 22.3  builds/half 4.9  raider hp x5.00
value spread: units CV 0.11  defences CV 0.30  ladles CV 0.34
units:    Grunt 28 v6.8 s7 %  Shieldbearer 78 v5.6 s6 %  Runner 29 v5.2 s11 %  Cook 54 v6.1 s21 %  Healer 19 v6.2 s7 %  Golem 69 v5.8 s13 %  Saboteur 32 v5.8 s14 %  Cloud 32 v6.4 s16 %  Brute 106 v6.0 s0 %  Bomber 18 v4.5 s5 %
defences: Catapult 56 v4.0 s63 %  Barricade 95 v2.9 s26 %  Spire 82 v2.1 s3 %  Griddle 36 v3.3 s0 %  Breeze 95 v3.1 s0 %  Puddle 49 v1.7 s1 %  Tower 91 v1.9 s1 %  Barrel 25 v2.0 s2 %  Tower 61 v2.1 s2 %  Snare 43 v1.9 s1 %
ladles:   Hearty v1.7 s13 %  Spicy v1.4 s39 %  Soothing v1.9 s20 %  Sour fog v1.7 s22 %  Stench v0.5 s7 %
exploit gaps: attacker +0 pts, defender +2 pts

== iteration 38/48  (1,246,544 sims, 85.3s)
raid fraction 0.477  sofra fell 2 %  units/half 22.2  builds/half 4.8  raider hp x5.00
value spread: units CV 0.10  defences CV 0.31  ladles CV 0.29
units:    Grunt 29 v6.5 s6 %  Shieldbearer 77 v5.5 s6 %  Runner 29 v5.0 s12 %  Cook 54 v6.3 s23 %  Healer 19 v6.3 s6 %  Golem 69 v5.8 s13 %  Saboteur 32 v5.7 s12 %  Cloud 32 v6.5 s16 %  Brute 106 v5.9 s0 %  Bomber 17 v4.8 s4 %
defences: Catapult 59 v4.0 s60 %  Barricade 95 v2.8 s32 %  Spire 82 v2.0 s2 %  Griddle 38 v3.3 s0 %  Breeze 95 v3.1 s0 %  Puddle 48 v1.7 s2 %  Tower 90 v1.9 s0 %  Barrel 25 v1.7 s0 %  Tower 61 v1.9 s1 %  Snare 42 v1.8 s1 %
ladles:   Hearty v1.9 s30 %  Spicy v1.3 s43 %  Soothing v2.0 s12 %  Sour fog v1.7 s10 %  Stench v0.8 s6 %
exploit gaps: attacker +1 pts, defender +0 pts

== iteration 39/48  (1,279,240 sims, 87.5s)
raid fraction 0.473  sofra fell 2 %  units/half 22.5  builds/half 4.8  raider hp x5.00
value spread: units CV 0.10  defences CV 0.32  ladles CV 0.32
units:    Grunt 29 v7.0 s6 %  Shieldbearer 76 v6.0 s6 %  Runner 28 v4.9 s12 %  Cook 55 v6.9 s25 %  Healer 19 v6.5 s6 %  Golem 69 v6.1 s12 %  Saboteur 32 v6.0 s13 %  Cloud 32 v6.7 s17 %  Brute 106 v6.4 s0 %  Bomber 17 v5.6 s4 %
defences: Catapult 62 v3.9 s51 %  Barricade 95 v2.3 s42 %  Spire 82 v2.0 s2 %  Griddle 40 v3.3 s0 %  Breeze 95 v2.6 s0 %  Puddle 47 v1.2 s1 %  Tower 90 v1.9 s0 %  Barrel 25 v2.2 s1 %  Tower 61 v1.9 s1 %  Snare 42 v1.9 s1 %
ladles:   Hearty v1.6 s21 %  Spicy v1.2 s48 %  Soothing v1.8 s18 %  Sour fog v1.8 s12 %  Stench v0.6 s1 %
exploit gaps: attacker +1 pts, defender +1 pts

== iteration 40/48  (1,311,936 sims, 89.9s)
raid fraction 0.498  sofra fell 2 %  units/half 22.9  builds/half 5.0  raider hp x5.00
value spread: units CV 0.09  defences CV 0.22  ladles CV 0.34
units:    Grunt 29 v6.7 s6 %  Shieldbearer 76 v5.9 s6 %  Runner 27 v5.0 s12 %  Cook 56 v6.2 s26 %  Healer 19 v5.9 s6 %  Golem 69 v5.9 s12 %  Saboteur 32 v6.0 s12 %  Cloud 32 v6.4 s16 %  Brute 106 v6.1 s0 %  Bomber 17 v4.9 s4 %
defences: Catapult 65 v3.7 s51 %  Barricade 95 v2.0 s40 %  Spire 81 v2.2 s4 %  Griddle 42 v3.3 s0 %  Breeze 95 v2.8 s0 %  Puddle 45 v2.0 s1 %  Tower 89 v2.1 s1 %  Barrel 25 v2.2 s1 %  Tower 60 v2.0 s1 %  Snare 41 v2.6 s1 %
ladles:   Hearty v1.8 s9 %  Spicy v1.2 s51 %  Soothing v1.9 s11 %  Sour fog v1.8 s24 %  Stench v0.6 s5 %
exploit gaps: attacker +1 pts, defender +1 pts

== iteration 41/48  (1,344,632 sims, 92.2s)
raid fraction 0.486  sofra fell 3 %  units/half 22.5  builds/half 5.0  raider hp x5.00
value spread: units CV 0.07  defences CV 0.23  ladles CV 0.30
units:    Grunt 29 v6.7 s6 %  Shieldbearer 76 v5.7 s5 %  Runner 26 v5.5 s12 %  Cook 57 v6.0 s27 %  Healer 19 v6.0 s5 %  Golem 69 v5.8 s11 %  Saboteur 32 v5.9 s13 %  Cloud 32 v6.3 s16 %  Brute 106 v5.8 s0 %  Bomber 17 v5.2 s4 %
defences: Catapult 68 v3.5 s53 %  Barricade 95 v2.1 s39 %  Spire 81 v2.1 s3 %  Griddle 44 v2.3 s0 %  Breeze 95 v2.7 s0 %  Puddle 44 v2.0 s1 %  Tower 88 v2.0 s0 %  Barrel 25 v1.7 s1 %  Tower 59 v1.9 s1 %  Snare 42 v1.8 s1 %
ladles:   Hearty v1.8 s19 %  Spicy v1.4 s63 %  Soothing v1.8 s0 %  Sour fog v1.7 s10 %  Stench v0.6 s8 %
exploit gaps: attacker +0 pts, defender +1 pts

== iteration 42/48  (1,377,328 sims, 94.5s)
raid fraction 0.492  sofra fell 2 %  units/half 22.7  builds/half 5.0  raider hp x5.00
value spread: units CV 0.06  defences CV 0.22  ladles CV 0.32
units:    Grunt 30 v6.3 s6 %  Shieldbearer 76 v5.6 s5 %  Runner 26 v5.1 s12 %  Cook 58 v5.9 s29 %  Healer 19 v5.8 s6 %  Golem 69 v5.8 s11 %  Saboteur 32 v5.8 s13 %  Cloud 32 v6.3 s14 %  Brute 106 v6.2 s0 %  Bomber 17 v5.3 s5 %
defences: Catapult 71 v3.7 s53 %  Barricade 95 v1.9 s39 %  Spire 81 v2.4 s3 %  Griddle 45 v3.2 s0 %  Breeze 95 v2.7 s0 %  Puddle 44 v1.9 s1 %  Tower 88 v2.3 s0 %  Barrel 25 v2.2 s1 %  Tower 58 v2.4 s1 %  Snare 41 v2.0 s1 %
ladles:   Hearty v1.8 s5 %  Spicy v1.3 s35 %  Soothing v2.0 s56 %  Sour fog v1.7 s4 %  Stench v0.6 s0 %
exploit gaps: attacker +0 pts, defender +2 pts

== iteration 43/48  (1,410,024 sims, 96.8s)
raid fraction 0.492  sofra fell 3 %  units/half 22.1  builds/half 4.6  raider hp x5.00
value spread: units CV 0.08  defences CV 0.33  ladles CV 0.39
units:    Grunt 30 v6.5 s7 %  Shieldbearer 76 v5.4 s5 %  Runner 26 v4.9 s11 %  Cook 59 v5.7 s28 %  Healer 19 v5.7 s5 %  Golem 69 v5.6 s11 %  Saboteur 32 v6.0 s14 %  Cloud 32 v6.3 s15 %  Brute 107 v5.8 s0 %  Bomber 17 v5.8 s5 %
defences: Catapult 74 v4.1 s39 %  Barricade 95 v1.3 s53 %  Spire 81 v2.8 s4 %  Griddle 47 v3.2 s0 %  Breeze 95 v2.8 s0 %  Puddle 43 v1.7 s1 %  Tower 88 v2.8 s0 %  Barrel 25 v1.9 s1 %  Tower 58 v3.5 s1 %  Snare 40 v1.8 s1 %
ladles:   Hearty v1.6 s16 %  Spicy v0.8 s62 %  Soothing v1.7 s14 %  Sour fog v1.4 s3 %  Stench v0.4 s5 %
exploit gaps: attacker +0 pts, defender +0 pts

== iteration 44/48  (1,442,720 sims, 99.1s)
raid fraction 0.491  sofra fell 2 %  units/half 22.6  builds/half 5.0  raider hp x5.00
value spread: units CV 0.09  defences CV 0.53  ladles CV 0.45
units:    Grunt 30 v6.4 s7 %  Shieldbearer 75 v5.7 s5 %  Runner 25 v5.2 s11 %  Cook 60 v5.4 s29 %  Healer 19 v5.5 s5 %  Golem 69 v5.9 s11 %  Saboteur 32 v6.2 s12 %  Cloud 32 v6.4 s14 %  Brute 107 v5.9 s0 %  Bomber 17 v7.1 s7 %
defences: Catapult 77 v5.5 s28 %  Barricade 91 v0.8 s60 %  Spire 81 v4.5 s8 %  Griddle 48 v9.3 s1 %  Breeze 95 v3.4 s0 %  Puddle 41 v2.1 s1 %  Tower 88 v3.9 s0 %  Barrel 25 v5.4 s0 %  Tower 60 v5.1 s2 %  Snare 38 v2.4 s1 %
ladles:   Hearty v1.4 s20 %  Spicy v0.6 s53 %  Soothing v1.6 s22 %  Sour fog v1.5 s5 %  Stench v0.4 s1 %
exploit gaps: attacker +2 pts, defender +2 pts

== iteration 45/48  (1,475,416 sims, 101.3s)
raid fraction 0.493  sofra fell 3 %  units/half 23.1  builds/half 6.1  raider hp x5.00
value spread: units CV 0.11  defences CV 0.49  ladles CV 0.37
units:    Grunt 30 v6.6 s10 %  Shieldbearer 75 v5.4 s4 %  Runner 25 v5.4 s9 %  Cook 60 v5.5 s26 %  Healer 19 v5.8 s4 %  Golem 69 v5.7 s10 %  Saboteur 32 v6.2 s13 %  Cloud 32 v6.2 s14 %  Brute 107 v5.7 s0 %  Bomber 17 v7.6 s10 %
defences: Catapult 81 v5.4 s25 %  Barricade 88 v0.6 s50 %  Spire 82 v4.9 s12 %  Griddle 50 v9.0 s1 %  Breeze 93 v4.1 s0 %  Puddle 39 v2.4 s5 %  Tower 87 v4.4 s0 %  Barrel 26 v5.5 s2 %  Tower 62 v5.7 s3 %  Snare 37 v2.5 s2 %
ladles:   Hearty v1.5 s23 %  Spicy v0.8 s40 %  Soothing v2.0 s20 %  Sour fog v1.9 s6 %  Stench v0.8 s12 %
exploit gaps: attacker +10 pts, defender +1 pts

== iteration 46/48  (1,508,112 sims, 103.5s)
raid fraction 0.494  sofra fell 4 %  units/half 23.6  builds/half 6.0  raider hp x5.00
value spread: units CV 0.11  defences CV 0.46  ladles CV 0.45
units:    Grunt 31 v6.5 s9 %  Shieldbearer 74 v5.4 s3 %  Runner 25 v5.8 s9 %  Cook 60 v5.4 s25 %  Healer 19 v5.7 s4 %  Golem 69 v5.5 s9 %  Saboteur 32 v6.3 s12 %  Cloud 32 v6.2 s13 %  Brute 107 v5.5 s0 %  Bomber 18 v7.6 s14 %
defences: Catapult 83 v5.7 s25 %  Barricade 85 v0.8 s49 %  Spire 83 v5.3 s13 %  Griddle 52 v8.8 s1 %  Breeze 92 v4.2 s0 %  Puddle 38 v2.6 s4 %  Tower 86 v4.8 s0 %  Barrel 27 v5.3 s1 %  Tower 64 v5.8 s4 %  Snare 36 v2.6 s2 %
ladles:   Hearty v1.1 s48 %  Spicy v0.6 s12 %  Soothing v1.6 s13 %  Sour fog v1.6 s17 %  Stench v0.5 s10 %
exploit gaps: attacker +10 pts, defender +4 pts

== iteration 47/48  (1,540,808 sims, 105.8s)
raid fraction 0.496  sofra fell 5 %  units/half 23.7  builds/half 6.1  raider hp x5.00
value spread: units CV 0.08  defences CV 0.43  ladles CV 0.32
units:    Grunt 31 v6.5 s9 %  Shieldbearer 73 v5.6 s3 %  Runner 25 v6.0 s8 %  Cook 60 v5.3 s22 %  Healer 19 v5.8 s4 %  Golem 69 v5.7 s8 %  Saboteur 32 v6.4 s13 %  Cloud 32 v6.3 s14 %  Brute 106 v5.7 s1 %  Bomber 19 v7.1 s17 %
defences: Catapult 85 v5.6 s25 %  Barricade 82 v1.0 s48 %  Spire 83 v5.4 s13 %  Griddle 54 v8.3 s1 %  Breeze 90 v4.6 s0 %  Puddle 37 v2.3 s5 %  Tower 86 v5.0 s0 %  Barrel 27 v5.3 s1 %  Tower 65 v6.1 s4 %  Snare 35 v2.7 s3 %
ladles:   Hearty v1.3 s20 %  Spicy v0.7 s34 %  Soothing v1.7 s27 %  Sour fog v1.5 s4 %  Stench v0.9 s14 %
exploit gaps: attacker +4 pts, defender +0 pts

== iteration 48/48  (1,573,504 sims, 108.1s)
raid fraction 0.542  sofra fell 8 %  units/half 25.4  builds/half 6.3  raider hp x5.00
value spread: units CV 0.17  defences CV 0.48  ladles CV 0.55
units:    Grunt 31 v6.4 s8 %  Shieldbearer 72 v5.4 s3 %  Runner 25 v6.8 s7 %  Cook 59 v5.6 s18 %  Healer 19 v5.6 s4 %  Golem 69 v5.6 s7 %  Saboteur 32 v6.1 s11 %  Cloud 32 v6.1 s12 %  Brute 106 v5.3 s1 %  Bomber 19 v9.0 s30 %
defences: Catapult 87 v6.5 s27 %  Barricade 79 v1.0 s47 %  Spire 83 v6.4 s14 %  Griddle 56 v9.4 s1 %  Breeze 89 v5.5 s0 %  Puddle 36 v2.3 s4 %  Tower 86 v5.9 s0 %  Barrel 27 v5.2 s1 %  Tower 66 v7.7 s3 %  Snare 34 v2.4 s2 %
ladles:   Hearty v1.1 s44 %  Spicy v0.1 s25 %  Soothing v1.4 s18 %  Sour fog v1.3 s3 %  Stench v0.5 s11 %
calibration 1: raider hp x5.00 -> raid fraction 0.525, fell 9 %
calibration 2: raider hp x5.00 -> raid fraction 0.550, fell 11 %
calibration 3: raider hp x5.00 -> raid fraction 0.558, fell 13 %
```
