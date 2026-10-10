using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// คอนโซลทดสอบ: กด F2 ในฉากเกม เปิดแผงด้านขวาไว้เสกมอน/บอส/อาวุธ/ไอเทม/พร ปรับสถานะผู้เล่น และวาร์ปแมพ
// ของที่วางในฉาก (มอน บอส ของดรอป กล่อง ร้าน กับดัก) กดปุ่มแล้วขึ้นการ์ดตัวอย่าง คลิกในฉากเพื่อวาง (DevPlacement)
// ไม่ต้องวางอะไรในฉาก: สร้างตัวเองตอนเริ่มเกม (อยู่ข้ามฉาก) สร้างหน้าต่างจากโค้ดตอนเปิดครั้งแรก
// รายชื่อของอ่านจาก Resources/DevCatalog (Editor เติมให้เองทุกครั้งที่กด Play)
// คลิกโลโก้เมนูหลัก 5 ครั้งเพื่อปลดล็อกก่อนเห็นแถวตั้งค่า ค่าเริ่มต้นปิดและไม่เปิดอัตโนมัติตอนปลดล็อก
// ปิดได้ในหน้าตั้งค่า (แถว "คอนโซลทดสอบ") ปิดแล้วกด F2 ไม่ขึ้น และสูตรที่เปิดค้างไว้กลับเป็นปกติ
// F1 เป็นของหน้าคู่มือการเล่น (GameHelpWindow)
public sealed class DevConsole : MonoBehaviour
{
    public const KeyCode ToggleKey = KeyCode.F2;
    const string EnabledKey = "quantumrift.devconsole";
    const string UnlockedKey = "quantumrift.devconsole.unlocked";
    const string TabKey = "quantumrift.devconsole.tab";

    public static DevConsole Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.ui != null && Instance.ui.Visible;
    // ESC ที่ใช้ปิดคอนโซลไม่นับเป็นการกดพักเกม (PauseManager เช็คตัวนี้)
    public static bool BlocksEscape => IsOpen || Time.frameCount == closedFrame;
    // กำลังคลิกวางของ: คลิกในฉากไม่ใช่การโจมตี (WeaponController เช็คตัวนี้)
    public static bool Placing => IsOpen && Instance.placement != null && Instance.placement.Active;
    static int closedFrame = -1;

    public static bool Unlocked => PlayerPrefs.GetInt(UnlockedKey, 0) == 1;
    public static event Action AccessChanged;

    // จำการปลดล็อกข้ามการเปิดเกม แต่การปลดล็อกไม่เท่ากับอนุญาตเปิดสูตรทันที
    // ค่าที่เคยเปิดไว้ก่อนมีระบบล็อกจะใช้ไม่ได้ จนคลิกโลโก้ครบและเปิดสวิตช์อีกครั้ง
    public static void UnlockFromLogo()
    {
        if (Unlocked) return;
        PlayerPrefs.SetInt(UnlockedKey, 1);
        Enabled = false;
        Debug.Log("ปลดล็อกคอนโซลทดสอบแล้ว เปิดใช้งานได้จากหน้าตั้งค่า (ค่าเริ่มต้นปิด)");
    }

