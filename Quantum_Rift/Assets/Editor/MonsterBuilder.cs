using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// ติดตั้งมอนสเตอร์ให้พร้อมใช้งานในเกม: ใส่ Rigidbody2D/Collider/MonsterController ให้ prefab, ตั้ง layer ศัตรู,
// เติมพารามิเตอร์กับ transition ที่ MonsterController ต้องใช้ลงใน Animator และเปิด enemyLayers ให้ดาบฟันโดน
// สั่งซ้ำได้ ของที่มีอยู่แล้วจะถูกใช้ซ้ำ ไม่สร้างซ้อน
public static class MonsterBuilder
{
    const string EnemyLayerName = "Enemy";
    const string HeroPrefabPath = "Assets/Prefab/Hero/Warrior_0.prefab";

    [MenuItem("Tools/Quantum Rift/Setup Monster/Rift-Drained Worker")]
    public static void SetupRiftDrainedWorker()
    {
        Setup(
            "Assets/Prefab/Monster/Rift-Drained Worker_0.prefab",
            "Assets/Data/Monster/Rift_Walker.asset",
            "Assets/image/Monster/Rift-Drained Worker/Rift-Drained Worker_0.controller",
            "Rift-Drained Worker_idel",
            "Rift-Drained Worker_move",
            "Rift-Drained Worker_die");
    }

    static void Setup(string prefabPath, string dataPath, string controllerPath, string idleState, string moveState, string dieState)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("ออกจาก Play Mode ก่อนสั่งติดตั้งมอนสเตอร์");

        var data = Load<MonsterData>(dataPath);
        var prefabAsset = Load<GameObject>(prefabPath);

        int enemyLayer = EnsureLayer(EnemyLayerName);
        SetupAnimator(controllerPath, idleState, moveState, dieState);
        SetupPrefab(prefabPath, data, enemyLayer, ReadHeroSortingLayer());

        // RoomController เสกมอนสเตอร์จากช่องนี้ ถ้า reference หลุดจะเสกไม่ออก
        if (data.monsterPrefab != prefabAsset)
        {
            data.monsterPrefab = prefabAsset;
            EditorUtility.SetDirty(data);
        }

        EnableEnemyLayerOnWeapon(enemyLayer);

