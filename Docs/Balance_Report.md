# Corba balance report

Generated 2026-10-29 15:06 by `Tools/CorbaSim` (1,526,704 simulated halves in 93s on 16 threads, 40 real exported maps).

**Method.** Attacker and defender strategy populations co-evolve against each other, so the numbers describe what *good* play does, not random play. Each value is a paired A/B test on the same matchup and seed: give one side a little extra of one thing and measure the change in raid score. Costs are then nudged until every option is worth about the same per ladle of soup.

- Mean raid score: **56 %** of the Sofra (target 55%); Sofra emptied in 8 % of halves; Sofra hunger 30
- Per half: 16.4 raiders sent, 5.7 defences built; ladle toss costs 7
- Value spread (coefficient of variation, lower is more even): units 0.20, defences 0.15, ladles 0.32

## Attack units

| Unit | Soup | Cost | Hunger per 100 ladles | Share of winning attackers' spend |
|---|---|---|---|---|
| Spoon Grunt | Mercimek | 17 | 6.6 | 17 % |
| Cauldron Shieldbearer | Mercimek | 49 | 7.0 | 27 % |
| Pepper Runner | Ezogelin | 20 | 6.0 | 8 % |
| Ember Cook | Ezogelin | 56 | 4.6 | 13 % |
| Mint Healer | Yayla | 32 | 5.0 | 1 % |
| Ayran Golem | Yayla | 62 | 6.6 | 5 % |
| Ferment Saboteur | Tarhana | 28 | 5.3 | 13 % |
| Sour Cloud | Tarhana | 24 | 6.6 | 13 % |
| Garlic Brute | Iskembe | 59 | 5.2 | 1 % |
| Vinegar Bomber | Iskembe | 27 | 3.2 | 1 % |

## Defences

| Defence | Soup | Kind | Cost | Hunger saved per 100 ladles | Share of winning defenders' spend |
|---|---|---|---|---|---|
| Ladle Catapult | Mercimek | Tower | 55 | 7.0 | 60 % |
| Lentil Barricade | Mercimek | Blocker | 95 | 7.2 | 0 % |
| Chili Spire | Ezogelin | Tower | 95 | 6.9 | 0 % |
| Hot Griddle | Ezogelin | Trap | 43 | 8.3 | 0 % |
| Mint Breeze | Yayla | Tower | 78 | 5.3 | 28 % |
| Yoghurt Puddle | Yayla | Trap | 31 | 6.0 | 1 % |
| Sour Ferment Tower | Tarhana | Tower | 88 | 6.9 | 0 % |
| Ferment Barrel | Tarhana | Trap | 32 | 8.1 | 12 % |
| Garlic Tower | Iskembe | Tower | 48 | 9.1 | 0 % |
| Vinegar Snare | Iskembe | Trap | 38 | 6.5 | 0 % |

## Ladle tosses

| Soup | Effect | Magnitude | Duration | Hunger per 100 ladles thrown | Share of tosses |
|---|---|---|---|---|---|
| Mercimek | Hearty | 0.75 | 6s | 3.5 | 74 % |
| Ezogelin | Spicy | 1.09 | 5s | 3.8 | 5 % |
| Yayla | Soothing | 25 | 0s | 7.5 | 11 % |
| Tarhana | Sour fog | 0.47 | 7s | 4.4 | 10 % |
| Iskembe | Stench | 1.17 | 9s | 3.7 | 0 % |

## Tuning log

