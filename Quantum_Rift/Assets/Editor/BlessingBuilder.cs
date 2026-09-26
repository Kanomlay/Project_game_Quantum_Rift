using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// พรควอนตัม (ชุดไอคอน QuantumRift-BuffIcons-v1): จบด่านแล้วเลือก 1 จาก 3 พรสุ่ม สะสมได้สูงสุด 4 พรต่อรอบ
// 1. ตั้งค่าไอคอนเป็น Sprite เดี่ยว ฟิลเตอร์ Point ไม่บีบอัด (ขนาด 128 ใช้บนการ์ด 64 ใช้บน HUD ตรงขนาดจริง ภาพจะคม)
// 2. BlessingData 9 ไฟล์ใน Data/Blessing: ข้อความ/ไอคอน/ภาพคลื่นเขียนทับทุกครั้ง ตัวเลขใส่ให้เฉพาะตอนสร้าง (ปรับเองใน Inspector)
// 3. ฉากเกม: Blessing_Panel (หน้าต่างเลือกพร) + GameplayHUD/BlessingHUD (แถบพรใต้หลอดเลือด) + BlessingManager แล้วต่อช่องให้ครบ
// สั่งซ้ำได้: ตำแหน่ง/ขนาด/สีของ UI ตั้งให้เฉพาะชิ้นที่เพิ่งสร้าง ลากปรับใน Scene view แล้วไม่โดนทับ
public static class BlessingBuilder
{
    const string IconFolder = "Assets/image/UI_Image/QuantumRift-BuffIcons-v1";
    const string DataParent = "Assets/Data";
    const string DataFolder = DataParent + "/Blessing";
    const string WaveSource = "Assets/Data/Weapon/Rift Sword.asset"; // คลื่นสะสมใช้ภาพคลื่นของดาบผ่ามิติ ย้อมสีต่าง
    const string PanelName = "Blessing_Panel";
    const string UndoName = "Setup Blessings";

    static readonly Vector2 WindowSize = new Vector2(1240f, 720f);
    static readonly Vector2 CardSize = new Vector2(360f, 460f);
    const float CardSpacing = 400f;
    static readonly Color Muted = new Color(0.72f, 0.74f, 0.86f);
    const int Slots = 4;
    const float SlotSize = 64f, SlotGap = 10f;

    sealed class Spec
    {
        public string File, Icon;
        public BlessingType Type;
        public BlessingCategory Category;
        public string NameTh, NameEn, AbilityTh, AbilityEn, LimitTh, LimitEn;
        public int Count, PerRoomCap;
        public float Amount, Threshold, Duration, Cooldown, Radius;
    }

