using UnityEngine;

namespace HBO
{
    /// <summary>
    /// ฉากหลังที่สร้างจากโค้ด: ไล่สีเต็มจอ + แสงฟุ้งหลังจุดกดจังหวะที่เต้นตามบีต
    /// มีไว้ไม่ให้ตัวละครลอยอยู่บนพื้นดำเปล่าๆ
    /// ทีมอาร์ตแทนด้วยภาพจริงได้: ใส่สไปรต์ลง backgroundSprite แล้วโค้ดจะไม่วาดไล่สีให้
    /// </summary>
    [DisallowMultipleComponent]
    public class Backdrop : MonoBehaviour
    {
        [Tooltip("ภาพฉากหลังจริง — เว้นว่าง = ใช้ไล่สีที่โค้ดวาดให้")]
        public Sprite backgroundSprite;
        public Color topColor = new Color(0.11f, 0.10f, 0.22f);
        public Color bottomColor = new Color(0.03f, 0.03f, 0.06f);
        public Color glowColor = new Color(0.35f, 0.55f, 1f);

        Camera cam;
        Conductor conductor;
        Transform follow;
        SpriteRenderer bg;
        SpriteRenderer glow;
        float pulse;

        /// <summary>สร้างฉากหลังตอนรัน ถ้ามีอยู่แล้วในซีนจะใช้ตัวเดิม</summary>
        public static Backdrop Create(Camera cam, Conductor conductor, Transform follow)
        {
            var existing = FindFirstObjectByType<Backdrop>();
            if (existing != null) return existing;

            var go = new GameObject("Backdrop");
            var b = go.AddComponent<Backdrop>();
            b.Init(cam, conductor, follow);
            return b;
        }

        void Init(Camera camera, Conductor cond, Transform followTarget)
        {
            cam = camera != null ? camera : Camera.main;
            conductor = cond;
            follow = followTarget;

            bg = NewLayer("Gradient", -100);
            bg.sprite = backgroundSprite != null
                ? backgroundSprite
                : PlaceholderAssets.VerticalGradient(64, topColor, bottomColor);

            glow = NewLayer("TargetGlow", -90);
            glow.sprite = PlaceholderAssets.SharedGlow;
            glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.16f);

            if (conductor != null) conductor.OnBeat += HandleBeat;
        }

        SpriteRenderer NewLayer(string layerName, int order)
        {
            var go = new GameObject(layerName);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            return sr;
        }

        void OnDestroy() { if (conductor != null) conductor.OnBeat -= HandleBeat; }

        void HandleBeat(int beatIndex) { pulse = 1f; }

        void LateUpdate()
        {
            if (cam == null || bg == null) return;

            // ยืดให้คลุมกล้องพอดี เผื่อหน้าต่างถูกปรับขนาด — ตรึงไว้กลางฉาก ไม่ตามกล้อง
            // ไม่งั้นตอน screen shake ฉากหลังจะนิ่งอยู่คนเดียวขณะที่ทุกอย่างสั่น
            float h = cam.orthographicSize * 2f;
            float w = h * cam.aspect;
            bg.transform.position = new Vector3(0f, 0f, 20f);
            bg.transform.localScale = new Vector3(w * 1.25f, h * 1.25f, 1f);

            pulse = Mathf.MoveTowards(pulse, 0f, Time.unscaledDeltaTime * 3.5f);

            if (follow != null)
                glow.transform.position = new Vector3(follow.position.x, follow.position.y, 15f);
            float s = 7f + pulse * 1.8f;
            glow.transform.localScale = new Vector3(s, s, 1f);
            var c = glow.color;
            c.a = 0.15f + pulse * 0.13f;
            glow.color = c;
        }
    }
}
