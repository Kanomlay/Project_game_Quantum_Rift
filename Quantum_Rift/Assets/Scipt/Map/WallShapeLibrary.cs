using System.Collections.Generic;
using UnityEngine;

// รูปทรงกำแพงในห้องที่ตัวสุ่มเลือกใช้ (RandomRoomWalls) เขียนเป็นแถวตัวอักษร: X = กำแพง . = ว่าง แถวแรกคือด้านบน
// ตั้งต้นจากกองกล่องของเพื่อน (Prefab/MapObjects/*/Piles) โดย RandomRoomObjectsBuilder แก้/เพิ่มรูปทรงใน Inspector ได้
[CreateAssetMenu(menuName = "Quantum Rift/Wall Shape Library", fileName = "WallShapes")]
public sealed class WallShapeLibrary : ScriptableObject
{
    [System.Serializable]
    public sealed class Shape
    {
        public string name;
        public string[] rows;
        public bool canRotate = true; // หมุน 90° / สะท้อนได้ (หัวใจไม่หมุน จะกลับหัว สะท้อนซ้ายขวาอย่างเดียว)
        [Min(0f)] public float weight = 1f;

        public List<Vector2Int> Cells()
        {
            var cells = new List<Vector2Int>();
            if (rows == null) return cells;
            for (int r = 0; r < rows.Length; r++)
            {
                string row = rows[r] ?? "";
                for (int c = 0; c < row.Length; c++)
                    if (row[c] == 'X' || row[c] == 'x') cells.Add(new Vector2Int(c, rows.Length - 1 - r));
            }
            return cells;
        }

        // ทุกท่าที่ต่างกันจริงของรูปนี้ (หมุน 0/90/180/270 × สะท้อน) เลื่อนให้มุมล่างซ้ายอยู่ที่ (0,0)
        public List<List<Vector2Int>> Variants()
        {
            var baseCells = Cells();
            var result = new List<List<Vector2Int>>();
            var seen = new HashSet<string>();
            int turns = canRotate ? 4 : 1;
            for (int mirror = 0; mirror < 2; mirror++)
                for (int turn = 0; turn < turns; turn++)
                {
                    var cells = new List<Vector2Int>();
                    foreach (var cell in baseCells)
                    {
                        var p = mirror == 1 ? new Vector2Int(-cell.x, cell.y) : cell;
                        for (int t = 0; t < turn; t++) p = new Vector2Int(-p.y, p.x);
                        cells.Add(p);
                    }
                    if (cells.Count == 0) continue;
                    int minX = int.MaxValue, minY = int.MaxValue;
                    foreach (var p in cells) { minX = Mathf.Min(minX, p.x); minY = Mathf.Min(minY, p.y); }
                    for (int i = 0; i < cells.Count; i++) cells[i] -= new Vector2Int(minX, minY);
                    cells.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
                    string key = string.Join(";", cells);
                    if (seen.Add(key)) result.Add(cells);
                }
            return result;
        }
    }

    public Shape[] shapes = new Shape[0];
}
