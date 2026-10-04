using System;
using UnityEngine;

[CreateAssetMenu(menuName="Quantum Rift/Cinematic Library")]
public sealed class CinematicLibrary : ScriptableObject
{
    [Serializable] public sealed class StoryPage
    {
        public string titleThai,titleEnglish;
        [TextArea(2,5)] public string bodyThai,bodyEnglish;
        public Sprite[] backgrounds;
        public Color accent=Color.cyan;
    }
    [Serializable] public sealed class BossProfile
    {
        public string id,displayName,titleThai,titleEnglish;
        public Sprite portrait;
        public Sprite[] backgrounds;
        public Color accent=Color.cyan;
        [Min(2)] public float duration=4.5f;
    }
    public StoryPage[] opening;
    public BossProfile[] bosses;
    public BossProfile Find(GameObject boss)
    {
        if(boss==null || bosses==null)return null;
        string id=boss.GetComponent<EchoCommanderBoss>()!=null?"echo":
            boss.GetComponent<AncientEntbornBoss>()!=null?"entborn":
            boss.GetComponent<ArchitectBossHealth>()!=null?"architect":null;
        return Array.Find(bosses,p=>p!=null&&p.id==id);
    }
}
