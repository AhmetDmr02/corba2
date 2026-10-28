using UnityEngine;

namespace Corba
{
    public enum SoupType { Mercimek = 0, Ezogelin = 1, Yayla = 2, Tarhana = 3, Iskembe = 4 }

    public enum MatchPhase : byte { Lobby = 0, Prep = 1, Raid = 2, HalfResult = 3, MatchOver = 4 }

    public enum DefenceKind : byte { Tower, Trap, Blocker }

    public enum DefenceId : byte { Catapult, Barricade, Chili, Griddle, Mint, Yogurt, Ferment, Barrel, Garlic, Snare }

    public enum UnitId : byte { Grunt, Shield, Runner, Ember, Healer, Golem, Saboteur, Cloud, Brute, Bomber, MiniGolem }

    public struct SoupInfo
    {
        public string Name;
        public string English;
        public Color Identity;
        public Color Liquid;
        public float SpillMult;
        public float MoveMult;
    }

    public struct DefenceInfo
    {
        public DefenceId Id;
        public SoupType Soup;
        public DefenceKind Kind;
        public string Name;
        public string English;
        public string Blurb;
        public int Cost;
        public float Damage;
        public float Interval;
        public float Range;
        public float Radius;
        public float Duration;
        public float Magnitude;
        public float Hp;
        public string Model;
    }

    public struct UnitInfo
    {
        public UnitId Id;
        public SoupType Soup;
        public string Name;
        public string English;
        public string Blurb;
        public int Cost;
        public float Hp;
        public float Speed;
        public int Bite;
        public float Armor;
        public string Model;
        public float Scale;
    }

    public static class GameData
    {
        public const float PrepSeconds = 50f;
        public const float RaidSeconds = 80f;
        public const float HalfResultSeconds = 7f;
        public const float SpawnInterval = 0.6f;

        public const float PotCapacity = 100f;
        public const float ComebackCapacityBonus = 0.15f;
        public const float SofraHunger = 120f;

        public const float CookSpeed = 6.2f;
        public const float SprintMult = 1.45f;
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

        public const float TowerHp = 300f;
        public const float PadOffset = 3.4f;
        public const float RoadWidth = 4.2f;

        public static readonly float[] UpgradeCostMult = { 1f, 0.8f, 1f };
        public static readonly float[] LevelStatMult = { 1f, 1.4f, 1.9f };
        public const int MaxLevel = 3;

        public static readonly SoupInfo[] Soups =
        {
            new SoupInfo { Name = "Mercimek", English = "Red Lentil", Identity = Hex("F2B33D"), Liquid = Hex("E8962E"), SpillMult = 0.70f, MoveMult = 0.92f },
            new SoupInfo { Name = "Ezogelin", English = "Spicy Bulgur", Identity = Hex("D9442B"), Liquid = Hex("C9502B"), SpillMult = 1.00f, MoveMult = 1.00f },
            new SoupInfo { Name = "Yayla", English = "Yoghurt Mint", Identity = Hex("7CC98E"), Liquid = Hex("EFE9D2"), SpillMult = 1.30f, MoveMult = 1.10f },
            new SoupInfo { Name = "Tarhana", English = "Fermented", Identity = Hex("C77A35"), Liquid = Hex("B5562F"), SpillMult = 0.85f, MoveMult = 0.97f },
            new SoupInfo { Name = "İşkembe", English = "Garlic Tripe", Identity = Hex("A88FC9"), Liquid = Hex("E6DCC4"), SpillMult = 1.10f, MoveMult = 1.00f },
        };

