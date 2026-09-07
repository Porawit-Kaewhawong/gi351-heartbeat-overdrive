using UnityEngine;

namespace HBO
{
    /// <summary>
    /// วงเป้า (Timing Zone) — ถ้ายังไม่มีอาร์ตจริง จะสร้างสไปรต์ placeholder ให้ตอนรัน
    /// เต้นตามบีตเบาๆ เพื่อเป็นเมโทรนอมทางสายตา
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class TargetRing : MonoBehaviour
    {
        public Color color = new Color(1f, 1f, 1f, 0.9f);
        [Tooltip("ขยายขึ้นกี่ส่วนตอนบีตตก")]
        public float pulseScale = 0.12f;

        Vector3 baseScale;
        float pulse;

        /// <summary>ขนาดจริงที่ไม่รวมการเต้นตามบีต — PulseSpawner ใช้ค่านี้เล็งขนาดปลายทางของวง</summary>
        public float BaseScaleX => baseScale.x;

        void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr.sprite == null) sr.sprite = PlaceholderAssets.Ring(256, 0.12f, Color.white);
            sr.color = color;
            baseScale = transform.localScale;
        }

        public void Pulse() { pulse = 1f; }

        void LateUpdate()
        {
            if (pulse <= 0f) return;
            pulse = Mathf.MoveTowards(pulse, 0f, Time.unscaledDeltaTime * 4f);
            transform.localScale = baseScale * (1f + pulse * pulseScale);
        }
    }
}
