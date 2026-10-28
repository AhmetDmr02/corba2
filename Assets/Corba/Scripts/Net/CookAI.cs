using System.Collections.Generic;
using UnityEngine;

namespace Corba
{
    /// <summary>Server-side brain for a bot cook. Drives the same ChefController input path a human uses.</summary>
    [RequireComponent(typeof(ChefController))]
    public class CookAI : MonoBehaviour
    {
        enum Goal { None, Fill, Pour, Build }

        ChefController chef;
        Goal goal;
        Station targetStation;
        BuildPad targetPad;
        int buildOption;
        SoupType wantSoup;
        List<Vector3> path = new List<Vector3>();
        int pathIndex;
        Vector3 pathTarget;
        float repathTimer, stuckTimer, recruitTimer, thinkTimer;
        Vector3 lastPos;
        bool pressInteract, pressOpt1, pressOpt2, pressShove;
        float actCooldown;
        readonly System.Random rng = new System.Random();

        MatchDirector D => MatchDirector.Instance;
        CorbaMap Map => CorbaMap.Instance;
        int Slot => chef.Slot.Value;
        bool Defending => D.IsDefender(Slot);

        void Awake()
        {
            chef = GetComponent<ChefController>();
            chef.Brain = Think;
            chef.Acted += _ => goal = Goal.None;
        }

        CookInput Think()
        {
            var input = new CookInput();
            if (D == null || Map == null || !D.InPlay || D.Paused.Value) return input;
            float dt = Time.deltaTime;
            actCooldown -= dt;
            thinkTimer -= dt;

            if (!Defending)
            {
                recruitTimer -= dt;
                if (recruitTimer <= 0f)
                {
                    recruitTimer = 0.4f;
                    Recruit();
                }
            }

            if (goal == Goal.None || thinkTimer <= 0f)
            {
                thinkTimer = 1.5f;
                Decide();
            }

            Vector3 dest = Destination();
            float reach = goal == Goal.Pour ? GameData.InteractRange + 1.2f : goal == Goal.Build ? GameData.InteractRange * 0.7f : GameData.InteractRange * 0.75f;
            float dist = CorbaMap.FlatDist(transform.position, dest);
            if (dist <= reach)
            {
                if (actCooldown <= 0f)
                {
                    actCooldown = 0.6f;
                    if (goal == Goal.Build && D.PadBuild(targetPad.Index) == null)
                    {
                        if (buildOption == 0) input.Option1 = true; else input.Option2 = true;
                    }
                    else input.Interact = true;
                }
                MaybeShove(ref input);
                return input;
            }

            repathTimer -= dt;
            if (path.Count == 0 || repathTimer <= 0f || CorbaMap.FlatDist(pathTarget, dest) > 1f)
            {
                path = Map.FindPath(transform.position, dest);
                pathIndex = 0;
                pathTarget = dest;
                repathTimer = 3f;
            }

            Vector3 waypoint = dest;
            while (pathIndex < path.Count && CorbaMap.FlatDist(transform.position, path[pathIndex]) < 0.9f) pathIndex++;
            if (pathIndex < path.Count) waypoint = path[pathIndex];
            Vector3 dir = waypoint - transform.position;
            dir.y = 0f;

            bool carrying = chef.Soup.Value >= 0 && chef.DisplayFill > 5f;
            float throttle = 1f;
            if (carrying)
            {
                float turn = Vector3.Angle(chef.Velocity, dir);
                throttle = chef.Velocity.sqrMagnitude > 1f && turn > 50f ? 0.45f : 0.85f;
                if (chef.SloshVector.magnitude > 0.75f) throttle *= 0.5f;
            }
            input.Move = dir.normalized * throttle;
            input.Sprint = !carrying && dist > 6f;

            stuckTimer += dt;
            if (stuckTimer > 0.8f)
            {
                if (CorbaMap.FlatDist(lastPos, transform.position) < 0.5f)
                {
                    sidestepUntil = Time.time + 0.7f;
                    sidestep = Quaternion.Euler(0, rng.Next(2) == 0 ? 100 : -100, 0) * dir.normalized;
                    repathTimer = 0.7f;
                }
                lastPos = transform.position;
                stuckTimer = 0f;
            }
            if (Time.time < sidestepUntil) input.Move = sidestep;

            MaybeShove(ref input);
            return input;
        }

