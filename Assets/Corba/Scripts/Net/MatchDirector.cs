using System.Collections.Generic;
using Corba.Sim;
using Unity.Netcode;
using UnityEngine;

namespace Corba
{
    public class MatchDirector : NetworkBehaviour
    {
        public static MatchDirector Instance { get; private set; }

        public readonly NetworkVariable<MatchPhase> Phase = new NetworkVariable<MatchPhase>(MatchPhase.Lobby);
        public readonly NetworkVariable<double> PhaseEnd = new NetworkVariable<double>();
        public readonly NetworkVariable<int> Course = new NetworkVariable<int>();
        public readonly NetworkVariable<int> Half = new NetworkVariable<int>();
        public readonly NetworkVariable<int> DefenderSlot = new NetworkVariable<int>();
        public readonly NetworkVariable<int> Seed = new NetworkVariable<int>(-1);
        public readonly NetworkVariable<int> WinsA = new NetworkVariable<int>();
        public readonly NetworkVariable<int> WinsB = new NetworkVariable<int>();
        public readonly NetworkVariable<int> ScoreA = new NetworkVariable<int>();
        public readonly NetworkVariable<int> ScoreB = new NetworkVariable<int>();
        public readonly NetworkVariable<float> SpillA = new NetworkVariable<float>();
        public readonly NetworkVariable<float> SpillB = new NetworkVariable<float>();
        public readonly NetworkVariable<float> Hunger = new NetworkVariable<float>(GameData.SofraHunger);
        public readonly NetworkVariable<int> BonusMask = new NetworkVariable<int>();
        public readonly NetworkVariable<int> Queued = new NetworkVariable<int>();
        public readonly NetworkVariable<int> Winner = new NetworkVariable<int>(-1);
        public readonly NetworkVariable<int> PlayerCount = new NetworkVariable<int>();
        public readonly NetworkVariable<bool> Paused = new NetworkVariable<bool>();
        public readonly NetworkVariable<int> Mode = new NetworkVariable<int>();
        public readonly NetworkVariable<int> Marching = new NetworkVariable<int>();
        public readonly NetworkVariable<double> WaveAutoAt = new NetworkVariable<double>();
        public NetworkList<int> Bank;

        public event System.Action<string, Color> Toast;

        readonly ulong[] slotClients = { ulong.MaxValue, ulong.MaxValue };
        static ulong BotId(int slot) => ulong.MaxValue - 1 - (ulong)slot;
        public bool IsBotSlot(int slot) => slotClients[slot] == BotId(slot);
        readonly Queue<UnitId> spawnQueue = new Queue<UnitId>();
        readonly List<UnitId> waveQueue = new List<UnitId>();
        readonly Dictionary<int, UnitAgent> unitViews = new Dictionary<int, UnitAgent>();
        readonly Dictionary<int, DefenceBuild> buildViews = new Dictionary<int, DefenceBuild>();
        readonly double[] nextLadle = new double[2];
        public NetworkList<int> Recruited;
        readonly List<(double at, SoupType soup, System.Numerics.Vector2 pos)> pendingLadles = new List<(double, SoupType, System.Numerics.Vector2)>();
        double nextSpawnTime;
        SimWorld world;
        float simAccumulator;
        int statBuilds, statUnits, statLadles;

        public SimWorld World => world;
        public float SimTime => world != null ? world.Time : 0f;
        public int WaveCount => waveQueue.Count;

        public static double Now => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening
            ? NetworkManager.Singleton.ServerTime.Time
            : Time.timeAsDouble;

        public int AttackerSlot => 1 - DefenderSlot.Value;
        public float TimeLeft => Mathf.Max(0f, (float)(PhaseEnd.Value - Now));
        public bool InPlay => Phase.Value == MatchPhase.Prep || Phase.Value == MatchPhase.Raid;

        void Awake()
        {
            Bank = new NetworkList<int>();
            Recruited = new NetworkList<int>();
        }

