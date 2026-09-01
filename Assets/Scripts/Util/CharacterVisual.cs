using System.Collections;
using UnityEngine;

namespace HBO
{
    /// <summary>
    /// วิชวลตัวละครแบบง่าย: กะพริบแดงตอนโดนตี พุ่งตัวตอนโจมตี
    /// ตอนนี้เป็นวงกลม placeholder — ทีมอาร์ตลากสไปรต์จริงใส่ SpriteRenderer ได้เลย
    /// (ภายหลังเปลี่ยนไปใช้ Animator ได้โดยเรียกเมธอดเดิมสองตัวนี้)
    /// </summary>
    public class CharacterVisual : MonoBehaviour
    {
        public Color bodyColor = Color.white;
        [Tooltip("ทิศที่พุ่งเข้าหาอีกฝ่าย: ผู้เล่น = +1, ศัตรู = -1")]
        public float lungeDirection = 1f;

        SpriteRenderer sr;
        Vector3 home;
        Coroutine co;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            if (sr.sprite == null) sr.sprite = PlaceholderAssets.Circle(256, Color.white);
            sr.color = bodyColor;
            home = transform.localPosition;
        }

        public void Lunge() { Play(LungeCo()); }
        public void FlashHurt() { Play(HurtCo()); }

        /// <summary>เปลี่ยนหน้าตาเป็นมอนสเตอร์ตัวใหม่ โดยคงความโปร่งใสปัจจุบันไว้ (จะได้เฟดเข้าต่อได้)</summary>
        public void Apply(Sprite sprite, Color color, float scale)
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sprite != null) sr.sprite = sprite;
            float alpha = sr.color.a;
            bodyColor = color;
            var c = color; c.a = color.a * alpha;
            sr.color = c;
            if (scale > 0f) transform.localScale = Vector3.one * scale;
        }

        public IEnumerator FadeOutRoutine(float duration) { return FadeCo(1f, 0f, duration); }
        public IEnumerator FadeInRoutine(float duration) { return FadeCo(0f, 1f, duration); }

        IEnumerator FadeCo(float from, float to, float duration)
        {
            if (co != null) { StopCoroutine(co); co = null; }
            transform.localPosition = home;
            // ใช้ unscaled เพราะ hit freeze อาจยังค้าง timeScale อยู่ตอนตัวสุดท้ายตาย
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(from, to, duration > 0f ? t / duration : 1f));
                yield return null;
            }
            SetAlpha(to);
        }

        void SetAlpha(float a)
        {
            if (sr == null) return;
            var c = bodyColor; c.a = bodyColor.a * a; sr.color = c;
        }

        void Play(IEnumerator routine)
        {
            if (co != null) StopCoroutine(co);
            transform.localPosition = home;
            sr.color = bodyColor;
            co = StartCoroutine(routine);
        }

        IEnumerator LungeCo()
        {
            const float d = 0.14f;
            for (float t = 0; t < d; t += Time.deltaTime)
            {
                float k = Mathf.Sin(t / d * Mathf.PI);
                transform.localPosition = home + Vector3.right * (lungeDirection * k * 0.6f);
                yield return null;
            }
            transform.localPosition = home;
        }

        IEnumerator HurtCo()
        {
            for (int i = 0; i < 3; i++)
            {
                sr.color = new Color(1f, 0.25f, 0.25f);
                yield return new WaitForSeconds(0.05f);
                sr.color = bodyColor;
                yield return new WaitForSeconds(0.05f);
            }
        }
    }
}
