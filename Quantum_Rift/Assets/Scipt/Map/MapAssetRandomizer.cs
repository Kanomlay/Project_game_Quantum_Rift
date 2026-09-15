using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Rerolls authored safe prop slots once per map instance, never per room or frame.</summary>
[DisallowMultipleComponent]
public sealed class MapAssetRandomizer : MonoBehaviour
{
    [Serializable]
    public sealed class Slot
    {
        public SpriteRenderer display;
        public Sprite[] variants;
        public Collider2D[] blockers;
        [Range(0f, 1f)] public float presence = .88f;
    }

    public string layoutKey;
    public Slot[] slots = Array.Empty<Slot>();
    public int LastSeed { get; private set; }
    static readonly Dictionary<string, int> lastAnchor = new Dictionary<string, int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetHistory() { lastAnchor.Clear(); }

    void Awake()
    {
        // A local RNG does not perturb enemy, loot or combat randomness.
        int seed = Guid.NewGuid().GetHashCode();
        ApplySeed(seed);
        // Guarantee at least one visible change on consecutive entries to this layout.
        if (slots.Length == 0 || !Usable(slots[0])) return;
        var anchor = slots[0];
        int index = Array.IndexOf(anchor.variants, anchor.display.sprite);
        string key = string.IsNullOrEmpty(layoutKey) ? gameObject.name : layoutKey;
        if (anchor.variants.Length > 1 && lastAnchor.TryGetValue(key, out int previous) && index == previous)
        {
            index = (index + 1) % anchor.variants.Length;
            anchor.display.sprite = anchor.variants[index];
        }
        lastAnchor[key] = index;
    }

    /// <summary>Deterministic preview/test entry point. Does not alter transforms or collider shapes.</summary>
    public void ApplySeed(int seed)
    {
        LastSeed = seed;
        var random = new System.Random(seed);
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (!Usable(slot)) continue;
            int variant = random.Next(slot.variants.Length);
            bool present = random.NextDouble() < Mathf.Clamp01(slot.presence) || i == 0;
            if (slot.variants[variant] == null) continue;
            slot.display.sprite = slot.variants[variant];
            slot.display.enabled = present;
            if (slot.blockers == null) continue;
            foreach (var blocker in slot.blockers)
                if (blocker != null && !blocker.isTrigger) blocker.enabled = present;
        }
    }

    static bool Usable(Slot slot)
    {
        return slot != null && slot.display != null && slot.variants != null && slot.variants.Length > 0;
    }
}