        public static readonly DefenceInfo[] Defences =
        {
            new DefenceInfo { Id = DefenceId.Catapult, Soup = SoupType.Mercimek, Kind = DefenceKind.Tower, Name = "Kepçe Mancınığı", English = "Ladle Catapult",
                Blurb = "Lobs lentil splash. Good vs swarms.", Cost = 70, Damage = 40, Interval = 1.6f, Range = 9, Radius = 1.8f, Hp = TowerHp, Model = "d_catapult" },
            new DefenceInfo { Id = DefenceId.Barricade, Soup = SoupType.Mercimek, Kind = DefenceKind.Blocker, Name = "Mercimek Seddi", English = "Lentil Barricade",
                Blurb = "600 HP wall across the road.", Cost = 50, Hp = 600, Radius = 1.2f, Model = "d_barricade" },
            new DefenceInfo { Id = DefenceId.Chili, Soup = SoupType.Ezogelin, Kind = DefenceKind.Tower, Name = "Pul Biber Kulesi", English = "Chili Spire",
                Blurb = "Rapid shots that set targets burning.", Cost = 80, Damage = 12, Interval = 0.5f, Range = 8, Duration = 3, Magnitude = 6, Hp = TowerHp, Model = "d_chili" },
            new DefenceInfo { Id = DefenceId.Griddle, Soup = SoupType.Ezogelin, Kind = DefenceKind.Trap, Name = "Kızgın Sac", English = "Hot Griddle",
                Blurb = "Scorches everything standing on it.", Cost = 45, Damage = 60, Radius = 1.6f, Model = "d_griddle" },
            new DefenceInfo { Id = DefenceId.Mint, Soup = SoupType.Yayla, Kind = DefenceKind.Tower, Name = "Nane Esintisi", English = "Mint Breeze",
                Blurb = "Chilling pulse that slows the area.", Cost = 70, Damage = 10, Interval = 2f, Range = 6, Duration = 2.5f, Magnitude = 0.4f, Hp = TowerHp, Model = "d_mint" },
            new DefenceInfo { Id = DefenceId.Yogurt, Soup = SoupType.Yayla, Kind = DefenceKind.Trap, Name = "Yoğurt Gölü", English = "Yoghurt Puddle",
                Blurb = "Sticky puddle, heavy slow.", Cost = 40, Radius = 1.6f, Duration = 1f, Magnitude = 0.55f, Model = "d_yogurt" },
            new DefenceInfo { Id = DefenceId.Ferment, Soup = SoupType.Tarhana, Kind = DefenceKind.Tower, Name = "Ekşi Maya Kulesi", English = "Sour Ferment Tower",
                Blurb = "Sour stacks make targets take more damage.", Cost = 85, Damage = 20, Interval = 1f, Range = 10, Duration = 5, Magnitude = 0.08f, Hp = TowerHp, Model = "d_ferment" },
            new DefenceInfo { Id = DefenceId.Barrel, Soup = SoupType.Tarhana, Kind = DefenceKind.Trap, Name = "Tarhana Fıçısı", English = "Ferment Barrel",
                Blurb = "Bursts into a poison cloud. Re-arms.", Cost = 55, Damage = 25, Interval = 15, Range = 2, Radius = 4, Duration = 6, Model = "d_barrel" },
            new DefenceInfo { Id = DefenceId.Garlic, Soup = SoupType.Iskembe, Kind = DefenceKind.Tower, Name = "Sarımsak Kulesi", English = "Garlic Tower",
                Blurb = "Stench pulse that drives units back.", Cost = 90, Damage = 15, Interval = 4, Range = 5, Magnitude = 4, Hp = TowerHp, Model = "d_garlic" },
            new DefenceInfo { Id = DefenceId.Snare, Soup = SoupType.Iskembe, Kind = DefenceKind.Trap, Name = "Sirke Kapanı", English = "Vinegar Snare",
                Blurb = "Roots up to 3 units. Re-arms.", Cost = 50, Damage = 40, Interval = 8, Radius = 1.6f, Duration = 2.5f, Magnitude = 3, Model = "d_snare" },
        };

