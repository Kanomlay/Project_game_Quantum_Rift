using UnityEngine;

// จำบนเครื่องเดิมข้ามการปิดเกม ไม่ผูกกับบัพหรือของในรอบเล่น
public static class CinematicProgress
{
    public const string WatchedKey="quantumrift.story.opening.v1";
    public const string CompletedKey="quantumrift.story.firstRunCompleted.v1";
    public static bool ShouldShowOpening=>PlayerPrefs.GetInt(WatchedKey,0)==0&&PlayerPrefs.GetInt(CompletedKey,0)==0;
    public static void MarkOpeningWatched(){PlayerPrefs.SetInt(WatchedKey,1);PlayerPrefs.Save();}
    public static void MarkRunFinished(){PlayerPrefs.SetInt(CompletedKey,1);PlayerPrefs.Save();}
}
