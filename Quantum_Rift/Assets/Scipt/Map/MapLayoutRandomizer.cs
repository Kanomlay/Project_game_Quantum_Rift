using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>เลือกผังครั้งเดียวตอนเข้าด่าน ไม่สลับห้องระหว่างต่อสู้หรือเมื่อเปิดวัตถุซ้ำ</summary>
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class MapLayoutRandomizer : MonoBehaviour
{
    public string themeKey;
    public GameObject[] layouts = Array.Empty<GameObject>();
    public int CurrentLayout { get; private set; } = -1;
    static readonly Dictionary<string, int> previousLayouts = new Dictionary<string, int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetHistory() { previousLayouts.Clear(); }

    void Awake()
    {
        var available = new List<int>();
        for (int i = 0; i < layouts.Length; i++)
            if (layouts[i] != null) available.Add(i);
        if (available.Count == 0) return;
        string key = string.IsNullOrEmpty(themeKey) ? gameObject.name : themeKey;
        // ใช้ RNG แยก ไม่รบกวนการสุ่มมอนสเตอร์หรือรางวัล และไม่ซ้ำผังที่เพิ่งเข้าในธีมเดียวกัน
        if (available.Count > 1 && previousLayouts.TryGetValue(key, out int previous))
            available.Remove(previous);
        CurrentLayout = available[new System.Random(Guid.NewGuid().GetHashCode()).Next(available.Count)];
        previousLayouts[key] = CurrentLayout;
        for (int i = 0; i < layouts.Length; i++)
            if (layouts[i] != null && i != CurrentLayout) layouts[i].SetActive(false);
        layouts[CurrentLayout].SetActive(true);
    }
}
