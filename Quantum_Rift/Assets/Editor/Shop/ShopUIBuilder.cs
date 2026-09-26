using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// ใส่ช่องสินค้าลงหน้าร้าน (ShopWindow.prefab ที่ ShopInteractionBuilder สร้างไว้ เป็นกรอบเปล่า 5 ช่อง)
// ตำแหน่งทุกชิ้นวัดจากภาพกรอบ ShopUI-5Items-Blank-Transparent.png (1536 x 1024 ย่อลงเป็น 960 x 640 = x0.625):
// - แถบบน: ไอคอนเหรียญ ชื่อร้าน และเงินที่มี
// - แถบฟ้าซ้าย "ขวดยา" คลุม 2 ช่องแรก, แถบม่วงขวา "อาวุธ" คลุม 3 ช่องหลัง
// - แต่ละช่อง: กล่องรูป → กล่องชื่อ → กล่องราคา (คลิกซื้อ)
// - กล่องล่างกลาง: คำแนะนำ / คำอธิบายสินค้าตอนชี้เมาส์ / ผลการซื้อ
// แล้วผูกรายการอาวุธ (ChestLoot) ให้ร้านทั้งสองธีม สั่งซ้ำได้ (ลบชุด ShopUI เดิมแล้วสร้างใหม่)
public static class ShopUIBuilder
{
    const string WindowPath = "Assets/Prefab/Shop/ShopWindow.prefab";
    const string CoinPath = "Assets/image/UI_Image/QuantumRift-UI-Objects-v1/runtime/Coin-Single.png";
    static readonly string[] ShopPrefabs = { "Assets/Prefab/Shop/ShopForest.prefab", "Assets/Prefab/Shop/ShopSpaceship.prefab" };

    const float Art = 0.625f; // พิกเซลภาพกรอบ → หน่วย UI
    static readonly float[] SlotX = { 215f, 502f, 797f, 1060f, 1327f };
    const float BoxY = 465f, NameY = 635f, PriceY = 725f;

    static readonly Color TextBlue = new Color(0.8f, 0.93f, 1f);
    static readonly Color Gold = new Color(1f, 0.87f, 0.35f);

    [MenuItem("Tools/Quantum Rift/Setup Shop (ระบบซื้อของ)")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งติดตั้งร้านค้า");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(WindowPath) == null)
            throw new InvalidOperationException($"ยังไม่มี {WindowPath} สั่ง Tools > Quantum Rift > Build Shop Click Window ก่อน");

        BuildWindow();

        var pool = AssetDatabase.LoadAssetAtPath<LootTable>(LootBuilder.LootPath);
        if (pool == null) Debug.LogWarning("ยังไม่มี ChestLoot ร้านจะขายได้แค่ขวดยา (สั่ง Setup Chest Loot ก่อน)");
        foreach (var path in ShopPrefabs)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var shop = root.GetComponent<ShopClickable>();
                if (shop == null) { Debug.LogWarning($"{path} ยังไม่มี ShopClickable ข้าม"); continue; }
                shop.itemPool = pool;
                if (shop.windowPrefab == null) shop.windowPrefab = AssetDatabase.LoadAssetAtPath<ShopWindow>(WindowPath);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("ติดตั้งร้านค้าแล้ว: ขวดยา 2 ช่อง + อาวุธสุ่ม 3 ช่อง ราคา 10 / 20 / 50 (ขวดยา 10) เดินใกล้ร้านแล้วกด F หรือคลิกร้าน");
    }

