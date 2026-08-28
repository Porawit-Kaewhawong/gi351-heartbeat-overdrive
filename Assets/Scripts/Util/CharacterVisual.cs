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
