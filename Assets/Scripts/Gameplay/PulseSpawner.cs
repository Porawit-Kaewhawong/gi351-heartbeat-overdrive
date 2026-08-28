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

        void HandleBeat(int beatIndex)
        {
            if (!spawning) return;
            if (config.beatsPerPulse > 1 && beatIndex % config.beatsPerPulse != 0) return;

            double now = Conductor.Now;
            double hitTime = now + conductor.BeatInterval * config.approachBeats;

            var go = new GameObject("PulseRing");
            go.transform.position = target != null ? target.position : Vector3.zero;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = ringColor;
            sr.sortingOrder = 10;
            var ring = go.AddComponent<PulseRing>();
            float targetScale = target != null ? target.localScale.x : 1f;
            ring.Init(now, hitTime, targetScale);
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
