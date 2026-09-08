using System.Collections.Generic;
using UnityEngine;

namespace HBO
{
    /// <summary>
    /// ฟังบีตจาก Conductor แล้วปล่อยวง Pulse วิ่งเข้าวงเป้า
    /// เก็บลิสต์วงที่ยังไม่ถูกตัดสินให้ InputJudge ใช้
    /// และเป็นคนย้ายจุดกดจังหวะไปมาด้วย (วงที่กำลังวิ่งอยู่เกาะจุดนี้ จึงเลื่อนตามไปทั้งชุด)
    /// </summary>
    public class PulseSpawner : MonoBehaviour
    {
        public GameConfig config;
        public Conductor conductor;
        [Tooltip("จุดวงเป้า (Timing Zone) ที่วง Pulse วิ่งเข้าหา")]
        public Transform target;
        [Tooltip("สไปรต์วง Pulse ของทีมอาร์ต — เว้นว่าง = ใช้วงแหวน placeholder")]
        public Sprite ringSprite;
        public Color ringColor = new Color(0.4f, 0.9f, 1f);

        public readonly List<PulseRing> Active = new List<PulseRing>();

        bool spawning;
        /// <summary>ตำแหน่งบีต (ทศนิยม) ที่วงถัดไปควรถูกปล่อย</summary>
        double nextSpawnBeat;

        TargetRing targetRing;
        Vector3 roamHome, roamFrom, roamTo;
        float roamT = 1f, roamDuration;

        void Awake()
        {
            if (target == null) return;
            roamHome = target.position;
            roamFrom = roamTo = roamHome;
            targetRing = target.GetComponent<TargetRing>();
        }

        public void Begin()
        {
            if (spawning) return;
            spawning = true;
            nextSpawnBeat = conductor.NextBeatIndex;
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
            if (config.targetRoams && config.targetMoveEveryBeats > 0
                && beatIndex > 0 && beatIndex % config.targetMoveEveryBeats == 0)
                PickRoamDestination();

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

        /// <summary>สุ่มจุดใหม่ให้วงเป้าย้ายไป ห่างจากจุดเดิมพอให้รู้สึกว่าขยับจริง</summary>
        void PickRoamDestination()
        {
            if (target == null) return;
            var area = config.targetRoamArea;

            Vector3 pick = roamTo;
            for (int i = 0; i < 6; i++)
            {
                pick = roamHome + new Vector3(Random.Range(-area.x, area.x), Random.Range(-area.y, area.y), 0f);
                if (Vector3.Distance(pick, target.position) > 1.5f) break;
            }

            roamFrom = target.position;
            roamTo = pick;
            roamDuration = Mathf.Max(0.05f, config.targetMoveDuration);
            roamT = 0f;
        }

        void Update()
        {
            if (target == null || roamT >= 1f) return;
            roamT = Mathf.Min(1f, roamT + Time.unscaledDeltaTime / roamDuration);
            float e = roamT * roamT * (3f - 2f * roamT); // smoothstep เข้า-ออกนุ่ม
            target.position = Vector3.Lerp(roamFrom, roamTo, e);
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
