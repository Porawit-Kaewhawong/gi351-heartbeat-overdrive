# 04 — คู่มือเทคนิคสำหรับทีมโค้ด

## ภาพรวมสถาปัตยกรรม

ทุกระบบเป็น component บน GameObject `GameSystems` ในซีน ต่อสายกันผ่านช่อง Inspector
(สร้างและต่อสายอัตโนมัติแล้วโดยเมนู Tools > Heartbeat Overdrive > Setup Main Scene)

```
Conductor (นาฬิกา dspTime, คุม BPM ← Climax Shift)
   │ OnBeat
   ▼
PulseSpawner (ปล่อยวงทุก N บีต) ──► PulseRing (หดเข้าวงเป้า)
   │ Active list
   ▼
InputJudge (Space/คลิก → Perfect/Great/Miss)
   │ OnJudged
   ▼
GameManager ──► HealthSystem (HP สองฝั่ง)
   │                │ OnChanged / OnBattleEnded
   ├─► FeedbackDirector (shake + hitstop + SFX)
   ├─► CharacterVisual (พุ่งตัว / กะพริบแดง)
   └─► HUDController (หลอดเลือด คอมโบ ป้ายตัดสิน)

RedlineEffect (อ่าน HP ผู้เล่นทุกเฟรม) ──► ขอบแดง + AudioDirector (หัวใจดัง/duck เพลง)
```

หลักการสำคัญที่ห้ามเปลี่ยน:

1. **เวลาอ้างอิงเดียวของจังหวะคือ `Conductor.Now` (= AudioSettings.dspTime)**
   ห้ามใช้ Time.time ตัดสินจังหวะเด็ดขาด ไม่งั้นเพี้ยนตาม framerate/timeScale
2. **ค่าบาลานซ์อยู่ใน GameConfig ที่เดียว** — จูนใน Inspector ได้ระหว่างกด Play
   (ค่าจะเด้งกลับตอนออก Play mode — จดค่าที่ชอบไว้แล้วค่อยใส่กลับ)
3. ระบบคุยกันผ่าน event (OnBeat / OnJudged / OnChanged / OnBattleEnded)
   จะเพิ่มฟีเจอร์ใหม่ให้ subscribe event ไม่ต้องไปแก้ไส้ระบบเดิม

## ตาราง GameConfig ที่จะได้จูนบ่อย

| ช่อง | ค่าเริ่มต้น | ผล |
|---|---|---|
| baseBpm / maxBpmBonus | 90 / 60 | จังหวะเริ่ม / เร่งสูงสุดตอนศัตรูใกล้ตาย (รวม 150) |
| beatsPerPulse | 2 | ปล่อยวงทุก 2 บีต (ลดเหลือ 1 = โหดขึ้นมาก) |
| approachBeats | 3 | วงวิ่งนานกี่บีต (มากขึ้น = อ่านง่ายขึ้น) |
| perfectWindow / greatWindow | 0.065 / 0.13 | หน้าต่างตัดสิน (วินาที) |
| perfectDamage / greatDamage | 7 / 3 | ดาเมจใส่ศัตรู |
| enemyCounterDamage | 10 | โดนสวนตอน Miss |
| redlineStartFraction | 0.5 | HP ต่ำกว่านี้เริ่ม Redline |
| hitStopDuration / shakePerfect | 0.05 / 0.25 | น้ำหนักฟีดแบ็ก |

## วิธีแทน placeholder ด้วยของจริง

- **ตัวละคร:** เลือก Player/Enemy ในซีน → ลากสไปรต์ใส่ช่อง Sprite ของ SpriteRenderer
  (โค้ดจะสร้างวงกลมให้ *เฉพาะเมื่อช่องว่าง* เท่านั้น) สีตัวละครตั้งที่ CharacterVisual.bodyColor
  — ถ้าใช้อาร์ตจริงให้ตั้งเป็นขาวเพื่อไม่ให้ tint ทับ
- **วง Pulse:** ใส่สไปรต์ไม่ได้จากซีนเพราะวงถูกสร้างตอนรัน → แก้ที่ `PulseSpawner.HandleBeat`
  เพิ่มช่อง `public Sprite ringSprite;` แล้ว assign ให้ sr.sprite ก่อน Init (งาน 5 บรรทัด)
  หรือเปลี่ยนไปใช้ Prefab ก็ได้ถ้าถนัดกว่า
- **วงเป้า / ขอบแดง:** ลากสไปรต์ใส่ TimingTarget (SpriteRenderer) และ RedVignette (Image) ตรงๆ
- **เสียงทั้งหมด:** ลากไฟล์ใส่ช่องใน AudioDirector บน GameSystems
- **ฉากหลัง:** เพิ่ม GameObject ใหม่ + SpriteRenderer, ตั้ง sortingOrder = -10

## อยากเพิ่มอนิเมชันจริง

CharacterVisual มีเมธอด `Lunge()` กับ `FlashHurt()` เป็นจุดต่อเดียวที่ GameManager เรียก
→ เปลี่ยนไส้ในสองเมธอดนี้ให้ไปสั่ง `animator.SetTrigger(...)` แทนได้เลย ไม่ต้องแตะที่อื่น

## ข้อควรระวัง

- ซีนถูกสร้างจากสคริปต์ — ถ้าซีนพัง/สายหลุด ให้รัน Setup Main Scene ใหม่ได้เสมอ
  แต่การแก้ซีนด้วยมือหลังจากนั้น (เช่น ใส่อาร์ต) จะหายถ้ารัน Setup ซ้ำ → ระวัง + commit บ่อย
- Retry ใช้การโหลดซีนใหม่ทั้งซีน ถ้าเพิ่มของที่ต้องรอดข้ามรอบ (เช่น เก็บสถิติ) ต้องใช้ static หรือ DontDestroyOnLoad
- ตอน build จริง: File > Build Profiles → ซีน Main ถูกใส่ใน Scene List ให้แล้วโดยสคริปต์ Setup
- โปรเจกต์ใช้ **Input Manager เดิม** (Input.GetKeyDown) — อย่าเผลอสลับ Active Input Handling
  เป็น Input System Package อย่างเดียว เดี๋ยวปุ่มเงียบทั้งเกม
