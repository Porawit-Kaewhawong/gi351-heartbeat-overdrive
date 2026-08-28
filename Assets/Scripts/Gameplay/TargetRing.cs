using UnityEngine;

namespace HBO
{
    /// <summary>
    /// วงเป้า (Timing Zone) — ถ้ายังไม่มีอาร์ตจริง จะสร้างสไปรต์ placeholder ให้ตอนรัน
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class TargetRing : MonoBehaviour
    {
        public Color color = new Color(1f, 1f, 1f, 0.9f);

        void Awake()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr.sprite == null) sr.sprite = PlaceholderAssets.Ring(256, 0.12f, Color.white);
            sr.color = color;
        }
    }
}
