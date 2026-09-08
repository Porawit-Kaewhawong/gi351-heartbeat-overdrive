using System.Collections.Generic;
using UnityEngine;

namespace HBO
{
    /// <summary>
    /// ฟังบีตจาก Conductor แล้วปล่อยวง Pulse วิ่งเข้าวงเป้า
    /// เก็บลิสต์วงที่ยังไม่ถูกตัดสินให้ InputJudge ใช้
    ///
    /// วง Pulse ทุกวงเกาะวงเป้าไว้ (ร่วมศูนย์กลางเดียวกัน) และวงเป้าจะ **ย้ายที่หลังเล่นจบแต่ละโน้ต**
    /// ผู้เล่นจึงต้องเลื่อนสายตาไปจุดใหม่ทุกครั้งที่กด แต่ยังตามแค่จุดเดียวไม่ต้องกวาดหาหลายจุด
    /// </summary>
    public class PulseSpawner : MonoBehaviour
    {
        public GameConfig config;
        public Conductor conductor;
        [Tooltip("วงเป้า (Timing Zone) — จะถูกเลื่อนไปจ่อโน้ตตัวถัดไปที่ต้องกด")]
        public Transform target;
        [Tooltip("สไปรต์วง Pulse ของทีมอาร์ต — เว้นว่าง = ใช้วงแหวน placeholder")]
        public Sprite ringSprite;
        public Color ringColor = new Color(0.4f, 0.9f, 1f);

        public readonly List<PulseRing> Active = new List<PulseRing>();

        bool spawning;
        /// <summary>ตำแหน่งบีต (ทศนิยม) ที่วงถัดไปควรถูกปล่อย</summary>
        double nextSpawnBeat;

        TargetRing targetRing;
        Vector3 roamHome;      // จุดตั้งต้นของวงเป้าในซีน ใช้เป็นศูนย์กลางของกรอบสุ่ม
        int resolvedCount;     // จำนวนโน้ตที่เล่นจบไปแล้ว (กดโดนหรือหลุดก็นับ)

        Vector3 moveFrom, moveTo;
        float moveT = 1f;

        void Awake()
        {
            if (target == null) return;
            roamHome = target.position;
            moveFrom = moveTo = roamHome;
            targetRing = target.GetComponent<TargetRing>();
        }

        public void Begin()
        {
            if (spawning) return;
            spawning = true;
            nextSpawnBeat = conductor.NextBeatIndex;
            resolvedCount = 0;
            conductor.OnBeat += HandleBeat;
        }

        public void End()
        {
            if (!spawning) return;
            spawning = false;
            conductor.OnBeat -= HandleBeat;
            ClearAll();
        }

        public float EffectiveBeatsPerPulse()
        {
            float progress = conductor != null && conductor.health != null
                ? conductor.health.LineupFraction : 1f;
            return config.BeatsPerPulseAt(progress);
        }

        void HandleBeat(int beatIndex)
        {
            if (!spawning) return;

            if (targetRing != null) targetRing.Pulse();

            // beatsPerPulse เป็นทศนิยมได้ (2.00 / 1.75 / 1.50 ...) จึงใช้ modulo ไม่ได้
            // สะสมตำแหน่งบีตในอุดมคติไว้แทน แล้วปล่อยวงที่บีตแรกที่เลยตำแหน่งนั้นไป
            // ค่า 1.75 จึงให้ระยะห่างจริงเป็น 2,2,2,1 วนไป — ถี่ขึ้นจริงโดยที่ทุกวงยังลงตรงบีตเป๊ะ
            if (beatIndex < nextSpawnBeat) return;
            nextSpawnBeat += Mathf.Max(0.01f, EffectiveBeatsPerPulse());

            // ตั้งเวลาจากบีตจริง ไม่ใช่ Conductor.Now ซึ่งช้ากว่าบีตได้ถึงหนึ่งเฟรม
            // ไม่งั้นทุกวงจะมี error สุ่มๆ 16-33 ms ซึ่งกินครึ่งหนึ่งของหน้าต่าง Perfect
            double spawnTime = conductor.CurrentBeatTime;
            // เล็งเป็น "หมายเลขบีต" ไม่ใช่เวลา วงจะได้ลงตรงบีตจริงเสมอ
            int targetBeat = beatIndex + Mathf.Max(1, Mathf.RoundToInt(config.approachBeats));

            var go = new GameObject("PulseRing");
            go.transform.position = target != null ? target.position : Vector3.zero;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ringSprite != null ? ringSprite : PlaceholderAssets.SharedPulseRing;
            sr.color = ringColor;
            sr.sortingOrder = 10;
            var ring = go.AddComponent<PulseRing>();
            ring.Init(conductor, target, spawnTime, targetBeat, TargetScale());
            Active.Add(ring);
        }

        /// <summary>ขนาดปลายทางของวง — ใช้ขนาดฐานของวงเป้า ไม่เอาการเต้นตามบีตมาปน</summary>
        float TargetScale()
        {
            if (targetRing != null) return targetRing.BaseScaleX;
            return target != null ? target.localScale.x : 1f;
        }

        /// <summary>
        /// ย้ายวงเป้าไปจุดสุ่มใหม่ เรียกหลังเล่นจบแต่ละโน้ต (ทุกๆ targetMoveEveryPulses โน้ต)
        ///
        /// ย้าย "หลังโน้ตจบ" ไม่ใช่ "ก่อนโน้ตลง" โดยตั้งใจ — โน้ตถัดไปยังเหลืออีกหลายบีตกว่าจะถึงเป้า
        /// วงเป้าจึงเลื่อนไปนิ่งรออยู่ก่อนแล้ว ไม่ไปขยับตอนผู้เล่นกำลังจะกดพอดี
        /// สุ่มใหม่จนกว่าจะห่างจากจุดเดิมเกิน targetMinMoveDistance ไม่งั้นบางรอบจะเหมือนไม่ขยับ
        /// </summary>
        void MoveTargetAfterNote()
        {
            if (target == null || !config.targetRoams) return;

            int every = Mathf.Max(1, config.targetMoveEveryPulses);
            if (resolvedCount % every != 0) return;

            var area = config.targetRoamArea;
            Vector3 pick = moveTo;
            for (int i = 0; i < 8; i++)
            {
                pick = roamHome + new Vector3(
                    Random.Range(-area.x, area.x), Random.Range(-area.y, area.y), 0f);
                if (Vector3.Distance(pick, target.position) >= config.targetMinMoveDistance) break;
            }

            moveFrom = target.position;
            moveTo = pick;
            moveT = 0f;
        }

        void Update()
        {
            if (target == null || moveT >= 1f) return;
            moveT = Mathf.Min(1f, moveT + Time.unscaledDeltaTime / Mathf.Max(0.02f, config.targetMoveDuration));
            float e = moveT * moveT * (3f - 2f * moveT); // smoothstep เข้า-ออกนุ่ม
            target.position = Vector3.Lerp(moveFrom, moveTo, e);
        }

        /// <summary>เรียกเมื่อโน้ตหนึ่งตัวเล่นจบ ไม่ว่าจะกดโดนหรือหลุดเวลา — เป็นจังหวะที่วงเป้าย้ายที่</summary>
        public void Remove(PulseRing ring)
        {
            Active.Remove(ring);
            if (ring != null) Destroy(ring.gameObject);

            resolvedCount++;
            MoveTargetAfterNote();
        }

        public void ClearAll()
        {
            foreach (var r in Active) if (r != null) Destroy(r.gameObject);
            Active.Clear();
        }
    }
}
