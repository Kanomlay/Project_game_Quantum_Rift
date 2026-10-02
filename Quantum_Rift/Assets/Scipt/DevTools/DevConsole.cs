using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// คอนโซลทดสอบ: กด F2 ในฉากเกม เปิดแผงด้านขวาไว้เสกมอน/บอส/อาวุธ/ไอเทม/พร ปรับสถานะผู้เล่น และวาร์ปแมพ
// ของที่วางในฉาก (มอน บอส ของดรอป กล่อง ร้าน กับดัก) กดปุ่มแล้วขึ้นการ์ดตัวอย่าง คลิกในฉากเพื่อวาง (DevPlacement)
// ไม่ต้องวางอะไรในฉาก: สร้างตัวเองตอนเริ่มเกม (อยู่ข้ามฉาก) สร้างหน้าต่างจากโค้ดตอนเปิดครั้งแรก
// รายชื่อของอ่านจาก Resources/DevCatalog (Editor เติมให้เองทุกครั้งที่กด Play)
// ปิดได้ในหน้าตั้งค่า (แถว "คอนโซลทดสอบ") ปิดแล้วกด F2 ไม่ขึ้น และสูตรที่เปิดค้างไว้กลับเป็นปกติ
// F1 เป็นของหน้าคู่มือการเล่น (GameHelpWindow)
public sealed class DevConsole : MonoBehaviour
{
    public const KeyCode ToggleKey = KeyCode.F2;
    const string EnabledKey = "quantumrift.devconsole";
    const string TabKey = "quantumrift.devconsole.tab";

    public static DevConsole Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.ui != null && Instance.ui.Visible;
    // ESC ที่ใช้ปิดคอนโซลไม่นับเป็นการกดพักเกม (PauseManager เช็คตัวนี้)
    public static bool BlocksEscape => IsOpen || Time.frameCount == closedFrame;
    // กำลังคลิกวางของ: คลิกในฉากไม่ใช่การโจมตี (WeaponController เช็คตัวนี้)
    public static bool Placing => IsOpen && Instance.placement != null && Instance.placement.Active;
    static int closedFrame = -1;

