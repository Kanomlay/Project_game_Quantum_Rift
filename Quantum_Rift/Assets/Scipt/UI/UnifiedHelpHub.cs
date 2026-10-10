using TMPro;
using UnityEngine;
using UnityEngine.UI;

// รวมทางเข้าหน้าเมนูเป็นปุ่มคู่มือเดียว สลับสองหมวดโดยปิดหน้าปัจจุบันก่อน ไม่ซ้อนหน้าต่าง
public sealed class UnifiedHelpHub : MonoBehaviour
{
    public GameHelpWindow guide;
    public MonsterCollectionWindow monsters;
    public Button[] guideTabs,monsterTabs;
    void OnEnable(){LanguageSettings.Changed+=Refresh;Refresh();}
    void OnDisable(){LanguageSettings.Changed-=Refresh;}
    void LateUpdate(){RefreshStates();}
    public void ShowGuide(){if(monsters!=null)monsters.Close();if(guide!=null)guide.Open();RefreshStates();}
    public void ShowMonsters()
    {
        if(guide!=null)guide.Close();
        if(monsters!=null&&!MonsterCollectionWindow.IsOpen)
        {
            monsters.Open();
            // เปิดหมวดแล้วแถวที่เลือกต้องอยู่ในช่วงที่มองเห็น แม้เคยเลื่อนรายการค้างไว้
            if(monsters.list!=null&&monsters.rows.Length>1)monsters.list.verticalNormalizedPosition=1f-(float)monsters.SelectedIndex/(monsters.rows.Length-1);
        }
        RefreshStates();
    }
    public void Refresh()
    {
        if(guideTabs!=null)foreach(var b in guideTabs)if(b!=null)b.GetComponentInChildren<TMP_Text>(true).text=LanguageSettings.IsThai?"คู่มือ":"GUIDE";
        if(monsterTabs!=null)foreach(var b in monsterTabs)if(b!=null)b.GetComponentInChildren<TMP_Text>(true).text=LanguageSettings.IsThai?"มอนสเตอร์":"MONSTERS";
        RefreshStates();
    }
    void RefreshStates()
    {
        if(guideTabs!=null)foreach(var b in guideTabs)if(b!=null){var s=b.GetComponent<QuantumUiButtonState>();if(s!=null){s.chosen=guide!=null&&guide.panel.activeSelf;s.Refresh();}}
        if(monsterTabs!=null)foreach(var b in monsterTabs)if(b!=null){var s=b.GetComponent<QuantumUiButtonState>();if(s!=null){s.chosen=monsters!=null&&monsters.panel.activeSelf;s.Refresh();}}
    }
}
