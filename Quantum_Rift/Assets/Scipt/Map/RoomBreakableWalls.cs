using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// กำแพงในห้องที่ทุบได้ ติดอยู่ที่ห้อง (ตัวเดียวกับ RoomController)
// ปกติ RandomRoomWalls สุ่มรูปทรงแล้วเรียก AddCells ตอนเข้าแมพ (ใส่ช่องไว้ใน cells ล่วงหน้าก็ได้ จะสร้างตอน Awake)
// ภาพเป็น tile ในแผนที่กำแพงของผัง สร้าง collider ช่องละชิ้น (BreakableWallCell) ไว้ใต้ห้อง
// - ผู้เล่นตีด้วยอาวุธ/กระสุนได้ แตกแล้วได้เม็ดพลังงานนิดหน่อย
// - มอนสเตอร์ของห้องนี้ทุบได้เมื่อกำแพงขวางทาง (ดู MonsterNavigator / MonsterCombatActions)
// แตกแล้วลบ tile ช่องนั้นทิ้ง พื้นข้างใต้โผล่มาเดินผ่านได้ ระบบหาที่เกิด/วางของเห็นเป็นพื้นว่างทันที
public sealed class RoomBreakableWalls : MonoBehaviour
{
    public Tilemap wallMap;
    public List<Vector3Int> cells = new List<Vector3Int>();
    [Min(1f)] public float cellHealth = 30f;              // อาวุธเริ่มต้นตี 4–10 ต่อครั้ง ของตำนาน 15–20
    public Vector2Int manaPerCell = new Vector2Int(1, 2);   // ผู้เล่นทุบแตก (มอนทุบไม่ได้)
    public Color debrisColor = new Color(0.5f, 0.6f, 0.75f);

    static readonly Color Worn = new Color(0.55f, 0.55f, 0.62f); // สีตอนเลือดใกล้หมด
    static readonly Dictionary<Sprite, Sprite[]> chunkCache = new Dictionary<Sprite, Sprite[]>();

    public RoomController Room { get; private set; }
    public int Remaining { get; private set; }
    Transform holder;

    void Awake()
    {
        Room = GetComponent<RoomController>();
        if (wallMap != null && cells.Count > 0) Build(cells);
    }

    // กำแพงที่เพิ่งระบาย tile ลงแผนที่กำแพง (ตัวสุ่มรูปทรงเรียกตอนเข้าแมพ)
    public void AddCells(Tilemap map, IList<Vector3Int> newCells)
    {
        wallMap = map;
        cells.AddRange(newCells);
        Build(newCells);
    }

    void Build(IEnumerable<Vector3Int> list)
    {
        if (holder == null)
        {
            holder = new GameObject("BreakableWalls").transform;
            holder.SetParent(transform, false);
        }
        Vector2 size = Vector2.Scale(wallMap.layoutGrid.cellSize, wallMap.transform.lossyScale);
        foreach (var cell in list)
        {
            if (!wallMap.HasTile(cell)) continue;
            wallMap.SetTileFlags(cell, TileFlags.None); // ให้ย้อมสี/สั่นเป็นรายช่องได้
            var go = new GameObject($"Wall_{cell.x}_{cell.y}");
            go.transform.SetParent(holder, false);
            go.transform.position = wallMap.GetCellCenterWorld(cell);
            var box = go.AddComponent<BoxCollider2D>();
            Vector3 scale = go.transform.lossyScale;
            box.size = new Vector2(size.x / Mathf.Abs(scale.x), size.y / Mathf.Abs(scale.y));
            go.AddComponent<BreakableWallCell>().Setup(this, cell);
            Remaining++;
        }
    }

    // k = เลือดที่เหลือ 0–1 ยิ่งน้อยยิ่งหมอง
    public void Wear(Vector3Int cell, float k) => wallMap.SetColor(cell, Color.Lerp(Worn, Color.white, Mathf.Clamp01(k)));

    public void Nudge(Vector3Int cell, Vector2 offset) =>
        wallMap.SetTransformMatrix(cell, Matrix4x4.Translate(new Vector3(offset.x, offset.y, 0f)));

    // แตก: ลบ tile แล้วภาพกำแพงแตกเป็น 4 ชิ้นกระเด็นออกแล้วจางหาย
    public void Crumble(Vector3Int cell)
    {
        var sprite = wallMap.GetSprite(cell);
        Color tint = wallMap.GetColor(cell);
        Vector2 center = wallMap.GetCellCenterWorld(cell);
        var tileRenderer = wallMap.GetComponent<TilemapRenderer>();
        int layer = tileRenderer != null ? tileRenderer.sortingLayerID : 0;
        wallMap.SetTile(cell, null);
        Remaining--;

        var chunks = Chunks(sprite);
        if (chunks == null) return;
        Vector2 cellSize = Vector2.Scale(wallMap.layoutGrid.cellSize, wallMap.transform.lossyScale);
        for (int i = 0; i < 4; i++)
        {
            Vector2 corner = new Vector2(i % 2 == 0 ? -1f : 1f, i < 2 ? -1f : 1f);
            var chunk = new GameObject("WallChunk").AddComponent<SpriteRenderer>();
            chunk.sprite = chunks[i];
            chunk.color = tint;
            chunk.sortingLayerID = layer;
            chunk.sortingOrder = 2;
            var t = chunk.transform;
            Vector2 start = center + corner * cellSize * 0.25f;
            Vector2 fly = corner * Random.Range(0.35f, 0.6f) + Random.insideUnitCircle * 0.15f;
            float spin = Random.Range(-240f, 240f);
            float scale = cellSize.x * 0.5f / Mathf.Max(0.01f, chunks[i].bounds.size.x);
            EchoFxTween.Play(chunk.gameObject, 0.45f, k =>
            {
                float travel = 1f - (1f - k) * (1f - k);
                t.position = start + fly * travel + Vector2.up * 0.25f * Mathf.Sin(k * Mathf.PI); // เด้งขึ้นแล้วตกลง
                t.rotation = Quaternion.Euler(0f, 0f, spin * k);
                t.localScale = Vector3.one * scale * (1f - 0.45f * k);
                chunk.color = new Color(tint.r, tint.g, tint.b, k < 0.55f ? 1f : (1f - k) / 0.45f);
            }, () => Destroy(chunk.gameObject));
        }
    }

    // ภาพ tile กำแพงแบ่ง 4 ส่วน (ล่างซ้าย, ล่างขวา, บนซ้าย, บนขวา) ทำครั้งเดียวต่อภาพ
    static Sprite[] Chunks(Sprite sprite)
    {
        if (sprite == null) return null;
        if (chunkCache.TryGetValue(sprite, out var chunks)) return chunks;
        Rect r = sprite.textureRect;
        float w = r.width * 0.5f, h = r.height * 0.5f;
        chunks = new Sprite[4];
        for (int i = 0; i < 4; i++)
        {
            var rect = new Rect(r.x + (i % 2) * w, r.y + (i / 2) * h, w, h);
            chunks[i] = Sprite.Create(sprite.texture, rect, new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit);
        }
        chunkCache[sprite] = chunks;
        return chunks;
    }
}