        public override void OnNetworkSpawn()
        {
            Instance = this;
            Seed.OnValueChanged += OnSeedChanged;
            if (Seed.Value >= 0) CorbaMap.Instance.Build(Seed.Value);

            if (IsServer)
            {
                for (int i = 0; i < 10; i++) Bank.Add(0);
                for (int i = 0; i < Balance.RecruitableUnitCount; i++) Recruited.Add(0);
                Seed.Value = Random.Range(1, int.MaxValue);
                CorbaMap.Instance.Build(Seed.Value);
                NetworkManager.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
                Mode.Value = (int)CorbaBootstrap.PendingMode;
                if (CorbaBootstrap.PendingMode != GameMode.AiVsAi)
                    foreach (var id in NetworkManager.ConnectedClientsIds) OnClientConnected(id);
                switch (CorbaBootstrap.PendingMode)
                {
                    case GameMode.VsAi:
                        AddBot(1);
                        StartMatch();
                        break;
                    case GameMode.AiVsAi:
                        AddBot(0);
                        AddBot(1);
                        StartMatch();
                        break;
                    case GameMode.Solo:
                        StartMatch();
                        break;
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            Seed.OnValueChanged -= OnSeedChanged;
            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
            if (Instance == this) Instance = null;
        }

        void OnSeedChanged(int _, int seed) => CorbaMap.Instance.Build(seed);

        public float Capacity(int slot) => GameData.PotCapacity * ((BonusMask.Value & (1 << slot)) != 0 ? 1f + GameData.ComebackCapacityBonus : 1f);

        public int BankOf(int slot, SoupType soup) => Bank.Count > slot * 5 + (int)soup ? Bank[slot * 5 + (int)soup] : 0;

        public bool IsDefender(int slot) => DefenderSlot.Value == slot;

        public int RecruitCost(UnitId unit) => Balance.RecruitCost(unit, Recruited.Count > (int)unit ? Recruited[(int)unit] : 0);

        public int BuildCost(DefenceId id)
        {
            int owned = 0;
            foreach (var b in DefenceBuild.All) if (b.Kind.Value == id) owned++;
            return Balance.BuildCost(id, owned);
        }

        #region Connections

        void AddBot(int slot)
        {
            slotClients[slot] = BotId(slot);
            PlayerCount.Value = CountPlayers();
            var prefab = Resources.Load<GameObject>("Corba/Net/Chef");
            var spawn = slot == DefenderSlot.Value ? CorbaMap.Instance.DefenderSpawn : CorbaMap.Instance.AttackerSpawn;
            var go = Instantiate(prefab, spawn + Vector3.up * 0.2f, Quaternion.identity);
            var chef = go.GetComponent<ChefController>();
            chef.Slot.Value = slot;
            chef.IsBot.Value = true;
            go.GetComponent<NetworkObject>().Spawn(true);
            go.AddComponent<CookAI>();
        }

        void OnClientConnected(ulong clientId)
        {
            if (Mode.Value == (int)GameMode.AiVsAi) return;
            if (slotClients[0] == clientId || slotClients[1] == clientId) return;
            int slot = slotClients[0] == ulong.MaxValue ? 0 : slotClients[1] == ulong.MaxValue ? 1 : -1;
            if (slot < 0)
            {
                NetworkManager.DisconnectClient(clientId);
                return;
            }
            slotClients[slot] = clientId;
            PlayerCount.Value = CountPlayers();

            var prefab = Resources.Load<GameObject>("Corba/Net/Chef");
            var spawn = slot == DefenderSlot.Value ? CorbaMap.Instance.DefenderSpawn : CorbaMap.Instance.AttackerSpawn;
            var go = Instantiate(prefab, spawn + Vector3.up * 0.2f, Quaternion.identity);
            var chef = go.GetComponent<ChefController>();
            chef.Slot.Value = slot;
            go.GetComponent<NetworkObject>().SpawnWithOwnership(clientId, true);
            SendToast(slot == 0 ? "Host cook joined" : "Rival cook joined", Color.white);
        }

        void OnClientDisconnected(ulong clientId)
        {
            for (int s = 0; s < 2; s++)
            {
                if (slotClients[s] != clientId) continue;
                slotClients[s] = ulong.MaxValue;
                PlayerCount.Value = CountPlayers();
                if (InPlay || Phase.Value == MatchPhase.HalfResult)
                {
                    Winner.Value = 1 - s;
                    SetPhase(MatchPhase.MatchOver, 0);
                    SendToast("Your rival left the kitchen", Color.white);
                }
            }
        }

        int CountPlayers() => (slotClients[0] != ulong.MaxValue ? 1 : 0) + (slotClients[1] != ulong.MaxValue ? 1 : 0);

        public int SlotOf(ulong clientId) => slotClients[0] == clientId ? 0 : slotClients[1] == clientId ? 1 : -1;

        #endregion

        #region Match flow

        [Rpc(SendTo.Server)]
        public void StartMatchRpc(bool soloTest, RpcParams p = default)
        {
            if (Phase.Value != MatchPhase.Lobby && Phase.Value != MatchPhase.MatchOver) return;
            if (SlotOf(p.Receive.SenderClientId) != 0) return;
            if (!soloTest && CountPlayers() < 2) return;
            StartMatch();
        }

        void StartMatch()
        {
            WinsA.Value = 0;
            WinsB.Value = 0;
            BonusMask.Value = 0;
            Winner.Value = -1;
            StartCourse(0);
        }

        void StartCourse(int course)
        {
            Course.Value = course;
            ScoreA.Value = 0;
            ScoreB.Value = 0;
            SpillA.Value = 0;
            SpillB.Value = 0;
            Seed.Value = Random.Range(1, int.MaxValue);
            CorbaMap.Instance.Build(Seed.Value);
            StartHalf(0);
        }

        void StartHalf(int half)
        {
            Half.Value = half;
            int firstDefender = Course.Value % 2 == 0 ? 0 : 1;
            DefenderSlot.Value = half == 0 ? firstDefender : 1 - firstDefender;
            ClearField();
            world = CreateWorld();
            statBuilds = statUnits = statLadles = 0;
            for (int i = 0; i < Bank.Count; i++) Bank[i] = 0;
            for (int i = 0; i < Recruited.Count; i++) Recruited[i] = 0;
            spawnQueue.Clear();
            waveQueue.Clear();
            pendingLadles.Clear();
            Queued.Value = 0;
            Marching.Value = 0;
            Hunger.Value = GameData.SofraHunger;

            for (int s = 0; s < 2; s++)
            {
                var chef = ChefController.BySlot[s];
                if (chef == null) continue;
                chef.ResetPot();
                var pos = s == DefenderSlot.Value ? CorbaMap.Instance.DefenderSpawn : CorbaMap.Instance.AttackerSpawn;
                chef.TeleportRpc(pos + Vector3.up * 0.3f);
            }
            SetPhase(MatchPhase.Prep, GameData.PrepSeconds);
            SendToast($"Course {Course.Value + 1} • Half {half + 1}", Color.white);
        }

        void BeginRaid()
        {
            SetPhase(MatchPhase.Raid, GameData.RaidSeconds);
            nextSpawnTime = Now;
            ServerSendWave(AttackerSlot);
            SendToast("Raid! Units are marching", GameData.Hex("F2B33D"));
        }

        void EndHalf(bool sofraFell)
        {
            if (Phase.Value != MatchPhase.Raid) return;
            int attacker = AttackerSlot;
            if (sofraFell)
            {
                int bonus = Mathf.FloorToInt(TimeLeft / 2f);
                AddScore(attacker, bonus);
                SendToast($"The Sofra is empty! +{bonus} speed bonus", GameData.Hex("F2B33D"));
            }
            Debug.Log($"[Corba] half end: course {Course.Value + 1} half {Half.Value + 1} attacker {AttackerSlot} score {(AttackerSlot == 0 ? ScoreA.Value : ScoreB.Value)} sofraFell {sofraFell} builds {statBuilds} raiders {statUnits} ladles {statLadles}");
            SetPhase(MatchPhase.HalfResult, GameData.HalfResultSeconds);
            spawnQueue.Clear();
            waveQueue.Clear();
            Queued.Value = 0;
            Marching.Value = 0;
        }

        void AfterHalfResult()
        {
            if (Half.Value == 0)
            {
                StartHalf(1);
                return;
            }

            int winner;
            if (ScoreA.Value != ScoreB.Value) winner = ScoreA.Value > ScoreB.Value ? 0 : 1;
            else winner = SpillA.Value <= SpillB.Value ? 0 : 1;

            if (winner == 0) WinsA.Value++; else WinsB.Value++;
            CheerRpc(winner);
            BonusMask.Value = 1 << (1 - winner);
            SendToast($"{SlotName(winner)} wins course {Course.Value + 1}!", GameData.Hex("F2B33D"));

            if (WinsA.Value >= 2 || WinsB.Value >= 2)
            {
                Winner.Value = WinsA.Value >= 2 ? 0 : 1;
                ClearField();
                SetPhase(MatchPhase.MatchOver, 0);
                return;
            }
            StartCourse(Course.Value + 1);
        }

        static string SlotName(int slot) => slot == 0 ? "Host cook" : "Guest cook";

        void SetPhase(MatchPhase phase, float seconds)
        {
            Phase.Value = phase;
            PhaseEnd.Value = Now + seconds;
        }

        void AddScore(int slot, int amount)
        {
            if (slot == 0) ScoreA.Value += amount; else ScoreB.Value += amount;
        }

        void ClearField()
        {
            foreach (var u in new List<UnitAgent>(UnitAgent.All))
                if (u.NetworkObject.IsSpawned) u.NetworkObject.Despawn(true);
            foreach (var b in new List<DefenceBuild>(DefenceBuild.All))
                if (b.NetworkObject.IsSpawned) b.NetworkObject.Despawn(true);
            unitViews.Clear();
            buildViews.Clear();
        }

        static SimWorld CreateWorld()
        {
            var map = CorbaMap.Instance;
            var pts = new System.Numerics.Vector2[map.RoadPoints.Count];
            for (int i = 0; i < pts.Length; i++) pts[i] = GameData.Sim(map.RoadPoints[i]);
            var pads = new SimPad[map.Pads.Count];
            for (int i = 0; i < pads.Length; i++)
                pads[i] = new SimPad { Tower = GameData.Sim(map.Pads[i].TowerPosition), RoadDistance = map.Pads[i].RoadDistance, HighGround = map.Pads[i].HighGround };
            return new SimWorld(new SimRoad(pts, map.RoadDistances), pads);
        }

        [Rpc(SendTo.Server)]
        public void SetPausedRpc(bool paused, RpcParams p = default)
        {
            if (SlotOf(p.Receive.SenderClientId) == 0) Paused.Value = paused;
        }

        public bool ServerRecruit(int slot, UnitId unit)
        {
            if (!InPlay || IsDefender(slot) || (int)unit >= GameData.RecruitableUnitCount) return false;
            var info = GameData.Unit(unit);
            int idx = slot * 5 + (int)info.Soup;
            int cost = RecruitCost(unit);
            if (Bank[idx] < cost) return false;
            Bank[idx] -= cost;
            Recruited[(int)unit]++;
            if (waveQueue.Count == 0) WaveAutoAt.Value = Now + Balance.AutoWaveSeconds;
            waveQueue.Add(unit);
            Queued.Value = waveQueue.Count;
            return true;
        }

        /// <summary>Sends every queued raider down the road. Holding builds a bigger, buff-able wave.</summary>
        public bool ServerSendWave(int slot)
        {
            if (Phase.Value != MatchPhase.Raid || IsDefender(slot) || waveQueue.Count == 0) return false;
            foreach (var u in waveQueue) spawnQueue.Enqueue(u);
            WaveFxRpc(waveQueue.Count);
            waveQueue.Clear();
            Queued.Value = 0;
            Marching.Value = spawnQueue.Count;
            return true;
        }

        [Rpc(SendTo.Server)]
        public void SendWaveRpc(RpcParams p = default)
        {
            int slot = SlotOf(p.Receive.SenderClientId);
            if (slot >= 0) ServerSendWave(slot);
        }

        public bool LadleReady(int slot) => Now >= nextLadle[slot];

        public bool ServerLadle(ChefController chef, Vector3 target)
        {
            int slot = chef.Slot.Value;
            if (Phase.Value != MatchPhase.Raid || IsDefender(slot) || chef.Soup.Value < 0 || world == null) return false;
            if (chef.Fill.Value + 0.01f < Balance.LadleCost || Now < nextLadle[slot]) return false;
            Vector3 from = chef.transform.position;
            Vector3 to = target - from;
            to.y = 0f;
            if (to.magnitude > Balance.LadleRange) target = from + to.normalized * Balance.LadleRange;
            target.y = CorbaMap.Instance.Height(target);
            nextLadle[slot] = Now + Balance.LadleCooldown;
            chef.Fill.Value -= Balance.LadleCost;
            var soup = (SoupType)chef.Soup.Value;
            if (chef.Fill.Value < 0.5f)
            {
                chef.Fill.Value = 0f;
                chef.Soup.Value = -1;
            }
            LadleFxRpc(from + Vector3.up * 1.3f, target, soup);
            statLadles++;
            pendingLadles.Add((Now + 0.45, soup, GameData.Sim(target)));
            return true;
        }

        void Update()
        {
            if (!IsServer || !IsSpawned) return;
            double now = Now;
            if (Paused.Value && Phase.Value == MatchPhase.Prep) PhaseEnd.Value += Time.deltaTime;
            switch (Phase.Value)
            {
                case MatchPhase.Prep:
                    if (now >= PhaseEnd.Value) BeginRaid();
                    break;
                case MatchPhase.Raid:
                    if (waveQueue.Count > 0 && (waveQueue.Count >= Balance.AutoWaveSize || now >= WaveAutoAt.Value)) ServerSendWave(AttackerSlot);
                    if (spawnQueue.Count > 0 && now >= nextSpawnTime)
                    {
                        var id = spawnQueue.Dequeue();
                        Marching.Value = spawnQueue.Count;
                        SpawnUnit(id, 0f, Random.Range(-1.1f, 1.1f));
                        nextSpawnTime = now + GameData.SpawnInterval;
                    }
                    for (int i = pendingLadles.Count - 1; i >= 0; i--)
                    {
                        if (now < pendingLadles[i].at) continue;
                        world?.Ladle(pendingLadles[i].soup, pendingLadles[i].pos);
                        pendingLadles.RemoveAt(i);
                    }
                    StepWorld();
                    if (Phase.Value == MatchPhase.Raid && now >= PhaseEnd.Value) EndHalf(false);
                    break;
                case MatchPhase.HalfResult:
                    if (now >= PhaseEnd.Value) AfterHalfResult();
                    break;
            }
        }

        #endregion

        #region Economy (server)

        public bool ServerFill(ChefController chef, int stationIndex)
        {
            if (!InPlay) return false;
            var map = CorbaMap.Instance;
            if (stationIndex < 0 || stationIndex >= map.Stations.Count) return false;
            var st = map.Stations[stationIndex];
            var expected = IsDefender(chef.Slot.Value) ? StationKind.DefenderCauldron : StationKind.AttackerCauldron;
            if (st.Kind != expected) return false;
            if (CorbaMap.FlatDist(chef.transform.position, st.Position) > GameData.InteractRange + 1.5f) return false;

            if (chef.Soup.Value != (int)st.Soup && chef.Fill.Value > 0.5f) AddSpill(chef.Slot.Value, chef.Fill.Value);
            chef.Soup.Value = (int)st.Soup;
            chef.Fill.Value = Capacity(chef.Slot.Value);
            return true;
        }

        public bool ServerPour(ChefController chef)
        {
            if (!InPlay || IsDefender(chef.Slot.Value) || chef.Soup.Value < 0) return false;
            var ocak = CorbaMap.Instance.Ocak;
            if (CorbaMap.FlatDist(chef.transform.position, ocak.Position) > GameData.InteractRange + 2.5f) return false;
            int ladles = Mathf.FloorToInt(chef.Fill.Value);
            if (ladles <= 0) return false;
            int idx = chef.Slot.Value * 5 + chef.Soup.Value;
            Bank[idx] += ladles;
            var soup = (SoupType)chef.Soup.Value;
            chef.Fill.Value = 0f;
            chef.Soup.Value = -1;
            PourFxRpc(ocak.Position + Vector3.up * 1.2f, GameData.Soup(soup).Liquid);
            return true;
        }

        public bool ServerBuild(ChefController chef, int padIndex, int option)
        {
            if (!InPlay || !IsDefender(chef.Slot.Value) || chef.Soup.Value < 0) return false;
            var map = CorbaMap.Instance;
            if (padIndex < 0 || padIndex >= map.Pads.Count) return false;
            var pad = map.Pads[padIndex];
            if (CorbaMap.FlatDist(chef.transform.position, pad.TowerPosition) > GameData.InteractRange + 1.5f) return false;
            var soup = (SoupType)chef.Soup.Value;

            var existing = PadBuild(padIndex);
            if (existing != null && existing.IsSpawned && existing.Sim != null)
            {
                var info = existing.Info;
                if (info.Soup != soup) return false;
                int cost = GameData.UpgradeCost(info.Id, existing.Sim.Level);
                if (chef.Fill.Value + 0.01f < cost) return false;
                chef.Fill.Value -= cost;
                world.Upgrade(existing.Sim);
                existing.Level.Value = existing.Sim.Level;
                existing.Hp.Value = existing.Sim.Hp;
                PourFxRpc(existing.transform.position + Vector3.up * 1.5f, GameData.Soup(soup).Liquid);
                return true;
            }

            var id = GameData.DefenceOption(soup, Mathf.Clamp(option, 0, 1));
            var def = GameData.Defence(id);
            int buildCost = BuildCost(id);
            if (chef.Fill.Value + 0.01f < buildCost) return false;
            chef.Fill.Value -= buildCost;

            var prefab = Resources.Load<GameObject>("Corba/Net/Build");
            bool onRoad = def.Kind != DefenceKind.Tower;
            var pos = onRoad ? pad.TrapPosition : pad.TowerPosition;
            var rot = onRoad ? pad.TrapRotation : Quaternion.identity;
            var go = Instantiate(prefab, pos, rot);
            var build = go.GetComponent<DefenceBuild>();
            var sim = world.AddBuild(id, padIndex, GameData.Sim(pos));
            build.Sim = sim;
            build.Kind.Value = id;
            build.Pad.Value = padIndex;
            build.Level.Value = 1;
            build.Hp.Value = sim.Hp;
            go.GetComponent<NetworkObject>().Spawn(true);
            buildViews[sim.Id] = build;
            statBuilds++;
            PourFxRpc(pos + Vector3.up * 1.2f, GameData.Soup(soup).Liquid);
            return true;
        }

        public DefenceBuild PadBuild(int padIndex)
        {
            foreach (var b in DefenceBuild.All)
                if (b.Pad.Value == padIndex) return b;
            return null;
        }

        [Rpc(SendTo.Server)]
        public void RecruitRpc(int unit, RpcParams p = default)
        {
            int slot = SlotOf(p.Receive.SenderClientId);
            if (slot < 0 || !InPlay || IsDefender(slot)) return;
            if (unit < 0) return;
            ServerRecruit(slot, (UnitId)unit);
        }

        public void AddSpill(int slot, float amount)
        {
            if (amount <= 0f) return;
            if (slot == 0) SpillA.Value += amount; else SpillB.Value += amount;
        }

        #endregion

        #region Units (server)

        public UnitAgent SpawnUnit(UnitId id, float dist, float lateral)
        {
            var sim = world.SpawnUnit(id, dist, lateral);
            statUnits++;
            return CreateUnitView(sim);
        }

        UnitAgent CreateUnitView(SimUnit sim)
        {
            var prefab = Resources.Load<GameObject>("Corba/Net/Unit");
            var pos = CorbaMap.Instance.SampleRoad(sim.Dist, sim.Lateral);
            var go = Instantiate(prefab, pos, Quaternion.identity);
            var unit = go.GetComponent<UnitAgent>();
            unit.Sim = sim;
            unit.Kind.Value = sim.Kind;
            unit.Distance.Value = sim.Dist;
            unit.Lateral.Value = sim.Lateral;
            unit.Hp.Value = sim.Hp;
            go.GetComponent<NetworkObject>().Spawn(true);
            unitViews[sim.Id] = unit;
            return unit;
        }

        void StepWorld()
        {
            if (world == null) return;
            var chef = ChefController.BySlot[AttackerSlot];
            world.RallyActive = chef != null;
            if (chef != null) world.RallyPoint = GameData.Sim(chef.transform.position);

            simAccumulator += Time.deltaTime;
            int steps = 0;
            while (simAccumulator >= Balance.SimStep && steps < 8)
            {
                simAccumulator -= Balance.SimStep;
                steps++;
                int bitten = world.Tick(Balance.SimStep);
                ProcessEvents();
                if (bitten > 0) ServerBite(bitten);
                if (Phase.Value != MatchPhase.Raid) break;
            }
        }

        void ProcessEvents()
        {
            var map = CorbaMap.Instance;
            foreach (var e in world.Events)
            {
                switch (e.Type)
                {
                    case SimEventType.Shot:
                    case SimEventType.Lob:
                    {
                        if (!buildViews.TryGetValue(e.Build, out var b) || b == null) break;
                        Vector3 from = b.transform.position + Vector3.up * 2.6f;
                        Vector3 to = unitViews.TryGetValue(e.Unit, out var target) && target != null
                            ? target.transform.position + Vector3.up * 0.8f
                            : GameData.World(e.To, map.Height(e.To.X, e.To.Y) + 0.4f);
                        var color = b.Kind.Value == DefenceId.Chili ? GameData.Hex("E0402A") : b.Kind.Value == DefenceId.Ferment ? GameData.Hex("C77A35") : GameData.Soup(b.Info.Soup).Liquid;
                        ShotFxRpc(b.NetworkObjectId, from, to, color, e.Value, e.Type == SimEventType.Lob);
                        break;
                    }
                    case SimEventType.Pulse:
                        RingFxRpc(GameData.World(e.To, map.Height(e.To.X, e.To.Y) + 0.5f), (DefenceId)e.Tag == DefenceId.Mint ? GameData.Hex("9FE3B5") : GameData.Hex("C9B3E6"), e.Value);
                        break;
                    case SimEventType.Cloud:
                        CloudFxRpc(GameData.World(e.To, map.Height(e.To.X, e.To.Y) + 0.4f), e.Value, e.Tag / 10f);
                        break;
                    case SimEventType.Sabotage:
                        RingFxRpc(GameData.World(e.To, map.Height(e.To.X, e.To.Y) + 0.3f), GameData.Hex("C77A35"), 1.5f);
                        break;
                    case SimEventType.Snare:
                        RingFxRpc(GameData.World(e.To, map.Height(e.To.X, e.To.Y) + 0.3f), GameData.Hex("A88FC9"), 2f);
                        break;
                    case SimEventType.Explode:
                        RingFxRpc(GameData.World(e.To, map.Height(e.To.X, e.To.Y) + 1f), GameData.Hex("D7E37A"), e.Value);
                        break;
                    case SimEventType.Ladle:
                        RingFxRpc(GameData.World(e.To, map.Height(e.To.X, e.To.Y) + 0.3f), GameData.Soup((SoupType)e.Tag).Identity, e.Value);
                        break;
                    case SimEventType.Bite:
                        if (unitViews.TryGetValue(e.Unit, out var biter) && biter != null)
                        {
                            biter.MarkBite();
                            if (biter.IsSpawned) biter.NetworkObject.Despawn(true);
                        }
                        unitViews.Remove(e.Unit);
                        break;
                    case SimEventType.UnitDied:
                        if (unitViews.TryGetValue(e.Unit, out var dead) && dead != null && dead.IsSpawned) dead.NetworkObject.Despawn(true);
                        unitViews.Remove(e.Unit);
                        break;
                    case SimEventType.UnitSpawned:
                    {
                        var u = world.FindUnit(e.Unit);
                        if (u != null) CreateUnitView(u);
                        break;
                    }
                    case SimEventType.BuildDestroyed:
                        if (buildViews.TryGetValue(e.Build, out var gone) && gone != null && gone.IsSpawned) gone.NetworkObject.Despawn(true);
                        buildViews.Remove(e.Build);
                        break;
                }
            }
            world.Events.Clear();
        }

        void ServerBite(int bite)
        {
            if (Phase.Value != MatchPhase.Raid) return;
            float dmg = Mathf.Min(bite, Hunger.Value);
            Hunger.Value -= dmg;
            AddScore(AttackerSlot, Mathf.RoundToInt(dmg));
            BiteFxRpc(CorbaMap.Instance.Sofra.Position + Vector3.up * 1.4f, Mathf.RoundToInt(dmg));
            if (Hunger.Value <= 0.01f) EndHalf(true);
        }

        #endregion

        #region Client feedback

        void SendToast(string msg, Color c) => ToastRpc(msg, c);

        [Rpc(SendTo.ClientsAndHost)]
        void CheerRpc(int slot)
        {
            ChefController.BySlot[slot]?.Cheer();
        }

        [Rpc(SendTo.ClientsAndHost)]
        void ToastRpc(string msg, Color c) => Toast?.Invoke(msg, c);

        [Rpc(SendTo.ClientsAndHost)]
        void PourFxRpc(Vector3 pos, Color c) => FX.Splash(pos, c, 14);

        [Rpc(SendTo.ClientsAndHost)]
        void ShotFxRpc(ulong buildObject, Vector3 from, Vector3 to, Color color, float flight, bool lob)
        {
            FX.Projectile(from, to, color, flight, lob);
            FX.Puff(from, new Color(1f, 0.95f, 0.85f, 0.7f));
            if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(buildObject, out var obj) && obj.TryGetComponent<DefenceBuild>(out var b)) b.Recoil();
        }

        [Rpc(SendTo.ClientsAndHost)]
        void RingFxRpc(Vector3 at, Color color, float radius) => FX.Ring(at, color, radius);

        [Rpc(SendTo.ClientsAndHost)]
        void CloudFxRpc(Vector3 at, float radius, float duration) => FX.Cloud(at, GameData.Hex("B9C86A"), radius, duration);

        [Rpc(SendTo.ClientsAndHost)]
        void LadleFxRpc(Vector3 from, Vector3 to, SoupType soup)
        {
            var info = GameData.Soup(soup);
            FX.Projectile(from, to, info.Liquid, 0.45f, true);
            Tween.Wait(0.45f, () =>
            {
                FX.Splash(to + Vector3.up * 0.3f, info.Liquid, 24);
                FX.Ring(to + Vector3.up * 0.3f, info.Identity, Balance.LadleRadius);
                for (int i = 0; i < 5; i++) FX.Splat(to + Random.insideUnitSphere * Balance.LadleRadius * 0.6f, info.Liquid, Random.Range(0.8f, 1.6f));
                FX.PopText(to + Vector3.up * 2.2f, Balance.Ladles[(int)soup].Name + "!", Color.Lerp(info.Identity, Color.white, 0.4f), 1.1f);
                CameraRig.Instance?.Shake(0.25f);
            });
        }

        [Rpc(SendTo.ClientsAndHost)]
        void WaveFxRpc(int count)
        {
            CorbaUI.Instance?.Announce($"WAVE \u00d7{count}!", GameData.Hex("FFB040"));
            var map = CorbaMap.Instance;
            if (map == null) return;
            FX.Ring(map.Ocak.Position + Vector3.up * 0.5f, GameData.Hex("FFB040"), 5f);
            if (map.OcakObject != null) Tween.Squash(map.OcakObject, 0.25f, 0.5f);
        }

        [Rpc(SendTo.ClientsAndHost)]
        void BiteFxRpc(Vector3 pos, int amount)
        {
            FX.PopText(pos + Vector3.up * 1.6f, "NOM!", GameData.Hex("FFD86A"), 1.6f);
            CameraRig.Instance?.Shake(0.35f);
            var map = CorbaMap.Instance;
            if (map != null && map.SofraObject != null) Tween.Squash(map.SofraObject, 0.2f, 0.4f);
            FX.Splash(pos, GameData.Hex("E8962E"), 6);
            FX.FloatText(pos + Vector3.up, "-" + amount, GameData.Hex("D9442B"));
        }

        #endregion
    }
}
