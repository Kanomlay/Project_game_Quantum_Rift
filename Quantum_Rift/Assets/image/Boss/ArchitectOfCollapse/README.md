# Architect of Collapse — Phase 1 และ Phase 2

โปรเจกต์ `D:/Project_Game/Project_game_Quantum_Rift/Quantum_Rift` สร้างบน branch `Boss_animation`

## เปิดดูใน Unity

เปิด `Assets/Scenes/Animation.unity` แล้วขยาย **ArchitectOfCollapse Phase1 Preview** หรือ **ArchitectOfCollapse Phase2 Preview** ใน Hierarchy เลือกตัวบอสหรือเอฟเฟกต์ แล้วใช้ **Window > Animation > Animation** เลือกคลิปและกด Play ในหน้าต่าง Animation

Prefab อยู่ `Assets/Prefab/Boss/ArchitectOfCollapse/Phase1` และ `Phase2`

- Phase1: `ArchitectOfCollapsePhase1.prefab`, `ArchitectPhase1Bullet.prefab`, `ArchitectPhase1Laser.prefab`
- Phase2: `ArchitectOfCollapsePhase2.prefab`, `ArchitectPhase2ScytheWave.prefab`, `ArchitectPhase2Laser.prefab`
- คลิปและ Animator อยู่ `Assets/Animation/Boss/ArchitectOfCollapse/Phase1` และ `Phase2`
- รูปและข้อมูล slicing อยู่ `Assets/image/Boss/ArchitectOfCollapse`

## คลิปทั้งหมด 15 ชุด — ชุดละ 7 เฟรม

| เฟส | คลิป | FPS | ความยาว | การเล่น |
|---|---|---:|---:|---|
| 1 | Idle | 8 | 0.875 s | วนซ้ำ |
| 1 | BulletPose | 10 | 0.700 s | ครั้งเดียว กลับ Idle |
| 1 | LaserPose | 8 | 0.875 s | ครั้งเดียว กลับ Idle |
| 1 | MixedPose | 10 | 0.700 s | ครั้งเดียว กลับ Idle |
| 1 | Phase2Charge | 8 | 0.875 s | ครั้งเดียว ค้างภาพสุดท้าย |
| 1 | BulletSpin | 12 | 0.583 s | วนซ้ำ |
| 1 | LaserBeam | 12 | 0.583 s | ครั้งเดียว ค้างภาพสุดท้าย |
| 2 | Idle | 8 | 0.875 s | วนซ้ำ |
| 2 | Float | 10 | 0.700 s | วนซ้ำ |
| 2 | AuraDash | 12 | 0.583 s | ครั้งเดียว ต่อ ReturnSlash |
| 2 | ReturnSlash | 12 | 0.583 s | ครั้งเดียว กลับ Idle/Float |
| 2 | LaserPose | 8 | 0.875 s | ครั้งเดียว กลับ Idle/Float |
| 2 | ScytheSlash | 12 | 0.583 s | ครั้งเดียว กลับ Idle/Float |
| 2 | ScytheWave | 12 | 0.583 s | วนซ้ำ |
| 2 | LaserBeam | 12 | 0.583 s | ครั้งเดียว ค้างภาพสุดท้าย |

## การควบคุม Animator

Phase 1 ใช้ Trigger `BulletPose`, `LaserPose`, `MixedPose`, `Phase2Charge` ชื่อ state ตรงกับชื่อคลิปในตาราง

Phase 2 ใช้ Bool `isFloating` และ Trigger `AuraDash`, `ReturnSlash`, `LaserPose`, `ScytheSlash` เมื่อเล่น AuraDash จบจะต่อ ReturnSlash แล้วกลับ Idle/Float ตาม isFloating การลอยขึ้นลงที่วาดในภาพยังคงอยู่ ส่วน transform ไม่มี animation curve เคลื่อนตำแหน่งจริง

