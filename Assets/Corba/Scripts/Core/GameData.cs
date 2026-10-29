using UnityEngine;

namespace Corba
{
    public enum MatchPhase : byte { Lobby = 0, Prep = 1, Raid = 2, HalfResult = 3, MatchOver = 4 }

    public struct SoupInfo
    {
        public string Name;
        public string English;
        public Color Identity;
        public Color Liquid;
        public float SpillMult;
        public float MoveMult;
    }

    /// <summary>Unity-side presentation data and cook feel. Combat numbers live in <see cref="Balance"/>.</summary>
    public static class GameData
    {
        public const float PrepSeconds = Balance.PrepSeconds;
        public const float RaidSeconds = Balance.RaidSeconds;
        public const float HalfResultSeconds = 7f;
        public const float SpawnInterval = Balance.SpawnInterval;

        public const float PotCapacity = Balance.PotCapacity;
        public const float ComebackCapacityBonus = Balance.ComebackCapacityBonus;
        public static float SofraHunger => Balance.SofraHunger;

        public const float CookSpeed = Balance.CookSpeed;
        public const float SprintMult = Balance.SprintMult;
        public const float CookAccel = 26f;
        public const float SloshStiffness = 34f;
        public const float SloshDamping = 5.5f;
        public const float SloshAccelGain = 0.9f;
        public const float SprintSloshGain = 1.7f;
        public const float SloshRim = 1f;
        public const float SpillRate = 55f;
        public const float BumpImpulse = 1.5f;
        public const float ShoveImpulse = 3.2f;
        public const float ShoveSelfImpulse = 0.6f;
        public const float ShoveRange = 1.9f;
        public const float ShoveCooldown = 6f;
        public const float ShoveKnockback = 2f;
        public const float IskembeAuraRadius = 4f;
        public const float IskembeAuraSlow = 0.15f;
        public const float InteractRange = 2.6f;

        public const float TowerHp = Balance.TowerHp;
        public const float RoadWidth = 4.2f;
        public const int MaxLevel = Balance.MaxLevel;
        public static float[] LevelStatMult => Balance.LevelStatMult;
        public const int RecruitableUnitCount = Balance.RecruitableUnitCount;

        public static readonly SoupInfo[] Soups =
        {
            new SoupInfo { Name = "Mercimek", English = "Red Lentil", Identity = Hex("F2B33D"), Liquid = Hex("E8962E") },
            new SoupInfo { Name = "Ezogelin", English = "Spicy Bulgur", Identity = Hex("D9442B"), Liquid = Hex("C9502B") },
            new SoupInfo { Name = "Yayla", English = "Yoghurt Mint", Identity = Hex("7CC98E"), Liquid = Hex("EFE9D2") },
            new SoupInfo { Name = "Tarhana", English = "Fermented", Identity = Hex("C77A35"), Liquid = Hex("B5562F") },
            new SoupInfo { Name = "İşkembe", English = "Garlic Tripe", Identity = Hex("A88FC9"), Liquid = Hex("E6DCC4") },
        };

        static GameData()
        {
            for (int i = 0; i < Soups.Length; i++)
            {
                Soups[i].SpillMult = Balance.SoupSpillMult[i];
                Soups[i].MoveMult = Balance.SoupMoveMult[i];
            }
        }

        public static DefenceInfo[] Defences => Balance.Defences;
        public static UnitInfo[] Units => Balance.Units;
        public static DefenceInfo Defence(DefenceId id) => Balance.Defence(id);
        public static UnitInfo Unit(UnitId id) => Balance.Unit(id);
        public static SoupInfo Soup(SoupType s) => Soups[(int)s];
        public static DefenceId DefenceOption(SoupType soup, int option) => Balance.DefenceOption(soup, option);
        public static UnitId UnitOption(SoupType soup, int option) => Balance.UnitOption(soup, option);
        public static int UpgradeCost(DefenceId id, int currentLevel) => Balance.UpgradeCost(id, currentLevel);

        public static Vector2 Flat(Vector3 v) => new Vector2(v.x, v.z);
        public static System.Numerics.Vector2 Sim(Vector3 v) => new System.Numerics.Vector2(v.x, v.z);
        public static Vector3 World(System.Numerics.Vector2 v, float y) => new Vector3(v.X, y, v.Y);

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
