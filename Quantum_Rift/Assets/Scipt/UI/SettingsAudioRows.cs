using TMPro;
using UnityEngine;
using UnityEngine.UI;

// เพิ่มแถบปรับเสียงเพลงและเสียงเอฟเฟกต์ในหน้าตั้งค่าตอนเริ่มฉาก (เมนูหลัก ฉากเกม สนามฝึก) โดยโคลนแถวและแถบของ "ความดังเสียง" เดิม
// ไม่ต้องแก้ฉากหรือสั่ง Builder ใหม่ แถบเดิมกลายเป็น "เสียงทั้งหมด" และยังคุมทุกเสียงเหมือนเดิม (AudioListener.volume)
// แถวภาษา ปุ่มปิด และแถวที่เพิ่มทีหลัง (SettingsExtraRow) เลื่อนลงตาม หน้าต่างสูงขึ้นเท่าที่เพิ่ม
// ต้องเรียกก่อน SettingsExtraRow.Add เพราะแถวเหล่านั้นวางตำแหน่งต่อจากแถวภาษา
public static class SettingsAudioRows
{
    const float Block = 110f; // ป้ายชื่อ 1 แถว + แถบเลื่อน 1 แถว (ระยะเดียวกับป้าย→แถบของเดิม คูณสอง)

    public static void Attach(SettingsMenu menu)
    {
        if (menu == null || menu.settingsPanel == null || menu.volumeSlider == null) return;
        var slider = menu.volumeSlider.transform as RectTransform;
        var window = slider != null ? slider.parent as RectTransform : null;
        var row = window != null ? window.Find("Row_Volume") as RectTransform : null;
        if (row == null)
        {
            Debug.LogWarning("หน้าตั้งค่าไม่มีแถว Row_Volume เลยเพิ่มแถบเสียงเพลง/เอฟเฟกต์ไม่ได้");
            return;
        }
        if (window.Find("Row_Music") != null) return;

        // ของที่อยู่ใต้แถบเสียงเดิมเลื่อนลงให้พอสองแถบใหม่ แล้วขยายหน้าต่างโดยให้ของทุกชิ้นอยู่กลางเหมือนเดิม
        float added = Block * 2f, below = slider.anchoredPosition.y;
        foreach (RectTransform child in window)
        {
            if (child.anchorMin != child.anchorMax) continue; // พื้นหลังที่ยืดเต็มหน้าต่างไม่ต้องเลื่อน
            if (child.anchoredPosition.y < below) child.anchoredPosition += Vector2.down * added;
            child.anchoredPosition += Vector2.up * added * 0.5f;
        }
        window.sizeDelta += Vector2.up * added;

        Rename(row, "MASTER VOLUME", "เสียงทั้งหมด");
        Add(window, row, slider, 1, "Music", "MUSIC", "เสียงเพลง", GameAudio.MusicVolume, value => GameAudio.MusicVolume = value);
        Add(window, row, slider, 2, "Sfx", "SOUND EFFECTS", "เสียงเอฟเฟกต์", GameAudio.SfxVolume, value =>
        {
            GameAudio.SfxVolume = value;
            Sfx.Play(SfxId.UiClick); // ให้ได้ยินระดับเสียงใหม่ระหว่างเลื่อน (คลังเสียงกันเล่นถี่เกินอยู่แล้ว)
        });
    }

    static void Add(RectTransform window, RectTransform rowSource, RectTransform sliderSource, int index,
                    string id, string english, string thai, float start, System.Action<float> changed)
    {
        var row = Object.Instantiate(rowSource, window);
        row.name = "Row_" + id;
        row.anchoredPosition = rowSource.anchoredPosition + Vector2.down * Block * index;
        Rename(row, english, thai);
        var valueNode = row.Find("Value");
        var value = valueNode != null ? valueNode.GetComponent<TMP_Text>() : null;

        var bar = Object.Instantiate(sliderSource, window);
        bar.name = "Slider_" + id;
        bar.anchoredPosition = sliderSource.anchoredPosition + Vector2.down * Block * index;
        var slider = bar.GetComponent<Slider>();
        if (slider == null) return;
        slider.onValueChanged = new Slider.SliderEvent(); // ตัดการผูกกับความดังรวมที่ติดมากับโคลนออก
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(start);
        Show(value, start);
        slider.onValueChanged.AddListener(v => { changed(v); Show(value, v); });
    }

    static void Show(TMP_Text value, float volume)
    {
        if (value != null) value.text = Mathf.RoundToInt(volume * 100f) + "%";
    }

    static void Rename(RectTransform row, string english, string thai)
    {
        var labelNode = row.Find("Label");
        var label = labelNode != null ? labelNode.GetComponent<TMP_Text>() : null;
        if (label == null) return;
        var localized = label.GetComponent<LocalizedText>();
        if (localized != null)
        {
            localized.englishText = english;
            localized.thaiText = thai;
            localized.Apply();
        }
        else label.text = thai;
    }
}
