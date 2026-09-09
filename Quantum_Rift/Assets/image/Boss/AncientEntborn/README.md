# AncientEntborn — animation assets

โปรเจกต์ `D:/Project_Game/Project_game_Quantum_Rift/Quantum_Rift`, branch ที่สร้าง `Boss_animation`

## ดูใน Unity

1. เปิด `Assets/Scenes/Animation.unity`
2. ขยาย **AncientEntborn Preview** ใน Hierarchy และเลือก **AncientEntborn**
3. ใช้ **Window > Animation > Animation** เลือกคลิป แล้วกดปุ่ม Play ของหน้าต่าง Animation
4. เลือก **AncientEntbornRootPillar** หรือ **AncientEntbornLaser** เพื่อพรีวิวเอฟเฟกต์แยก

## ไฟล์ที่ใช้

- `Assets/Prefab/Boss/AncientEntborn/AncientEntborn.prefab`
- `Assets/Prefab/Boss/AncientEntborn/AncientEntbornRootPillar.prefab`
- `Assets/Prefab/Boss/AncientEntborn/AncientEntbornLaser.prefab`
- คลิปและ Animator: `Assets/Animation/Boss/AncientEntborn`
- รูปและข้อมูล slicing: `Assets/image/Boss/AncientEntborn`

| คลิป | ภาพ | FPS | ความยาว | การเล่น |
|---|---:|---:|---:|---|
| Idle | 1 | 1 | 1.000 s | ค้างท่าเตรียมร่ายเลเซอร์ วนซ้ำ |
| Walk | 7 | 8 | 0.875 s | วนซ้ำ |
| LaserCast | 7 | 8 | 0.875 s | ครั้งเดียว กลับ Idle/Walk |
| Stomp | 7 | 10 | 0.700 s | ครั้งเดียว กลับ Idle/Walk |
| RootErupt | 7 | 10 | 0.700 s | ครั้งเดียว ค้างภาพสุดท้าย |
| LaserBeam | 7 | 12 | 0.583 s | ครั้งเดียว ค้างภาพสุดท้าย |

## การควบคุม

Animator บอสใช้ Bool `isWalking` และ Trigger `LaserCast`, `Stomp` เมื่อเล่นท่าโจมตีจบจะกลับ Idle หรือ Walk ตาม `isWalking` ไม่มีการขยับตำแหน่ง GameObject ด้วยคลิป

Component `AncientEntbornAnimation` มี `SetWalking(bool)`, `FaceLeft(bool)`, `PlayIdle()`, `PlayWalk()`, `PlayLaserCast()`, `PlayStomp()` และเมนู Context หมวด Preview สำหรับทดลองใน Play Mode

Prefab เอฟเฟกต์เป็นภาพอนิเมชันแยก มี SpriteRenderer และ Animator ใช้ state `RootPillar` / `Laser` ตามลำดับ เริ่มเล่นตั้งแต่ต้นเมื่อ instantiate; ถ้าจะเล่นซ้ำบน instance เดิมใช้ `Animator.Play("RootPillar", 0, 0f)` หรือ `Animator.Play("Laser", 0, 0f)`

ชุดนี้ยังไม่มี AI, ดาเมจ, collision, การเสกราก, การวางเลเซอร์ที่มือ หรือการหมุนกวาดแผนที่ การซ่อน/คืน pool หลังเอฟเฟกต์จบเป็นหน้าที่ระบบ gameplay ชุดภาพไม่มีท่าตาย จึงไม่ได้สร้าง Death animation

## รูปต้นฉบับและจุดยึด

คัดลอกไฟล์ `-Clean.png` ทั้งสามจาก `output/imagegen/AncientEntborn-EchoStyle-v2/Transparent-NoWhiteEdge` โดยไม่แก้ภาพ PNG

- Body: 1672 × 941, อ่านแต่ละแถวซ้ายไปขวา: Walk, LaserCast, Stomp
- RootPillar: 1659 × 948, อ่านซ้ายไปขวา
- Laser: 1024 × 1536, อ่านบนลงล่าง
- ตัด rect รายเฟรมตามขอบ alpha พร้อมขอบว่าง 2 พิกเซล ตรวจว่าครอบคลุมพิกเซลที่ไม่โปร่งใสทั้งหมด
- บอสยึดฐานกลางลำตัว โดยไม่ไล่จุดยึดตามเท้าที่ยกในท่า Stomp รากยึดฐานบนพื้น เลเซอร์ยึดต้นลำแสงด้านซ้าย
- Point filtering, ไม่มี compression/mipmap; Body 100 PPU, Root 150 PPU, Laser 200 PPU

## เครื่องมือและการตรวจสอบ

**Tools > Quantum Rift > Build Ancient Entborn** (`Ctrl+Shift+J`) สร้างสไปรต์ คลิป controller prefab และเพิ่มตัวอย่างเฉพาะ AncientEntborn ในฉาก Animation ให้หยุด Preview/Play และบันทึกฉากก่อนรัน การสร้างซ้ำรักษา GUID และไม่เขียนทับ prefab/controller ที่มีแล้ว เพื่อรักษาการปรับด้วยมือ

**Tools > Quantum Rift > Validate Ancient Entborn** ตรวจจำนวนสไปรต์ จุดยึด ทุกเฟรมของคลิป references ใน prefab และเส้นทาง Animator จริงด้วย PlayableGraph ผลอยู่ `Library/AncientEntbornValidation.txt`
