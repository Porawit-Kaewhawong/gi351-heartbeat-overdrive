using UnityEngine;

namespace HBO
{
    /// <summary>
    /// วงแหวนที่ระเบิดออกแล้วจางหาย — ใช้เน้นตอนกด Perfect ให้ต่างจาก Great ชัดๆ
    /// ใช้สไปรต์ที่ cache ไว้แล้ว จึงไม่สร้างเท็กซ์เจอร์ใหม่ทุกครั้งที่ตี
    /// </summary>
    public class HitBurst : MonoBehaviour
    {
        public static void Spawn(Vector3 pos, Color color, float fromScale, float toScale,
            float duration, int sortingOrder = 20)
        {
            var go = new GameObject("HitBurst");
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderAssets.SharedPulseRing;
            sr.color = color;
            sr.sortingOrder = sortingOrder;

            var burst = go.AddComponent<HitBurst>();
            burst.sr = sr;
            burst.fromScale = fromScale;
            burst.toScale = toScale;
            burst.duration = Mathf.Max(0.01f, duration);
        }

        SpriteRenderer sr;
        float fromScale, toScale, duration, elapsed;

        void Update()
        {
            // unscaled เพราะ hit freeze หยุด timeScale อยู่พอดีตอนที่เอฟเฟกต์นี้เล่น
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / duration);

            float eased = 1f - (1f - k) * (1f - k); // ease-out: พุ่งออกเร็วแล้วช้าลง
            float s = Mathf.Lerp(fromScale, toScale, eased);
            transform.localScale = new Vector3(s, s, 1f);

            var c = sr.color;
            c.a = 1f - k;
            sr.color = c;

            if (k >= 1f) Destroy(gameObject);
        }
    }
}
