namespace Corba
{
    public enum SoupType { Mercimek = 0, Ezogelin = 1, Yayla = 2, Tarhana = 3, Iskembe = 4 }

    public enum DefenceKind : byte { Tower, Trap, Blocker }

    public enum DefenceId : byte { Catapult, Barricade, Chili, Griddle, Mint, Yogurt, Ferment, Barrel, Garlic, Snare }

    public enum UnitId : byte { Grunt, Shield, Runner, Ember, Healer, Golem, Saboteur, Cloud, Brute, Bomber, MiniGolem }

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

    public struct LadleInfo
    {
        public string Name;
        public string Blurb;
        public float Duration;
        public float Magnitude;
    }

    /// <summary>
    /// Every gameplay number lives here. Pure C# so the Unity server and the headless Monte Carlo tools share it.
    /// Tuned overrides are applied from BalanceTuning.g.cs, which Tools/CorbaSim regenerates.
    /// </summary>
    public static partial class Balance
    {
        public const float PrepSeconds = 50f;
        public const float RaidSeconds = 90f;
        public const float SimStep = 0.05f;

        public const float PotCapacity = 100f;
        public const float ComebackCapacityBonus = 0.15f;
        public static float SofraHunger = 60f;
        // Global raider toughness: the tuner's knob for attack versus defence, so the Sofra can stay a readable size.
        public static float UnitHpScale = 1f;
        public const float CookSpeed = 6.2f;
        public const float SprintMult = 1.45f;

        public const float TowerHp = 300f;
        public const float HighGroundRangeMult = 1.25f;
        public const int MaxLevel = 3;
        public static readonly float[] UpgradeCostMult = { 1f, 0.8f, 1f };
        public static readonly float[] LevelStatMult = { 1f, 1.4f, 1.9f };

        public static readonly float[] SoupSpillMult = { 0.70f, 1.00f, 1.30f, 0.85f, 1.10f };
        public static readonly float[] SoupMoveMult = { 0.92f, 1.00f, 1.10f, 0.97f, 1.00f };

        // Wave control: queued raiders wait at the Ocak until sent, or until the queue is big or old enough.
        public const float SpawnInterval = 0.45f;
        public const int AutoWaveSize = 10;
        public const float AutoWaveSeconds = 14f;

        // Rally: raiders near the attacking cook march faster and shrug off damage.
        public static float RallyRadius = 5f;
        public static float RallySpeed = 0.2f;
        public static float RallyArmor = 0.15f;

        // Ladle toss: the attacker throws soup from the pot onto the battlefield.
        public static int LadleCost = 10;
        public const float LadleRange = 13f;
        public const float LadleRadius = 3.6f;
        public const float LadleCooldown = 1.2f;

        public static readonly LadleInfo[] Ladles =
        {
            new LadleInfo { Name = "Hearty", Blurb = "Raiders take much less damage.", Duration = 6f, Magnitude = 0.5f },
            new LadleInfo { Name = "Spicy", Blurb = "Raiders sprint past the towers.", Duration = 5f, Magnitude = 0.6f },
            new LadleInfo { Name = "Soothing", Blurb = "Heals and cleanses raiders.", Duration = 0f, Magnitude = 110f },
            new LadleInfo { Name = "Sour fog", Blurb = "Towers in the fog fire far slower.", Duration = 7f, Magnitude = 0.6f },
            new LadleInfo { Name = "Stench", Blurb = "Stuns towers, disarms traps; raiders ignore fear and roots.", Duration = 9f, Magnitude = 1.5f },
        };

        public static float ShieldTauntRange = 9f;
        public static float RunnerBurstMult = 1.3f;
        public static float RunnerBurstTime = 3f;
        public static float EmberRange = 6f;
        public static float EmberDps = 18f;
        public static float HealerRadius = 4f;
        public static float HealerHps = 12f;
        public static float SaboteurDisarmTime = 10f;
        public const int SaboteurCharges = 2;
        public static float CloudAuraRadius = 6f;
        public static float CloudAuraSlow = 0.35f;
        public static float BruteRange = 4f;
        public static float BruteDps = 22f;
        public static float BomberSeekRange = 5f;
        public static float BomberDamage = 120f;
        public static float BomberDisableRadius = 4f;
        public static float BomberDisableTime = 5f;
        public static float GruntBarricadeDps = 15f;
        public static float BarricadeOtherDps = 10f;
        public static float SourStackMax = 5f;
        // Control sets up kills: slowed, rooted or feared raiders take extra damage from everything.
        public static float ControlVulnerability = 0.2f;
        // Armour, Hearty and rally stack multiplicatively but never past this.
        public const float MaxDamageReduction = 0.6f;

