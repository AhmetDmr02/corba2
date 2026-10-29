# Corba balance audit

Generated 2026-10-29 15:40 from the costs currently in `BalanceTuning.g.cs`. All numbers are attacker raid score as a fraction of the Sofra (higher is better for the attacker).

## 1. How varied is the meta?

Elite vs elite raid score: **50 %**.

"Effective options" is 1 / sum of squared spend shares: 10 means every option is used equally, 1 means one option is everything.

| | Effective options | Out of |
|---|---|---|
| Attack units | 8.0 | 10 |
| Defences | 5.5 | 10 |
| Attacker soups | 4.7 | 5 |
| Defender soups | 3.8 | 5 |
| Ladle soups | 3.1 | 5 |

| Soup | Attacker spend | Defender spend | Ladle tosses |
|---|---|---|---|
| Mercimek | 24 % | 31 % | 48 % |
| Ezogelin | 13 % | 0 % | 13 % |
| Yayla | 27 % | 16 % | 3 % |
| Tarhana | 22 % | 23 % | 24 % |
| Iskembe | 15 % | 31 % | 12 % |

## 2. Can a dedicated counter-strategy break the meta?

- Average elite attacker scores **49 %**; the best counter-attacker found scores **59 %** (+10 points).
- Average elite defender concedes **49 %**; the best counter-defender found concedes **46 %** (-3 points).
- Best counter-attacker spends most on: Sour Cloud 29 %, Pepper Runner 28 %, Ferment Saboteur 21 %. Waves of 2, escorts 0 % of raid trips.
- Best counter-defender prefers: Lentil Barricade 39 %, Garlic Tower 26 %, Mint Breeze 20 %.

A gap under ~10 points means the meta is hard to exploit. A big gap means the elites are beatable by a specific plan, which is either a balance hole or a sign the bots are not playing well.

## 3. Can one soup carry a side on its own?

| Soup | Attacker using only this soup | Defender using only this soup |
|---|---|---|
| Mercimek | 45 % (elite 49 %) | concedes 52 % (elite 49 %) |
| Ezogelin | 50 % (elite 49 %) | concedes 64 % (elite 49 %) |
| Yayla | 46 % (elite 49 %) | concedes 58 % (elite 49 %) |
| Tarhana | 56 % (elite 49 %) | concedes 57 % (elite 49 %) |
| Iskembe | 39 % (elite 49 %) | concedes 54 % (elite 49 %) |

A single-soup plan should do clearly worse than a mixed elite. If one matches or beats the elites, that soup is a solved strategy.

_Audit took 24s._