    // ตัวเลขตามตารางพรที่ผู้ใช้ให้มา (ข้อความเอ่ยถึงตัวเลขเดียวกัน ถ้าปรับตัวเลขใน Inspector ต้องแก้ข้อความตามด้วย)
    static readonly Spec[] Specs =
    {
        new Spec { File = "01 BulletCleave", Icon = "01-bullet-cleave", Type = BlessingType.BulletCleave, Category = BlessingCategory.Attack,
                   NameTh = "คมสลายมิติ", NameEn = "Bullet Cleave",
                   AbilityTh = "ฟันลบกระสุนศัตรูในแนวโจมตี", AbilityEn = "Melee strikes erase enemy bullets in their path.",
                   LimitTh = "สูงสุด 3 นัดต่อการฟัน ไม่ลบเลเซอร์หรือพื้นอันตราย", LimitEn = "Up to 3 bullets per strike. Lasers and hazard floors are not erased.",
                   Count = 3 },
        new Spec { File = "02 PhasePiercing", Icon = "02-phase-piercing", Type = BlessingType.PhasePiercing, Category = BlessingCategory.Attack,
                   NameTh = "กระสุนทะลุมิติ", NameEn = "Phase Piercing",
                   AbilityTh = "กระสุนและลูกธนูทะลุศัตรูเพิ่ม 1 ตัว", AbilityEn = "Bullets and arrows pierce 1 extra enemy.",
                   LimitTh = "ไม่ใช้กับจรวด ศัตรูตัวถัดไปรับดาเมจ 60%", LimitEn = "Not for rockets. The next enemy takes 60% damage.",
                   Count = 1, Amount = 0.6f },
        new Spec { File = "03 ChargedWave", Icon = "03-charged-wave", Type = BlessingType.ChargedWave, Category = BlessingCategory.Attack,
                   NameTh = "คลื่นสะสม", NameEn = "Charged Wave",
                   AbilityTh = "โจมตีโดนศัตรูครบ 5 ครั้ง ปล่อยคลื่นพลัง", AbilityEn = "Every 5 hits on enemies release an energy wave.",
                   LimitTh = "คลื่นทำดาเมจ 50% ของอาวุธ นับสูงสุดครั้งเดียวต่อการโจมตี", LimitEn = "The wave deals 50% weapon damage. Each attack counts once.",
                   Count = 5, Amount = 0.5f, Radius = 0.7f },
        new Spec { File = "04 EmergencyShield", Icon = "04-emergency-shield", Type = BlessingType.EmergencyShield, Category = BlessingCategory.Defense,
                   NameTh = "เกราะฉุกเฉิน", NameEn = "Emergency Shield",
                   AbilityTh = "เมื่อเลือดลดถึง 30% ได้เกราะรับความเสียหายเท่ากับ 15% ของเลือดสูงสุด นาน 4 วินาที",
                   AbilityEn = "At 30% HP, gain a shield worth 15% of max HP for 4 seconds.",
                   LimitTh = "ทำงานได้ห้องละ 1 ครั้ง", LimitEn = "Once per room.",
                   Threshold = 0.3f, Amount = 0.15f, Duration = 4f },
        new Spec { File = "05 RiftStep", Icon = "05-rift-step", Type = BlessingType.RiftStep, Category = BlessingCategory.Defense,
                   NameTh = "ก้าวพ้นรอยแยก", NameEn = "Rift Step",
                   AbilityTh = "หลังรับดาเมจ เคลื่อนที่เร็วขึ้น 25% นาน 2 วินาที", AbilityEn = "After taking damage, move 25% faster for 2 seconds.",
                   LimitTh = "คูลดาวน์ 8 วินาที", LimitEn = "8-second cooldown.",
                   Amount = 0.25f, Duration = 2f, Cooldown = 8f },
        new Spec { File = "06 BulletSlowField", Icon = "06-bullet-slow-field", Type = BlessingType.BulletSlowField, Category = BlessingCategory.Defense,
                   NameTh = "สนามชะลอกระสุน", NameEn = "Bullet Slow Field",
                   AbilityTh = "กระสุนศัตรูที่อยู่ใกล้ตัวเคลื่อนที่ช้าลง 20%", AbilityEn = "Enemy bullets near you move 20% slower.",
                   LimitTh = "รัศมีสั้น ไม่ส่งผลต่อเลเซอร์หรือความเร็วศัตรู", LimitEn = "Short radius. No effect on lasers or enemy speed.",
                   Amount = 0.2f, Radius = 2.5f },
        new Spec { File = "07 EnergyHarvest", Icon = "07-energy-harvest", Type = BlessingType.EnergyHarvest, Category = BlessingCategory.Resource,
                   NameTh = "เก็บเกี่ยวพลังงาน", NameEn = "Energy Harvest",
                   AbilityTh = "กำจัดศัตรูด้วยตัวเองแล้วฟื้นพลังงาน 2 หน่วย", AbilityEn = "Defeating an enemy restores 2 energy.",
                   LimitTh = "ฟื้นได้สูงสุด 10 หน่วยต่อห้อง", LimitEn = "Up to 10 energy per room.",
                   Count = 2, PerRoomCap = 10 },
        new Spec { File = "08 HealingPulse", Icon = "08-healing-pulse", Type = BlessingType.HealingPulse, Category = BlessingCategory.Resource,
                   NameTh = "ชีพจรฟื้นฟู", NameEn = "Healing Pulse",
                   AbilityTh = "เคลียร์ห้องต่อสู้แล้วฟื้นเลือด 3% ของเลือดสูงสุด", AbilityEn = "Clearing a combat room heals 3% of max HP.",
                   LimitTh = "ทำงานครั้งเดียวต่อห้อง", LimitEn = "Once per room.",
                   Amount = 0.03f },
        new Spec { File = "09 EnergyReserve", Icon = "09-energy-reserve", Type = BlessingType.EnergyReserve, Category = BlessingCategory.Resource,
                   NameTh = "พลังงานสำรอง", NameEn = "Energy Reserve",
                   AbilityTh = "การโจมตีที่เสียพลังงานครั้งที่ 5 ไม่เสียพลังงาน", AbilityEn = "Every 5th energy-costing attack is free.",
                   LimitTh = "ไม่นับการโจมตีฟรีและสกิลฮีโร่", LimitEn = "Free attacks and hero skills don't count.",
                   Count = 5 },
    };