Component **ArchitectPhase1Animation** และ **ArchitectPhase2Animation** มีเมนู Context หมวด Preview สำหรับทดลองใน Play Mode รวมถึงเมธอด `PlayIdle()` และ `Play<ชื่อท่า>()` Phase 2 มี `SetFloating(bool)` กับ `FaceLeft(bool)` เพิ่ม

Phase2Charge ค้างไว้เพื่อรอระบบ gameplay สลับจาก prefab เฟสแรกไปเฟสสอง ใช้ `PlayIdle()` ถ้าต้องการเริ่มทดลองเฟสแรกใหม่ ทั้งสอง prefab ยังไม่มีตัวควบคุมการเปลี่ยนเฟสตาม HP

เอฟเฟกต์มี Animator ของตัวเอง เริ่มเล่นเมื่อ instantiate ถ้าจะเล่นใหม่บน instance เดิม ใช้ `Animator.Play("BulletSpin", 0, 0f)`, `Animator.Play("ScytheWave", 0, 0f)` หรือ `Animator.Play("LaserBeam", 0, 0f)` ตามชนิดเอฟเฟกต์

ชุดนี้เป็นตัวภาพและอนิเมชัน ยังไม่มี AI, ดาเมจ, collision, การเคลื่อนที่พุ่งจริง, การยิงกระสุน หรือการวางเลเซอร์ที่มือ การสร้าง/ซ่อน/คืน pool ของเอฟเฟกต์และการสลับเฟสเป็นหน้าที่ระบบ gameplay ไม่มีท่าตายในชุดภาพ จึงไม่ได้สร้าง Death clip

## แหล่งภาพและจุดยึด

- Phase 1: `output/imagegen/ArchitectOfCollapse-Flesh-v3/final`
- Phase 2: `output/imagegen/ArchitectOfCollapse-Phase2-EchoStyle-v2/Transparent-Clean-v3/PNG`
- ใช้แผ่นรวมตัวบอสและเอฟเฟกต์รวม 6 PNG คัดลอกต้นฉบับโดยไม่แก้ไขรูป
- Phase 1: ช่องตัวบอส 448 × 448, pivot `(0.5, 0.5)`; กระสุน 320 × 320 pivot กลาง; เลเซอร์ 1024 × 256 pivot `(100/1024, 0.5)`
- Phase 2: ช่องตัวบอส 640 × 640, pivot `(0.5, 0.25)`; คลื่นเคียว 384 × 384 pivot กลาง; เลเซอร์ 1024 × 256 pivot `(112/1024, 0.5)`
- ตัวบอสและกระสุน/คลื่นอ่านซ้ายไปขวา เลเซอร์อ่านบนลงล่าง
- Point filtering, ไม่มี compression หรือ mipmap, ไม่มีการย่อ texture ตอน import (แผ่นใหญ่สุด 4480 × 3840 ใช้ Max Size 8192)
- ตัวบอส 100 PPU; กระสุน Phase 1 300 PPU; คลื่นเคียวและเลเซอร์ 200 PPU

## เครื่องมือสร้างและตรวจสอบ

**Tools > Quantum Rift > Build Architect Of Collapse Both Phases** (`Ctrl+Shift+K`) สร้าง assets และกลุ่มพรีวิวในฉาก Animation ให้หยุด Preview/Play และบันทึกฉากก่อนใช้ การสร้างซ้ำคง GUID และไม่เขียนทับ prefab/controller ที่มีแล้ว เพื่อรักษาการแก้ด้วยมือ

**Tools > Quantum Rift > Validate Architect Of Collapse Both Phases** ตรวจขนาด texture จริง สไปรต์และจุดยึด 105 เฟรม, ทุกเฟรมและเวลาของ 15 คลิป, references ของ 6 prefab และเส้นทาง Animator จริงผ่าน PlayableGraph รวมถึง Charge ค้างปลายคลิปและ AuraDash ต่อ ReturnSlash ผลอยู่ `Library/ArchitectOfCollapseValidation.txt`
