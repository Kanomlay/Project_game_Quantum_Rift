# เครดิตและสัญญาอนุญาตของไฟล์เสียง

ไฟล์เสียงเอฟเฟกต์ทั้งหมดอยู่ใน `SFX/` แยกโฟลเดอร์ตามที่มา ข้อความเดียวกันนี้แสดงในเกมที่หน้า ตั้งค่า → เครดิต (`Assets/Resources/Credits.txt`)
ถ้าเพิ่มเสียงหรือเพลงจากแหล่งใหม่ ให้เพิ่มทั้งในไฟล์นี้และใน `Credits.txt`

## SFX/PixelCombat (ต้องให้เครดิต)

- ผลงาน: "Pixel Combat SFX" โดย Helton Yan
- ที่มา: https://heltonyan.itch.io/pixelcombat
- สัญญาอนุญาต: Creative Commons Attribution 4.0 International (CC BY 4.0) https://creativecommons.org/licenses/by/4.0/
- การดัดแปลง: แปลงจาก 96 kHz / 24 บิต สเตอริโอ เป็น 44.1 kHz / 16 บิต โมโน ตัดความเงียบท้ายไฟล์ ใช้ 3 แบบย่อยแรกของแต่ละเสียง และเปลี่ยนชื่อไฟล์

## SFX/RetroMecha (ต้องให้เครดิต)

- ผลงาน: "Retro Mecha SFX" โดย Helton Yan
- ที่มา: https://heltonyan.itch.io/retro-mecha-sfx
- สัญญาอนุญาต: CC BY 4.0 https://creativecommons.org/licenses/by/4.0/
- การดัดแปลง: แปลงเป็น 44.1 kHz / 16 บิต โมโน ตัดความเงียบท้ายไฟล์ และเปลี่ยนชื่อไฟล์

## SFX/OpenGameArt (สาธารณสมบัติ CC0 ไม่บังคับเครดิต)

| ไฟล์ | ผลงาน | ผู้ทำ | ที่มา |
|---|---|---|---|
| `SheathSqueeze.wav`, `SeaxUnsheathe.wav` | Fantasy Sound Effects (Tinysized SFX) | Vehicle | https://opengameart.org/content/fantasy-sound-effects-tinysized-sfx |
| `Bang02.ogg` | 25 CC0 bang / firework SFX | rubberduck | https://opengameart.org/content/25-cc0-bang-firework-sfx |
| `Teleport02.ogg` | 50 CC0 Sci-Fi SFX | rubberduck | https://opengameart.org/content/50-cc0-sci-fi-sfx |

## การจับคู่เสียงกับเหตุการณ์ในเกม

ตารางอยู่ใน `Assets/Editor/SfxLibraryBuilder.cs` (เหตุการณ์ → ชื่อไฟล์ ความดัง ระดับเสียง) คลังเสียงจริงคือ `Assets/Resources/SfxLibrary.asset` ที่ Editor สร้างให้ทุกครั้งที่กด Play
