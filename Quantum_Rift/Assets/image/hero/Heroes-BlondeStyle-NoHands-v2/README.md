# Heroes BlondeStyle NoHands v2 — Unity

ใช้เฉพาะภาพจาก `output/imagegen/Heroes-BlondeStyle-NoHands-v2/final`:
Hero01_Knight, Hero02_Blonde, Hero03_BlueHair และ Hero04_WhiteHair
คัดลอกแผ่น `*-21Frames.png` ตรงจากต้นฉบับโดยไม่แก้ไขพิกเซล

แต่ละตัวมี Idle 7 เฟรม (8 FPS, วนซ้ำ), Walk 7 เฟรม (10 FPS, วนซ้ำ)
และ Death 7 เฟรม (8 FPS, เล่นครั้งเดียวค้างเฟรมสุดท้าย) รวม 84 เฟรม / 12 คลิป / 4 prefab
แผ่นภาพ 2240×960, ช่อง 320×320, เรียงแถวบนลงล่าง Idle / Walk / Death
Pivot (0.5, 0.15), 100 pixels per unit, Point filter, ไม่บีบอัดและไม่ใช้ mipmaps

Prefab: `Assets/Prefab/Hero/Heroes-BlondeStyle-NoHands-v2`
คลิปและ Animator: `Assets/Animation/Hero/Heroes-BlondeStyle-NoHands-v2/<ชื่อตัว>`
ภาพ: `Assets/image/hero/Heroes-BlondeStyle-NoHands-v2`
ตัวอย่างทั้งสี่อยู่ใน `Assets/Scenes/Animation.unity` กลุ่ม `Heroes NoHands v2 Preview`

Animator ใช้ `isWalking` (bool) ชื่อเดียวกับ PlayerMovement เดิม และ `isDead` (bool)
สคริปต์ HeroNoHandsAnimation มี SetWalking, FaceLeft, PlayDeath และ ResetToIdle
Death มีลำดับก่อนการเดิน และค้างท่าจนสั่งคืนชีพอย่างชัดเจน
ทดสอบแต่ละท่าได้จาก Animation window หรือ Context Menu ของสคริปต์ขณะ Play Mode

เป็น prefab ภาพเคลื่อนไหวที่มี SpriteRenderer, Animator และสคริปต์ควบคุมท่า
ยังไม่ผูกระบบเลือด อาวุธ หรือการรับอินพุต และไม่มีท่าโจมตีในภาพชุดนี้

สร้างผ่าน `Tools > Quantum Rift > Build Heroes NoHands v2` ขณะหยุด Play/Animation Preview
ตรวจด้วย `Tools > Quantum Rift > Validate Heroes NoHands v2`
รายงานตรวจของ Unity อยู่ใน `Library/HeroesNoHandsValidation.txt`
Builder รักษา GUID ของสไปรต์ และเก็บ controller/prefab ที่มีอยู่เมื่อรันซ้ำ

Prefab Transform Scale: X=0.5, Y=0.5, Z=1 ให้ขนาดใกล้เคียงตัวละครเดิมในฉาก Animation