    [MenuItem("Tools/Quantum Rift/Setup Blessings (พร)")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งตั้งระบบพร");

        FixIcons();
        var data = BuildData();
        BuildScene(data);
        AssetDatabase.SaveAssets();
        Debug.Log($"ตั้งระบบพรเสร็จ: ข้อมูลพร {data.Length} แบบใน {DataFolder} + หน้าต่างเลือกพร + แถบพรบน HUD (เซฟฉากเกมให้แล้ว)");
    }

    // ---------- ไอคอน ----------

    static void FixIcons()
    {
        foreach (var size in new[] { "icons-64", "icons-128", "icons-256" })
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { $"{IconFolder}/{size}" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
                if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single &&
                    importer.filterMode == FilterMode.Point && importer.textureCompression == TextureImporterCompression.Uncompressed &&
                    !importer.mipmapEnabled) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
    }

    static Sprite LoadIcon(string size, string id)
    {
        string path = $"{IconFolder}/{size}/{id}.png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new InvalidOperationException($"ไม่เจอไอคอน {path}");
        return sprite;
    }

    // ---------- ข้อมูลพร ----------

    static BlessingData[] BuildData()
    {
        if (!AssetDatabase.IsValidFolder(DataFolder)) AssetDatabase.CreateFolder(DataParent, "Blessing");
        var rift = AssetDatabase.LoadAssetAtPath<WeaponData>(WaveSource);
        var waveFrames = rift != null ? rift.specialFrames : null;
        if (waveFrames == null || waveFrames.Length == 0)
            Debug.LogWarning($"ไม่เจอภาพคลื่นใน {WaveSource} คลื่นสะสมจะใช้วงแหวนแทน (สั่ง Setup Weapon > All Weapons ก่อนแล้วสั่งเมนูนี้ซ้ำ)");

        var list = new List<BlessingData>();
        foreach (var spec in Specs)
        {
            string path = $"{DataFolder}/{spec.File}.asset";
            var data = AssetDatabase.LoadAssetAtPath<BlessingData>(path);
            bool created = data == null;
            if (created)
            {
                data = ScriptableObject.CreateInstance<BlessingData>();
                AssetDatabase.CreateAsset(data, path);
            }

            data.type = spec.Type;
            data.category = spec.Category;
            data.icon = LoadIcon("icons-128", spec.Icon);
            data.hudIcon = LoadIcon("icons-64", spec.Icon);
            data.nameThai = spec.NameTh;
            data.nameEnglish = spec.NameEn;
            data.abilityThai = spec.AbilityTh;
            data.abilityEnglish = spec.AbilityEn;
            data.limitThai = spec.LimitTh;
            data.limitEnglish = spec.LimitEn;
            if (spec.Type == BlessingType.ChargedWave && waveFrames != null && waveFrames.Length > 0)
                data.waveFrames = waveFrames;

            if (created)
            {
                data.count = spec.Count;
                data.amount = spec.Amount;
                data.threshold = spec.Threshold;
                data.duration = spec.Duration;
                data.cooldown = spec.Cooldown;
                data.radius = spec.Radius;
                data.perRoomCap = spec.PerRoomCap;
            }
            EditorUtility.SetDirty(data);
            list.Add(data);
        }
        return list.ToArray();
    }