        public static readonly DefenceInfo[] Defences =
        {
            new DefenceInfo { Id = DefenceId.Catapult, Soup = SoupType.Mercimek, Kind = DefenceKind.Tower, Name = "Kepçe Mancınığı", English = "Ladle Catapult",
                Blurb = "Lobs lentil splash. Good vs swarms.", Cost = 70, Damage = 40, Interval = 1.6f, Range = 9, Radius = 1.8f, Hp = TowerHp, Model = "d_catapult" },
            new DefenceInfo { Id = DefenceId.Barricade, Soup = SoupType.Mercimek, Kind = DefenceKind.Blocker, Name = "Mercimek Seddi", English = "Lentil Barricade",
                Blurb = "Wall across the road.", Cost = 50, Hp = 600, Radius = 1.2f, Model = "d_barricade" },
            new DefenceInfo { Id = DefenceId.Chili, Soup = SoupType.Ezogelin, Kind = DefenceKind.Tower, Name = "Pul Biber Kulesi", English = "Chili Spire",
                Blurb = "Rapid shots that set targets burning.", Cost = 80, Damage = 12, Interval = 0.5f, Range = 8, Duration = 3, Magnitude = 6, Hp = TowerHp, Model = "d_chili" },
            new DefenceInfo { Id = DefenceId.Griddle, Soup = SoupType.Ezogelin, Kind = DefenceKind.Trap, Name = "Kızgın Sac", English = "Hot Griddle",
                Blurb = "Scorches everything standing on it.", Cost = 45, Damage = 60, Radius = 1.6f, Model = "d_griddle" },
            new DefenceInfo { Id = DefenceId.Mint, Soup = SoupType.Yayla, Kind = DefenceKind.Tower, Name = "Nane Esintisi", English = "Mint Breeze",
                Blurb = "Chilling pulse that slows the area.", Cost = 70, Damage = 22, Interval = 2f, Range = 6, Duration = 2.5f, Magnitude = 0.4f, Hp = TowerHp, Model = "d_mint" },
            new DefenceInfo { Id = DefenceId.Yogurt, Soup = SoupType.Yayla, Kind = DefenceKind.Trap, Name = "Yoğurt Gölü", English = "Yoghurt Puddle",
                Blurb = "Sticky puddle, heavy slow.", Cost = 40, Radius = 1.6f, Duration = 1f, Magnitude = 0.55f, Model = "d_yogurt" },
            new DefenceInfo { Id = DefenceId.Ferment, Soup = SoupType.Tarhana, Kind = DefenceKind.Tower, Name = "Ekşi Maya Kulesi", English = "Sour Ferment Tower",
                Blurb = "Sour stacks make targets take more damage.", Cost = 85, Damage = 20, Interval = 1f, Range = 10, Duration = 5, Magnitude = 0.08f, Hp = TowerHp, Model = "d_ferment" },
            new DefenceInfo { Id = DefenceId.Barrel, Soup = SoupType.Tarhana, Kind = DefenceKind.Trap, Name = "Tarhana Fıçısı", English = "Ferment Barrel",
                Blurb = "Bursts into a poison cloud. Re-arms.", Cost = 55, Damage = 25, Interval = 15, Range = 2, Radius = 4, Duration = 6, Model = "d_barrel" },
            new DefenceInfo { Id = DefenceId.Garlic, Soup = SoupType.Iskembe, Kind = DefenceKind.Tower, Name = "Sarımsak Kulesi", English = "Garlic Tower",
                Blurb = "Stench pulse that drives units back.", Cost = 90, Damage = 30, Interval = 4, Range = 5, Magnitude = 4, Hp = TowerHp, Model = "d_garlic" },
            new DefenceInfo { Id = DefenceId.Snare, Soup = SoupType.Iskembe, Kind = DefenceKind.Trap, Name = "Sirke Kapanı", English = "Vinegar Snare",
                Blurb = "Roots up to 3 units. Re-arms.", Cost = 50, Damage = 40, Interval = 8, Radius = 1.6f, Duration = 2.5f, Magnitude = 3, Model = "d_snare" },
        };

