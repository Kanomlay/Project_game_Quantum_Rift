# Shop Idle — Forest / Spaceship

ใช้ภาพ `Forest-Idle-7Frames.png` และ `Spaceship-Idle-7Frames.png` จาก
`output/imagegen/QuantumRift-ShopIdle-UI-v3/final` โดยคัดลอกตรงจากต้นฉบับ

- ร้านค้าธีมป่าและธีมยานอวกาศ แบบละ 7 เฟรม รวม 14 เฟรม
- Idle วนซ้ำ 6 FPS รอบละ 7/6 วินาที (~1.167 วินาที)
- แผ่นภาพ 3136×448 ช่องละ 448×448 เรียงซ้ายไปขวา
- Pivot (224,32) จากล่างซ้าย หรือ normalized (0.5, 0.07142857)
- Point filter, 100 PPU, ไม่บีบอัด ไม่ใช้ mipmaps และไม่ย่อภาพตอนนำเข้า
- Prefab Scale (0.5, 0.5, 1) รักษาสัดส่วนเดียวกับภาพและ Hero ชุดใหม่

เปิด `Assets/Scenes/Animation.unity` แล้วเลือกกลุ่ม **Shop Idle Preview**
เลือก ShopForest หรือ ShopSpaceship และเปิดหน้าต่าง Animation เพื่อพรีวิว Idle
ลาก prefab จาก `Assets/Prefab/Shop` ไปใช้ในฉากอื่นได้ โดย Animator เริ่ม Idle อัตโนมัติ
คลิปและ controller อยู่ที่ `Assets/Animation/Shop`

เมนูสร้าง: Tools > Quantum Rift > Build Shop Idle
เมนูตรวจ: Tools > Quantum Rift > Validate Shop Idle
ผลตรวจ: Library/ShopIdleValidation.txt
การสร้างซ้ำรักษา GUID ของสไปรต์และเก็บ controller/prefab ที่มีอยู่แล้ว

งานนี้เป็นภาพเคลื่อนไหวของ NPC ร้านค้า ยังไม่ได้เชื่อมเปิดหน้าร้าน การซื้อสินค้า
หรือระบบสุ่มสินค้า ภาพ UI ในโฟลเดอร์ต้นทางเป็นคนละส่วนกับแอนิเมชัน Idle นี้