    // ---------- ฉากเกม ----------

    static void BuildScene(BlessingData[] data)
    {
        var scene = GameSceneUI.OpenGameScene();
        var canvas = GameSceneUI.FindCanvas();
        var ui = new MenuWindowUI(UndoName);

        // หน้าต่างเลือกพร
        var panel = ui.EnsureBackdrop(canvas.transform, PanelName);
        var window = ui.EnsureWindow(panel.transform, "BlessingWindow", WindowSize);

        var title = ui.EnsureText(window.transform, "Blessing_Title", "CHOOSE A BLESSING", "เลือกพรควอนตัม", 50f);
        if (ui.IsNew(title))
        {
            MenuWindowUI.Place(title.rectTransform, new Vector2(0f, 305f), new Vector2(WindowSize.x, 70f));
            title.fontStyle = FontStyles.Bold;
        }

        var subtitle = ui.EnsureText(window.transform, "Blessing_Subtitle", "-", 24f);
        if (ui.IsNew(subtitle))
        {
            MenuWindowUI.Place(subtitle.rectTransform, new Vector2(0f, 255f), new Vector2(WindowSize.x - 80f, 36f));
            subtitle.color = Muted;
        }

        var cards = new BlessingCardView[3];
        for (int i = 0; i < cards.Length; i++)
            cards[i] = BuildCard(ui, window.transform, i, data.Length > i ? data[i] : null);

        var ownedRow = ui.EnsureRect(window.transform, "OwnedRow");
        if (ui.IsNew(ownedRow)) MenuWindowUI.Place(ownedRow, new Vector2(0f, -305f), new Vector2(640f, 64f));
        var ownedLabel = ui.EnsureText(ownedRow, "Label", "BLESSINGS", "พรที่มี", 24f, TextAlignmentOptions.Right);
        if (ui.IsNew(ownedLabel))
        {
            MenuWindowUI.Place(ownedLabel.rectTransform, new Vector2(-200f, 0f), new Vector2(200f, 40f));
            ownedLabel.color = Muted;
        }
        var ownedIcons = new Image[Slots];
        for (int i = 0; i < Slots; i++)
        {
            var icon = ui.EnsureImage(ownedRow, $"Owned_{i + 1}");
            if (ui.IsNew(icon)) MenuWindowUI.Place(icon.rectTransform, new Vector2(-60f + i * (SlotSize + 8f), 0f), Vector2.one * SlotSize);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            ownedIcons[i] = icon;
        }

        // แถบพรบน HUD: ใต้หลอดเลือด/พลังงาน (VitalsHUD สูง 220 ห่างขอบบน 28)
        var gameplay = canvas.transform.Find("GameplayHUD");
        var bar = ui.EnsureRect(gameplay != null ? gameplay : canvas.transform, "BlessingHUD");
        if (ui.IsNew(bar))
        {
            bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0f, 1f);
            bar.anchoredPosition = new Vector2(32f, -262f);
            bar.sizeDelta = new Vector2(Slots * SlotSize + (Slots - 1) * SlotGap, SlotSize);
        }
        var hud = ui.EnsureComponent<BlessingHud>(bar.gameObject);
        hud.slots = new BlessingSlotView[Slots];
        for (int i = 0; i < Slots; i++)
            hud.slots[i] = BuildSlot(ui, bar, i, data.Length > i ? data[i] : null);
        EditorUtility.SetDirty(hud);

        // ตัวจัดการ (หน้าต่างเลือกพรอยู่ด้วยกัน เพราะ panel ปิดอยู่เกือบตลอด สคริปต์บน panel จะไม่ทำงาน)
        var manager = GameSceneUI.EnsureManager<BlessingManager>("BlessingManager", UndoName);
        var chooser = ui.EnsureComponent<BlessingWindow>(manager.gameObject);
        chooser.panel = panel.gameObject;
        chooser.subtitleText = subtitle;
        chooser.cards = cards;
        chooser.ownedIcons = ownedIcons;
        EditorUtility.SetDirty(chooser);

