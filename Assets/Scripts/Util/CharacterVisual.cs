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

        [Header("ท่าโจมตี")]
        [Tooltip("สไปรต์ท่าโจมตี — ตอนตีจะสุ่มมาหนึ่งใบใส่แทนท่ายืน แล้วคืนท่ายืนเมื่อจบ\n" +
                 "เว้นว่าง = ไม่สลับสไปรต์ พุ่งตัวอย่างเดียวเหมือนเดิม")]
        public Sprite[] attackSprites;
        [Tooltip("ค้างท่าโจมตีไว้กี่วินาที (สั้นกว่านี้จะเห็นไม่ทัน)")]
        public float attackPoseTime = 0.18f;

        SpriteRenderer sr;
        Vector3 home;
        Coroutine co;
        /// <summary>ท่ายืนที่ต้องกลับไปหาเสมอหลังโจมตีจบ</summary>
        Sprite idleSprite;
        int lastAttackIndex = -1;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            if (sr.sprite == null) sr.sprite = PlaceholderAssets.Circle(256, Color.white);
            idleSprite = sr.sprite;
            sr.color = bodyColor;
            home = transform.localPosition;
        }

        public void Lunge() { Play(LungeCo()); }
        public void FlashHurt() { Play(HurtCo()); }

        /// <summary>
        /// สุ่มท่าโจมตีหนึ่งใบ เลี่ยงไม่ให้ซ้ำท่าเดิมติดกันเพื่อไม่ให้ดูเป็นลูป
        /// คืน null ถ้ายังไม่มีอาร์ตท่าโจมตี (ผู้เรียกจะข้ามการสลับสไปรต์ไป)
        /// </summary>
        Sprite PickAttackSprite()
        {
            if (attackSprites == null || attackSprites.Length == 0) return null;
            if (attackSprites.Length == 1) return attackSprites[0];

            int i = Random.Range(0, attackSprites.Length);
            if (i == lastAttackIndex) i = (i + 1) % attackSprites.Length;
            lastAttackIndex = i;
            return attackSprites[i];
        }

        void RestoreIdle()
        {
            if (sr != null && idleSprite != null) sr.sprite = idleSprite;
        }

        /// <summary>
        /// เปลี่ยนหน้าตาเป็นมอนสเตอร์ตัวใหม่ โดยคงความโปร่งใสปัจจุบันไว้ (จะได้เฟดเข้าต่อได้)
        /// ท่าโจมตีถูกเซ็ตทับทุกครั้ง เพราะแต่ละตัวมีท่าของตัวเอง — ตัวที่ไม่มีอาร์ตท่าโจมตี
        /// ต้องไม่ไปหยิบท่าของตัวก่อนหน้ามาใช้
        /// </summary>
        public void Apply(Sprite sprite, Sprite[] attacks, Color color, float scale)
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sprite != null) { sr.sprite = sprite; idleSprite = sprite; }
            attackSprites = attacks;
            lastAttackIndex = -1;
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
            RestoreIdle();
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
            // ตัดจบท่าโจมตีที่ค้างอยู่ ไม่งั้นถ้าโดนตีสวนกลางท่า สไปรต์จะค้างเป็นท่าโจมตีถาวร
            RestoreIdle();
            co = StartCoroutine(routine);
        }

        IEnumerator LungeCo()
        {
            Sprite pose = PickAttackSprite();
            if (pose != null && sr != null) sr.sprite = pose;

            const float move = 0.14f;                              // ระยะเวลาที่ตัวพุ่งออกไปแล้วกลับ
            float hold = Mathf.Max(move, attackPoseTime);          // ท่าโจมตีค้างอย่างน้อยเท่าการพุ่ง
            for (float t = 0f; t < hold; t += Time.deltaTime)
            {
                float k = t < move ? Mathf.Sin(t / move * Mathf.PI) : 0f;
                transform.localPosition = home + Vector3.right * (lungeDirection * k * 0.6f);
                yield return null;
            }
            transform.localPosition = home;
            RestoreIdle();
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
