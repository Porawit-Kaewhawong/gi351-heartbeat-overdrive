using System.Collections;
using UnityEngine;

namespace HBO
{
    /// <summary>
    /// วิชวลตัวละครแบบง่าย: ท่ายืน + สุ่มท่าโจมตี, กะพริบแดงตอนโดนตี, พุ่งตัวตอนโจมตี
    /// สไปรต์ทั้งหมดใส่ที่คอมโพเนนต์นี้ที่เดียว ไม่ต้องไปยุ่งกับ SpriteRenderer
    /// (ภายหลังเปลี่ยนไปใช้ Animator ได้โดยแก้ไส้ใน Lunge()/FlashHurt())
    /// </summary>
    public class CharacterVisual : MonoBehaviour
    {
        [Header("สไปรต์")]
        [Tooltip("ท่ายืนปกติ — ท่าที่ตัวละครกลับมาหาเสมอหลังโจมตีจบ\n" +
                 "เว้นว่าง = ใช้สไปรต์ที่อยู่ใน SpriteRenderer อยู่แล้ว หรือวงกลม placeholder")]
        public Sprite idleSprite;
        [Tooltip("สไปรต์ท่าโจมตี — ตอนตีจะสุ่มมาหนึ่งใบใส่แทนท่ายืน แล้วคืนท่ายืนเมื่อจบ\n" +
                 "เว้นว่าง = ไม่สลับสไปรต์ พุ่งตัวอย่างเดียวเหมือนเดิม")]
        public Sprite[] attackSprites;
        [Tooltip("ค้างท่าโจมตีไว้กี่วินาที (สั้นกว่านี้จะเห็นไม่ทัน)")]
        public float attackPoseTime = 0.18f;

        [Header("อื่นๆ")]
        [Tooltip("สีที่ tint ทับสไปรต์ — ใช้อาร์ตจริงให้ตั้งเป็นขาวไม่ให้เพี้ยน")]
        public Color bodyColor = Color.white;
        [Tooltip("ทิศที่พุ่งเข้าหาอีกฝ่าย: ผู้เล่น = +1, ศัตรู = -1")]
        public float lungeDirection = 1f;

        SpriteRenderer sr;
        Vector3 home;
        Coroutine co;
        /// <summary>ท่ายืนที่ authoring ไว้ตั้งแต่แรก ใช้เป็นตัวสำรองเวลามอนสเตอร์ตัวใหม่ไม่มีอาร์ต</summary>
        Sprite baseSprite;
        int lastAttackIndex = -1;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

            // ลำดับความสำคัญ: ช่อง Idle Sprite > สไปรต์ที่ค้างอยู่ใน SpriteRenderer > วงกลม placeholder
            if (idleSprite == null)
                idleSprite = sr.sprite != null ? sr.sprite : PlaceholderAssets.Circle(256, Color.white);
            sr.sprite = idleSprite;

            baseSprite = idleSprite;
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
        /// ท่ายืนและท่าโจมตีถูกเซ็ตทับทุกครั้ง เพราะแต่ละตัวมีอาร์ตของตัวเอง — ตัวที่ยังไม่มีอาร์ต
        /// ต้องถอยไปใช้ท่าตั้งต้นของ GameObject ไม่ใช่ไปหยิบท่าของมอนสเตอร์ตัวก่อนหน้ามาใช้
        /// </summary>
        public void Apply(Sprite sprite, Sprite[] attacks, Color color, float scale)
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            idleSprite = sprite != null ? sprite : baseSprite;
            sr.sprite = idleSprite;
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