        public static readonly UnitInfo[] Units =
        {
            new UnitInfo { Id = UnitId.Grunt, Soup = SoupType.Mercimek, Name = "Kaşıkçı Er", English = "Spoon Grunt", Blurb = "Cheap and numerous.",
                Cost = 15, Hp = 90, Speed = 3.6f, Bite = 2, Model = "u_grunt", Scale = 1f },
            new UnitInfo { Id = UnitId.Shield, Soup = SoupType.Mercimek, Name = "Kazan Kalkanlı", English = "Cauldron Shieldbearer", Blurb = "Armoured. Taunts towers.",
                Cost = 45, Hp = 380, Speed = 2.5f, Bite = 4, Armor = 0.25f, Model = "u_shield", Scale = 1.15f },
            new UnitInfo { Id = UnitId.Runner, Soup = SoupType.Ezogelin, Name = "Biber Koşucusu", English = "Pepper Runner", Blurb = "Fast. Sprints when hit.",
                Cost = 20, Hp = 60, Speed = 6.0f, Bite = 2, Model = "u_runner", Scale = 0.9f },
            new UnitInfo { Id = UnitId.Ember, Soup = SoupType.Ezogelin, Name = "Közcü Aşçı", English = "Ember Cook", Blurb = "Throws embers at towers.",
                Cost = 40, Hp = 160, Speed = 3.4f, Bite = 3, Model = "u_ember", Scale = 1f },
            new UnitInfo { Id = UnitId.Healer, Soup = SoupType.Yayla, Name = "Nane Şifacısı", English = "Mint Healer", Blurb = "Heals nearby allies.",
                Cost = 35, Hp = 110, Speed = 3.4f, Bite = 1, Model = "u_healer", Scale = 1f },
            new UnitInfo { Id = UnitId.Golem, Soup = SoupType.Yayla, Name = "Ayran Golemi", English = "Ayran Golem", Blurb = "Slow-immune. Splits on death.",
                Cost = 50, Hp = 300, Speed = 2.8f, Bite = 4, Model = "u_golem", Scale = 1.2f },
            new UnitInfo { Id = UnitId.Saboteur, Soup = SoupType.Tarhana, Name = "Maya Sabotajcısı", English = "Ferment Saboteur", Blurb = "Disarms traps.",
                Cost = 30, Hp = 100, Speed = 4.0f, Bite = 2, Model = "u_saboteur", Scale = 0.95f },
            new UnitInfo { Id = UnitId.Cloud, Soup = SoupType.Tarhana, Name = "Ekşi Bulut", English = "Sour Cloud", Blurb = "Slows nearby towers.",
                Cost = 40, Hp = 130, Speed = 3.1f, Bite = 2, Model = "u_cloud", Scale = 1f },
            new UnitInfo { Id = UnitId.Brute, Soup = SoupType.Iskembe, Name = "Sarımsak Devi", English = "Garlic Brute", Blurb = "Huge, fearless. Smashes towers.",
                Cost = 60, Hp = 380, Speed = 2.4f, Bite = 6, Model = "u_brute", Scale = 1.35f },
            new UnitInfo { Id = UnitId.Bomber, Soup = SoupType.Iskembe, Name = "Sirke Bombacısı", English = "Vinegar Bomber", Blurb = "Blows up a tower.",
                Cost = 35, Hp = 120, Speed = 4.2f, Bite = 3, Model = "u_bomber", Scale = 1f },
            new UnitInfo { Id = UnitId.MiniGolem, Soup = SoupType.Yayla, Name = "Minik Golem", English = "Mini Golem", Blurb = "",
                Cost = 0, Hp = 80, Speed = 3.6f, Bite = 1, Model = "u_golem", Scale = 0.65f },
        };

        // Relative worth the bots assign to each choice. The Monte Carlo tuner rewrites these from measured value.
        public static float[] AiDefenceValue = { 1f, 0.7f, 1f, 0.8f, 0.9f, 0.7f, 1f, 0.8f, 0.9f, 0.8f };
        public static float[] AiUnitValue = { 1f, 0.9f, 0.9f, 0.8f, 0.7f, 0.9f, 0.7f, 0.7f, 0.9f, 0.8f, 0f };
        public static float[] AiLadleValue = { 1f, 1f, 0.8f, 1f, 0.8f };

        public const int RecruitableUnitCount = 10;
        public const int DefenceCount = 10;

        static Balance()
        {
            ApplyTuning();
        }

        static partial void ApplyTuning();

        public static DefenceInfo Defence(DefenceId id) => Defences[(int)id];
        public static UnitInfo Unit(UnitId id) => Units[(int)id];
        public static float UnitMaxHp(UnitId id) => Units[(int)id].Hp * UnitHpScale;
        public static DefenceId DefenceOption(SoupType soup, int option) => (DefenceId)((int)soup * 2 + option);
        public static UnitId UnitOption(SoupType soup, int option) => (UnitId)((int)soup * 2 + option);

        // Repeats get pricier so a single build or unit can't carry the whole strategy.
        public static float BuildEscalation = 0.3f;
        public static float RecruitEscalation = 0.05f;
        public const int RecruitEscalationCap = 10;

        public static int BuildCost(DefenceId id, int sameOwned) => (int)System.Math.Round(Defence(id).Cost * (1f + BuildEscalation * sameOwned));

        public static int RecruitCost(UnitId id, int sameRecruited) =>
            (int)System.Math.Round(Unit(id).Cost * (1f + RecruitEscalation * System.Math.Min(sameRecruited, RecruitEscalationCap)));

        public static int UpgradeCost(DefenceId id, int currentLevel)
        {
            if (currentLevel >= MaxLevel) return int.MaxValue;
            return (int)System.Math.Round(Defence(id).Cost * UpgradeCostMult[currentLevel]);
        }
    }
}