        float sidestepUntil;
        Vector3 sidestep;

        void MaybeShove(ref CookInput input)
        {
            var rival = ChefController.BySlot[1 - Slot];
            if (rival == null || !chef.ShoveReady) return;
            Vector3 to = rival.transform.position - transform.position;
            to.y = 0f;
            if (to.magnitude > GameData.ShoveRange * 0.9f) return;
            if (Vector3.Dot(transform.forward, to.normalized) < 0.4f) return;
            bool worth = rival.DisplayFill > 35f && chef.DisplayFill < 40f;
            if (worth && rng.NextDouble() < 0.5) input.Shove = true;
        }

        Vector3 Destination()
        {
            switch (goal)
            {
                case Goal.Fill: return targetStation.Position;
                case Goal.Pour: return Map.Ocak.Position;
                case Goal.Build: return targetPad.TowerPosition;
                default: return transform.position;
            }
        }

        void Decide()
        {
            bool hasSoup = chef.Soup.Value >= 0 && chef.DisplayFill > 1f;
            if (!Defending)
            {
                if (hasSoup && chef.DisplayFill > 20f)
                {
                    goal = Goal.Pour;
                    return;
                }
                wantSoup = AttackSoup();
                SetFill(StationKind.AttackerCauldron, wantSoup);
                return;
            }

            if (hasSoup)
            {
                var soup = (SoupType)chef.Soup.Value;
                if (PickBuild(soup, chef.DisplayFill)) return;
            }
            wantSoup = DefenceSoup();
            if (hasSoup && chef.Soup.Value == (int)wantSoup && chef.DisplayFill > 90f && PickBuild(wantSoup, chef.DisplayFill)) return;
            SetFill(StationKind.DefenderCauldron, wantSoup);
        }

        void SetFill(StationKind kind, SoupType soup)
        {
            goal = Goal.Fill;
            targetStation = Map.Stations.Find(s => s.Kind == kind && s.Soup == soup);
        }

        bool PickBuild(SoupType soup, float fill)
        {
            BuildPad best = null;
            float bestScore = float.MinValue;
            int bestOption = 0;
            Vector3 kitchen = Map.CellCenter(Map.KitchenCell);
            foreach (var pad in Map.Pads)
            {
                var existing = D.PadBuild(pad.Index);
                float travel = CorbaMap.FlatDist(kitchen, pad.TowerPosition);
                float progress = pad.RoadDistance / Mathf.Max(1f, Map.RoadLength);
                if (existing == null)
                {
                    int option = ChooseOption(soup, pad);
                    var info = GameData.Defence(GameData.DefenceOption(soup, option));
                    if (fill + 0.01f < info.Cost) continue;
                    float score = 10f - travel * 0.12f + (pad.HighGround && info.Kind == DefenceKind.Tower ? 4f : 0f) + (progress > 0.3f && progress < 0.85f ? 3f : 0f);
                    if (score > bestScore) { bestScore = score; best = pad; bestOption = option; }
                }
                else if (existing.Info.Soup == soup && existing.Level.Value < GameData.MaxLevel)
                {
                    int cost = GameData.UpgradeCost(existing.Info.Id, existing.Level.Value);
                    if (fill + 0.01f < cost) continue;
                    float score = 4f - travel * 0.12f;
                    if (score > bestScore) { bestScore = score; best = pad; bestOption = 0; }
                }
            }
            if (best == null) return false;
            goal = Goal.Build;
            targetPad = best;
            buildOption = bestOption;
            return true;
        }

        int ChooseOption(SoupType soup, BuildPad pad)
        {
            int towers = 0, traps = 0;
            foreach (var b in DefenceBuild.All)
                if (b.Info.Kind == DefenceKind.Tower) towers++; else traps++;
            if (pad.HighGround) return 0;
            return traps * 2 < towers ? 1 : 0;
        }

        int AttackerBank(SoupType s) => D.BankOf(D.AttackerSlot, s);

