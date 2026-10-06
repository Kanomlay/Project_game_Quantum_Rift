using System.Collections;
using UnityEngine;

/// <summary>Seven poses: index 0 open, index 6 closed. GameObject stays enabled.</summary>
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public sealed class AnimatedRoomGate : MonoBehaviour
{
    public Sprite[] frames = new Sprite[7];
    [Min(1f)] public float framesPerSecond = 10f;
    public bool initiallyClosed;
    // ประตูเปิดสุด (เฟรม 0): แบ่งภาพเป็นเสาซ้าย / รางพื้นช่วงกลาง / เสาขวา รางพื้นวาดใต้เท้าตัวละคร เสายังอยู่ชั้นเดิมของประตู
    // สัดส่วนความกว้างภาพที่เป็นช่วงกลาง (ระหว่างเสาสองต้น) ภาพประตูทุกธีมกว้าง 256 เสาอยู่นอกช่วงนี้
    [Range(0f, .5f)] public float floorBandStart = .27f;
    [Range(.5f, 1f)] public float floorBandEnd = .73f;
    const string FloorLayer = "bg2"; // ชั้นเดียวกับผู้เล่น (ลำดับ 5) และกับดักฝังพื้น (ลำดับ 0)
    const int FloorOrder = 1;
    private SpriteRenderer[] openParts;
    public bool IsClosed { get; private set; }
    public int CurrentFrame { get; private set; }
    private SpriteRenderer display;
    private BoxCollider2D blocker;
    private Coroutine transition;
    private void Awake() { Cache(); SetClosed(initiallyClosed, true); }
    private void Cache()
    {
        if (display == null) display = GetComponent<SpriteRenderer>();
        if (blocker == null) blocker = GetComponent<BoxCollider2D>();
    }
    public void SetClosed(bool closed, bool immediate = false)
    {
        Cache(); IsClosed = closed;
        if (transition != null) StopCoroutine(transition);
        transition = null;
        int target = closed ? Mathf.Max(0, (frames == null ? 0 : frames.Length) - 1) : 0;
        // Inset triggers keep the player past the threshold before closing.
        if (closed) blocker.enabled = true;
        if (immediate || !Application.isPlaying || !isActiveAndEnabled)
        {
            Apply(target); blocker.enabled = closed; return;
        }
        transition = StartCoroutine(Animate(target, closed));
    }
    private IEnumerator Animate(int target, bool closed)
    {
        while (CurrentFrame != target)
        {
            Apply(CurrentFrame + (target > CurrentFrame ? 1 : -1));
            yield return new WaitForSeconds(1f / Mathf.Max(1f, framesPerSecond));
        }
        blocker.enabled = closed; transition = null;
    }
    private void Apply(int index)
    {
        CurrentFrame = index;
        if (frames != null && frames.Length > index && frames[index] != null) display.sprite = frames[index];
        if (!Application.isPlaying) return;
        bool split = index == 0 && BuildOpenParts();
        display.enabled = !split;
        if (openParts != null) foreach (var part in openParts) part.enabled = split;
    }
    // ตัดภาพเฟรมเปิดสุดเป็น 3 ท่อนตามแนวตั้ง ทำครั้งเดียวตอนเล่น (ไม่แก้ไฟล์ภาพและ prefab)
    private bool BuildOpenParts()
    {
        if (openParts != null) return openParts.Length > 0;
        openParts = new SpriteRenderer[0];
        var open = frames != null && frames.Length > 0 ? frames[0] : null;
        if (open == null || open.packed || floorBandEnd <= floorBandStart) return false;
        Rect rect = open.rect;
        float[] cuts = { 0f, Mathf.Round(rect.width * floorBandStart), Mathf.Round(rect.width * floorBandEnd), rect.width };
        var parts = new SpriteRenderer[3];
        for (int i = 0; i < 3; i++)
        {
            float width = cuts[i + 1] - cuts[i];
            if (width < 1f) return false;
            var piece = Sprite.Create(open.texture, new Rect(rect.x + cuts[i], rect.y, width, rect.height),
                new Vector2(.5f, .5f), open.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            var go = new GameObject(i == 1 ? "GateFloorRail" : "GatePost");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3((cuts[i] + width * .5f - open.pivot.x) / open.pixelsPerUnit,
                (rect.height * .5f - open.pivot.y) / open.pixelsPerUnit, 0f);
            var view = go.AddComponent<SpriteRenderer>();
            view.sprite = piece; view.sharedMaterial = display.sharedMaterial; view.color = display.color;
            view.flipX = display.flipX; view.flipY = display.flipY;
            if (i == 1) { view.sortingLayerName = FloorLayer; view.sortingOrder = FloorOrder; }
            else { view.sortingLayerID = display.sortingLayerID; view.sortingOrder = display.sortingOrder; }
            view.enabled = false;
            parts[i] = view;
        }
        openParts = parts;
        return true;
    }
    private void OnDisable()
    {
        if (transition != null) StopCoroutine(transition);
        transition = null;
    }
}