        manager.all = data;
        manager.window = chooser;
        manager.hud = hud;
        EditorUtility.SetDirty(manager);

        GameSceneUI.KeepTransitionOnTop(canvas.transform, panel.transform);
        panel.gameObject.SetActive(false); // เปิดเองตอนจบด่าน

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // การ์ด 1 ใบ: กรอบสีตามหมวด (BlessingCardView ย้อมตอนเปิด) ข้างในเป็นไอคอน ชื่อ หมวด ความสามารถ เส้นคั่น ข้อจำกัด และเลขปุ่มลัด
    static BlessingCardView BuildCard(MenuWindowUI ui, Transform window, int index, BlessingData preview)
    {
        var frame = ui.EnsureImage(window, $"Card_{index + 1}");
        if (ui.IsNew(frame))
        {
            MenuWindowUI.Place(frame.rectTransform, new Vector2((index - 1) * CardSpacing, 0f), CardSize);
            frame.color = MenuUIPack.Border;
        }
        frame.raycastTarget = true;

        var button = ui.EnsureComponent<Button>(frame.gameObject);
        button.targetGraphic = frame;
        button.transition = Selectable.Transition.None; // หน้าตาตอนชี้ BlessingCardView ทำเอง (ขยาย + กรอบสว่าง)
        var navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        ui.EnsureComponent<CanvasGroup>(frame.gameObject);
        var view = ui.EnsureComponent<BlessingCardView>(frame.gameObject);

        var inner = ui.EnsureImage(frame.transform, "Inner");
        if (ui.IsNew(inner))
        {
            MenuWindowUI.Stretch(inner.rectTransform, 5f);
            inner.color = MenuUIPack.Surface;
            inner.transform.SetAsFirstSibling();
        }
        inner.raycastTarget = false;

        var icon = ui.EnsureImage(frame.transform, "Icon");
        if (ui.IsNew(icon))
        {
            MenuWindowUI.Place(icon.rectTransform, new Vector2(0f, 140f), Vector2.one * 128f);
            if (preview != null) icon.sprite = preview.icon;
        }
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        var name = CardText(ui, frame.transform, "Name", 32f, new Vector2(0f, 50f), new Vector2(330f, 46f), null);
        if (ui.IsNew(name)) name.fontStyle = FontStyles.Bold;
        var category = CardText(ui, frame.transform, "Category", 19f, new Vector2(0f, 17f), new Vector2(330f, 28f), null);
        var ability = CardText(ui, frame.transform, "Ability", 23f, new Vector2(0f, -55f), new Vector2(316f, 110f), null);

        var divider = ui.EnsureImage(frame.transform, "Divider");
        if (ui.IsNew(divider))
        {
            MenuWindowUI.Place(divider.rectTransform, new Vector2(0f, -120f), new Vector2(260f, 2f));
            divider.color = MenuUIPack.Border;
        }
        divider.raycastTarget = false;

        var limit = CardText(ui, frame.transform, "Limit", 19f, new Vector2(0f, -172f), new Vector2(316f, 84f), Muted);
        var key = CardText(ui, frame.transform, "Key", 22f, new Vector2(-150f, 205f), new Vector2(40f, 34f), null);
        if (ui.IsNew(key)) key.fontStyle = FontStyles.Bold;

        view.button = button;
        view.frame = frame;
        view.icon = icon;
        view.nameText = name;
        view.categoryText = category;
        view.abilityText = ability;
        view.limitText = limit;
        view.keyText = key;
        EditorUtility.SetDirty(view);
        return view;
    }