    public static bool Enabled
    {
        get => Unlocked && PlayerPrefs.GetInt(EnabledKey, 0) == 1;
        set
        {
            bool enabled = value && Unlocked;
            PlayerPrefs.SetInt(EnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            if (enabled) RunHistory.MarkTestRun(); // เปิดแล้วปิดสูตรก่อนจบรอบก็ไม่นับเป็นสถิติจริง
            if (!enabled)
            {
                DevCheats.ResetAll();
                if (Instance != null) Instance.Close();
            }
            AccessChanged?.Invoke();
        }
    }

    static readonly string[] Tabs = { "มอน", "บอส", "อาวุธ", "ไอเทม", "พร", "ผู้เล่น", "แมพ", "เสียง", "ทดสอบ" };
    static readonly float[] DamageScales = { 1f, 5f, 20f, 9999f };
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
        if (!Enabled) DevCheats.ResetAll(); // กันสูตรเก่าค้างเมื่อปิดการโหลดโดเมนใน Unity
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
        // End = ถ่ายภาพหน้าจอเกม (Shift+End = ละเอียดสองเท่า) ใช้ได้ทุกฉาก
        if (Enabled && Input.GetKeyDown(KeyCode.End))
            Screenshot(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 2 : 1);
        // Home = สลับเซฟทดสอบ/เซฟจริง ใช้ได้ทั้งในเมนูหลักและในเกม (ค่านี้จำข้ามการกด Play จึงต้องสลับกลับได้จากทุกที่)
        // ไม่ใช้ Shift+F2: เครื่อง HP OMEN ใช้คีย์นั้นเปิดหน้าต่างซ้อนทับของตัวเอง
        if (Enabled && Input.GetKeyDown(KeyCode.Home))
        {
            RunSave.UseTestFile = !RunSave.UseTestFile;
            Done("ใช้" + (RunSave.UseTestFile ? "เซฟทดสอบ (ไม่แตะเซฟจริง)" : "เซฟจริง") + ": " + RunSave.DebugSummary());
            ContinueMenu.RefreshNow();
        }
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
        Hotkeys();
        if (!IsOpen) return;
        // แมพเปลี่ยนเสร็จแล้ว (วาร์ป/เดินผ่านประตู): วาดแท็บใหม่ ปุ่มแมพปัจจุบันและสถานะจะได้ไม่ค้างของแมพเก่า
        var shownNow = MapManager.instance.CurrentMap;
        if (shownNow != shownMap && !MapManager.instance.IsLoading && !placement.Active) Rebuild();
        if (placement.Active)
        {
            // ระหว่างวาง ESC/คลิกขวาเลิกวางอย่างเดียว คอนโซลยังเปิดอยู่
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) CancelPlacement();
            else TickPlacement();
        }
        else if (Input.GetKeyDown(KeyCode.Escape)) Close();
        else if (Input.GetKeyDown(KeyCode.PageDown)) ui.ScrollBy(420f);
        else if (Input.GetKeyDown(KeyCode.PageUp)) ui.ScrollBy(-420f);
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
        if (!Enabled || MapManager.instance == null) return; // กันการเรียกตรงข้ามระบบล็อกด้วย
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
        shownMap = MapManager.instance != null ? MapManager.instance.CurrentMap : null;
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
                case 7: BuildSounds(); break;
                default: BuildTesting(); break;
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
        PlayerButton(grid, "ติดไฟ 2 วิ", p => p.ApplyBurn(2f), "ติดไฟ 2 วินาที (1 หน่วยต่อวินาที)");
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

    // ---------- แท็บทดสอบ: ทางลัดสำหรับไล่ทดสอบทั้งเกม ----------

    MapData shownMap;

    // ปุ่มลัดใช้ได้แม้คอนโซลปิดอยู่ (กดคีย์แม่นกว่าคลิกปุ่ม) F1 = คู่มือ F2 = คอนโซล เลี่ยง F10-F12 ที่ระบบ/Editor ใช้
    static readonly string[] HotkeyHelp =
    {
        "F3 ไม่ตาย", "F4 ดาเมจ x1/x5/x20/ตีทีเดียวตาย", "F5 หยุด AI", "F6 ความเร็วเกม",
        "F7 ฆ่ามอนในห้อง", "F8 วาร์ปไปเป้าถัดไป", "F9 บอสสุดท้าย: แปลงร่าง → ฉากแกนกลาง",
        "Shift+F3 แกนเหลือ 1", "Shift+F4 แมพถัดไป", "Shift+F5 โหลดแมพนี้ใหม่", "Shift+F6 เติมเลือด/พลังงาน",
        "Shift+F7 ตายทันที", "Shift+F8 บันทึกเดี๋ยวนี้", "Shift+F9 ลบเซฟ", "Home สลับเซฟทดสอบ/เซฟจริง (ใช้ในเมนูหลักได้)",
        "PageUp/PageDown เลื่อนรายการในคอนโซล", "End ถ่ายภาพหน้าจอเกม (Shift+End ละเอียดสองเท่า)",
    };

