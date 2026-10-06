using UnityEngine;

// เฉพาะฉากฝึก: เปิดจากเมนูหรือกด Play ตรง ๆ ก็มีฮีโร่ ไม่เปลี่ยน CharacterData ต้นฉบับ
[DefaultExecutionOrder(-2000)]
public sealed class TutorialBootstrap : MonoBehaviour
{
    public CharacterData defaultCharacter;
    void Awake()
    {
        MapManager.startOverride=null;
        if(GameManager.selectedCharacter==null)GameManager.selectedCharacter=defaultCharacter;
        Time.timeScale=1;PauseManager.isGamePaused=false;
    }
}
