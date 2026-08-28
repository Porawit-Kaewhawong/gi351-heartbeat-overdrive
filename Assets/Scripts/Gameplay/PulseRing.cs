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

        SpriteRenderer sr;

        public void Init(double spawnTime, double hitTime, float targetScale)
        {
            SpawnTime = spawnTime;
            HitTime = hitTime;
            this.targetScale = targetScale;
            sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == null)
                sr.sprite = PlaceholderAssets.Ring(256, 0.09f, Color.white);
            UpdateVisual();
        }

        void Update() { UpdateVisual(); }

        void UpdateVisual()
        {
            if (HitTime <= SpawnTime) return;
            double now = Conductor.Now;
            float t = (float)((now - SpawnTime) / (HitTime - SpawnTime));
            float s = Mathf.LerpUnclamped(startScale, targetScale, Mathf.Min(t, 1.15f));
            transform.localScale = new Vector3(s, s, 1f);

            if (sr != null)
            {
                // ค่อยๆ ชัดขึ้นระหว่างวิ่งเข้า สว่างสุดตอนถึงจังหวะ
                float a = Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(t));
                var c = sr.color; c.a = Consumed ? 0f : a; sr.color = c;
            }
        }
    }
}