```
Corba Monte Carlo tuner: 40 maps, 1000 samples/iteration, 16 threads

== iteration 1/48  (34,192 sims, 2.5s)
raid fraction 0.088  sofra fell 0 %  units/half 16.3  builds/half 5.0  sofra 120
value spread: units CV 0.48  defences CV 0.44  ladles CV 0.71
units:    Grunt 15 v5.8 s9 %  Shieldbearer 45 v8.0 s20 %  Runner 20 v0.5 s7 %  Cook 40 v4.5 s9 %  Healer 35 v2.2 s9 %  Golem 50 v6.2 s6 %  Saboteur 30 v6.1 s8 %  Cloud 40 v2.2 s9 %  Brute 60 v6.6 s14 %  Bomber 35 v4.9 s8 %
defences: Catapult 70 v3.9 s0 %  Barricade 50 v6.4 s0 %  Spire 80 v4.3 s30 %  Griddle 45 v3.4 s27 %  Breeze 70 v2.9 s0 %  Puddle 40 v3.6 s2 %  Tower 85 v4.5 s26 %  Barrel 55 v1.8 s1 %  Tower 90 v1.3 s1 %  Snare 50 v1.6 s14 %
ladles:   Hearty v2.9 s15 %  Spicy v0.1 s24 %  Soothing v8.4 s16 %  Sour fog v3.9 s16 %  Stench v3.4 s29 %

== iteration 2/48  (64,288 sims, 4.1s)
raid fraction 0.180  sofra fell 0 %  units/half 15.0  builds/half 4.3  sofra 106
value spread: units CV 0.57  defences CV 0.38  ladles CV 1.06
units:    Grunt 16 v5.3 s8 %  Shieldbearer 53 v6.1 s14 %  Runner 17 v0.7 s6 %  Cook 37 v4.5 s9 %  Healer 30 v2.5 s9 %  Golem 54 v4.8 s5 %  Saboteur 32 v11.2 s11 %  Cloud 34 v2.4 s5 %  Brute 67 v6.7 s25 %  Bomber 34 v4.1 s8 %
defences: Catapult 74 v5.7 s3 %  Barricade 59 v7.7 s0 %  Spire 94 v5.9 s7 %  Griddle 46 v6.1 s72 %  Breeze 63 v4.9 s0 %  Puddle 40 v10.0 s0 %  Tower 95 v6.0 s1 %  Barrel 47 v3.5 s16 %  Tower 76 v2.2 s1 %  Snare 42 v3.7 s0 %
ladles:   Hearty v1.6 s4 %  Spicy v-0.7 s14 %  Soothing v10.3 s17 %  Sour fog v1.1 s3 %  Stench v7.2 s62 %

== iteration 3/48  (94,384 sims, 5.8s)
raid fraction 0.229  sofra fell 0 %  units/half 14.4  builds/half 4.1  sofra 93
value spread: units CV 0.31  defences CV 0.27  ladles CV 0.77
units:    Grunt 17 v6.2 s7 %  Shieldbearer 59 v6.6 s14 %  Runner 15 v2.3 s5 %  Cook 36 v6.2 s9 %  Healer 26 v3.7 s9 %  Golem 55 v6.3 s5 %  Saboteur 37 v7.6 s13 %  Cloud 30 v3.6 s5 %  Brute 77 v7.5 s28 %  Bomber 32 v5.0 s6 %
defences: Catapult 73 v6.3 s17 %  Barricade 67 v8.9 s3 %  Spire 95 v7.0 s2 %  Griddle 53 v5.8 s54 %  Breeze 59 v7.1 s2 %  Puddle 46 v8.3 s0 %  Tower 95 v7.1 s2 %  Barrel 41 v4.1 s18 %  Tower 66 v3.1 s3 %  Snare 37 v5.2 s0 %
ladles:   Hearty v2.2 s6 %  Spicy v1.0 s21 %  Soothing v10.2 s16 %  Sour fog v2.7 s5 %  Stench v5.4 s52 %

== iteration 4/48  (124,480 sims, 7.4s)
raid fraction 0.215  sofra fell 0 %  units/half 14.7  builds/half 4.5  sofra 82
value spread: units CV 0.18  defences CV 0.22  ladles CV 0.78
units:    Grunt 17 v6.3 s6 %  Shieldbearer 60 v5.7 s13 %  Runner 13 v3.5 s5 %  Cook 36 v6.6 s9 %  Healer 23 v4.7 s8 %  Golem 55 v6.1 s5 %  Saboteur 40 v6.2 s13 %  Cloud 27 v4.6 s5 %  Brute 86 v6.5 s23 %  Bomber 29 v5.0 s13 %
defences: Catapult 72 v5.0 s29 %  Barricade 75 v7.0 s10 %  Spire 95 v5.8 s1 %  Griddle 56 v3.6 s37 %  Breeze 60 v6.0 s5 %  Puddle 50 v4.3 s0 %  Tower 95 v5.8 s1 %  Barrel 36 v4.0 s16 %  Tower 58 v3.8 s1 %  Snare 34 v4.2 s0 %
ladles:   Hearty v2.9 s8 %  Spicy v1.8 s23 %  Soothing v13.2 s17 %  Sour fog v3.0 s5 %  Stench v5.6 s46 %

== iteration 5/48  (154,576 sims, 9.2s)
raid fraction 0.251  sofra fell 0 %  units/half 18.6  builds/half 4.5  sofra 72
value spread: units CV 0.14  defences CV 0.09  ladles CV 0.59
units:    Grunt 17 v5.9 s7 %  Shieldbearer 59 v5.6 s13 %  Runner 12 v4.7 s7 %  Cook 38 v5.6 s12 %  Healer 21 v5.2 s9 %  Golem 56 v5.5 s8 %  Saboteur 41 v7.2 s11 %  Cloud 25 v4.1 s7 %  Brute 90 v5.4 s10 %  Bomber 27 v5.3 s15 %
defences: Catapult 77 v5.4 s27 %  Barricade 85 v6.3 s12 %  Spire 95 v6.0 s2 %  Griddle 55 v5.1 s37 %  Breeze 65 v6.6 s6 %  Puddle 49 v4.7 s0 %  Tower 95 v5.9 s1 %  Barrel 34 v5.5 s16 %  Tower 54 v5.6 s0 %  Snare 33 v5.6 s0 %
ladles:   Hearty v4.9 s13 %  Spicy v4.0 s18 %  Soothing v15.0 s18 %  Sour fog v3.6 s6 %  Stench v8.4 s45 %

== iteration 6/48  (184,672 sims, 11.0s)
raid fraction 0.292  sofra fell 0 %  units/half 19.1  builds/half 4.5  sofra 63
value spread: units CV 0.15  defences CV 0.11  ladles CV 0.51
units:    Grunt 17 v5.8 s7 %  Shieldbearer 60 v5.7 s14 %  Runner 11 v4.3 s7 %  Cook 38 v6.0 s13 %  Healer 21 v5.0 s7 %  Golem 56 v5.9 s7 %  Saboteur 45 v7.4 s11 %  Cloud 23 v4.4 s8 %  Brute 90 v5.4 s11 %  Bomber 27 v5.7 s14 %
defences: Catapult 78 v4.7 s28 %  Barricade 88 v6.5 s0 %  Spire 95 v6.4 s2 %  Griddle 57 v5.2 s47 %  Breeze 69 v6.6 s4 %  Puddle 46 v5.4 s1 %  Tower 95 v6.4 s0 %  Barrel 34 v5.3 s17 %  Tower 54 v5.5 s1 %  Snare 33 v6.0 s0 %
ladles:   Hearty v5.1 s12 %  Spicy v6.6 s13 %  Soothing v15.5 s19 %  Sour fog v3.9 s7 %  Stench v10.6 s49 %

== iteration 7/48  (214,768 sims, 12.9s)
raid fraction 0.363  sofra fell 1 %  units/half 20.6  builds/half 4.0  sofra 55
value spread: units CV 0.07  defences CV 0.10  ladles CV 0.51
units:    Grunt 17 v6.3 s7 %  Shieldbearer 60 v5.8 s14 %  Runner 10 v6.0 s10 %  Cook 39 v6.0 s13 %  Healer 20 v6.1 s7 %  Golem 57 v5.8 s7 %  Saboteur 49 v6.5 s11 %  Cloud 21 v5.0 s11 %  Brute 88 v6.2 s10 %  Bomber 27 v5.8 s11 %
defences: Catapult 76 v4.9 s28 %  Barricade 92 v7.0 s14 %  Spire 95 v6.2 s3 %  Griddle 60 v5.2 s34 %  Breeze 72 v5.7 s4 %  Puddle 45 v5.8 s0 %  Tower 95 v6.2 s0 %  Barrel 33 v6.2 s16 %  Tower 53 v5.2 s1 %  Snare 33 v6.3 s1 %
ladles:   Hearty v5.5 s13 %  Spicy v5.9 s19 %  Soothing v15.8 s22 %  Sour fog v4.2 s7 %  Stench v10.1 s40 %

== iteration 8/48  (244,864 sims, 14.9s)
raid fraction 0.422  sofra fell 3 %  units/half 21.4  builds/half 4.3  sofra 48
value spread: units CV 0.09  defences CV 0.10  ladles CV 0.40
units:    Grunt 17 v6.8 s7 %  Shieldbearer 59 v5.8 s13 %  Runner 10 v7.3 s9 %  Cook 39 v6.3 s13 %  Healer 20 v6.2 s6 %  Golem 57 v5.9 s8 %  Saboteur 50 v6.9 s11 %  Cloud 20 v5.4 s13 %  Brute 89 v5.7 s8 %  Bomber 27 v6.0 s12 %
defences: Catapult 74 v5.1 s32 %  Barricade 95 v6.9 s5 %  Spire 95 v7.1 s3 %  Griddle 60 v5.8 s34 %  Breeze 71 v7.1 s5 %  Puddle 45 v6.0 s1 %  Tower 95 v6.8 s1 %  Barrel 33 v7.0 s17 %  Tower 51 v6.7 s1 %  Snare 33 v6.6 s2 %
ladles:   Hearty v6.6 s18 %  Spicy v7.3 s17 %  Soothing v15.1 s20 %  Sour fog v5.1 s7 %  Stench v10.0 s38 %

== iteration 9/48  (274,960 sims, 16.8s)
raid fraction 0.458  sofra fell 4 %  units/half 21.7  builds/half 4.3  sofra 44
value spread: units CV 0.07  defences CV 0.13  ladles CV 0.33
units:    Grunt 18 v6.6 s7 %  Shieldbearer 58 v6.2 s13 %  Runner 11 v7.3 s9 %  Cook 39 v6.4 s13 %  Healer 20 v6.5 s6 %  Golem 57 v6.1 s8 %  Saboteur 52 v7.3 s12 %  Cloud 19 v6.3 s14 %  Brute 88 v5.7 s7 %  Bomber 27 v6.4 s10 %
defences: Catapult 71 v5.0 s37 %  Barricade 95 v7.8 s0 %  Spire 95 v7.4 s2 %  Griddle 60 v5.9 s37 %  Breeze 72 v7.3 s4 %  Puddle 43 v7.0 s1 %  Tower 95 v6.9 s1 %  Barrel 33 v7.4 s16 %  Tower 51 v6.3 s1 %  Snare 33 v7.9 s1 %
ladles:   Hearty v6.3 s22 %  Spicy v7.0 s14 %  Soothing v13.6 s21 %  Sour fog v6.2 s10 %  Stench v10.2 s33 %

== iteration 10/48  (305,056 sims, 18.7s)
raid fraction 0.489  sofra fell 6 %  units/half 21.2  builds/half 4.3  sofra 42
value spread: units CV 0.06  defences CV 0.11  ladles CV 0.29
units:    Grunt 18 v6.0 s7 %  Shieldbearer 57 v6.1 s12 %  Runner 11 v6.9 s9 %  Cook 39 v6.5 s13 %  Healer 20 v6.2 s7 %  Golem 56 v6.4 s9 %  Saboteur 54 v6.5 s12 %  Cloud 19 v6.0 s13 %  Brute 85 v5.9 s10 %  Bomber 27 v5.6 s9 %
defences: Catapult 68 v5.8 s38 %  Barricade 95 v8.7 s0 %  Spire 95 v8.2 s2 %  Griddle 60 v6.3 s36 %  Breeze 72 v8.2 s6 %  Puddle 43 v7.0 s1 %  Tower 94 v7.8 s1 %  Barrel 33 v7.6 s14 %  Tower 49 v7.1 s1 %  Snare 34 v7.3 s1 %
ladles:   Hearty v6.2 s22 %  Spicy v6.4 s13 %  Soothing v12.7 s21 %  Sour fog v7.2 s9 %  Stench v9.0 s35 %

== iteration 11/48  (335,152 sims, 20.5s)
raid fraction 0.477  sofra fell 5 %  units/half 21.5  builds/half 4.2  sofra 41
value spread: units CV 0.06  defences CV 0.11  ladles CV 0.27
units:    Grunt 18 v5.8 s7 %  Shieldbearer 57 v6.1 s13 %  Runner 11 v7.0 s10 %  Cook 40 v6.5 s14 %  Healer 20 v6.3 s6 %  Golem 57 v6.3 s9 %  Saboteur 55 v6.9 s12 %  Cloud 19 v6.3 s13 %  Brute 84 v6.1 s9 %  Bomber 26 v6.7 s8 %
defences: Catapult 67 v5.2 s35 %  Barricade 95 v8.0 s0 %  Spire 95 v7.6 s2 %  Griddle 60 v6.0 s37 %  Breeze 74 v7.4 s7 %  Puddle 42 v6.8 s1 %  Tower 95 v7.1 s0 %  Barrel 33 v7.0 s15 %  Tower 48 v6.8 s1 %  Snare 34 v7.4 s1 %
ladles:   Hearty v7.1 s21 %  Spicy v7.9 s14 %  Soothing v13.6 s25 %  Sour fog v7.0 s8 %  Stench v10.4 s31 %

== iteration 12/48  (365,248 sims, 22.4s)
raid fraction 0.548  sofra fell 9 %  units/half 22.4  builds/half 4.3  sofra 39
value spread: units CV 0.06  defences CV 0.14  ladles CV 0.22
units:    Grunt 18 v5.9 s7 %  Shieldbearer 57 v6.2 s11 %  Runner 11 v7.1 s14 %  Cook 40 v6.4 s13 %  Healer 20 v6.3 s6 %  Golem 57 v6.0 s9 %  Saboteur 56 v6.3 s13 %  Cloud 19 v6.0 s11 %  Brute 83 v6.0 s9 %  Bomber 26 v5.9 s7 %
defences: Catapult 65 v5.4 s37 %  Barricade 95 v9.1 s0 %  Spire 95 v8.6 s2 %  Griddle 61 v6.5 s36 %  Breeze 75 v8.1 s6 %  Puddle 42 v8.7 s1 %  Tower 95 v7.7 s0 %  Barrel 33 v8.0 s16 %  Tower 48 v7.3 s1 %  Snare 34 v7.3 s1 %
ladles:   Hearty v7.7 s26 %  Spicy v8.1 s14 %  Soothing v12.6 s22 %  Sour fog v7.5 s10 %  Stench v11.4 s28 %

== iteration 13/48  (395,344 sims, 24.2s)
raid fraction 0.529  sofra fell 8 %  units/half 22.1  builds/half 4.1  sofra 39
value spread: units CV 0.05  defences CV 0.13  ladles CV 0.25
units:    Grunt 18 v6.4 s7 %  Shieldbearer 57 v6.2 s12 %  Runner 11 v6.7 s12 %  Cook 40 v6.4 s13 %  Healer 20 v7.2 s5 %  Golem 57 v6.0 s8 %  Saboteur 56 v6.7 s16 %  Cloud 19 v6.3 s13 %  Brute 83 v6.2 s8 %  Bomber 26 v6.8 s6 %
defences: Catapult 62 v5.5 s33 %  Barricade 95 v7.9 s2 %  Spire 95 v8.3 s3 %  Griddle 61 v5.9 s40 %  Breeze 76 v7.5 s5 %  Puddle 43 v8.1 s1 %  Tower 95 v7.5 s0 %  Barrel 33 v7.0 s15 %  Tower 47 v7.0 s1 %  Snare 33 v8.5 s1 %
ladles:   Hearty v6.5 s25 %  Spicy v9.2 s12 %  Soothing v11.9 s21 %  Sour fog v6.5 s11 %  Stench v11.1 s31 %

== iteration 14/48  (425,440 sims, 26.2s)
raid fraction 0.528  sofra fell 7 %  units/half 22.6  builds/half 4.4  sofra 39
value spread: units CV 0.10  defences CV 0.14  ladles CV 0.11
units:    Grunt 18 v6.6 s7 %  Shieldbearer 57 v5.5 s12 %  Runner 11 v7.7 s10 %  Cook 40 v6.4 s14 %  Healer 21 v6.0 s4 %  Golem 56 v5.9 s7 %  Saboteur 57 v5.7 s19 %  Cloud 19 v5.9 s15 %  Brute 82 v5.5 s6 %  Bomber 26 v6.1 s6 %
defences: Catapult 60 v5.9 s37 %  Barricade 95 v8.9 s1 %  Spire 95 v9.2 s1 %  Griddle 61 v6.0 s35 %  Breeze 76 v9.1 s7 %  Puddle 44 v8.0 s1 %  Tower 95 v8.2 s0 %  Barrel 32 v7.4 s17 %  Tower 46 v8.5 s1 %  Snare 34 v8.4 s1 %
ladles:   Hearty v8.1 s28 %  Spicy v8.0 s14 %  Soothing v10.0 s22 %  Sour fog v7.4 s11 %  Stench v8.8 s25 %

== iteration 15/48  (455,536 sims, 28.1s)
raid fraction 0.546  sofra fell 9 %  units/half 23.7  builds/half 4.6  sofra 39
value spread: units CV 0.07  defences CV 0.13  ladles CV 0.12
units:    Grunt 18 v6.2 s7 %  Shieldbearer 56 v6.1 s12 %  Runner 12 v7.1 s11 %  Cook 41 v6.4 s14 %  Healer 21 v6.5 s4 %  Golem 56 v6.0 s7 %  Saboteur 56 v5.8 s20 %  Cloud 19 v5.8 s16 %  Brute 81 v5.7 s5 %  Bomber 26 v6.7 s5 %
defences: Catapult 58 v6.7 s38 %  Barricade 95 v10.4 s0 %  Spire 95 v10.2 s1 %  Griddle 59 v7.3 s31 %  Breeze 77 v10.1 s10 %  Puddle 44 v9.2 s1 %  Tower 95 v9.0 s0 %  Barrel 31 v9.6 s18 %  Tower 46 v9.2 s1 %  Snare 34 v8.3 s2 %
ladles:   Hearty v8.4 s27 %  Spicy v7.5 s12 %  Soothing v10.7 s29 %  Sour fog v8.6 s12 %  Stench v9.0 s20 %

== iteration 16/48  (485,632 sims, 30.0s)
raid fraction 0.516  sofra fell 8 %  units/half 23.9  builds/half 4.7  sofra 39
value spread: units CV 0.10  defences CV 0.15  ladles CV 0.09
units:    Grunt 18 v5.6 s7 %  Shieldbearer 56 v5.0 s12 %  Runner 12 v7.0 s11 %  Cook 41 v6.1 s15 %  Healer 21 v6.2 s4 %  Golem 56 v5.6 s7 %  Saboteur 55 v5.6 s19 %  Cloud 19 v5.7 s17 %  Brute 80 v4.9 s4 %  Bomber 26 v6.3 s5 %
defences: Catapult 56 v5.8 s40 %  Barricade 95 v10.3 s0 %  Spire 95 v10.0 s1 %  Griddle 58 v7.2 s30 %  Breeze 78 v9.7 s11 %  Puddle 44 v9.0 s1 %  Tower 95 v8.9 s0 %  Barrel 31 v9.7 s16 %  Tower 46 v8.7 s1 %  Snare 33 v9.5 s1 %
ladles:   Hearty v8.0 s33 %  Spicy v9.6 s13 %  Soothing v9.3 s29 %  Sour fog v9.6 s11 %  Stench v7.8 s14 %

== iteration 17/48  (515,728 sims, 31.9s)
raid fraction 0.511  sofra fell 8 %  units/half 24.5  builds/half 4.8  sofra 38
value spread: units CV 0.12  defences CV 0.14  ladles CV 0.12
units:    Grunt 18 v5.6 s6 %  Shieldbearer 55 v5.4 s11 %  Runner 13 v7.5 s13 %  Cook 42 v6.1 s16 %  Healer 21 v6.6 s3 %  Golem 56 v5.4 s7 %  Saboteur 55 v5.5 s19 %  Cloud 19 v5.7 s17 %  Brute 78 v5.1 s3 %  Bomber 27 v7.0 s4 %
defences: Catapult 53 v6.5 s38 %  Barricade 95 v11.1 s0 %  Spire 95 v10.5 s1 %  Griddle 56 v8.1 s31 %  Breeze 79 v9.8 s14 %  Puddle 44 v10.4 s1 %  Tower 94 v9.3 s0 %  Barrel 31 v10.1 s14 %  Tower 45 v10.5 s0 %  Snare 33 v10.9 s2 %
ladles:   Hearty v6.8 s30 %  Spicy v7.0 s14 %  Soothing v9.0 s28 %  Sour fog v8.6 s13 %  Stench v6.8 s14 %

== iteration 18/48  (545,824 sims, 33.8s)
raid fraction 0.474  sofra fell 6 %  units/half 24.5  builds/half 5.1  sofra 37
value spread: units CV 0.10  defences CV 0.14  ladles CV 0.12
units:    Grunt 18 v6.1 s6 %  Shieldbearer 54 v6.0 s10 %  Runner 14 v6.8 s12 %  Cook 43 v6.5 s16 %  Healer 22 v7.6 s3 %  Golem 56 v6.1 s6 %  Saboteur 55 v6.0 s21 %  Cloud 19 v5.8 s19 %  Brute 76 v5.2 s2 %  Bomber 28 v6.8 s4 %
defences: Catapult 50 v6.1 s35 %  Barricade 95 v10.4 s0 %  Spire 95 v9.7 s0 %  Griddle 55 v7.3 s29 %  Breeze 78 v9.3 s16 %  Puddle 44 v7.9 s1 %  Tower 92 v8.9 s1 %  Barrel 31 v9.8 s16 %  Tower 45 v9.0 s0 %  Snare 33 v8.7 s1 %
ladles:   Hearty v7.3 s33 %  Spicy v6.8 s13 %  Soothing v9.0 s25 %  Sour fog v9.0 s16 %  Stench v7.5 s13 %

== iteration 19/48  (575,920 sims, 35.8s)
raid fraction 0.495  sofra fell 9 %  units/half 25.5  builds/half 5.3  sofra 36
value spread: units CV 0.11  defences CV 0.11  ladles CV 0.10
units:    Grunt 18 v5.0 s6 %  Shieldbearer 54 v5.0 s10 %  Runner 14 v6.4 s13 %  Cook 44 v6.1 s19 %  Healer 23 v6.3 s3 %  Golem 56 v5.4 s6 %  Saboteur 55 v4.9 s20 %  Cloud 19 v5.2 s19 %  Brute 74 v5.0 s2 %  Bomber 29 v6.5 s3 %
defences: Catapult 48 v7.3 s38 %  Barricade 95 v11.2 s0 %  Spire 95 v10.4 s0 %  Griddle 54 v8.4 s25 %  Breeze 79 v9.7 s18 %  Puddle 43 v9.3 s2 %  Tower 92 v9.4 s1 %  Barrel 32 v9.9 s16 %  Tower 45 v10.3 s0 %  Snare 33 v10.1 s1 %
ladles:   Hearty v7.8 s34 %  Spicy v7.9 s11 %  Soothing v8.9 s30 %  Sour fog v8.9 s17 %  Stench v6.6 s7 %

== iteration 20/48  (606,016 sims, 37.7s)
raid fraction 0.500  sofra fell 8 %  units/half 24.6  builds/half 5.3  sofra 35
value spread: units CV 0.10  defences CV 0.14  ladles CV 0.09
units:    Grunt 18 v4.9 s5 %  Shieldbearer 53 v5.8 s8 %  Runner 14 v6.8 s15 %  Cook 45 v6.0 s21 %  Healer 24 v6.4 s3 %  Golem 56 v5.6 s6 %  Saboteur 54 v5.1 s20 %  Cloud 19 v5.4 s17 %  Brute 73 v5.7 s2 %  Bomber 30 v6.5 s3 %
defences: Catapult 47 v6.3 s37 %  Barricade 95 v11.1 s0 %  Spire 95 v10.5 s0 %  Griddle 53 v8.6 s22 %  Breeze 79 v9.4 s20 %  Puddle 43 v9.0 s1 %  Tower 91 v9.6 s1 %  Barrel 32 v10.1 s17 %  Tower 45 v10.0 s0 %  Snare 33 v11.0 s1 %
ladles:   Hearty v7.9 s49 %  Spicy v7.3 s9 %  Soothing v8.9 s25 %  Sour fog v9.0 s13 %  Stench v7.3 s5 %

== iteration 21/48  (636,112 sims, 39.6s)
raid fraction 0.520  sofra fell 11 %  units/half 25.4  builds/half 5.5  sofra 34
value spread: units CV 0.14  defences CV 0.11  ladles CV 0.10
units:    Grunt 17 v5.1 s5 %  Shieldbearer 53 v5.1 s8 %  Runner 14 v7.1 s16 %  Cook 45 v5.9 s22 %  Healer 24 v6.6 s2 %  Golem 56 v5.4 s5 %  Saboteur 53 v4.5 s20 %  Cloud 19 v5.6 s17 %  Brute 73 v4.6 s2 %  Bomber 31 v6.2 s3 %
defences: Catapult 45 v7.4 s37 %  Barricade 95 v11.2 s0 %  Spire 95 v10.6 s0 %  Griddle 52 v8.9 s19 %  Breeze 79 v9.1 s22 %  Puddle 42 v9.1 s2 %  Tower 91 v9.7 s1 %  Barrel 32 v9.9 s18 %  Tower 45 v10.3 s0 %  Snare 34 v10.4 s1 %
ladles:   Hearty v8.4 s44 %  Spicy v8.2 s11 %  Soothing v9.1 s25 %  Sour fog v9.5 s15 %  Stench v6.9 s6 %

== iteration 22/48  (666,208 sims, 41.5s)
raid fraction 0.478  sofra fell 8 %  units/half 24.4  builds/half 5.7  sofra 34
value spread: units CV 0.10  defences CV 0.11  ladles CV 0.09
units:    Grunt 17 v5.1 s5 %  Shieldbearer 52 v5.5 s7 %  Runner 15 v6.3 s17 %  Cook 46 v6.1 s23 %  Healer 25 v6.6 s2 %  Golem 56 v5.6 s4 %  Saboteur 51 v4.5 s20 %  Cloud 19 v5.9 s17 %  Brute 71 v5.6 s2 %  Bomber 32 v6.3 s3 %
defences: Catapult 44 v6.8 s38 %  Barricade 95 v9.7 s0 %  Spire 95 v10.1 s0 %  Griddle 51 v8.2 s14 %  Breeze 78 v8.6 s23 %  Puddle 41 v9.1 s1 %  Tower 91 v9.1 s1 %  Barrel 32 v9.8 s21 %  Tower 45 v9.5 s0 %  Snare 34 v10.0 s1 %
ladles:   Hearty v8.8 s44 %  Spicy v9.6 s9 %  Soothing v9.7 s28 %  Sour fog v9.5 s16 %  Stench v7.5 s4 %

== iteration 23/48  (696,304 sims, 43.5s)
raid fraction 0.525  sofra fell 12 %  units/half 25.3  builds/half 5.7  sofra 33
value spread: units CV 0.14  defences CV 0.08  ladles CV 0.08
units:    Grunt 17 v4.1 s4 %  Shieldbearer 52 v4.9 s6 %  Runner 15 v5.8 s18 %  Cook 47 v5.6 s25 %  Healer 26 v6.6 s2 %  Golem 56 v4.9 s4 %  Saboteur 49 v4.9 s20 %  Cloud 19 v4.8 s18 %  Brute 71 v4.7 s2 %  Bomber 33 v6.2 s2 %
defences: Catapult 43 v8.2 s38 %  Barricade 95 v10.3 s0 %  Spire 95 v10.7 s0 %  Griddle 50 v8.9 s13 %  Breeze 78 v9.1 s24 %  Puddle 41 v9.4 s2 %  Tower 91 v9.7 s1 %  Barrel 32 v10.3 s22 %  Tower 45 v10.7 s0 %  Snare 34 v10.2 s1 %
ladles:   Hearty v7.4 s46 %  Spicy v8.0 s9 %  Soothing v9.0 s26 %  Sour fog v8.2 s16 %  Stench v7.1 s3 %

== iteration 24/48  (726,400 sims, 45.5s)
raid fraction 0.501  sofra fell 11 %  units/half 24.9  builds/half 5.8  sofra 33
value spread: units CV 0.13  defences CV 0.08  ladles CV 0.12
units:    Grunt 16 v4.0 s4 %  Shieldbearer 52 v4.7 s6 %  Runner 15 v6.2 s17 %  Cook 49 v5.0 s27 %  Healer 27 v5.5 s1 %  Golem 56 v4.6 s3 %  Saboteur 49 v4.2 s20 %  Cloud 19 v4.9 s17 %  Brute 70 v4.8 s2 %  Bomber 34 v5.8 s2 %
defences: Catapult 43 v8.1 s38 %  Barricade 95 v10.4 s0 %  Spire 95 v10.6 s0 %  Griddle 49 v9.6 s11 %  Breeze 78 v8.7 s26 %  Puddle 41 v9.1 s1 %  Tower 91 v9.8 s1 %  Barrel 32 v10.4 s22 %  Tower 46 v9.8 s0 %  Snare 34 v10.5 s1 %
ladles:   Hearty v6.6 s45 %  Spicy v6.6 s9 %  Soothing v8.0 s28 %  Sour fog v7.9 s14 %  Stench v5.8 s3 %

== iteration 25/48  (756,496 sims, 47.3s)
raid fraction 0.483  sofra fell 10 %  units/half 23.3  builds/half 6.0  sofra 32
value spread: units CV 0.14  defences CV 0.09  ladles CV 0.14
units:    Grunt 16 v3.9 s4 %  Shieldbearer 52 v4.5 s6 %  Runner 16 v5.8 s17 %  Cook 50 v5.1 s28 %  Healer 28 v5.1 s2 %  Golem 56 v4.9 s4 %  Saboteur 48 v3.6 s19 %  Cloud 19 v4.6 s17 %  Brute 70 v4.7 s2 %  Bomber 35 v5.9 s2 %
defences: Catapult 43 v7.5 s40 %  Barricade 95 v10.4 s0 %  Spire 95 v10.1 s0 %  Griddle 49 v9.0 s6 %  Breeze 78 v8.8 s29 %  Puddle 40 v9.3 s2 %  Tower 91 v9.4 s0 %  Barrel 32 v10.0 s21 %  Tower 46 v10.2 s0 %  Snare 34 v10.5 s1 %
ladles:   Hearty v6.1 s54 %  Spicy v6.7 s7 %  Soothing v7.9 s25 %  Sour fog v8.0 s12 %  Stench v5.6 s1 %

== iteration 26/48  (786,592 sims, 49.2s)
raid fraction 0.488  sofra fell 10 %  units/half 23.5  builds/half 6.1  sofra 31
value spread: units CV 0.16  defences CV 0.11  ladles CV 0.15
units:    Grunt 15 v4.8 s3 %  Shieldbearer 51 v4.6 s5 %  Runner 16 v6.6 s18 %  Cook 51 v5.1 s31 %  Healer 28 v6.6 s2 %  Golem 56 v5.2 s4 %  Saboteur 46 v4.2 s17 %  Cloud 19 v5.0 s17 %  Brute 70 v4.7 s2 %  Bomber 36 v6.3 s2 %
defences: Catapult 43 v7.2 s41 %  Barricade 95 v10.3 s0 %  Spire 95 v9.8 s0 %  Griddle 48 v9.0 s5 %  Breeze 78 v8.2 s29 %  Puddle 40 v7.6 s1 %  Tower 90 v9.1 s1 %  Barrel 32 v9.8 s22 %  Tower 46 v9.0 s0 %  Snare 34 v9.8 s1 %
ladles:   Hearty v6.3 s54 %  Spicy v6.7 s10 %  Soothing v8.4 s24 %  Sour fog v8.1 s11 %  Stench v5.6 s2 %

== iteration 27/48  (816,688 sims, 51.1s)
raid fraction 0.508  sofra fell 11 %  units/half 22.7  builds/half 6.0  sofra 30
value spread: units CV 0.16  defences CV 0.10  ladles CV 0.20
units:    Grunt 15 v4.2 s3 %  Shieldbearer 50 v4.1 s4 %  Runner 17 v6.0 s19 %  Cook 52 v4.8 s33 %  Healer 29 v6.1 s1 %  Golem 56 v4.7 s4 %  Saboteur 45 v3.7 s16 %  Cloud 19 v5.1 s17 %  Brute 69 v4.3 s2 %  Bomber 37 v5.4 s2 %
defences: Catapult 43 v7.4 s44 %  Barricade 95 v11.0 s0 %  Spire 95 v10.4 s0 %  Griddle 48 v9.5 s4 %  Breeze 78 v8.6 s30 %  Puddle 39 v9.3 s1 %  Tower 90 v9.6 s0 %  Barrel 33 v9.8 s19 %  Tower 46 v9.9 s0 %  Snare 34 v10.6 s0 %
ladles:   Hearty v5.8 s55 %  Spicy v6.1 s14 %  Soothing v8.3 s22 %  Sour fog v8.2 s8 %  Stench v5.1 s2 %

== iteration 28/48  (846,784 sims, 53.0s)
raid fraction 0.487  sofra fell 10 %  units/half 21.6  builds/half 6.2  sofra 30
value spread: units CV 0.14  defences CV 0.12  ladles CV 0.16
units:    Grunt 15 v4.6 s2 %  Shieldbearer 49 v4.9 s3 %  Runner 18 v5.5 s18 %  Cook 53 v5.3 s35 %  Healer 30 v6.0 s2 %  Golem 56 v5.3 s4 %  Saboteur 43 v3.6 s16 %  Cloud 19 v5.4 s15 %  Brute 68 v4.3 s2 %  Bomber 38 v5.8 s2 %
defences: Catapult 43 v7.2 s44 %  Barricade 95 v10.6 s0 %  Spire 95 v10.0 s0 %  Griddle 48 v8.9 s3 %  Breeze 78 v7.8 s31 %  Puddle 39 v8.9 s1 %  Tower 90 v9.3 s0 %  Barrel 33 v10.4 s21 %  Tower 46 v10.0 s0 %  Snare 34 v10.9 s0 %
ladles:   Hearty v5.7 s55 %  Spicy v6.4 s11 %  Soothing v7.9 s21 %  Sour fog v7.7 s12 %  Stench v5.3 s1 %

== iteration 29/48  (876,880 sims, 54.7s)
raid fraction 0.540  sofra fell 13 %  units/half 20.6  builds/half 6.1  sofra 30
value spread: units CV 0.11  defences CV 0.10  ladles CV 0.20
units:    Grunt 15 v5.5 s2 %  Shieldbearer 48 v5.4 s3 %  Runner 18 v6.4 s17 %  Cook 54 v5.5 s36 %  Healer 31 v5.2 s2 %  Golem 56 v5.5 s5 %  Saboteur 41 v4.0 s16 %  Cloud 19 v6.1 s14 %  Brute 66 v5.2 s3 %  Bomber 39 v6.0 s3 %
defences: Catapult 43 v8.2 s35 %  Barricade 95 v10.1 s0 %  Spire 95 v10.3 s0 %  Griddle 47 v9.4 s1 %  Breeze 77 v7.7 s32 %  Puddle 39 v8.0 s16 %  Tower 89 v9.5 s0 %  Barrel 33 v9.7 s16 %  Tower 46 v9.3 s0 %  Snare 35 v9.9 s0 %
ladles:   Hearty v5.5 s56 %  Spicy v6.2 s11 %  Soothing v7.9 s24 %  Sour fog v7.5 s10 %  Stench v4.4 s1 %

== iteration 30/48  (906,976 sims, 56.5s)
raid fraction 0.521  sofra fell 10 %  units/half 19.7  builds/half 6.3  sofra 30
value spread: units CV 0.12  defences CV 0.09  ladles CV 0.20
units:    Grunt 15 v4.9 s1 %  Shieldbearer 48 v4.6 s2 %  Runner 18 v5.8 s18 %  Cook 55 v4.8 s37 %  Healer 31 v5.0 s2 %  Golem 56 v5.2 s6 %  Saboteur 39 v3.7 s15 %  Cloud 19 v6.0 s13 %  Brute 65 v4.6 s3 %  Bomber 40 v5.3 s2 %
defences: Catapult 43 v7.5 s37 %  Barricade 95 v9.8 s0 %  Spire 95 v9.8 s0 %  Griddle 47 v9.5 s1 %  Breeze 76 v7.7 s31 %  Puddle 38 v8.3 s15 %  Tower 89 v9.2 s0 %  Barrel 33 v9.3 s16 %  Tower 46 v9.4 s0 %  Snare 35 v9.8 s0 %
ladles:   Hearty v5.5 s52 %  Spicy v5.5 s11 %  Soothing v7.8 s25 %  Sour fog v6.9 s11 %  Stench v4.3 s1 %

== iteration 31/48  (937,072 sims, 58.2s)
raid fraction 0.505  sofra fell 10 %  units/half 19.2  builds/half 6.3  sofra 30
value spread: units CV 0.07  defences CV 0.09  ladles CV 0.17
units:    Grunt 15 v5.8 s2 %  Shieldbearer 47 v5.3 s2 %  Runner 18 v5.7 s17 %  Cook 56 v5.1 s37 %  Healer 31 v5.7 s2 %  Golem 56 v5.6 s6 %  Saboteur 37 v4.6 s14 %  Cloud 20 v5.5 s14 %  Brute 64 v5.8 s4 %  Bomber 40 v5.4 s3 %
defences: Catapult 43 v7.3 s37 %  Barricade 95 v9.3 s0 %  Spire 95 v9.0 s0 %  Griddle 47 v9.0 s1 %  Breeze 75 v7.5 s31 %  Puddle 37 v7.6 s14 %  Tower 89 v8.5 s0 %  Barrel 33 v9.0 s17 %  Tower 46 v8.8 s0 %  Snare 35 v9.2 s0 %
ladles:   Hearty v5.6 s54 %  Spicy v5.8 s9 %  Soothing v7.7 s26 %  Sour fog v6.7 s11 %  Stench v4.7 s1 %

== iteration 32/48  (967,168 sims, 60.0s)
raid fraction 0.514  sofra fell 11 %  units/half 18.9  builds/half 6.4  sofra 30
value spread: units CV 0.08  defences CV 0.07  ladles CV 0.17
units:    Grunt 15 v4.8 s1 %  Shieldbearer 47 v4.9 s2 %  Runner 18 v5.5 s16 %  Cook 57 v4.7 s37 %  Healer 31 v5.0 s2 %  Golem 56 v5.3 s6 %  Saboteur 36 v3.9 s14 %  Cloud 20 v5.2 s13 %  Brute 64 v5.4 s5 %  Bomber 40 v5.2 s3 %
defences: Catapult 43 v8.2 s36 %  Barricade 95 v8.9 s0 %  Spire 95 v8.9 s0 %  Griddle 47 v9.2 s1 %  Breeze 75 v7.7 s32 %  Puddle 36 v7.8 s14 %  Tower 88 v8.8 s0 %  Barrel 33 v9.9 s18 %  Tower 46 v8.8 s0 %  Snare 35 v9.2 s0 %
ladles:   Hearty v4.9 s54 %  Spicy v5.7 s9 %  Soothing v7.4 s28 %  Sour fog v6.3 s8 %  Stench v4.7 s1 %

== iteration 33/48  (997,264 sims, 61.8s)
raid fraction 0.534  sofra fell 11 %  units/half 18.8  builds/half 6.4  sofra 30
value spread: units CV 0.08  defences CV 0.08  ladles CV 0.20
units:    Grunt 15 v6.1 s1 %  Shieldbearer 47 v6.1 s2 %  Runner 18 v6.3 s16 %  Cook 58 v5.7 s36 %  Healer 31 v6.1 s2 %  Golem 56 v6.2 s6 %  Saboteur 35 v5.0 s15 %  Cloud 20 v6.9 s14 %  Brute 65 v6.2 s4 %  Bomber 40 v5.8 s3 %
defences: Catapult 44 v8.3 s37 %  Barricade 95 v9.7 s0 %  Spire 95 v9.6 s0 %  Griddle 47 v9.4 s0 %  Breeze 75 v8.1 s31 %  Puddle 35 v8.3 s16 %  Tower 88 v9.3 s0 %  Barrel 34 v9.6 s15 %  Tower 46 v8.8 s0 %  Snare 35 v10.3 s0 %
ladles:   Hearty v4.7 s58 %  Spicy v5.0 s8 %  Soothing v7.4 s23 %  Sour fog v6.4 s10 %  Stench v4.4 s1 %

== iteration 34/48  (1,027,360 sims, 63.5s)
raid fraction 0.513  sofra fell 11 %  units/half 18.1  builds/half 6.5  sofra 30
value spread: units CV 0.10  defences CV 0.07  ladles CV 0.20
units:    Grunt 15 v5.5 s2 %  Shieldbearer 47 v4.7 s2 %  Runner 18 v5.2 s15 %  Cook 59 v4.4 s36 %  Healer 31 v5.3 s2 %  Golem 56 v5.2 s7 %  Saboteur 34 v4.0 s14 %  Cloud 20 v5.7 s14 %  Brute 65 v4.8 s5 %  Bomber 40 v4.6 s3 %
defences: Catapult 44 v7.9 s40 %  Barricade 95 v9.6 s0 %  Spire 95 v9.2 s0 %  Griddle 47 v9.3 s0 %  Breeze 75 v8.0 s31 %  Puddle 34 v8.4 s15 %  Tower 88 v9.1 s0 %  Barrel 34 v9.6 s14 %  Tower 46 v9.0 s0 %  Snare 35 v9.7 s0 %
ladles:   Hearty v4.3 s57 %  Spicy v4.7 s8 %  Soothing v6.5 s25 %  Sour fog v5.8 s10 %  Stench v3.7 s0 %

== iteration 35/48  (1,057,456 sims, 65.4s)
raid fraction 0.512  sofra fell 10 %  units/half 18.4  builds/half 6.4  sofra 30
value spread: units CV 0.11  defences CV 0.08  ladles CV 0.23
units:    Grunt 15 v5.9 s2 %  Shieldbearer 47 v5.8 s3 %  Runner 18 v6.5 s14 %  Cook 59 v5.1 s33 %  Healer 31 v5.5 s2 %  Golem 56 v5.7 s7 %  Saboteur 33 v4.8 s14 %  Cloud 20 v6.2 s15 %  Brute 65 v5.7 s6 %  Bomber 40 v4.3 s3 %
defences: Catapult 44 v7.1 s43 %  Barricade 95 v9.8 s0 %  Spire 95 v9.1 s0 %  Griddle 47 v8.5 s0 %  Breeze 75 v8.3 s30 %  Puddle 34 v9.1 s11 %  Tower 88 v8.7 s0 %  Barrel 34 v8.5 s16 %  Tower 46 v8.3 s0 %  Snare 35 v9.0 s0 %
ladles:   Hearty v4.1 s57 %  Spicy v4.4 s7 %  Soothing v6.7 s26 %  Sour fog v5.8 s9 %  Stench v3.7 s1 %

== iteration 36/48  (1,087,552 sims, 67.2s)
raid fraction 0.491  sofra fell 8 %  units/half 18.2  builds/half 6.4  sofra 30
value spread: units CV 0.13  defences CV 0.11  ladles CV 0.22
units:    Grunt 15 v4.8 s2 %  Shieldbearer 47 v4.5 s3 %  Runner 18 v5.9 s14 %  Cook 59 v4.2 s33 %  Healer 31 v5.4 s2 %  Golem 56 v4.6 s7 %  Saboteur 32 v4.1 s14 %  Cloud 20 v5.4 s16 %  Brute 65 v4.1 s5 %  Bomber 38 v4.1 s4 %
defences: Catapult 44 v6.7 s45 %  Barricade 95 v10.1 s0 %  Spire 95 v8.6 s0 %  Griddle 47 v7.7 s0 %  Breeze 76 v8.0 s30 %  Puddle 34 v8.2 s11 %  Tower 88 v8.3 s0 %  Barrel 34 v7.7 s14 %  Tower 46 v8.3 s0 %  Snare 35 v9.3 s0 %
ladles:   Hearty v3.9 s61 %  Spicy v4.1 s7 %  Soothing v6.6 s23 %  Sour fog v5.2 s9 %  Stench v3.9 s0 %

== iteration 37/48  (1,117,648 sims, 69.0s)
raid fraction 0.495  sofra fell 8 %  units/half 17.8  builds/half 6.4  sofra 30
value spread: units CV 0.14  defences CV 0.13  ladles CV 0.22
units:    Grunt 15 v6.1 s2 %  Shieldbearer 47 v5.2 s3 %  Runner 19 v6.7 s14 %  Cook 59 v4.9 s32 %  Healer 32 v5.4 s3 %  Golem 56 v6.0 s7 %  Saboteur 31 v4.6 s14 %  Cloud 20 v6.4 s17 %  Brute 64 v5.1 s4 %  Bomber 37 v4.2 s4 %
defences: Catapult 44 v6.7 s47 %  Barricade 95 v10.2 s0 %  Spire 95 v8.4 s0 %  Griddle 47 v7.3 s0 %  Breeze 77 v7.9 s29 %  Puddle 34 v9.4 s10 %  Tower 88 v8.1 s0 %  Barrel 34 v7.2 s14 %  Tower 46 v8.2 s0 %  Snare 36 v9.5 s0 %
ladles:   Hearty v3.7 s65 %  Spicy v3.8 s8 %  Soothing v6.1 s19 %  Sour fog v4.7 s8 %  Stench v3.5 s0 %

== iteration 38/48  (1,147,744 sims, 70.7s)
raid fraction 0.449  sofra fell 6 %  units/half 17.5  builds/half 6.4  sofra 30
value spread: units CV 0.11  defences CV 0.13  ladles CV 0.21
units:    Grunt 15 v4.7 s2 %  Shieldbearer 47 v4.1 s4 %  Runner 20 v4.8 s14 %  Cook 60 v3.7 s31 %  Healer 32 v4.8 s2 %  Golem 57 v4.3 s7 %  Saboteur 30 v4.2 s15 %  Cloud 21 v5.1 s17 %  Brute 64 v3.9 s5 %  Bomber 36 v3.9 s4 %
defences: Catapult 44 v5.6 s54 %  Barricade 95 v9.2 s0 %  Spire 95 v7.7 s0 %  Griddle 46 v6.7 s0 %  Breeze 78 v7.6 s27 %  Puddle 35 v8.8 s1 %  Tower 88 v7.6 s0 %  Barrel 33 v7.1 s17 %  Tower 46 v8.0 s0 %  Snare 37 v8.3 s0 %
ladles:   Hearty v3.4 s61 %  Spicy v4.2 s8 %  Soothing v6.2 s20 %  Sour fog v4.4 s10 %  Stench v4.0 s1 %

== iteration 39/48  (1,177,840 sims, 72.5s)
raid fraction 0.454  sofra fell 6 %  units/half 17.2  builds/half 6.5  sofra 30
value spread: units CV 0.10  defences CV 0.15  ladles CV 0.21
units:    Grunt 15 v6.4 s3 %  Shieldbearer 47 v5.8 s4 %  Runner 20 v5.8 s13 %  Cook 60 v5.1 s30 %  Healer 33 v5.5 s2 %  Golem 57 v5.8 s8 %  Saboteur 30 v5.1 s16 %  Cloud 22 v6.4 s16 %  Brute 63 v5.3 s5 %  Bomber 36 v4.4 s4 %
defences: Catapult 44 v5.2 s55 %  Barricade 95 v9.2 s0 %  Spire 95 v7.2 s0 %  Griddle 45 v5.8 s0 %  Breeze 79 v7.2 s26 %  Puddle 36 v7.3 s3 %  Tower 88 v7.3 s0 %  Barrel 33 v6.5 s17 %  Tower 46 v7.4 s0 %  Snare 37 v8.2 s0 %
ladles:   Hearty v3.5 s61 %  Spicy v4.0 s10 %  Soothing v6.1 s20 %  Sour fog v4.5 s9 %  Stench v3.9 s0 %

== iteration 40/48  (1,207,936 sims, 74.2s)
raid fraction 0.491  sofra fell 8 %  units/half 17.4  builds/half 6.5  sofra 30
value spread: units CV 0.11  defences CV 0.12  ladles CV 0.24
units:    Grunt 15 v6.5 s3 %  Shieldbearer 47 v5.3 s5 %  Runner 20 v5.3 s13 %  Cook 60 v5.0 s28 %  Healer 33 v5.5 s2 %  Golem 57 v5.9 s7 %  Saboteur 30 v5.3 s17 %  Cloud 22 v5.5 s16 %  Brute 62 v4.8 s5 %  Bomber 35 v4.1 s4 %
defences: Catapult 44 v5.6 s54 %  Barricade 95 v9.8 s0 %  Spire 95 v8.2 s0 %  Griddle 44 v7.2 s0 %  Breeze 80 v7.8 s26 %  Puddle 36 v7.8 s3 %  Tower 88 v7.9 s0 %  Barrel 32 v7.7 s16 %  Tower 46 v8.1 s0 %  Snare 38 v7.7 s0 %
ladles:   Hearty v3.5 s63 %  Spicy v3.7 s7 %  Soothing v6.2 s19 %  Sour fog v4.6 s10 %  Stench v3.6 s1 %

== iteration 41/48  (1,238,032 sims, 76.0s)
raid fraction 0.473  sofra fell 8 %  units/half 17.2  builds/half 6.4  sofra 30
value spread: units CV 0.14  defences CV 0.10  ladles CV 0.23
units:    Grunt 15 v5.9 s3 %  Shieldbearer 47 v5.0 s5 %  Runner 20 v4.9 s13 %  Cook 60 v4.4 s26 %  Healer 33 v4.7 s2 %  Golem 58 v5.0 s7 %  Saboteur 30 v4.5 s18 %  Cloud 22 v5.5 s17 %  Brute 61 v4.4 s4 %  Bomber 34 v3.3 s4 %
defences: Catapult 44 v5.8 s55 %  Barricade 95 v8.6 s0 %  Spire 95 v7.1 s0 %  Griddle 44 v6.5 s0 %  Breeze 81 v7.1 s25 %  Puddle 36 v6.7 s4 %  Tower 88 v7.1 s0 %  Barrel 32 v7.0 s15 %  Tower 46 v7.6 s0 %  Snare 38 v7.1 s0 %
ladles:   Hearty v3.2 s68 %  Spicy v3.9 s6 %  Soothing v5.8 s17 %  Sour fog v4.2 s9 %  Stench v3.3 s0 %

== iteration 42/48  (1,268,128 sims, 77.7s)
raid fraction 0.511  sofra fell 8 %  units/half 17.6  builds/half 6.3  sofra 30
value spread: units CV 0.16  defences CV 0.10  ladles CV 0.25
units:    Grunt 15 v7.1 s3 %  Shieldbearer 47 v5.0 s6 %  Runner 20 v5.1 s12 %  Cook 60 v4.7 s23 %  Healer 33 v5.1 s2 %  Golem 58 v5.5 s7 %  Saboteur 30 v4.9 s20 %  Cloud 22 v5.7 s18 %  Brute 60 v4.9 s4 %  Bomber 33 v3.6 s4 %
defences: Catapult 45 v6.4 s55 %  Barricade 95 v9.2 s0 %  Spire 95 v7.9 s0 %  Griddle 44 v6.7 s0 %  Breeze 82 v7.7 s24 %  Puddle 36 v6.6 s7 %  Tower 88 v7.7 s0 %  Barrel 32 v7.4 s14 %  Tower 46 v7.9 s0 %  Snare 38 v6.9 s0 %
ladles:   Hearty v3.1 s69 %  Spicy v3.7 s6 %  Soothing v6.1 s16 %  Sour fog v4.0 s9 %  Stench v3.7 s0 %

== iteration 43/48  (1,298,224 sims, 79.5s)
raid fraction 0.523  sofra fell 9 %  units/half 16.9  builds/half 6.3  sofra 30
value spread: units CV 0.12  defences CV 0.09  ladles CV 0.33
units:    Grunt 16 v6.4 s5 %  Shieldbearer 47 v5.7 s8 %  Runner 20 v5.5 s12 %  Cook 60 v4.8 s23 %  Healer 33 v5.0 s2 %  Golem 59 v5.9 s7 %  Saboteur 30 v5.5 s19 %  Cloud 22 v6.2 s17 %  Brute 60 v5.3 s4 %  Bomber 32 v4.0 s3 %
defences: Catapult 46 v6.9 s55 %  Barricade 95 v9.0 s0 %  Spire 95 v7.8 s0 %  Griddle 43 v7.3 s0 %  Breeze 83 v7.1 s24 %  Puddle 35 v6.3 s8 %  Tower 88 v7.6 s0 %  Barrel 32 v7.8 s12 %  Tower 46 v8.1 s0 %  Snare 38 v7.5 s0 %
ladles:   Hearty v2.7 s69 %  Spicy v3.3 s6 %  Soothing v6.2 s18 %  Sour fog v4.4 s8 %  Stench v2.8 s0 %

== iteration 44/48  (1,328,320 sims, 81.3s)
raid fraction 0.550  sofra fell 9 %  units/half 16.9  builds/half 6.2  sofra 30
value spread: units CV 0.19  defences CV 0.08  ladles CV 0.28
units:    Grunt 16 v6.7 s5 %  Shieldbearer 47 v5.8 s10 %  Runner 20 v6.2 s11 %  Cook 59 v4.8 s21 %  Healer 33 v5.0 s2 %  Golem 60 v5.9 s7 %  Saboteur 30 v4.8 s20 %  Cloud 22 v6.6 s17 %  Brute 60 v5.2 s3 %  Bomber 31 v3.0 s3 %
defences: Catapult 47 v7.3 s57 %  Barricade 95 v8.0 s0 %  Spire 95 v7.7 s0 %  Griddle 43 v7.3 s0 %  Breeze 83 v6.5 s24 %  Puddle 34 v6.0 s8 %  Tower 88 v7.7 s0 %  Barrel 32 v7.6 s11 %  Tower 46 v7.7 s0 %  Snare 38 v7.0 s0 %
ladles:   Hearty v3.3 s70 %  Spicy v4.3 s7 %  Soothing v6.3 s15 %  Sour fog v4.2 s7 %  Stench v2.9 s0 %

== iteration 45/48  (1,358,416 sims, 83.0s)
raid fraction 0.525  sofra fell 8 %  units/half 16.6  builds/half 5.9  sofra 30
value spread: units CV 0.21  defences CV 0.11  ladles CV 0.29
units:    Grunt 16 v6.5 s6 %  Shieldbearer 47 v5.8 s11 %  Runner 20 v5.2 s11 %  Cook 58 v4.5 s21 %  Healer 33 v4.4 s2 %  Golem 61 v5.2 s8 %  Saboteur 29 v4.3 s19 %  Cloud 23 v6.4 s17 %  Brute 60 v4.1 s2 %  Bomber 30 v2.9 s3 %
defences: Catapult 49 v6.6 s60 %  Barricade 95 v8.2 s0 %  Spire 95 v7.3 s0 %  Griddle 43 v7.0 s0 %  Breeze 82 v5.9 s25 %  Puddle 33 v5.8 s5 %  Tower 88 v7.0 s0 %  Barrel 32 v7.6 s10 %  Tower 46 v7.6 s0 %  Snare 38 v7.3 s0 %
ladles:   Hearty v3.3 s71 %  Spicy v4.0 s5 %  Soothing v6.3 s17 %  Sour fog v4.3 s7 %  Stench v2.8 s0 %

== iteration 46/48  (1,388,512 sims, 84.7s)
raid fraction 0.522  sofra fell 8 %  units/half 16.6  builds/half 5.4  sofra 30
value spread: units CV 0.17  defences CV 0.10  ladles CV 0.25
units:    Grunt 17 v7.3 s8 %  Shieldbearer 48 v6.3 s13 %  Runner 20 v6.9 s11 %  Cook 58 v5.0 s20 %  Healer 33 v5.4 s2 %  Golem 62 v6.0 s7 %  Saboteur 29 v5.2 s18 %  Cloud 24 v6.7 s18 %  Brute 59 v5.7 s2 %  Bomber 29 v3.8 s2 %
defences: Catapult 51 v6.4 s60 %  Barricade 95 v8.1 s3 %  Spire 95 v7.2 s0 %  Griddle 43 v6.2 s0 %  Breeze 81 v6.0 s24 %  Puddle 32 v5.7 s3 %  Tower 88 v6.9 s0 %  Barrel 32 v6.5 s9 %  Tower 46 v7.4 s0 %  Snare 38 v7.1 s0 %
ladles:   Hearty v4.0 s74 %  Spicy v4.7 s6 %  Soothing v7.0 s14 %  Sour fog v4.7 s5 %  Stench v3.4 s0 %

== iteration 47/48  (1,418,608 sims, 86.5s)
raid fraction 0.550  sofra fell 8 %  units/half 16.5  builds/half 5.8  sofra 30
value spread: units CV 0.21  defences CV 0.11  ladles CV 0.28
units:    Grunt 17 v7.0 s10 %  Shieldbearer 48 v6.4 s17 %  Runner 20 v5.7 s10 %  Cook 57 v4.7 s18 %  Healer 33 v4.4 s2 %  Golem 62 v5.7 s7 %  Saboteur 29 v4.7 s17 %  Cloud 24 v6.3 s15 %  Brute 59 v5.5 s2 %  Bomber 28 v2.9 s2 %
defences: Catapult 53 v6.7 s57 %  Barricade 95 v7.6 s0 %  Spire 95 v7.1 s0 %  Griddle 43 v7.6 s0 %  Breeze 80 v5.5 s27 %  Puddle 31 v6.4 s2 %  Tower 88 v7.1 s0 %  Barrel 32 v7.7 s14 %  Tower 47 v8.2 s0 %  Snare 38 v6.7 s0 %
ladles:   Hearty v3.8 s74 %  Spicy v4.0 s5 %  Soothing v7.2 s13 %  Sour fog v4.6 s8 %  Stench v3.6 s0 %

== iteration 48/48  (1,448,704 sims, 88.2s)
raid fraction 0.545  sofra fell 8 %  units/half 16.8  builds/half 5.7  sofra 30
value spread: units CV 0.20  defences CV 0.13  ladles CV 0.27
units:    Grunt 17 v7.4 s13 %  Shieldbearer 49 v7.0 s19 %  Runner 20 v6.2 s10 %  Cook 56 v4.6 s17 %  Healer 32 v4.7 s1 %  Golem 62 v6.1 s6 %  Saboteur 28 v5.6 s16 %  Cloud 24 v6.8 s14 %  Brute 59 v5.7 s1 %  Bomber 27 v3.5 s2 %
defences: Catapult 55 v6.8 s58 %  Barricade 95 v7.5 s0 %  Spire 95 v7.2 s0 %  Griddle 43 v7.6 s0 %  Breeze 78 v5.6 s28 %  Puddle 31 v5.8 s1 %  Tower 88 v7.1 s0 %  Barrel 32 v7.6 s13 %  Tower 48 v8.6 s0 %  Snare 38 v6.2 s0 %
ladles:   Hearty v3.7 s75 %  Spicy v4.3 s5 %  Soothing v7.4 s13 %  Sour fog v5.0 s7 %  Stench v4.3 s0 %
calibration 1: sofra 30 -> raid fraction 0.521, fell 7 %
calibration 2: sofra 30 -> raid fraction 0.559, fell 9 %
calibration 3: sofra 30 -> raid fraction 0.561, fell 8 %
```