        AssetDatabase.SaveAssets();
        Debug.Log($"ติดตั้ง {Path.GetFileNameWithoutExtension(prefabPath)} เรียบร้อย (layer {EnemyLayerName} = {enemyLayer})");
    }

    static void SetupPrefab(string prefabPath, MonsterData data, int enemyLayer, int sortingLayerID)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            root.layer = enemyLayer;

            var body = Ensure<Rigidbody2D>(root);
            body.bodyType = RigidbodyType2D.Dynamic;  // MonsterController สั่ง knockback ผ่าน linearVelocity
            body.gravityScale = 0f;                   // เกม top-down ไม่ต้องมีแรงโน้มถ่วง
            body.freezeRotation = true;               // โดนชนแล้วอย่าให้ตัวหมุน
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var box = Ensure<BoxCollider2D>(root);    // ต้องมี collider ดาบถึงจะ OverlapCircle เจอ และชนตัวผู้เล่นได้
            box.isTrigger = false;
            var renderer = root.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                if (box.size == Vector2.zero && renderer.sprite != null)
                    box.size = renderer.sprite.bounds.size;

                // ต้องอยู่ sorting layer เดียวกับตัวละคร ไม่งั้นมอนสเตอร์จะถูกวาดอยู่หลังพื้นแมพจนมองไม่เห็น
                renderer.sortingLayerID = sortingLayerID;
            }

            Ensure<MonsterController>(root).myData = data;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void SetupAnimator(string controllerPath, string idleState, string moveState, string dieState)
    {
        var controller = Load<AnimatorController>(controllerPath);

        EnsureParameter(controller, "isWalking", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);

        var machine = controller.layers[0].stateMachine;
        var idle = FindState(machine, idleState);
        var move = FindState(machine, moveState);
        if (idle == null || move == null)
            throw new InvalidOperationException($"ไม่เจอ state \"{idleState}\" หรือ \"{moveState}\" ใน {controllerPath}");

        // ของเดิมเป็น transition แบบ exit time ไม่มีเงื่อนไข ท่าเลยสลับเองมั่วโดยไม่สนว่ามอนสเตอร์เดินอยู่จริงไหม
        ReplaceTransition(idle, move, "isWalking", true);
        ReplaceTransition(move, idle, "isWalking", false);

        var die = FindState(machine, dieState);
        if (die != null) RemoveAutoTransitionsTo(machine, die);

        EditorUtility.SetDirty(controller);
    }

    // transition เข้าท่าตายเดิมเป็นแบบ exit time ไม่มีเงื่อนไข พอเล่นท่ายืน/เดินจบรอบนึงมอนสเตอร์ก็ล้มลงนอนเองทั้งที่ยังไม่ตาย
    // MonsterController ตอนนี้ตายแล้วปิด GameObject ทิ้งเลย ไม่ได้สั่งเล่นท่าตาย จึงตัดเส้นพวกนี้ออก
    static void RemoveAutoTransitionsTo(AnimatorStateMachine machine, AnimatorState target)
    {
        foreach (var child in machine.states)
        {
            var state = child.state;
            if (state == null || state == target) continue;

            foreach (var transition in state.transitions.Where(t => t.destinationState == target && t.conditions.Length == 0).ToArray())
                state.RemoveTransition(transition);
        }

        foreach (var transition in machine.anyStateTransitions.Where(t => t.destinationState == target && t.conditions.Length == 0).ToArray())
            machine.RemoveAnyStateTransition(transition);
    }

    static void ReplaceTransition(AnimatorState from, AnimatorState to, string parameter, bool value)
    {
        foreach (var existing in from.transitions.Where(t => t.destinationState == to).ToArray())
            from.RemoveTransition(existing);

        var transition = from.AddTransition(to);
        transition.hasExitTime = false; // สลับทันทีที่เงื่อนไขเปลี่ยน ไม่ต้องรอเล่นอนิเมชันจบ
        transition.duration = 0f;
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
    }

    // ดาบเช็คการโดนด้วย LayerMask ถ้าไม่เปิด layer ศัตรูไว้จะฟันไม่โดนอะไรเลย
    static void EnableEnemyLayerOnWeapon(int enemyLayer)
    {
        var root = PrefabUtility.LoadPrefabContents(HeroPrefabPath);
        try
        {
            var weapon = root.GetComponentInChildren<WeaponController>(true);
            if (weapon == null)
            {
                Debug.LogWarning($"ไม่เจอ WeaponController ใน {HeroPrefabPath} ต้องไปตั้ง Enemy Layers เองใน Inspector");
                return;
            }

            int mask = weapon.enemyLayers.value | (1 << enemyLayer);
            if (weapon.enemyLayers.value == mask) return;

            weapon.enemyLayers = mask;
            PrefabUtility.SaveAsPrefabAsset(root, HeroPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ยึด sorting layer ของตัวละครเป็นหลัก จะได้ไม่ต้องมาไล่ตั้งเองทุกครั้งที่เพิ่มมอนสเตอร์ตัวใหม่
    static int ReadHeroSortingLayer()
    {
        var hero = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
        var renderer = hero != null ? hero.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (renderer == null)
        {
            Debug.LogWarning($"อ่าน sorting layer จาก {HeroPrefabPath} ไม่ได้ มอนสเตอร์อาจถูกวาดหลังพื้นแมพ");
            return 0;
        }
        return renderer.sortingLayerID;
    }

    static int EnsureLayer(string layerName)
    {
        var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset").FirstOrDefault();
        if (asset == null) throw new InvalidOperationException("เปิด TagManager.asset ไม่ได้");

        var tagManager = new SerializedObject(asset);
        var layers = tagManager.FindProperty("layers");

        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == layerName) return i;

        for (int i = 8; i < layers.arraySize; i++) // 0-7 เป็นช่องที่ Unity จองไว้
        {
            var slot = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(slot.stringValue)) continue;

            slot.stringValue = layerName;
            tagManager.ApplyModifiedProperties();
            return i;
        }

        throw new InvalidOperationException("ช่อง layer เต็มแล้ว ต้องไปลบของที่ไม่ใช้ออกก่อน");
    }

    static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        if (controller.parameters.Any(p => p.name == name)) return;
        controller.AddParameter(name, type);
    }

    static AnimatorState FindState(AnimatorStateMachine machine, string name)
    {
        return machine.states.FirstOrDefault(s => s.state != null && s.state.name == name).state;
    }

    static T Ensure<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    static T Load<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new InvalidOperationException($"ไม่เจอไฟล์ {path}");
        return asset;
    }
}
