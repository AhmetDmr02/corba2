using UnityEngine;

namespace Corba
{
    public enum MatchPhase : byte { Lobby = 0, Prep = 1, Raid = 2, HalfResult = 3, MatchOver = 4 }

    public struct SoupInfo
    {
        public string Name;
        public string English;
        /// <summary>One-word role shown everywhere the soup appears.</summary>
        public string Keyword;
        /// <summary>UI sprite with the soup's shape glyph, so soups read without relying on colour alone.</summary>
        public string Glyph;
        public string Carry;
        public string Theme;
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
            new SoupInfo { Name = "Mercimek", English = "Red Lentil", Keyword = "Sturdy", Glyph = "glyph_shield",
                Carry = "Thick: hard to spill, a little slow.", Theme = "Tough walls and armoured raiders.",
                Identity = Hex("F5B92E"), Liquid = Hex("F0A92A") },
            new SoupInfo { Name = "Ezogelin", English = "Spicy Bulgur", Keyword = "Spicy", Glyph = "glyph_flame",
                Carry = "Normal soup, no quirks.", Theme = "Burning damage and fast raiders.",
                Identity = Hex("E8412C"), Liquid = Hex("D0331C") },
            new SoupInfo { Name = "Yayla", English = "Yoghurt Mint", Keyword = "Cooling", Glyph = "glyph_leaf",
                Carry = "Thin: spills easily, but you move faster.", Theme = "Slows on defence, healing on attack.",
                Identity = Hex("47C46B"), Liquid = Hex("B8EDB4") },
            new SoupInfo { Name = "Tarhana", English = "Fermented", Keyword = "Sour", Glyph = "glyph_bubbles",
                Carry = "Thick and forgiving.", Theme = "Weakens enemies: sour stacks, poison, tower slow.",
                Identity = Hex("9A57D6"), Liquid = Hex("8B47C4") },
            new SoupInfo { Name = "İşkembe", English = "Garlic Tripe", Keyword = "Pungent", Glyph = "glyph_stink",
                Carry = "Stinks: rival cooks near you move slower.", Theme = "Disruption: fear, roots, stuns, demolition.",
                Identity = Hex("38A3DB"), Liquid = Hex("7FC4EA") },
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
