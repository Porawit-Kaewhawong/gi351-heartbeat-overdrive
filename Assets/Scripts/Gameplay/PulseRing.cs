using UnityEngine;

namespace HBO
{
    /// <summary>
    /// วง Pulse หนึ่งวง: หดจากขนาดเริ่มต้นเข้าหาวงเป้า ให้ผู้เล่นกดตอนขนาดพอดี
    /// ตำแหน่ง/ขนาดคำนวณจาก dspTime ตรงๆ จึงไม่เพี้ยนตาม framerate
    /// </summary>
    public class PulseRing : MonoBehaviour
    {
        public double SpawnTime { get; private set; }
        public double HitTime { get; private set; }
        public bool Consumed { get; set; }

        public float startScale = 3.2f;
        float targetScale = 1f;

        Conductor conductor;
        int targetBeat;

        SpriteRenderer sr;

        /// <summary>วงนี้ต้องถูกกดตอนบีตหมายเลข targetBeat ไม่ใช่ตอนเวลาที่ตายตัว</summary>
        public void Init(Conductor conductor, double spawnTime, int targetBeat, float targetScale)
        {
            this.conductor = conductor;
            this.targetBeat = targetBeat;
            SpawnTime = spawnTime;
            this.targetScale = targetScale;
            sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == null)
                sr.sprite = PlaceholderAssets.SharedPulseRing;
            RefreshHitTime();
            UpdateVisual();
        }

        // เล็งเวลาใหม่ทุกเฟรม เพื่อให้วงลงตรงบีตจริงเสมอแม้ Climax Shift จะเร่ง BPM ระหว่างที่วงกำลังวิ่ง
        void RefreshHitTime()
        {
            if (conductor != null) HitTime = conductor.TimeOfBeat(targetBeat);
        }

        void Update() { RefreshHitTime(); UpdateVisual(); }

        void UpdateVisual()
        {
            if (HitTime <= SpawnTime) return;
            double now = Conductor.Now;
            float t = (float)((now - SpawnTime) / (HitTime - SpawnTime));
            float s = Mathf.LerpUnclamped(startScale, targetScale, Mathf.Min(t, 1.15f));
            transform.localScale = new Vector3(s, s, 1f);

            if (sr != null)
            {
                // ค่อยๆ ชัดขึ้นระหว่างวิ่งเข้า สว่างสุดตอนถึงจังหวะ แล้วจางหายเองถ้าเลยจังหวะไป
                // (ปกติ AutoMissSweep เก็บวงทิ้งก่อนอยู่แล้ว จะเห็นตอนปิดตัดสิน เช่น ช่วงสลับมอนสเตอร์)
                float a = Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(t));
                if (t > 1f) a *= Mathf.Clamp01(1f - (t - 1f) * 6f);
                var c = sr.color; c.a = Consumed ? 0f : a; sr.color = c;
            }
        }
    }
}
