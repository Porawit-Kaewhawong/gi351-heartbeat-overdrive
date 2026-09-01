using System.Collections.Generic;
using UnityEngine;

namespace HBO
{
    /// <summary>
    /// ฟังบีตจาก Conductor แล้วปล่อยวง Pulse วิ่งเข้าวงเป้า
    /// เก็บลิสต์วงที่ยังไม่ถูกตัดสินให้ InputJudge ใช้
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

        public void Begin()
        {
            if (spawning) return;
            spawning = true;
            conductor.OnBeat += HandleBeat;
        }

        public void End()
        {
            if (!spawning) return;
            spawning = false;
            conductor.OnBeat -= HandleBeat;
            ClearAll();
        }

        int EffectiveBeatsPerPulse()
        {
            float progress = conductor != null && conductor.health != null
                ? conductor.health.LineupFraction : 1f;
            return config.BeatsPerPulseAt(progress);
        }

        void HandleBeat(int beatIndex)
        {
            if (!spawning) return;
            int every = EffectiveBeatsPerPulse();
            if (every > 1 && beatIndex % every != 0) return;

            // ตั้งเวลาจากบีตจริง ไม่ใช่ Conductor.Now ซึ่งช้ากว่าบีตได้ถึงหนึ่งเฟรม
            // ไม่งั้นทุกวงจะมี error สุ่มๆ 16-33 ms ซึ่งกินครึ่งหนึ่งของหน้าต่าง Perfect
            double spawnTime = conductor.CurrentBeatTime;
            // เล็งเป็น "หมายเลขบีต" ไม่ใช่เวลา วงจะได้ลงตรงบีตจริงแม้ BPM จะเร่งขึ้นระหว่างที่วงวิ่งอยู่
            int targetBeat = beatIndex + Mathf.Max(1, Mathf.RoundToInt(config.approachBeats));

            var go = new GameObject("PulseRing");
            go.transform.position = target != null ? target.position : Vector3.zero;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ringSprite != null ? ringSprite : PlaceholderAssets.SharedPulseRing;
            sr.color = ringColor;
            sr.sortingOrder = 10;
            var ring = go.AddComponent<PulseRing>();
            float targetScale = target != null ? target.localScale.x : 1f;
            ring.Init(conductor, spawnTime, targetBeat, targetScale);
            Active.Add(ring);
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