    void Hotkeys()
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (!shift)
        {
            if (Input.GetKeyDown(KeyCode.F3)) { DevCheats.GodMode = !DevCheats.GodMode; Done($"ไม่ตาย: {OnOff(DevCheats.GodMode)}"); }
            else if (Input.GetKeyDown(KeyCode.F4)) { DevCheats.DamageScale = Next(DamageScales, DevCheats.DamageScale); Done("ดาเมจผู้เล่น " + DamageLabel(DevCheats.DamageScale)); }
            else if (Input.GetKeyDown(KeyCode.F5)) { DevCheats.FreezeMonsters = !DevCheats.FreezeMonsters; Done($"หยุด AI มอน/บอส: {OnOff(DevCheats.FreezeMonsters)}"); }
            else if (Input.GetKeyDown(KeyCode.F6)) { DevCheats.SetTimeScale(Next(TimeScales, DevCheats.TimeScale)); Done($"ความเร็วเกม x{DevCheats.TimeScale}"); }
            else if (Input.GetKeyDown(KeyCode.F7)) KillRoomMonsters();
            else if (Input.GetKeyDown(KeyCode.F8)) TeleportToNextTarget();
            else if (Input.GetKeyDown(KeyCode.F9)) AdvanceArchitect();
            return;
        }
        if (Input.GetKeyDown(KeyCode.F3)) SetCoreHealth(1f);
        else if (Input.GetKeyDown(KeyCode.F4)) WarpNext();
        else if (Input.GetKeyDown(KeyCode.F5)) { if (MapManager.instance.CurrentMap != null) Warp(MapManager.instance.CurrentMap); }
        else if (Input.GetKeyDown(KeyCode.F6)) Refill();
        else if (Input.GetKeyDown(KeyCode.F7)) { var p = Player; if (p != null && !p.isDead) { Close(); p.Kill(); } }
        else if (Input.GetKeyDown(KeyCode.F8)) SaveNow();
        else if (Input.GetKeyDown(KeyCode.F9)) { RunSave.Delete(); Done("ลบเซฟแล้ว" + SaveTag()); }
    }

    static string OnOff(bool on) => on ? "เปิด" : "ปิด";
    static string DamageLabel(float scale) => scale >= 9999f ? "ตีทีเดียวตาย" : "x" + scale;
    static string SaveTag() => RunSave.UseTestFile ? " (เซฟทดสอบ)" : " (เซฟจริง)";

    static float Next(float[] steps, float current)
    {
        for (int i = 0; i < steps.Length; i++)
            if (Mathf.Approximately(steps[i], current)) return steps[(i + 1) % steps.Length];
        return steps[0];
    }

    void Done(string text)
    {
        Say(text);
        if (IsOpen) Rebuild();
    }

    void BuildTesting()
    {
        var manager = MapManager.instance;
        var current = manager != null ? manager.CurrentMap : null;
        var room = CurrentRoom();
        ui.Note($"ตอนนี้: {(current != null ? current.mapName : "-")} · ห้อง {(room != null ? room.name : "-")}" +
                (room != null ? $" (มอนเหลือ {room.AliveMonstersCount}{(room.IsCleared ? " เคลียร์แล้ว" : "")})" : ""));

        ui.Section("ดาเมจของผู้เล่น");
        var grid = ui.Grid(4);
        foreach (float scale in DamageScales)
        {
            float wanted = scale;
            Radio(grid, DamageLabel(scale), Mathf.Approximately(DevCheats.DamageScale, scale), () =>
            {
                DevCheats.DamageScale = wanted;
                Say("ดาเมจผู้เล่น " + DamageLabel(wanted));
            });
        }
        ui.Note("คูณดาเมจทุกแบบของผู้เล่น (อาวุธ สกิล พร) รวมตอนตีกล่อง กำแพง และแกนบอส");

        ui.Section("ข้ามการต่อสู้");
        grid = ui.Grid(2);
        ui.Button(grid, "ฆ่ามอนในห้องนี้", DevConsoleUI.DangerColor, KillRoomMonsters);
        ui.Button(grid, "เติมเลือด/พลังงาน", DevConsoleUI.ButtonColor, Refill);
        ui.Note("ห้องที่มีมอนหลายระลอก กดซ้ำจนกว่าห้องจะเคลียร์");

        ui.Section("วาร์ปผู้เล่น (ในแมพนี้)");
        grid = ui.Grid(2);
        ui.Button(grid, "ไปเป้าถัดไป", DevConsoleUI.OnColor, TeleportToNextTarget);
        ui.Button(grid, "ไปหาบอส / แกน", DevConsoleUI.BossColor, () => TeleportTo(BossSpot(), "บอส"));
        ui.Button(grid, "ไปห้องที่ยังไม่เคลียร์", DevConsoleUI.ButtonColor, () => TeleportTo(NextRoomSpot(), "ห้องที่ยังไม่เคลียร์"));
        ui.Button(grid, "ไปประตูออก", DevConsoleUI.ButtonColor, () => TeleportTo(PortalSpot(), "ประตูออก"));
        ui.Note("เป้าถัดไป = แกนบอส > บอส > ห้องที่ยังไม่เคลียร์ > ประตูออก");

        ui.Section("แมพ");
        grid = ui.Grid(2);
        ui.Button(grid, "แมพถัดไป", DevConsoleUI.ButtonColor, WarpNext);
        ui.Button(grid, "โหลดแมพนี้ใหม่", DevConsoleUI.ButtonColor, () => { if (current != null) Warp(current); });

        ui.Section("บอสตัวสุดท้าย (Architect)");
        grid = ui.Grid(1);
        ui.Button(grid, "ขั้นถัดไป: แปลงร่าง → ฉากแกนกลางถล่ม", DevConsoleUI.BossColor, AdvanceArchitect);
        grid = ui.Grid(2);
        ui.Button(grid, "เลือดแกนเหลือ 1", DevConsoleUI.ButtonColor, () => SetCoreHealth(1f));
        ui.Button(grid, "เลือดแกนเต็ม", DevConsoleUI.ButtonColor, () => SetCoreHealth(float.MaxValue));
        ui.Note("ต้องเดินเข้าสนามให้บอสเริ่มสู้ก่อน แกนเหลือ 1 แล้วตีอีกครั้ง = แกนแตก (ดูทางชนะ) ปล่อยให้หมดเวลา = บอสกลับมา");

        ui.Section("เซฟ");
        ui.Note((RunSave.UseTestFile ? "เซฟทดสอบ: " : "เซฟจริง: ") + RunSave.DebugSummary());
        grid = ui.Grid(1);
        Toggle(grid, "ใช้เซฟทดสอบ (ไม่แตะเซฟจริง)", () => RunSave.UseTestFile, on => RunSave.UseTestFile = on);
        grid = ui.Grid(2);
        ui.Button(grid, "บันทึกเดี๋ยวนี้", DevConsoleUI.ButtonColor, SaveNow);
        ui.Button(grid, "ลบเซฟ", DevConsoleUI.DangerColor, () => { RunSave.Delete(); Done("ลบเซฟแล้ว" + SaveTag()); });
        ui.Note("เปิดเซฟทดสอบแล้ว เกมอ่าน/เขียนไฟล์แยกทั้งในเมนูหลักและในเกม จนกว่าจะปิด เซฟจริงอยู่ครบ");

        ui.Section("ปุ่มลัด (ใช้ได้แม้ปิดคอนโซล)");
        ui.Note(string.Join("\n", HotkeyHelp));
    }

    void KillRoomMonsters()
    {
        var room = CurrentRoom();
        int killed = 0;
        foreach (var monster in FindObjectsByType<MonsterController>(FindObjectsSortMode.None))
        {
            if (!monster.IsAlive || IsBoss(monster)) continue;
            if (room != null && monster.currentRoom != null && monster.currentRoom != room) continue;
            monster.SelfDestruct();
            killed++;
        }
        Done(killed > 0 ? $"ฆ่ามอน {killed} ตัว" : "ไม่มีมอนในห้องนี้");
    }

    void Refill()
    {
        var hero = Player;
        if (hero == null || hero.isDead) { Say("ยังไม่มีผู้เล่น (หรือตายแล้ว)"); return; }
        hero.Heal(hero.maxHP);
        hero.RestoreEnergy(hero.maxEnergy);
        Done("เติมเลือดและพลังงานเต็ม");
    }

    void WarpNext()
    {
        var current = MapManager.instance != null ? MapManager.instance.CurrentMap : null;
        if (current == null || current.nextMap == null) { Say("ไม่มีแมพถัดไป (ด่านสุดท้ายแล้ว)"); return; }
        Warp(current.nextMap);
    }

    void SaveNow()
    {
        var manager = MapManager.instance;
        var hero = Player;
        if (manager == null || hero == null) { Say("ยังไม่มีผู้เล่น"); return; }
        RunSave.Capture(manager.CurrentMap, hero.gameObject);
        Done("บันทึกแล้ว" + SaveTag() + ": " + RunSave.DebugSummary());
    }

    // Architect: ร่าง 1 → ตั้งเลือดให้แปลงร่าง, ร่าง 2 → เข้าฉากแกนกลางถล่มทันที (ไม่ต้องไล่ตีให้เลือดหมด)
    void AdvanceArchitect()
    {
        var architect = LiveArchitect();
        if (architect == null) { Say("ไม่มี Architect ในแมพนี้ (วาร์ปไปห้องบอสแมพ 3)"); return; }
        if (!architect.SecondForm)
        {
            Done(architect.SetHealthForTesting(0.5f) ? "Architect กำลังแปลงร่าง กดอีกครั้งหลังแปลงเสร็จเพื่อเข้าฉากแกนกลาง"
                                                    : "Architect ยังไม่เริ่มสู้ (เดินเข้าสนามก่อน) หรือกำลังแปลงร่างอยู่");
            return;
        }
        Done(architect.ForceLastStandForTesting() ?? "เข้าฉากแกนกลางถล่ม: ร่างเงาลอยเหนือบ่อกลางห้อง ตีให้แตกใน 10 วินาที");
    }

    void SetCoreHealth(float value)
    {
        var core = FindFirstObjectByType<ArchitectCore>();
        if (core == null) { Say("แกนยังไม่เปิด (เข้าฉากแกนกลางก่อน)"); return; }
        core.SetHealthForTesting(value);
        Done($"เลือดแกน {core.Health:0.#}/{core.MaxHealth:0.#}");
    }

    // ---------- วาร์ปผู้เล่นในแมพ ----------

    void TeleportToNextTarget()
    {
        Vector2? spot = BossSpot();
        string what = "บอส";
        if (!spot.HasValue) { spot = NextRoomSpot(); what = "ห้องที่ยังไม่เคลียร์"; }
        if (!spot.HasValue) { spot = PortalSpot(); what = "ประตูออก"; }
        TeleportTo(spot, what);
    }

    void TeleportTo(Vector2? spot, string what)
    {
        var hero = Player;
        if (hero == null || hero.isDead) { Say("ยังไม่มีผู้เล่น (หรือตายแล้ว)"); return; }
        if (!spot.HasValue) { Say("ไม่เจอ" + what + "ในแมพนี้"); return; }
        hero.transform.position = spot.Value;
        var body = hero.GetComponent<Rigidbody2D>();
        if (body != null) { body.position = spot.Value; body.linearVelocity = Vector2.zero; }
        Done("วาร์ปไป" + what);
    }

    // ยืนใต้เป้าเล็กน้อย: แกนกลาง (ถ้าเปิดอยู่) ก่อน แล้วค่อยบอสที่ยังไม่ตาย
    Vector2? BossSpot()
    {
        var core = FindFirstObjectByType<ArchitectCore>();
        if (core != null) return (Vector2)core.transform.position + Vector2.down * 2.2f;
        var architect = LiveArchitect();
        if (architect != null)
        {
            var body = architect.hitbox != null && architect.hitbox.enabled ? (Vector2)architect.hitbox.bounds.center : (Vector2)architect.transform.position;
            return body + Vector2.down * 2.5f;
        }
        foreach (var boss in LiveBosses()) return (Vector2)boss.transform.position + Vector2.down * 2.5f;
        return null;
    }

    Vector2? NextRoomSpot()
    {
        var root = MapManager.instance != null ? MapManager.instance.CurrentMapRoot : null;
        var hero = Player;
        if (root == null || hero == null) return null;
        var here = CurrentRoom();
        Vector2? best = null;
        float nearest = float.MaxValue;
        foreach (var room in root.GetComponentsInChildren<RoomController>(false))
        {
            if (room.IsCleared || room == here) continue;
            foreach (var area in room.GetComponents<Collider2D>())
            {
                if (!area.isTrigger || !area.enabled) continue;
                float distance = Vector2.Distance(area.bounds.center, hero.transform.position);
                if (distance < nearest) { nearest = distance; best = area.bounds.center; }
                break;
            }
        }
        return best;
    }

    Vector2? PortalSpot()
    {
        var root = MapManager.instance != null ? MapManager.instance.CurrentMapRoot : null;
        if (root == null) return null;
        foreach (var portal in root.GetComponentsInChildren<MapPortal>(false))
        {
            var area = portal.GetComponent<Collider2D>();
            return area != null ? (Vector2)area.bounds.center : (Vector2)portal.transform.position;
        }
        return null;
    }

    // ---------- แถบสถานะ: ขึ้นเมื่อมีสูตรเปิดอยู่ (ตัวอักษรระบบ จึงใช้อังกฤษ) ----------

    GUIStyle stripStyle;

    // ---------- ถ่ายภาพหน้าจอ ----------

    // ถ่ายจากภาพที่เกมเรนเดอร์จริง (ความละเอียดของจอเกม เช่น 1920x1080) ไม่ใช่ถ่ายจากหน้าต่าง Unity ที่ถูกย่อจนเบลอ
    // HUD อยู่ในภาพตามปกติ แถบสถานะของคอนโซลทดสอบถูกซ่อนช่วงถ่าย คอนโซลที่เปิดอยู่จะติดในภาพ (ปิดก่อนถ้าไม่ต้องการ)
    // ไฟล์อยู่ในโฟลเดอร์ Screenshots ข้างโฟลเดอร์ Assets (นอกโปรเจกต์ที่ Unity import และไม่เข้า git)
    int hideStripUntil = -1;
    public static string LastScreenshot { get; private set; }

    void Screenshot(int scale)
    {
        string folder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "Screenshots");
        System.IO.Directory.CreateDirectory(folder);
        var map = MapManager.instance != null ? MapManager.instance.CurrentMap : null;
        string name = (map != null ? map.name : SceneManager.GetActiveScene().name) + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", System.Globalization.CultureInfo.InvariantCulture) + ".png";
        LastScreenshot = System.IO.Path.Combine(folder, name);
        hideStripUntil = Time.frameCount + 3; // ไฟล์ถูกเขียนตอนจบเฟรม เผื่อไว้อีกสองเฟรม
        ScreenCapture.CaptureScreenshot(LastScreenshot, scale);
        Debug.Log($"[คอนโซลทดสอบ] ถ่ายภาพหน้าจอ {Screen.width * scale}x{Screen.height * scale}: {LastScreenshot}");
    }

    void OnGUI()
    {
        if (!Enabled || Time.frameCount <= hideStripUntil) return;
        // เมนูหลัก: ขึ้นเฉพาะตอนใช้เซฟทดสอบ (ปุ่มเล่นต่อจะอิงไฟล์ทดสอบ ไม่ใช่เซฟจริง ต้องเห็นว่าเปิดค้างอยู่)
        bool inGame = MapManager.instance != null;
        if (!inGame && !RunSave.UseTestFile) return;
        var parts = new List<string>();
        if (inGame && DevCheats.GodMode) parts.Add("GOD");
        if (!Mathf.Approximately(DevCheats.DamageScale, 1f)) parts.Add(DevCheats.DamageScale >= 9999f ? "DMG ONE-HIT" : "DMG x" + DevCheats.DamageScale);
        if (DevCheats.FreezeMonsters) parts.Add("AI OFF");
        if (!Mathf.Approximately(DevCheats.TimeScale, 1f)) parts.Add("SPEED x" + DevCheats.TimeScale);
        if (DevCheats.InfiniteEnergy) parts.Add("ENERGY");
        if (DevCheats.NoSkillCooldown) parts.Add("NO CD");
        if (RunSave.UseTestFile) parts.Add("TEST SAVE (Home = real save)");
        if (parts.Count == 0) return;
        var map = inGame ? MapManager.instance.CurrentMap : null;
        string text = "DEV  |  " + (inGame ? (map != null ? map.name : "-") : "MENU") + "  |  " + string.Join("  |  ", parts);
        if (stripStyle == null)
            stripStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        stripStyle.fontSize = Mathf.Max(11, Screen.height / 54);
        float width = Mathf.Min(Screen.width * 0.6f, 900f), height = stripStyle.fontSize + 10f;
        var box = new Rect((Screen.width - width) * 0.5f, Screen.height - height - 4f, width, height);
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = new Color(1f, 0.85f, 0.35f);
        GUI.Label(box, text, stripStyle);
        GUI.color = Color.white;
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
