# EchoCommander animations

ทำงานในโปรเจกต์ `D:/Project_Game/Project_game_Quantum_Rift/Quantum_Rift` ฉาก `Assets/Scenes/Animation.unity`

## เปิดดูใน Unity

1. เปิดฉาก **Animation** แล้วขยาย **EchoCommander Preview** ใน Hierarchy
2. เลือก **EchoCommander** แล้วเปิดหน้าต่าง **Window > Animation > Animation**
3. เลือกคลิปจากรายการและกด Play ในหน้าต่าง Animation เพื่อพรีวิว
4. เลือก **EchoCommanderProjectile** เพื่อดูคลิปกระสุน Spin

## ไฟล์

- บอส: `Assets/Prefab/Boss/EchoCommander/EchoCommander.prefab`
- กระสุน: `Assets/Prefab/Boss/EchoCommander/EchoCommanderProjectile.prefab`
- คลิปและ Animator: `Assets/Animation/Boss/EchoCommander`
- รูปต้นฉบับและข้อมูล slicing: `Assets/image/Boss/EchoCommander`

| คลิป | เฟรม | FPS | การเล่น |
|---|---:|---:|---|
| Idle | 1 | 1 | ค้างท่ายืนจาก Shoot เฟรมแรก |
| Walk | 7 | 10 | วนซ้ำ |
| Summon | 7 | 8 | ครั้งเดียว ค้างประตูเฟรมสุดท้ายเพิ่ม 3 เฟรมเวลา |
| Shoot | 7 | 10 | ครั้งเดียว |
| Melee | 7 | 12 | ครั้งเดียว |
| ProjectileSpin | 7 | 12 | วนซ้ำ |

## ควบคุม Animator

- Bool `isWalking`: false = Idle, true = Walk
- Trigger `Summon`, `Shoot`, `Attack` (ท่า Melee)
- เมื่อท่าโจมตีจบ จะกลับ Idle หรือ Walk ตามค่า `isWalking`
- Component `EchoCommanderAnimation` มี `SetWalking(bool)`, `FaceLeft(bool)`, `PlayIdle()`, `PlayWalk()`, `PlaySummon()`, `PlayShoot()`, `PlayMelee()`
- ใน Play Mode ใช้เมนู Context ของ component หมวด Preview เพื่อทดลองท่าได้

Prefab เป็นตัวภาพและอนิเมชัน ยังไม่มี AI บอส, ดาเมจ, การเสกลูกน้อง หรือการยิงกระสุนจริง ชุดภาพที่ให้ไม่มีท่าตาย จึงไม่มีคลิปตายเพิ่ม

## การนำเข้ารูปและตรวจสอบ

ใช้ Actions-28Frames-v2 และ Projectile-7Frames จาก `output/imagegen/EchoCommander` โดยคัดลอก PNG เดิมทุกไบต์ ไม่มีการวาดหรือแก้ภาพใหม่

Atlas จริงมีขนาด 1659 x 948 พิกเซลทั้งสองไฟล์ จึงใช้ขอบเฟรมรายตัวแทนกริดเท่ากัน จุดยึดบอสอยู่กึ่งกลางเท้า กระสุนยึดแกนเรืองแสง ภาพใช้ Point filtering, ไม่มี compression/mipmap, 100 PPU สำหรับบอส และ 300 PPU สำหรับกระสุน

ขอบสไปรต์เว้นระยะเพิ่ม 2 พิกเซลจาก alpha > 32 เพื่อตัดพื้นที่โปร่งใสและจุด alpha จางหลงเหลือนอกภาพ ตัว PNG ต้นฉบับยังเก็บครบ การเลื่อนตัวเล็กน้อยจากสัดส่วนที่ต่างกันในภาพแต่ละเฟรมยังเป็นลักษณะของต้นฉบับ

เมนู **Tools > Quantum Rift > Validate Echo Commander** ตรวจจำนวนสไปรต์ จุดยึด ทุกเฟรมของคลิป prefab และเส้นทางเปลี่ยนสถานะ Animator ผ่าน Unity PlayableGraph ผลอยู่ `Library/EchoCommanderValidation.txt`

เมนู **Build Echo Commander** ใช้สร้างใหม่หรือปรับ slicing/คลิป โดยรักษา GUID ของสไปรต์และ assets ที่มีแล้ว ไม่เขียนทับ prefab หรือ controller ที่มีแล้ว และไม่สร้างชุดพรีวิวซ้ำ ให้หยุดพรีวิวและบันทึกฉากก่อนรัน