        public static readonly UnitInfo[] Units =
        {
            new UnitInfo { Id = UnitId.Grunt, Soup = SoupType.Mercimek, Name = "Kaşıkçı Er", English = "Spoon Grunt", Blurb = "Cheap and numerous.",
                Cost = 15, Hp = 90, Speed = 2.6f, Bite = 2, Model = "u_grunt", Scale = 1f },
            new UnitInfo { Id = UnitId.Shield, Soup = SoupType.Mercimek, Name = "Kazan Kalkanlı", English = "Cauldron Shieldbearer", Blurb = "Armoured. Taunts towers.",
                Cost = 45, Hp = 380, Speed = 1.8f, Bite = 4, Armor = 0.25f, Model = "u_shield", Scale = 1.15f },
            new UnitInfo { Id = UnitId.Runner, Soup = SoupType.Ezogelin, Name = "Biber Koşucusu", English = "Pepper Runner", Blurb = "Fast. Sprints when hit.",
                Cost = 20, Hp = 60, Speed = 4.4f, Bite = 2, Model = "u_runner", Scale = 0.9f },
            new UnitInfo { Id = UnitId.Ember, Soup = SoupType.Ezogelin, Name = "Közcü Aşçı", English = "Ember Cook", Blurb = "Throws embers at towers.",
                Cost = 40, Hp = 160, Speed = 2.4f, Bite = 3, Model = "u_ember", Scale = 1f },
            new UnitInfo { Id = UnitId.Healer, Soup = SoupType.Yayla, Name = "Nane Şifacısı", English = "Mint Healer", Blurb = "Heals nearby allies.",
                Cost = 35, Hp = 110, Speed = 2.4f, Bite = 1, Model = "u_healer", Scale = 1f },
            new UnitInfo { Id = UnitId.Golem, Soup = SoupType.Yayla, Name = "Ayran Golemi", English = "Ayran Golem", Blurb = "Slow-immune. Splits on death.",
                Cost = 50, Hp = 300, Speed = 2.0f, Bite = 4, Model = "u_golem", Scale = 1.2f },
            new UnitInfo { Id = UnitId.Saboteur, Soup = SoupType.Tarhana, Name = "Maya Sabotajcısı", English = "Ferment Saboteur", Blurb = "Disarms traps.",
                Cost = 30, Hp = 100, Speed = 2.9f, Bite = 2, Model = "u_saboteur", Scale = 0.95f },
            new UnitInfo { Id = UnitId.Cloud, Soup = SoupType.Tarhana, Name = "Ekşi Bulut", English = "Sour Cloud", Blurb = "Slows nearby towers.",
                Cost = 40, Hp = 130, Speed = 2.2f, Bite = 2, Model = "u_cloud", Scale = 1f },
            new UnitInfo { Id = UnitId.Brute, Soup = SoupType.Iskembe, Name = "Sarımsak Devi", English = "Garlic Brute", Blurb = "Huge. Smashes towers.",
                Cost = 60, Hp = 520, Speed = 1.7f, Bite = 6, Model = "u_brute", Scale = 1.35f },
            new UnitInfo { Id = UnitId.Bomber, Soup = SoupType.Iskembe, Name = "Sirke Bombacısı", English = "Vinegar Bomber", Blurb = "Blows up a tower.",
                Cost = 35, Hp = 120, Speed = 3.0f, Bite = 3, Model = "u_bomber", Scale = 1f },
            new UnitInfo { Id = UnitId.MiniGolem, Soup = SoupType.Yayla, Name = "Minik Golem", English = "Mini Golem", Blurb = "",
                Cost = 0, Hp = 80, Speed = 2.6f, Bite = 1, Model = "u_golem", Scale = 0.65f },
        };

        public const int RecruitableUnitCount = 10;

        public const float ShieldTauntRange = 9f;
        public const float RunnerBurstMult = 1.3f;
        public const float RunnerBurstTime = 3f;
        public const float EmberRange = 6f;
        public const float EmberDps = 18f;
        public const float HealerRadius = 4f;
        public const float HealerHps = 12f;
        public const float SaboteurDisarmTime = 10f;
        public const int SaboteurCharges = 2;
        public const float CloudAuraRadius = 6f;
        public const float CloudAuraSlow = 0.35f;
        public const float BruteRange = 4f;
        public const float BruteDps = 30f;
        public const float BomberSeekRange = 5f;
        public const float BomberDamage = 120f;
        public const float BomberDisableRadius = 4f;
        public const float BomberDisableTime = 5f;
        public const float GruntBarricadeDps = 15f;
        public const float BarricadeOtherDps = 10f;

        public static DefenceInfo Defence(DefenceId id) => Defences[(int)id];
        public static UnitInfo Unit(UnitId id) => Units[(int)id];
        public static SoupInfo Soup(SoupType s) => Soups[(int)s];

        public static DefenceId DefenceOption(SoupType soup, int option) => (DefenceId)((int)soup * 2 + option);
        public static UnitId UnitOption(SoupType soup, int option) => (UnitId)((int)soup * 2 + option);

        public static int UpgradeCost(DefenceId id, int currentLevel)
        {
            if (currentLevel >= MaxLevel) return int.MaxValue;
            return Mathf.RoundToInt(Defence(id).Cost * UpgradeCostMult[currentLevel]);
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
