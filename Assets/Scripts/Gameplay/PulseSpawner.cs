using System.Collections.Generic;
using UnityEngine;

namespace HBO
{
    /// <summary>
    /// ฟังบีตจาก Conductor แล้วปล่อยวง Pulse วิ่งเข้าวงเป้า
    /// เก็บลิสต์วงที่ยังไม่ถูกตัดสินให้ InputJudge ใช้
    ///
    /// แบบ osu: **แต่ละโน้ตเกิดคนละตำแหน่ง** และวงเป้า (TargetRing) จะวิ่งไปจ่อโน้ตตัวถัดไป
    /// ที่ต้องกด ผู้เล่นจึงต้องกวาดสายตาไปมาแทนที่จะจ้องจุดเดียวกลางจอ
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
        Vector3 roamHome;        // จุดตั้งต้นของวงเป้าในซีน ใช้เป็นศูนย์กลางของกรอบสุ่ม
        Vector3 lastNotePos;     // ตำแหน่งของโน้ตที่ปล่อยไปล่าสุด
        int pulseCount;

        Vector3 focusFrom, focusTo;
        float focusT = 1f;

        void Awake()
        {
            if (target == null) return;
            roamHome = target.position;
            lastNotePos = focusFrom = focusTo = roamHome;
            targetRing = target.GetComponent<TargetRing>();
        }

        public void Begin()
        {
            if (spawning) return;
            spawning = true;
            nextSpawnBeat = conductor.NextBeatIndex;
            pulseCount = 0;
            lastNotePos = roamHome;
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

            Vector3 pos = PickNotePosition();
            pulseCount++;

            var go = new GameObject("PulseRing");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ringSprite != null ? ringSprite : PlaceholderAssets.SharedPulseRing;
            sr.color = ringColor;
            sr.sortingOrder = 10;
            var ring = go.AddComponent<PulseRing>();
            ring.Init(conductor, pos, spawnTime, targetBeat, TargetScale());
            Active.Add(ring);
        }

        /// <summary>ขนาดปลายทางของวง — ใช้ขนาดฐานของวงเป้า ไม่เอาการเต้นตามบีตมาปน</summary>
        float TargetScale()
        {
            if (targetRing != null) return targetRing.BaseScaleX;
            return target != null ? target.localScale.x : 1f;
        }

        /// <summary>
        /// เลือกตำแหน่งให้โน้ตตัวถัดไป — ย้ายทุกๆ targetMoveEveryPulses โน้ต
        /// สุ่มใหม่จนกว่าจะห่างจากโน้ตก่อนหน้าเกิน targetMinMoveDistance
        /// ไม่งั้นบางรอบจะได้จุดที่แทบทับของเดิมจนดูเหมือนไม่ขยับ
        /// </summary>
        Vector3 PickNotePosition()
        {
            if (!config.targetRoams) return roamHome;

            int every = Mathf.Max(1, config.targetMoveEveryPulses);
            if (pulseCount % every != 0) return lastNotePos;

            var area = config.targetRoamArea;
            Vector3 pick = lastNotePos;
            for (int i = 0; i < 8; i++)
            {
                pick = roamHome + new Vector3(
                    Random.Range(-area.x, area.x), Random.Range(-area.y, area.y), 0f);
                if (Vector3.Distance(pick, lastNotePos) >= config.targetMinMoveDistance) break;
            }

            lastNotePos = pick;
            return pick;
        }

        /// <summary>โน้ตตัวถัดไปที่ต้องกด — วงเป้าจะวิ่งไปจ่อจุดนี้</summary>
        Vector3 NextNoteFocus()
        {
            for (int i = 0; i < Active.Count; i++)
                if (Active[i] != null && !Active[i].Consumed) return Active[i].transform.position;
            return roamHome; // ไม่มีวงในสนาม กลับไปรอที่จุดตั้งต้น
        }

        void Update()
        {
            if (target == null) return;

            Vector3 want = NextNoteFocus();
            if ((want - focusTo).sqrMagnitude > 0.0001f)
            {
                focusFrom = target.position;
                focusTo = want;
                focusT = 0f;
            }

            if (focusT >= 1f) return;
            focusT = Mathf.Min(1f, focusT + Time.unscaledDeltaTime / Mathf.Max(0.02f, config.targetMoveDuration));
            float e = focusT * focusT * (3f - 2f * focusT); // smoothstep เข้า-ออกนุ่ม
            target.position = Vector3.Lerp(focusFrom, focusTo, e);
        }

        public void Remove(PulseRing ring)
        {
            Active.Remove(ring);
            if (ring != null) Destroy(ring.gameObject);
        }

        public void ClearAll()
        {
            foreach (var r in Active) if (r != null) Destroy(r.gameObject);
            Active.Clear();
        }
    }
}
