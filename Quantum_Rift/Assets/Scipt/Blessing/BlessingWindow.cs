using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// หน้าต่างเลือกพร: การ์ดสูงสุด 3 ใบ (ไอคอน ชื่อ หมวด ความสามารถ ข้อจำกัด) คลิกการ์ดหรือกดเลข 1–3 เพื่อเลือก
// แถวล่างโชว์พรที่มีแล้ว ส่วนประกอบสร้างโดย BlessingBuilder
// สคริปต์นี้อยู่กับ BlessingManager (เปิดอยู่ตลอด) ส่วน panel ปิดไว้จนกว่าจะเปิดหน้าต่าง ระหว่างเปิดเกมหยุด (timeScale 0) จึงใช้เวลาจริง
public sealed class BlessingWindow : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text subtitleText;
    public BlessingCardView[] cards;
    public Image[] ownedIcons;

    const float InputGuard = 0.35f; // กันคลิก/กดค้างจากก่อนหน้าต่างขึ้น แล้วเลือกไปโดยไม่ได้ตั้งใจ

    readonly List<BlessingData> options = new List<BlessingData>();
    Action<BlessingData> onChosen;
    float openedAt;

    public bool IsOpen => panel != null && panel.activeSelf;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (cards == null) return;
        for (int i = 0; i < cards.Length; i++)
        {
            int index = i;
            if (cards[i] != null && cards[i].button != null) cards[i].button.onClick.AddListener(() => Choose(index));
        }
    }

    public void Open(IList<BlessingData> offer, IReadOnlyList<BlessingData> owned, int max, Action<BlessingData> chosen)
    {
        options.Clear();
        options.AddRange(offer);
        onChosen = chosen;
        openedAt = Time.unscaledTime;
        panel.SetActive(true);

        if (subtitleText != null)
            subtitleText.text = LanguageSettings.IsThai
                ? $"เลือก 1 อย่าง (กด 1–{options.Count} หรือคลิกการ์ด) · พรที่มี {owned.Count}/{max}"
                : $"Pick one (press 1–{options.Count} or click a card) · Blessings {owned.Count}/{max}";

        if (cards != null)
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;
                bool used = i < options.Count;
                cards[i].gameObject.SetActive(used);
                if (used) cards[i].Show(options[i], i, InputGuard * 0.5f + i * 0.08f);
            }

        if (ownedIcons != null)
            for (int i = 0; i < ownedIcons.Length; i++)
            {
                if (ownedIcons[i] == null) continue;
                var blessing = i < owned.Count ? owned[i] : null;
                ownedIcons[i].sprite = blessing != null ? blessing.hudIcon : null;
                ownedIcons[i].color = blessing != null ? Color.white : new Color(1f, 1f, 1f, 0.12f); // ช่องว่างจาง ๆ ให้เห็นว่าเหลือกี่ช่อง
            }

        // ปุ่มที่ถูกเลือกค้างจากหน้าต่างอื่น กด Enter/Space แล้วจะไปโดนปุ่มนั้นแทน
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    void Update()
    {
        if (!IsOpen || Time.unscaledTime - openedAt < InputGuard) return;
        for (int i = 0; i < options.Count && i < 9; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                Choose(i);
                return;
            }
    }

    public void Choose(int index)
    {
        if (!IsOpen || index < 0 || index >= options.Count || Time.unscaledTime - openedAt < InputGuard) return;
        var chosen = options[index];
        panel.SetActive(false);
        var callback = onChosen;
        onChosen = null;
        callback?.Invoke(chosen);
    }
}