        SoupType DefenceSoup()
        {
            var weights = new float[5] { 1.2f, 1.1f, 0.8f, 0.8f, 0.7f };
            weights[(int)SoupType.Yayla] += AttackerBank(SoupType.Ezogelin) * 0.02f + UnitCount(UnitId.Runner) * 0.4f;
            weights[(int)SoupType.Mercimek] += AttackerBank(SoupType.Mercimek) * 0.02f + UnitCount(UnitId.Grunt) * 0.2f;
            weights[(int)SoupType.Tarhana] += (AttackerBank(SoupType.Yayla) + AttackerBank(SoupType.Iskembe)) * 0.02f + UnitCount(UnitId.Golem) * 0.5f + UnitCount(UnitId.Brute) * 0.5f;
            weights[(int)SoupType.Ezogelin] += AttackerBank(SoupType.Tarhana) * 0.02f + UnitCount(UnitId.Saboteur) * 0.4f;
            weights[(int)SoupType.Iskembe] += UnitCount(UnitId.Runner) * 0.2f;
            foreach (var b in DefenceBuild.All) weights[(int)b.Info.Soup] -= 0.35f;
            return WeightedPick(weights);
        }

        SoupType AttackSoup()
        {
            int towers = 0, traps = 0, slows = 0;
            foreach (var b in DefenceBuild.All)
            {
                if (b.Info.Kind == DefenceKind.Tower) towers++; else traps++;
                if (b.Info.Id == DefenceId.Mint || b.Info.Id == DefenceId.Yogurt) slows++;
            }
            var weights = new float[5] { 1.5f, 1.0f, 0.6f, 0.6f, 0.6f };
            weights[(int)SoupType.Tarhana] += traps * 0.6f;
            weights[(int)SoupType.Iskembe] += towers * 0.35f;
            weights[(int)SoupType.Yayla] += slows * 0.7f;
            weights[(int)SoupType.Ezogelin] += towers * 0.15f;
            return WeightedPick(weights);
        }

        SoupType WeightedPick(float[] w)
        {
            float total = 0f;
            for (int i = 0; i < w.Length; i++) { w[i] = Mathf.Max(0.05f, w[i]); total += w[i]; }
            float r = (float)rng.NextDouble() * total;
            for (int i = 0; i < w.Length; i++)
            {
                r -= w[i];
                if (r <= 0f) return (SoupType)i;
            }
            return SoupType.Mercimek;
        }

        static int UnitCount(UnitId id)
        {
            int n = 0;
            foreach (var u in UnitAgent.All) if (u.Kind.Value == id) n++;
            return n;
        }

        void Recruit()
        {
            int towers = 0, traps = 0;
            foreach (var b in DefenceBuild.All)
                if (b.Info.Kind == DefenceKind.Tower) towers++; else traps++;
            for (int s = 0; s < 5; s++)
            {
                var soup = (SoupType)s;
                int bank = D.BankOf(Slot, soup);
                if (bank <= 0) continue;
                UnitId pick;
                switch (soup)
                {
                    case SoupType.Mercimek: pick = bank >= 45 && rng.NextDouble() < 0.35 ? UnitId.Shield : UnitId.Grunt; break;
                    case SoupType.Ezogelin: pick = towers >= 3 && bank >= 40 && rng.NextDouble() < 0.5 ? UnitId.Ember : UnitId.Runner; break;
                    case SoupType.Yayla: pick = bank >= 50 && rng.NextDouble() < 0.6 ? UnitId.Golem : UnitId.Healer; break;
                    case SoupType.Tarhana: pick = traps >= 2 || rng.NextDouble() < 0.5 ? UnitId.Saboteur : UnitId.Cloud; break;
                    default: pick = bank >= 60 && rng.NextDouble() < 0.5 ? UnitId.Brute : UnitId.Bomber; break;
                }
                if (bank < GameData.Unit(pick).Cost)
                {
                    var alt = GameData.UnitOption(soup, (int)pick % 2 == 0 ? 1 : 0);
                    if (bank >= GameData.Unit(alt).Cost) pick = alt; else continue;
                }
                if (D.ServerRecruit(Slot, pick)) return;
            }
        }
    }
}
