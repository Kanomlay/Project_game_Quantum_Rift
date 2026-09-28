using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ติดตั้งท่าโจมตีให้บอสสุดท้าย (The Architect of Collapse) ในห้องบอสแมพ 3 (Map_boss.prefab)
// (ArchitectOfCollapseBuilder ของเพื่อนสร้างภาพ/คลิป/prefab ร่าง 1–2 ส่วนสนาม/หลอดเลือดเพื่อนวางไว้ใน Map_boss แล้ว)
// 1. ArchitectBossHealth ตามตาราง 1.8: เลือด 500/500 = หลอดเดียว 1000 แปลงร่างที่ 50% คลั่งที่ 25% แปลงร่าง 2 วินาที
// 2. ใส่ ArchitectBossAI ที่ตัวบอส พร้อมภาพกระสุน / เลเซอร์ร่าง 1–2 / คลื่นเคียว / ภาพ Echo Commander (ตัดประตูมิติ)
//    (ไม่แตะ Animator และ prefab ของเพื่อน)
// 3. MapData_boss เป็นห้องบอส (ชนะแล้วขึ้นหน้าสรุป "จบเกม")
// สั่งซ้ำได้: ตัวเลขท่าที่ปรับเองใน Inspector ของ ArchitectBossAI จะไม่ถูกทับ (ภาพใส่ใหม่ทุกครั้ง)
public static class ArchitectBossBuilder
{
    const string BossMapPrefabPath = "Assets/Prefab/Map_boss.prefab";
    const string BossMapDataPath = "Assets/Data/Map/MapData_boss.asset";
    const string OrbSheet = "Assets/image/Boss/ArchitectOfCollapse/Phase1/Architect-Bullet-7Frames.png";
    const string LaserOneSheet = "Assets/image/Boss/ArchitectOfCollapse/Phase1/Architect-Laser-7Frames.png";
    const string LaserTwoSheet = "Assets/image/Boss/ArchitectOfCollapse/Phase2/Architect2-Laser-7Frames.png";
    const string WaveSheet = "Assets/image/Boss/ArchitectOfCollapse/Phase2/Architect2-ScytheWave-7Frames.png";
    const string EchoAtlasPath = "Assets/image/Boss/EchoCommander/EchoCommander-Actions-28Frames-v2.png"; // ตัดรูปประตูมิติ (ตารางเลเซอร์)
    const float PhaseHealth = 500f; // ตาราง 1.8: ชีวิต 500/500

    [MenuItem("Tools/Quantum Rift/Setup Boss/Architect of Collapse (แมพ 3)")]
    public static void SetupArchitect()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งติดตั้งบอส");

        var orbs = Frames(OrbSheet);
        var laserOne = Frames(LaserOneSheet);
        var laserTwo = Frames(LaserTwoSheet);
        var waves = Frames(WaveSheet);
        var echoAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(EchoAtlasPath);
        if (echoAtlas == null) Debug.LogWarning($"ไม่เจอ {EchoAtlasPath} ตารางเลเซอร์จะไม่มีภาพประตูมิติ (ยังยิงได้)");

        var root = PrefabUtility.LoadPrefabContents(BossMapPrefabPath);
        bool added;
        try
        {
            var health = root.GetComponentInChildren<ArchitectBossHealth>(true);
            if (health == null) throw new InvalidOperationException($"ไม่เจอ ArchitectBossHealth ใน {BossMapPrefabPath}");
            if (health.phaseOne == null || health.phaseTwo == null || health.arena == null || health.hitbox == null)
                throw new InvalidOperationException("ArchitectBossHealth ยังผูก phaseOne / phaseTwo / arena / hitbox ไม่ครบ");

            health.maxHealth = PhaseHealth * 2f;
            health.phaseTwoThreshold = 0.5f;
            health.enragedThreshold = 0.25f;
            health.transformationSeconds = 2f;

            var ai = health.GetComponent<ArchitectBossAI>();
            added = ai == null;
            if (added) ai = health.gameObject.AddComponent<ArchitectBossAI>();
            ai.orbFrames = orbs;
            ai.laserOneFrames = laserOne;
            ai.laserTwoFrames = laserTwo;
            ai.waveFrames = waves;
            ai.echoAtlas = echoAtlas;

            PrefabUtility.SaveAsPrefabAsset(root, BossMapPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        var map = AssetDatabase.LoadAssetAtPath<MapData>(BossMapDataPath);
        if (map != null && !map.isBossRoom)
        {
            map.isBossRoom = true;
            EditorUtility.SetDirty(map);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"ติดตั้งบอส Architect of Collapse เรียบร้อย: เลือด {PhaseHealth}/{PhaseHealth} (หลอด {PhaseHealth * 2f}) " +
                  $"{(added ? "ใส่ ArchitectBossAI ใหม่" : "อัปเดตภาพใน ArchitectBossAI เดิม")} ใน {BossMapPrefabPath}");
    }

    static Sprite[] Frames(string sheet)
    {
        var frames = AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (frames.Length < 7) throw new InvalidOperationException($"ภาพใน {sheet} มีไม่ครบ 7 เฟรม (เจอ {frames.Length}) สั่ง Build Architect Of Collapse ของเพื่อนก่อน");
        return frames.Take(7).ToArray();
    }
}