    public static bool Enabled
    {
        get => PlayerPrefs.GetInt(EnabledKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0);
            PlayerPrefs.Save();
            if (value) return;
            DevCheats.ResetAll();
            if (Instance != null) Instance.Close();
        }
    }

    static readonly string[] Tabs = { "มอน", "บอส", "อาวุธ", "ไอเทม", "พร", "ผู้เล่น", "แมพ", "เสียง" };
    static readonly int[] Counts = { 1, 3, 5, 10 };
    static readonly float[] BossHealthSteps = { 1f, 0.75f, 0.6f, 0.5f, 0.3f, 0.25f, 0.1f, 0.01f };
    static readonly float[] TimeScales = { 0.25f, 0.5f, 1f, 2f };
    const float WeaponSize = 1.75f; // อาวุธบนพื้นขนาดประมาณนี้ (WeaponPickup ย่อตามความหายาก)

    // ห้องทดสอบ: MapData สร้างตอนเล่น ยืมผังห้องบอสแมพ 1 (ห้องเดียวกว้าง) MapManager ถอดมอนของห้องออกให้ (isTestLab)
    // เก็บแบบ static อยู่ข้ามการโหลดฉากใหม่ตอนเปลี่ยนอาชีพ
    static MapData testLab;

    DevConsoleUI ui;
    DevPlacement placement;
    MonsterData placingMonster; // มอนทั่วไปที่กำลังวาง (เปลี่ยนจำนวน/แบบผิดเพี้ยนแล้วการ์ดอัปเดตตาม)
    DevCatalog catalog;
    PlayerStats player;
    int tab;
    int anomaly = -1;   // -1 = มอนปกติ ไม่งั้นเป็น (int)AnomalyMonster.Kind
    int count = 1;
    bool dropWeapons;   // อาวุธ: วางบนพื้น แทนการใส่มือทันที

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Instance != null) return;
        var go = new GameObject("DevConsole");
        DontDestroyOnLoad(go);
        go.AddComponent<DevConsole>();
    }

    void Awake()
    {
        Instance = this;
        placement = new DevPlacement(transform);
    }

    void Update()
    {
        // มีเฉพาะในฉากเกม (มี MapManager) เมนูหลักกด F2 ไม่มีอะไรเกิดขึ้น
        if (!Enabled || MapManager.instance == null)
        {
            if (IsOpen) Close();
            return;
        }
        if (Input.GetKeyDown(ToggleKey))
        {
            if (IsOpen) Close();
            else Open();
            return;
        }
        if (!IsOpen) return;
        if (placement.Active)
        {
            // ระหว่างวาง ESC/คลิกขวาเลิกวางอย่างเดียว คอนโซลยังเปิดอยู่
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) CancelPlacement();
            else TickPlacement();
        }
        else if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    // สูตรที่ต้องทำทุกเฟรม ทำหลังระบบเกม (หักพลังงาน/เริ่มคูลดาวน์/HitStop คืนความเร็ว) จะได้ทับทีหลัง
    void LateUpdate()
    {
        if (!Enabled) return;
        var hero = Player;
        if (hero != null && !hero.isDead)
        {
            if (DevCheats.InfiniteEnergy && hero.currentEnergy < hero.maxEnergy) hero.RestoreEnergy(hero.maxEnergy);
            if (DevCheats.NoSkillCooldown) hero.ResetSkillCooldowns();
        }
        if (!Mathf.Approximately(DevCheats.TimeScale, 1f) && Mathf.Approximately(Time.timeScale, 1f))
            Time.timeScale = DevCheats.TimeScale;
    }

    void Open()
    {
        catalog = DevCatalog.Load();
        if (ui == null) ui = new DevConsoleUI(transform, "คอนโซลทดสอบ", Tabs, index => SelectTab(index, true), Close);
        ui.Visible = true;
        SelectTab(Mathf.Clamp(PlayerPrefs.GetInt(TabKey, 0), 0, Tabs.Length - 1), true);
        if (catalog == null) Say("ไม่เจอรายชื่อของ (Resources/DevCatalog) กด Play ใน Unity ใหม่อีกครั้ง หรือสั่ง Tools > Quantum Rift > Dev Console > Refresh Catalog");
        else Say("F2 เปิด/ปิด · กดปุ่มของแล้วคลิกในฉากเพื่อวาง · ESC ปิด");
    }

    void Close()
    {
        if (!IsOpen) return;
        if (placement.Active)
        {
            placement.Cancel();
            ui.HideCard();
        }
        placingMonster = null;
        ui.Visible = false;
        closedFrame = Time.frameCount;
    }

    void SelectTab(int index, bool resetScroll)
    {
        tab = index;
        PlayerPrefs.SetInt(TabKey, index);
        ui.SelectTab(index);
        Rebuild(!resetScroll);
    }

    void Rebuild(bool keepScroll = true)
    {
        if (ui == null) return;
        float scrolled = keepScroll ? ui.Scrolled : 0f;
        ui.Clear();
        if (catalog == null) ui.Note("ยังไม่มีรายชื่อของให้เสก");
        else
            switch (tab)
            {
                case 0: BuildMonsters(); break;
                case 1: BuildBosses(); break;
                case 2: BuildWeapons(); break;
                case 3: BuildItems(); break;
                case 4: BuildBlessings(); break;
                case 5: BuildPlayer(); break;
                case 6: BuildMaps(); break;
                default: BuildSounds(); break;
            }
        ui.Restore(scrolled);
    }

    void Say(string text)
    {
        if (ui != null) ui.Status(text);
        Debug.Log("[คอนโซลทดสอบ] " + text);
    }

    // ---------- ปุ่มแบบเลือกหนึ่ง / เปิดปิด ----------

    void Radio(Transform parent, string label, bool on, Action choose, Color? textColor = null)
    {
        ui.Button(parent, label, on ? DevConsoleUI.OnColor : DevConsoleUI.ButtonColor, () =>
        {
            choose();
            Rebuild();
        }, textColor);
    }

    void Toggle(Transform parent, string label, Func<bool> get, Action<bool> set)
    {
        bool on = get();
        ui.Button(parent, $"{label}: {(on ? "เปิด" : "ปิด")}", on ? DevConsoleUI.OnColor : DevConsoleUI.ButtonColor, () =>
        {
            set(!get());
            Say($"{label}: {(get() ? "เปิด" : "ปิด")}");
            Rebuild();
        });
    }

    // ---------- คลิกวาง ----------

    // กดปุ่มของ = การ์ดตัวอย่าง + เงาตามเมาส์ คลิกซ้ายในฉากวาง (วางซ้ำได้) คลิกขวา/ESC/ปุ่มเลิกวาง = เลิก
    void BeginPlacement(DevPlacement.Spec spec)
    {
        placement.Begin(spec);
        ui.ShowCard(spec.sprite, spec.title, spec.details, CancelPlacement);
        Say($"คลิกในฉากเพื่อวาง {spec.title}");
    }

    void CancelPlacement()
    {
        placingMonster = null;
        if (!placement.Active) return;
        placement.Cancel();
        if (ui != null) ui.HideCard();
        Say("เลิกวางแล้ว");
    }

    void TickPlacement()
    {
        if (!placement.Tick(RoomAt, out var room, out var spot, out var problem)) return;
        Say(problem ?? placement.Current.place(room, spot));
    }

    // ภาพตัวอย่างจาก SpriteRenderer ตัวแรกของ prefab ขนาดและตำแหน่งตามของจริงที่จะเสก
    static void Picture(DevPlacement.Spec spec, GameObject prefab, Sprite sprite = null)
    {
        var view = prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>(true) : null;
        spec.sprite = sprite != null ? sprite : view != null ? view.sprite : null;
        if (view == null) return;
        spec.spriteScale = view.transform.lossyScale;
        spec.spriteOffset = view.transform.position - prefab.transform.position;
    }

    // ของบนพื้น (FloorItem): ภาพย่อให้ด้านยาวสุดเท่า size อยู่กลางจุดวาง
    static void FloorPicture(DevPlacement.Spec spec, Sprite sprite, float size)
    {
        spec.sprite = sprite;
        float longest = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 0f;
        spec.spriteScale = Vector2.one * (longest > 0f ? size / longest : size);
        spec.body = Base(spec, 0f);
    }

    // พื้นที่ที่ต้องว่างจากขนาดภาพ height > 0 = เอาแค่ฐานสูงเท่านี้ (ร้านค้าตัวสูง หัวเกยผนังด้านหลังได้)
    static SpawnPlacement.Footprint Base(DevPlacement.Spec spec, float height)
    {
        if (spec.sprite == null) return new SpawnPlacement.Footprint { size = new Vector2(0.6f, 0.6f) };
        Bounds bounds = spec.sprite.bounds;
        var scale = new Vector2(Mathf.Abs(spec.spriteScale.x), Mathf.Abs(spec.spriteScale.y));
        Vector2 size = Vector2.Scale(bounds.size, scale);
        Vector2 center = spec.spriteOffset + Vector2.Scale(bounds.center, spec.spriteScale);
        if (height <= 0f || height >= size.y) return new SpawnPlacement.Footprint { size = size * 0.8f, offset = center };
        float bottom = center.y - size.y * 0.5f;
        return new SpawnPlacement.Footprint
        {
            size = new Vector2(size.x * 0.8f, height),
            offset = new Vector2(center.x, bottom + height * 0.5f),
        };
    }

    // ---------- แท็บมอน ----------

    void BuildMonsters()
    {
        ui.Section("มอนผิดเพี้ยน (Anomaly)");
        var grid = ui.Grid(4);
        Radio(grid, "ปกติ", anomaly < 0, () => SetMonsterOption(() => anomaly = -1));
        foreach (AnomalyMonster.Kind kind in Enum.GetValues(typeof(AnomalyMonster.Kind)))
            Radio(grid, AnomalyName(kind), anomaly == (int)kind, () => SetMonsterOption(() => anomaly = (int)kind), AnomalyMonster.ColorOf(kind));

        ui.Section("จำนวนต่อคลิก");
        grid = ui.Grid(4);
        foreach (int n in Counts) Radio(grid, "x" + n, count == n, () => SetMonsterOption(() => count = n));

        ui.Section("มอน (กดแล้วคลิกในฉากเพื่อวาง)");
        grid = ui.Grid(2, 58);
        foreach (var entry in catalog.monsters)
        {
            if (entry == null || entry.data == null || entry.boss) continue;
            var data = entry.data;
            ui.Button(grid, $"{data.monsterName}\n<size=70%>{entry.map}</size>",
                      placingMonster == data ? DevConsoleUI.OnColor : DevConsoleUI.ButtonColor, () =>
                      {
                          placingMonster = data;
                          BeginPlacement(MonsterSpec(data, count, CurrentAnomaly));
                          Rebuild();
                      });
        }
        ui.Note("มอนที่เสกจากคอนโซลไม่นับเป็นมอนของห้อง: ตายแล้วห้องไม่เคลียร์ ไม่ได้กล่อง ประตูไม่ปิด");

        ui.Section("หุ่นฝึก");
        grid = ui.Grid(1);
        ui.Button(grid, "วางหุ่นฝึก", DevConsoleUI.ButtonColor, PlaceDummy);
        ui.Note("ยืนนิ่ง ไม่โจมตี เลือด x1000 ไว้ดูตัวเลขดาเมจ คริติคอล และการเซของอาวุธแต่ละแบบ");

        ui.Section("จัดการมอน");
        grid = ui.Grid(2);
        ui.Button(grid, "ฆ่ามอนทั้งหมด", DevConsoleUI.DangerColor, KillMonsters);
        Toggle(grid, "หยุด AI มอน/บอส", () => DevCheats.FreezeMonsters, on => DevCheats.FreezeMonsters = on);
        ui.Note("หยุด AI: มอนและบอสทุกตัวยืนนิ่ง ไม่เริ่มท่าใหม่ ท่าที่ร่ายอยู่ค้างไว้จนกว่าจะปล่อย ยังโดนตี เซ ตายได้ (กระสุนที่ยิงออกไปแล้วยังบินต่อ)");
    }

    // เปลี่ยนจำนวน/แบบผิดเพี้ยนระหว่างวางมอนอยู่ การ์ดกับของที่จะวางเปลี่ยนตามทันที
    void SetMonsterOption(Action change)
    {
        change();
        if (placingMonster != null && placement.Active) BeginPlacement(MonsterSpec(placingMonster, count, CurrentAnomaly));
    }

    AnomalyMonster.Kind? CurrentAnomaly => anomaly < 0 ? (AnomalyMonster.Kind?)null : (AnomalyMonster.Kind)anomaly;

    static string AnomalyName(AnomalyMonster.Kind kind)
    {
        switch (kind)
        {
            case AnomalyMonster.Kind.Colossal: return "ร่างยักษ์";
            case AnomalyMonster.Kind.Frenzied: return "โหมกระหน่ำ";
            default: return "เกราะหนา";
        }
    }

    DevPlacement.Spec MonsterSpec(MonsterData data, int amount, AnomalyMonster.Kind? kind, string title = null, string note = null,
                                  Action<MonsterController> setup = null)
    {
        var spec = new DevPlacement.Spec
        {
            title = title ?? (kind.HasValue ? $"{data.monsterName} (ผิดเพี้ยน: {AnomalyName(kind.Value)})" : data.monsterName),
            body = SpawnPlacement.Measure(data.monsterPrefab),
        };
        Picture(spec, data.monsterPrefab);
        string details = $"เลือด {data.maxHealth:0.#} · ดาเมจ {data.attackDamage:0.##} · ความเร็ว {data.moveSpeed:0.#}\n" +
                         $"ระยะโจมตี {data.attackRange:0.#} · หน่วงโจมตี {data.attackCooldown:0.#} วิ";
        if (kind.HasValue) details += $"\n{AnomalyMonster.Describe(kind.Value)}";
        if (amount > 1) details += $"\nคลิกหนึ่งครั้งได้ {amount} ตัว";
        if (!string.IsNullOrEmpty(note)) details += "\n" + note;
        spec.details = details;
        spec.place = (room, spot) => PlaceMonsters(data, room, spot, amount, kind, setup);
        return spec;
    }

    // ตัวแรกลงตรงที่คลิก ที่เหลือกระจายรอบ ๆ (บนพื้น ไม่ทับกันเอง)
    string PlaceMonsters(MonsterData data, RoomController room, Vector2 spot, int amount, AnomalyMonster.Kind? kind,
                         Action<MonsterController> setup)
    {
        var body = SpawnPlacement.Measure(data.monsterPrefab);
        var taken = new List<Vector2>();
        for (int i = 0; i < amount; i++)
        {
            Vector2 at = spot;
            if (i > 0)
            {
                bool found = false;
                for (int attempt = 0; attempt < 12 && !found; attempt++)
                {
                    at = spot + UnityEngine.Random.insideUnitCircle * (1f + 0.25f * i);
                    found = Apart(at, taken, 0.8f) && SpawnPlacement.FitsAt(room, body, at);
                }
                if (!found) at = spot;
            }
            taken.Add(at);
            room.SpawnForTest(data, at, kind, setup);
        }
        string tag = kind.HasValue ? $" (ผิดเพี้ยน: {AnomalyName(kind.Value)})" : "";
        return $"เสก {data.monsterName} x{amount}{tag}";
    }

    static bool Apart(Vector2 point, List<Vector2> others, float gap)
    {
        foreach (var other in others)
            if (Vector2.Distance(point, other) < gap) return false;
        return true;
    }

    void PlaceDummy()
    {
        var entry = catalog.monsters.FirstOrDefault(m => m != null && m.data != null && !m.boss);
        if (entry == null) { Say("ไม่มีมอนให้ทำหุ่นฝึก"); return; }
        placingMonster = null;
        BeginPlacement(MonsterSpec(entry.data, 1, null, $"หุ่นฝึก ({entry.data.monsterName})",
                                   "ยืนนิ่ง ไม่โจมตี เลือด x1000", monster =>
                                   {
                                       monster.HealthScale = 1000f;
                                       monster.Frozen = true;
                                   }));
        Rebuild();
    }

    void KillMonsters()
    {
        int killed = 0;
        foreach (var monster in FindObjectsByType<MonsterController>(FindObjectsSortMode.None))
        {
            if (!monster.IsAlive || IsBoss(monster)) continue;
            monster.SelfDestruct();
            killed++;
        }
        Say($"ฆ่ามอน {killed} ตัว (ไม่รวมบอส)");
    }

    static bool IsBoss(MonsterController monster) => monster.GetComponent<BossHealthHudLink>() != null;

    // ---------- แท็บบอส ----------

    void BuildBosses()
    {
        ui.Note(BossStatus());

        ui.Section("บอส (กดแล้วคลิกในฉากเพื่อวาง)");
        var grid = ui.Grid(1, 54);
        foreach (var entry in catalog.monsters)
        {
            if (entry == null || entry.data == null || !entry.boss) continue;
            var data = entry.data;
            ui.Button(grid, $"{data.monsterName}  <size=75%>({entry.map})</size>", DevConsoleUI.BossColor, () =>
            {
                placingMonster = null;
                BeginPlacement(MonsterSpec(data, 1, null, data.monsterName + " (บอส)", "หลอดเลือดบอสขึ้นเมื่อเสก"));
            });
        }
        ui.Note("วางในห้องทดสอบ (แท็บแมพ) ห้องกว้างพอให้บอสใช้ท่าได้ครบ · Architect ผูกกับสนามของตัวเอง ต้องวาร์ปไปห้องบอสแมพ 3");

        ui.Section("ห้องบอสจริง");
        grid = ui.Grid(1);
        foreach (var map in catalog.maps)
            if (map != null && map.isBossRoom)
            {
                var target = map;
                ui.Button(grid, "วาร์ปไป " + map.mapName, DevConsoleUI.BossColor, () => Warp(target));
            }

        ui.Section("เลือดบอสที่สู้อยู่");
        grid = ui.Grid(4);
        foreach (float step in BossHealthSteps)
        {
            float fraction = step;
            ui.Button(grid, Mathf.RoundToInt(fraction * 100f) + "%", DevConsoleUI.ButtonColor, () => SetBossHealth(fraction));
        }
        grid = ui.Grid(2);
        ui.Button(grid, "ฆ่าบอสทันที (ดูฉากตาย)", DevConsoleUI.DangerColor, KillBosses);
        Toggle(grid, "หยุด AI มอน/บอส", () => DevCheats.FreezeMonsters, on => DevCheats.FreezeMonsters = on);
        ui.Note("เกณฑ์เปลี่ยนเฟส: Echo Commander คลั่งที่ 30% · Ancient Entborn เฟส 2 ที่ 60% คลั่งที่ 30% (เลือดล็อก 30% จนเข้าเฟสคลั่ง) · " +
                "Architect แปลงร่างที่ 50% คลั่งที่ 25% เลือดหมดแล้วเข้าฉากแกนกลาง");
    }

    string BossStatus()
    {
        var parts = new List<string>();
        foreach (var boss in LiveBosses())
            parts.Add($"{boss.myData.monsterName} {Mathf.RoundToInt(boss.HealthFraction * 100f)}%");
        var architect = LiveArchitect();
        if (architect != null)
            parts.Add($"Architect {Mathf.RoundToInt(architect.CurrentHealth / Mathf.Max(1f, architect.maxHealth) * 100f)}%");
        return parts.Count > 0 ? "บอสในแมพ: " + string.Join(" · ", parts) : "ยังไม่มีบอสในแมพ";
    }

    static IEnumerable<MonsterController> LiveBosses() =>
        FindObjectsByType<MonsterController>(FindObjectsSortMode.None).Where(m => m.IsAlive && m.myData != null && IsBoss(m));

    static ArchitectBossHealth LiveArchitect() =>
        FindObjectsByType<ArchitectBossHealth>(FindObjectsSortMode.None).FirstOrDefault(a => a.isActiveAndEnabled && !a.IsDefeated);

    void SetBossHealth(float fraction)
    {
        int changed = 0;
        foreach (var boss in LiveBosses())
        {
            boss.SetHealthForTesting(fraction);
            changed++;
        }
        var architect = LiveArchitect();
        if (architect != null)
        {
            if (architect.SetHealthForTesting(fraction)) changed++;
            else Say("Architect ยังไม่เริ่มสู้ (เดินเข้าสนามก่อน) หรือกำลังแปลงร่างอยู่");
        }
        if (changed == 0 && architect == null) Say("ยังไม่มีบอสในแมพ");
        else if (changed > 0) Say($"ตั้งเลือดบอสเป็น {Mathf.RoundToInt(fraction * 100f)}% · {BossStatus()}");
        Rebuild();
    }

    void KillBosses()
    {
        int killed = 0;
        foreach (var boss in LiveBosses().ToList())
        {
            boss.SelfDestruct();
            killed++;
        }
        var architect = LiveArchitect();
        if (architect != null)
        {
            architect.Kill();
            killed++;
        }
        Say(killed > 0 ? $"ฆ่าบอส {killed} ตัว" : "ยังไม่มีบอสในแมพ");
        Rebuild();
    }

    // ---------- แท็บอาวุธ ----------

    void BuildWeapons()
    {
        ui.Section("เอาอาวุธแบบไหน");
        var grid = ui.Grid(2);
        Radio(grid, "ใส่มือทันที", !dropWeapons, () => dropWeapons = false);
        Radio(grid, "คลิกวางบนพื้น", dropWeapons, () => dropWeapons = true);
        ui.Note(dropWeapons ? "กดอาวุธแล้วคลิกในฉากเพื่อวาง กด F เก็บเองแบบในเกม"
                            : "ช่อง 2 ว่างจะใส่ช่อง 2 ไม่ว่างจะแทนอาวุธที่ถืออยู่ (อาวุธเดิมหายไป)");

        foreach (WeaponRarity rarity in Enum.GetValues(typeof(WeaponRarity)))
        {
            var list = catalog.weapons.Where(w => w != null && w.rarity == rarity).ToList();
            if (list.Count == 0) continue;
            ui.Section(RarityName(rarity));
            grid = ui.Grid(2, 58);
            foreach (var weapon in list)
            {
                var data = weapon;
                string energy = data.energyCost > 0 ? $" · พลังงาน {data.energyCost}" : "";
                ui.Button(grid, $"{data.weaponName}\n<size=70%>ดาเมจ {data.attackDamage:0.#}{energy}</size>", DevConsoleUI.ButtonColor,
                          () => GiveWeapon(data), WeaponPickup.RarityColor(data.rarity));
            }
        }
    }

    static string RarityName(WeaponRarity rarity)
    {
        switch (rarity)
        {
            case WeaponRarity.Starter: return "อาวุธเริ่มต้น";
            case WeaponRarity.Common: return "อาวุธทั่วไป";
            case WeaponRarity.Rare: return "อาวุธหายาก";
            default: return "อาวุธระดับตำนาน";
        }
    }

    void GiveWeapon(WeaponData weapon)
    {
        var hero = Player;
        if (hero == null || hero.isDead) { Say("ยังไม่มีผู้เล่น"); return; }
        if (!dropWeapons)
        {
            hero.PickUpWeapon(weapon);
            Say($"ถือ {weapon.weaponName}");
            return;
        }
        placingMonster = null;
        var spec = new DevPlacement.Spec { title = weapon.weaponName };
        FloorPicture(spec, weapon.weaponIcon, WeaponSize);
        string energy = weapon.energyCost > 0 ? $" · พลังงาน {weapon.energyCost}" : "";
        spec.details = $"{RarityName(weapon.rarity)} · ดาเมจ {weapon.attackDamage:0.#} · ตี {weapon.attackSpeed:0.#} ครั้ง/วิ{energy}" +
                       (string.IsNullOrEmpty(weapon.abilityDescription) ? "" : "\n" + weapon.abilityDescription);
        spec.place = (room, spot) =>
        {
            WeaponPickup.Create(weapon, room.transform, spot);
            return $"วาง {weapon.weaponName} (กด F เก็บ)";
        };
        BeginPlacement(spec);
    }

    // ---------- แท็บไอเทม ----------

    void BuildItems()
    {
        var supplies = catalog.chests.FirstOrDefault(t => t != null && t.hpPotionSprite != null);
        var pile = catalog.chests.FirstOrDefault(t => t != null && t.coinPileSprite != null);

        ui.Section("ของดรอป (กดแล้วคลิกในฉากเพื่อวาง)");
        var grid = ui.Grid(3);
        if (supplies != null)
        {
            ui.Button(grid, "ยาเลือด", DevConsoleUI.ButtonColor, () => BeginPlacement(LootSpec("ยาเลือด",
                      $"ฟื้นเลือด {supplies.hpRestore:0.#} ไม่เกินเลือดสูงสุด (แถบเต็มจะวางค้างบนพื้น)",
                      LootPickup.Kind.HpPotion, supplies.hpRestore, supplies.hpPotionSprite, supplies.potionSize, 1)));
            ui.Button(grid, "ยาพลังงาน", DevConsoleUI.ButtonColor, () => BeginPlacement(LootSpec("ยาพลังงาน",
                      $"ฟื้นพลังงาน {supplies.energyRestore} ไม่เกินพลังงานสูงสุด (แถบเต็มจะวางค้างบนพื้น)",
                      LootPickup.Kind.EnergyPotion, supplies.energyRestore, supplies.energyPotionSprite, supplies.potionSize, 1)));
            ui.Button(grid, "เหรียญ x10", DevConsoleUI.ButtonColor, () => BeginPlacement(LootSpec("เหรียญ x10",
                      "เหรียญละ 1 กระจายรอบจุดที่คลิก เดินใกล้แล้วดูดเข้าตัว",
                      LootPickup.Kind.Coin, 1f, supplies.coinSprite, supplies.coinSize, 10)));
        }
        if (pile != null)
            ui.Button(grid, "กองเหรียญ 25", DevConsoleUI.ButtonColor, () => BeginPlacement(LootSpec("กองเหรียญ 25",
                      "กองเดียวได้ 25 เหรียญ (แบบกล่องบอส)", LootPickup.Kind.Coin, 25f, pile.coinPileSprite, pile.coinPileSize, 1)));
        ui.Button(grid, "มานา x5", DevConsoleUI.ButtonColor, PlaceMana);

        ui.Section("กล่องสมบัติ (เดินเข้าใกล้แล้วเปิดเอง)");
        grid = ui.Grid(1);
        foreach (var table in catalog.chests)
        {
            if (table == null || table.chestPrefab == null) continue;
            var loot = table;
            ui.Button(grid, (loot.bossChest ? "กล่องบอส  " : "กล่องปกติ  ") + $"<size=75%>{loot.name}</size>",
                      loot.bossChest ? DevConsoleUI.BossColor : DevConsoleUI.ButtonColor, () => PlaceChest(loot));
        }
    }

    DevPlacement.Spec LootSpec(string title, string details, LootPickup.Kind kind, float amount, Sprite sprite, float size, int pieces)
    {
        placingMonster = null;
        var spec = new DevPlacement.Spec { title = title, details = details };
        FloorPicture(spec, sprite, size);
        spec.place = (room, spot) =>
        {
            for (int i = 0; i < pieces; i++)
            {
                var item = LootPickup.Create(kind, amount, sprite, size, room.transform, spot);
                if (pieces > 1) item.Toss(spot, spot + UnityEngine.Random.insideUnitCircle * 0.9f);
            }
            return "วาง" + title;
        };
        return spec;
    }

    void PlaceMana()
    {
        placingMonster = null;
        var spec = new DevPlacement.Spec { title = "มานา x5", details = "เม็ดละ +1 พลังงาน ลอยเข้าหาตัวเอง" };
        FloorPicture(spec, ProceduralSprites.Disc, 0.5f);
        spec.place = (room, spot) =>
        {
            ManaMotes.Spawn(spot, 5);
            return "ปล่อยมานา 5 เม็ด";
        };
        BeginPlacement(spec);
    }

    void PlaceChest(LootTable loot)
    {
        placingMonster = null;
        string kind = loot.bossChest ? "กล่องบอส" : "กล่องปกติ";
        var spec = new DevPlacement.Spec { title = $"{kind} ({loot.name})" };
        Picture(spec, loot.chestPrefab.gameObject, loot.chestPrefab.closedSprite);
        spec.body = Base(spec, 0f);
        string weapons = loot.weaponCount > 1 ? $"อาวุธ {loot.weaponCount} ชิ้น" : "อาวุธ 1 ชิ้น";
        float total = Mathf.Max(0.01f, loot.commonWeight + loot.rareWeight + loot.legendaryWeight);
        spec.details = $"เงิน {loot.currencyMin}-{loot.currencyMax} · ยาเลือด {loot.hpPotionCount} · ยาพลังงาน {loot.energyPotionCount} · {weapons}\n" +
                       $"โอกาสอาวุธ ทั่วไป {loot.commonWeight / total * 100f:0}% · หายาก {loot.rareWeight / total * 100f:0}% · " +
                       $"ตำนาน {loot.legendaryWeight / total * 100f:0}%";
        spec.place = (room, spot) =>
        {
            var chest = TreasureChest.Spawn(loot, room);
            if (chest == null) return "ตารางนี้ไม่มีกล่อง";
            chest.transform.position = spot; // Spawn หาที่ตามกฎห้องเอง ย้ายมาตรงที่คลิกก่อนกล่องเด้งโผล่ (Start)
            return $"วาง{kind}";
        };
        BeginPlacement(spec);
    }

    // ---------- แท็บพร ----------

    void BuildBlessings()
    {
        var manager = BlessingManager.Instance;
        if (manager == null) { ui.Note("ฉากนี้ไม่มี BlessingManager"); return; }

        ui.Note($"มีพรอยู่ {manager.Owned.Count}/{manager.maxBlessings} ช่อง · กดระดับที่ต้องการ (กดต่ำกว่าระดับที่มี = ล้างพรนั้นแล้วใส่ใหม่)");
        ui.Section("ใส่พร");
        foreach (var blessing in catalog.blessings)
        {
            if (blessing == null) continue;
            var have = manager.Get(blessing.type);
            int level = have != null ? manager.LevelOf(have) : 0;
            var row = ui.Row();
            ui.Label(row, blessing.nameThai, 2.4f, have != null ? new Color(0.55f, 0.92f, 1f) : (Color?)null);
            for (int lv = 1; lv <= 3; lv++)
            {
                int wanted = lv;
                if (lv > blessing.MaxLevel) { ui.Label(row, "", 1f); continue; }
                ui.Button(row, "Lv" + lv, level == lv ? DevConsoleUI.OnColor : DevConsoleUI.ButtonColor, () =>
                {
                    string error = manager.GrantForTesting(blessing, wanted);
                    Say(error ?? $"ได้พร {blessing.nameThai} Lv{wanted}");
                    Rebuild();
                });
            }
        }

        ui.Section("จัดการพร");
        var grid = ui.Grid(2);
        ui.Button(grid, "เปิดหน้าต่างเลือกพร", DevConsoleUI.ButtonColor, () =>
        {
            if (BlessingManager.IsChoosing) return;
            Close();
            if (!manager.OfferChoice(null)) Say("ไม่มีพรให้เลือกแล้ว (เต็มและอัปครบทุกอัน)");
        });
        ui.Button(grid, "ล้างพรทั้งหมด", DevConsoleUI.DangerColor, () =>
        {
            manager.ClearForTesting();
            Say("ล้างพรทั้งหมดแล้ว");
            Rebuild();
        });
    }

    // ---------- แท็บผู้เล่น ----------

    void BuildPlayer()
    {
        var hero = Player;
        if (hero != null)
            ui.Note($"เลือด {hero.currentHP:0.#}/{hero.maxHP:0.#} · พลังงาน {hero.currentEnergy}/{hero.maxEnergy} · เงิน {hero.currentCurrency}");

        ui.Section("สูตร");
        var grid = ui.Grid(2);
        Toggle(grid, "ไม่ตาย", () => DevCheats.GodMode, on => DevCheats.GodMode = on);
        Toggle(grid, "พลังงานไม่จำกัด", () => DevCheats.InfiniteEnergy, on => DevCheats.InfiniteEnergy = on);
        Toggle(grid, "สกิลไม่มีคูลดาวน์", () => DevCheats.NoSkillCooldown, on => DevCheats.NoSkillCooldown = on);
        ui.Note("ไม่ตาย: ยังโดนดาเมจและเลือดลดให้เห็น แต่เลือดไม่ต่ำกว่า 1");

        ui.Section("เลือด / พลังงาน / เงิน");
        grid = ui.Grid(3);
        PlayerButton(grid, "เติมเลือดเต็ม", p => p.Heal(p.maxHP), "เติมเลือดเต็ม");
        PlayerButton(grid, "เลือดเหลือ 1", p => p.SetHealthForTesting(1f), "เลือดเหลือ 1");
        PlayerButton(grid, "เติมพลังงาน", p => p.RestoreEnergy(p.maxEnergy), "เติมพลังงานเต็ม");
        PlayerButton(grid, "พลังงานเป็น 0", p => p.UseEnergy(p.currentEnergy), "พลังงานเป็น 0");
        PlayerButton(grid, "+50 เหรียญ", p => p.AddCurrency(50), "+50 เหรียญ");
        PlayerButton(grid, "+500 เหรียญ", p => p.AddCurrency(500), "+500 เหรียญ");

        ui.Section("สถานะผิดปกติ");
        grid = ui.Grid(2);
        PlayerButton(grid, "ติดไฟ 5 วิ", p => p.ApplyBurn(5f), "ติดไฟ 5 วินาที");
        PlayerButton(grid, "ติดพิษ 5 วิ", p => p.ApplyPoison(5f, 0.5f), "ติดพิษ 5 วินาที");

        if (catalog.characters != null && catalog.characters.Length > 0)
        {
            ui.Section("เปลี่ยนอาชีพ");
            grid = ui.Grid(2);
            foreach (var character in catalog.characters)
            {
                if (character == null) continue;
                var pick = character;
                Radio(grid, character.DisplayName, GameManager.selectedCharacter == character, () => ChangeClass(pick));
            }
            ui.Note("โหลดฉากใหม่แล้วกลับมาแมพเดิม อาวุธ พร และเงินเริ่มใหม่");
        }

        ui.Section("ความเร็วเกม");
        grid = ui.Grid(4);
        foreach (float scale in TimeScales)
        {
            float wanted = scale;
            Radio(grid, "x" + scale, Mathf.Approximately(DevCheats.TimeScale, scale), () =>
            {
                DevCheats.SetTimeScale(wanted);
                Say($"ความเร็วเกม x{wanted}");
            });
        }

        ui.Section("อื่น ๆ");
        grid = ui.Grid(1);
        ui.Button(grid, "ตายทันที (ดูหน้าสรุปตอนแพ้)", DevConsoleUI.DangerColor, () =>
        {
            var p = Player;
            if (p == null || p.isDead) return;
            Close();
            p.Kill();
        });
    }

    void PlayerButton(Transform grid, string label, Action<PlayerStats> action, string done)
    {
        ui.Button(grid, label, DevConsoleUI.ButtonColor, () =>
        {
            var hero = Player;
            if (hero == null || hero.isDead) { Say("ยังไม่มีผู้เล่น (หรือตายแล้ว)"); return; }
            action(hero);
            Say(done);
            Rebuild();
        });
    }

    void ChangeClass(CharacterData character)
    {
        var manager = MapManager.instance;
        GameManager.selectedCharacter = character;
        MapManager.startOverride = manager != null ? manager.CurrentMap : null;
        Close();
        // สูตรกับความเร็วเกมเป็น static อยู่ต่อในฉากใหม่ (PlayerStats ตั้งความเร็วกลับ 1 แล้ว LateUpdate ใส่ให้ใหม่)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ---------- แท็บแมพ ----------

    void BuildMaps()
    {
        var manager = MapManager.instance;
        var current = manager != null ? manager.CurrentMap : null;
        var room = CurrentRoom();
        ui.Note($"ตอนนี้: {(current != null ? current.mapName : "-")} · ห้อง {(room != null ? room.name : "-")}" +
                (room != null ? $" (มอนของห้องเหลือ {room.AliveMonstersCount})" : ""));

        ui.Section("ห้องทดสอบ");
        var grid = ui.Grid(1, 54);
        var lab = TestLab;
        if (lab != null)
            ui.Button(grid, "ไปห้องทดสอบ  <size=75%>(ห้องโล่ง มอนไม่เกิดเอง)</size>",
                      current == lab ? DevConsoleUI.OnColor : DevConsoleUI.ButtonColor, () => Warp(lab));
        ui.Note("ประตูในห้องทดสอบพาไปด่านแรก (เริ่มเกมจริง) ออกทางนี้ไม่ได้พร");

        ui.Section("วาร์ปไปแมพ");
        grid = ui.Grid(2);
        foreach (var map in catalog.maps)
        {
            if (map == null) continue;
            var target = map;
            ui.Button(grid, map.mapName + (map.isBossRoom ? "  <size=75%>(บอส)</size>" : ""),
                      map == current ? DevConsoleUI.OnColor : map.isBossRoom ? DevConsoleUI.BossColor : DevConsoleUI.ButtonColor,
                      () => Warp(target));
        }

        ui.Section("วางของในแมพ (กดแล้วคลิกในฉาก)");
        grid = ui.Grid(2);
        foreach (var shop in catalog.shops)
            if (shop != null && shop.prefab != null)
            {
                var item = shop;
                ui.Button(grid, item.label, DevConsoleUI.ButtonColor,
                          () => PlacePrefab(item, "สุ่มสินค้าใหม่ทุกครั้งที่วาง เดินเข้าใกล้แล้วกด F", 0.9f));
            }
        foreach (var trap in catalog.traps)
            if (trap != null && trap.prefab != null)
            {
                var item = trap;
                var spikes = item.prefab.GetComponent<FixedSpikeTrap>();
                string details = spikes != null
                    ? $"หนามขึ้นทุก {spikes.cycleSeconds:0.#} วิ เตือนก่อน {spikes.warningSeconds:0.#} วิ ดาเมจ {spikes.damage:0.#}"
                    : "กับดักบนพื้น";
                ui.Button(grid, item.label, DevConsoleUI.ButtonColor, () => PlacePrefab(item, details, 0f));
            }
    }

    MapData TestLab
    {
        get
        {
            if (testLab == null && catalog != null && catalog.labArena != null)
            {
                testLab = ScriptableObject.CreateInstance<MapData>();
                testLab.name = "TestLab";
                testLab.hideFlags = HideFlags.DontUnloadUnusedAsset; // โหลดฉากใหม่แล้วไม่โดนเก็บกวาดทิ้ง
                testLab.mapName = "ห้องทดสอบ";
                testLab.mapPrefab = catalog.labArena.mapPrefab;
                testLab.spawnPosition = catalog.labArena.spawnPosition;
                testLab.possibleEvents = new MapEventData[0];
                testLab.isTestLab = true;
            }
            if (testLab != null && MapManager.instance != null) testLab.nextMap = MapManager.instance.firstMap;
            return testLab;
        }
    }

    void Warp(MapData map)
    {
        var manager = MapManager.instance;
        if (manager == null || map == null) return;
        if (manager.IsLoading) { Say("กำลังโหลดแมพอยู่ รอสักครู่"); return; }
        if (BlessingManager.IsChoosing) { Say("เลือกพรให้เสร็จก่อน"); return; }
        var hero = Player;
        if (hero != null && hero.isDead) { Say("ผู้เล่นตายแล้ว"); return; }
        manager.LoadMap(map);
        Say($"วาร์ปไป {map.mapName}");
        Rebuild();
    }

    // ของที่เป็น prefab (ร้าน กับดัก) baseHeight > 0 = เช็คพื้นว่างแค่ฐาน (ตัวร้านสูง หัวเกยผนังได้)
    void PlacePrefab(DevCatalog.Placeable item, string details, float baseHeight)
    {
        placingMonster = null;
        var spec = new DevPlacement.Spec { title = item.label, details = details };
        Picture(spec, item.prefab);
        spec.body = Base(spec, baseHeight);
        spec.place = (room, spot) =>
        {
            Instantiate(item.prefab, spot, Quaternion.identity, room.transform);
            return "วาง" + item.label;
        };
        BeginPlacement(spec);
    }

    // ---------- แท็บเสียง ----------

    // กดเพื่อฟังเสียงของแต่ละเหตุการณ์ตามที่ตั้งไว้ในคลังเสียง (ความดัง/ระดับเสียง/สุ่มแบบย่อย เหมือนในเกมจริง)
    void BuildSounds()
    {
        var library = Sfx.Library;
        if (library == null || library.entries == null)
        {
            ui.Note("ไม่เจอคลังเสียง Resources/SfxLibrary (กด Play ใน Unity ใหม่อีกครั้งให้ Editor สร้างให้)");
            return;
        }
        ui.Note($"{library.entries.Length} เหตุการณ์ · กดเพื่อฟัง (กดซ้ำ = สุ่มแบบย่อยถัดไป) · ปรับความดัง/ระดับเสียงได้ที่ Resources/SfxLibrary ใน Inspector");
        var grid = ui.Grid(2, 56);
        foreach (var entry in library.entries)
        {
            if (entry == null) continue;
            var sound = entry;
            int count = sound.clips != null ? sound.clips.Length : 0;
            string clip = count > 0 && sound.clips[0] != null ? sound.clips[0].name : "ไม่มีไฟล์";
            ui.Button(grid, $"{sound.id}\n<size=70%>{clip} · {count} ไฟล์</size>",
                      count > 0 ? DevConsoleUI.ButtonColor : DevConsoleUI.DangerColor, () =>
                      {
                          Sfx.Play(sound.id);
                          Say($"เล่นเสียง {sound.id} (ความดัง {sound.volume:0.##} ระดับเสียง {sound.pitch:0.##})");
                      });
        }
    }

    // ---------- ตัวช่วย ----------

    PlayerStats Player
    {
        get
        {
            if (player == null)
            {
                var hero = GameObject.FindGameObjectWithTag("Player");
                player = hero != null ? hero.GetComponent<PlayerStats>() : null;
            }
            return player;
        }
    }

    RoomController CurrentRoom()
    {
        var hero = Player;
        return hero != null ? RoomAt(hero.transform.position) : null;
    }

    // ห้องที่จุดนี้อยู่ (ทางเดินระหว่างห้อง = ห้องที่ใกล้ที่สุด) นับเฉพาะผังที่เปิดใช้อยู่
    RoomController RoomAt(Vector2 at)
    {
        var root = MapManager.instance != null ? MapManager.instance.CurrentMapRoot : null;
        if (root == null) return null;
        RoomController nearest = null;
        float best = float.MaxValue;
        foreach (var room in root.GetComponentsInChildren<RoomController>(false))
            foreach (var area in room.GetComponents<Collider2D>())
            {
                if (!area.isTrigger || !area.enabled) continue;
                if (area.OverlapPoint(at)) return room;
                float distance = Vector2.Distance(area.ClosestPoint(at), at);
                if (distance < best) { best = distance; nearest = room; }
            }
        return nearest;
    }
}