    static void BuildWindow()
    {
        var root = PrefabUtility.LoadPrefabContents(WindowPath);
        try
        {
            var window = root.GetComponent<ShopWindow>();
            var frame = FindDeep(root.transform, "BlankShopFrame");
            if (window == null || frame == null) throw new InvalidOperationException("ShopWindow.prefab ไม่มี ShopWindow/BlankShopFrame");

            var old = frame.Find("ShopUI");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var ui = Rect("ShopUI", frame, Vector2.zero, ((RectTransform)frame).sizeDelta);
            ui.SetSiblingIndex(0); // อยู่หลังปุ่มปิด ปุ่มปิดจะได้กดได้เสมอ
            var close = frame.Find("CloseButton");
            if (close != null) close.SetAsLastSibling();

            // แถบบน
            var coin = Rect("CoinIcon", ui, At(155f, 172f), new Vector2(60f, 60f)).gameObject.AddComponent<Image>();
            coin.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CoinPath);
            coin.preserveAspect = true;
            coin.raycastTarget = false;
            Label("Title", ui, At(670f, 172f), new Vector2(540f, 60f), "ร้านค้ามิติ", 34f, TextBlue, FontStyles.Bold);
            window.currencyText = Label("Currency", ui, At(1212f, 177f), new Vector2(112f, 46f), "0", 26f, Gold, FontStyles.Bold);

            // หัวข้อหมวด
            Label("PotionHeader", ui, At(360f, 295f), new Vector2(340f, 30f), "ขวดยา", 20f, Color.white, FontStyles.Bold);
            Label("WeaponHeader", ui, At(1072f, 295f), new Vector2(470f, 30f), "อาวุธ", 20f, Color.white, FontStyles.Bold);

            // ช่องสินค้า
            window.slots = new ShopSlotView[SlotX.Length];
            for (int i = 0; i < SlotX.Length; i++) window.slots[i] = Slot(ui, i);

            // กล่องล่าง: คำแนะนำ / คำอธิบาย / ผลการซื้อ (ยาวได้ ย่อขนาดตัวอักษรเอง)
            var message = Label("Message", ui, At(770f, 855f), new Vector2(560f, 70f), "", 17f, TextBlue, FontStyles.Normal);
            message.enableAutoSizing = true;
            message.fontSizeMin = 11f;
            message.fontSizeMax = 17f;
            message.textWrappingMode = TextWrappingModes.Normal;
            window.messageText = message;

            PrefabUtility.SaveAsPrefabAsset(root, WindowPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static ShopSlotView Slot(RectTransform parent, int i)
    {
        // ตัวช่อง = กล่องรูป (รับเมาส์ไว้แสดงคำอธิบาย) ชื่อกับปุ่มราคาเป็นลูก วางต่ำลงไปตามกล่องในภาพ
        var slot = Rect($"Slot{i + 1}", parent, At(SlotX[i], BoxY), new Vector2(150f, 150f));
        var hover = slot.gameObject.AddComponent<Image>();
        hover.color = new Color(1f, 1f, 1f, 0f);
        var view = slot.gameObject.AddComponent<ShopSlotView>();

        var icon = Rect("Icon", slot, Vector2.zero, new Vector2(122f, 122f)).gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        view.icon = icon;

        view.nameText = Label("Name", slot, new Vector2(0f, (BoxY - NameY) * Art), new Vector2(140f, 34f), "-", 16f, Color.white, FontStyles.Bold);
        view.nameText.enableAutoSizing = true;
        view.nameText.fontSizeMin = 10f;
        view.nameText.fontSizeMax = 16f;

        var price = Rect("Price", slot, new Vector2(0f, (BoxY - PriceY) * Art), new Vector2(146f, 52f));
        var priceImage = price.gameObject.AddComponent<Image>();
        var button = price.gameObject.AddComponent<Button>();
        button.targetGraphic = priceImage;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.colors = new ColorBlock
        {
            normalColor = new Color(1f, 1f, 1f, 0f),
            highlightedColor = new Color(0.5f, 1f, 1f, 0.22f),
            pressedColor = new Color(0.5f, 1f, 1f, 0.4f),
            selectedColor = new Color(1f, 1f, 1f, 0f),
            disabledColor = new Color(0f, 0f, 0f, 0.3f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f,
        };
        view.buyButton = button;
        view.priceText = Label("Text", price, Vector2.zero, new Vector2(140f, 48f), "-", 22f, Gold, FontStyles.Bold);
        return view;
    }

    // ตำแหน่งในภาพกรอบ (นับจากมุมซ้ายบน) → ตำแหน่ง UI เทียบกลางกรอบ
    static Vector2 At(float px, float py) => new Vector2((px - 768f) * Art, (512f - py) * Art);

    static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    static TextMeshProUGUI Label(string name, Transform parent, Vector2 position, Vector2 size, string text,
                                 float fontSize, Color color, FontStyles style)
    {
        var label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset; // Kanit (ภาษาไทย)
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            var found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
