# Heartbeat Overdrive

> ยิ่งใกล้ชนะ จังหวะยิ่งเร่ง... ยิ่งบาดเจ็บ เสียงหัวใจยิ่งกลบสมาธิ!

เกม 2D Rhythm Combat ดวล 1v1 ด้วยปุ่มเดียว — งานกลุ่มวิชา GI351 (ทีม 4 คน / 1 สัปดาห์)

## เริ่มต้นใช้งาน (ทุกคนในทีมอ่านตรงนี้ก่อน)

1. ติดตั้ง **Unity 6.3 LTS (6000.3.2f1)** ผ่าน Unity Hub
2. Clone repo นี้ แล้วเปิดโฟลเดอร์ด้วย Unity Hub (Add project from disk)
   - เปิดครั้งแรก Unity จะ import สักพัก เป็นเรื่องปกติ
3. ไปที่เมนู **Tools > Heartbeat Overdrive > Setup Main Scene**
   - จะได้ซีน `Assets/Scenes/Main.unity` ที่ต่อสายระบบครบ พร้อมกราฟิก/เสียง placeholder
4. กด **Play** แล้วกด **Space** — เล่นได้เลย!

## วิธีเล่น

- **Space / คลิกซ้าย** ปุ่มเดียวเท่านั้น
- วง Pulse จะหดเข้าหาวงเป้า กดตอนขนาดพอดี = **Perfect** (ดาเมจหนัก) / เฉียดหน่อย = **Great**
- กดพลาดหรือปล่อยหลุด = **Miss** → ศัตรูสวนกลับทันที
- ศัตรูเลือดยิ่งน้อย จังหวะยิ่งเร่ง (**Climax Shift**)
- เราเลือดน้อย ขอบจอแดงกะพริบ + เสียงหัวใจดังกลบเพลง (**Redline Pressure**)

## โครงสร้างโปรเจกต์

```
Assets/
  Scripts/
    Core/      GameConfig (ค่าบาลานซ์ทั้งหมด), Conductor (นาฬิกาจังหวะ),
               HealthSystem, GameManager (สถานะเกม)
    Gameplay/  PulseRing, PulseSpawner, InputJudge (ตัดสิน Perfect/Great/Miss), TargetRing
    Feedback/  FeedbackDirector (shake+hitstop), RedlineEffect, AudioDirector
    UI/        HUDController
    Util/      PlaceholderAssets (สไปรต์ชั่วคราว), ProceduralAudio (เสียงชั่วคราว), CharacterVisual
  Editor/      SceneSetup (สร้างซีนอัตโนมัติ)
  Art/         << ทีมอาร์ตวางไฟล์ภาพที่นี่
  Audio/       << วางไฟล์เสียง/เพลงที่นี่
  Scenes/      Main.unity (สร้างจากเมนู Setup)
```

## เอกสารทีม (อยู่ในโฟลเดอร์ docs/)

| ไฟล์ | สำหรับ |
|---|---|
| `01_GAME_ANALYSIS.md` | วิเคราะห์เกม ความเสี่ยง และแนวทางบาลานซ์ — อ่านก่อนเริ่ม |
| `02_TEAM_PLAN.md` | แผนงาน 7 วัน แบ่งงาน 4 คน + กติกาใช้ git |
| `03_ART_SPEC.md` | สเปกงานอาร์ตทุกชิ้น (ขนาด/จำนวนเฟรม/ไฟล์เสียง) |
| `04_SETUP_GUIDE.md` | คู่มือเทคนิค: ระบบต่อกันยังไง แทน placeholder ยังไง ปรับบาลานซ์ตรงไหน |

## กติกา Git สั้นๆ

- **ห้ามแก้ Main.unity พร้อมกันสองคน** (ไฟล์ซีน merge ไม่ได้) — ใครจะแก้ซีนให้บอกในกลุ่มก่อน
- pull ก่อน push ทุกครั้ง / commit เป็นงานย่อยๆ พร้อมข้อความอธิบาย
- ไฟล์ `.meta` ต้อง commit ด้วยเสมอ (Unity สร้างคู่กับทุกไฟล์)