    // ข้อความบนการ์ดเปลี่ยนตามพรตอนเปิดหน้าต่าง ย่อขนาดเองถ้ายาวเกินกรอบ (ภาษาไทยยาวกว่าอังกฤษ)
    static TextMeshProUGUI CardText(MenuWindowUI ui, Transform parent, string name, float size, Vector2 position, Vector2 box, Color? color)
    {
        var text = ui.EnsureText(parent, name, "-", size);
        if (ui.IsNew(text))
        {
            MenuWindowUI.Place(text.rectTransform, position, box);
            text.enableAutoSizing = true;
            text.fontSizeMax = size;
            text.fontSizeMin = Mathf.Max(12f, size * 0.65f);
            if (color.HasValue) text.color = color.Value;
        }
        return text;
    }

    // ช่องพรบน HUD: แสงฟุ้ง (หลังสุด) → ไอคอน → แผ่นมืดวงหมุน (คูลดาวน์) → ตัวเลขมุมขวาล่าง
    static BlessingSlotView BuildSlot(MenuWindowUI ui, RectTransform bar, int index, BlessingData preview)
    {
        var slot = ui.EnsureRect(bar, $"Slot_{index + 1}");
        if (ui.IsNew(slot))
        {
            slot.anchorMin = slot.anchorMax = new Vector2(0f, 1f);
            slot.pivot = MenuWindowUI.Center;
            slot.anchoredPosition = new Vector2(SlotSize * 0.5f + index * (SlotSize + SlotGap), -SlotSize * 0.5f);
            slot.sizeDelta = Vector2.one * SlotSize;
        }
        var view = ui.EnsureComponent<BlessingSlotView>(slot.gameObject);

        var glow = ui.EnsureImage(slot, "Glow");
        if (ui.IsNew(glow))
        {
            MenuWindowUI.Place(glow.rectTransform, Vector2.zero, Vector2.one * SlotSize * 1.75f);
            glow.color = new Color(1f, 1f, 1f, 0f);
        }
        glow.raycastTarget = false;

        var icon = ui.EnsureImage(slot, "Icon");
        if (ui.IsNew(icon))
        {
            MenuWindowUI.Place(icon.rectTransform, Vector2.zero, Vector2.one * SlotSize);
            if (preview != null) icon.sprite = preview.hudIcon; // ให้เห็นตำแหน่งใน Scene view ตอนเล่นจะเปลี่ยนตามพรที่มี
        }
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        var cooldown = ui.EnsureImage(slot, "Cooldown");
        if (ui.IsNew(cooldown))
        {
            MenuWindowUI.Place(cooldown.rectTransform, Vector2.zero, Vector2.one * SlotSize);
            cooldown.color = new Color(0f, 0f, 0f, 0.62f);
            if (preview != null) cooldown.sprite = preview.hudIcon;
        }
        cooldown.type = Image.Type.Filled;
        cooldown.fillMethod = Image.FillMethod.Radial360;
        cooldown.fillOrigin = (int)Image.Origin360.Top;
        cooldown.fillClockwise = false;
        cooldown.fillAmount = 0f;
        cooldown.preserveAspect = true;
        cooldown.raycastTarget = false;

        var badgeBack = ui.EnsureImage(slot, "BadgeBack");
        if (ui.IsNew(badgeBack))
        {
            MenuWindowUI.Place(badgeBack.rectTransform, new Vector2(20f, -24f), new Vector2(46f, 22f));
            badgeBack.color = new Color(0.05f, 0.07f, 0.14f, 0.85f);
        }
        badgeBack.raycastTarget = false;

        var badge = ui.EnsureText(badgeBack.transform, "Badge", "", 17f);
        if (ui.IsNew(badge))
        {
            MenuWindowUI.Stretch(badge.rectTransform);
            badge.fontStyle = FontStyles.Bold;
            badge.enableAutoSizing = true;
            badge.fontSizeMax = 17f;
            badge.fontSizeMin = 10f;
        }

        view.glow = glow;
        view.icon = icon;
        view.cooldown = cooldown;
        view.badgeBack = badgeBack.gameObject;
        view.badge = badge;
        EditorUtility.SetDirty(view);
        return view;
    }
}
