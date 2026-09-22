using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// ของใช้ร่วมสำหรับชุดภาพเมนู QuantumRift-Menu-Logo-TH-EN-v1
// ทั้งหน้าเมนูหลักและหน้าต่าง Pause ดึงภาพจากที่เดียวกัน จะได้ไม่ต้องแก้ path สองที่เวลาแพ็กเปลี่ยน
public static class MenuUIPack
{
    public const string Folder = "Assets/image/UI_Image/QuantumRift-Menu-Logo-TH-EN-v1";
    public const string SpriteFolder = Folder + "/sprites";

    public const string Language = "EN"; // เปลี่ยนเป็น "TH" ถ้าจะสลับไปใช้ปุ่มภาษาไทยทั้งชุด

    public const float ButtonAspect = 704f / 192f; // สัดส่วนภาพปุ่มในแพ็ก ใช้คำนวณความสูงจากความกว้าง

    // สีตาม README-Design-Guide ของแพ็ก
    public static readonly Color Backdrop = new Color32(0x08, 0x0D, 0x20, 0xC0); // พื้นเข้มคลุมจอตอนพัก
    public static readonly Color Surface = new Color32(0x21, 0x16, 0x37, 0xFF);  // ผิวหน้าต่าง
    public static readonly Color Border = new Color32(0x66, 0x51, 0x85, 0xFF);   // กรอบโลหะม่วง
    public static readonly Color TextMain = new Color32(0xF3, 0xF3, 0xFF, 0xFF); // ข้อความหลัก
    public static readonly Color Accent = new Color32(0x24, 0xE4, 0xFA, 0xFF);   // สีเลือก/โฟกัส
    public static readonly Color Sunken = new Color32(0x11, 0x0B, 0x1E, 0xFF);   // ร่องที่จมลงไป เช่น รางของแถบเลื่อน

    // แพ็กมาแบบ Sprite Mode = Multiple ทำให้ Logo โดน auto-slice หั่นเป็นหลายชิ้นจนใช้เป็นภาพเดียวไม่ได้
    // และ Filter Mode = Bilinear ทำให้ขอบพิกเซลเบลอ ต้องแก้ก่อนถึงจะเอามาใช้ได้
    public static void FixImportSettings()
    {
        var files = Directory.GetFiles(Folder, "*.png", SearchOption.AllDirectories)
            // AssetDatabase ใช้ / เสมอ แต่ GetFiles บนวินโดวส์คืน path มาเป็น backslash
            .Select(p => p.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        foreach (var file in files)
        {
            var importer = AssetImporter.GetAtPath(file) as TextureImporter;
            if (importer == null) continue;

            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single &&
                importer.filterMode == FilterMode.Point) continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
    }

    public static Sprite Load(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new InvalidOperationException($"โหลดสไปรต์ {path} ไม่ได้");
        return sprite;
    }

    // word = Start / Continue / Settings / Resume / Back / Quit (ตามชื่อไฟล์ในแพ็ก)
    public static Sprite LoadButton(string word) => LoadButton(word, Language);

    // ระบุภาษาเองได้ ใช้ตอนใส่ภาพทั้งสองภาษาให้ LocalizedImage เก็บไว้สลับตอนเล่น
    public static Sprite LoadButton(string word, string language) => Load($"{SpriteFolder}/{language}-{word}.png");
}
