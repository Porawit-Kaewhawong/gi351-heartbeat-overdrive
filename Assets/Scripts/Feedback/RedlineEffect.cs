using UnityEngine;
using UnityEngine.UI;

namespace HBO
{
    /// <summary>
    /// Redline Pressure: เมื่อ HP ผู้เล่นต่ำ
    /// - ขอบจอแดงกะพริบเป็นจังหวะ "ตุบ-ตับ" ของชีพจร ยิ่งใกล้ตายยิ่งถี่/เข้ม
    /// - สั่ง AudioDirector ให้เสียงหัวใจดังขึ้นและกดเสียงเพลงลง
    /// </summary>
    public class RedlineEffect : MonoBehaviour
    {
        public GameConfig config;
        public HealthSystem health;
        public AudioDirector audioDirector;
        [Tooltip("Image ขอบแดงเต็มจอบน Canvas (สีแดง, สไปรต์เว้นว่างได้)")]
        public Image vignette;

        float phase;
        bool battleOver;

        void Start()
        {
            if (vignette != null && vignette.sprite == null)
                vignette.sprite = PlaceholderAssets.Vignette(256);
            if (health != null) health.OnBattleEnded += HandleBattleEnd;
        }

        void HandleBattleEnd(bool playerWon) { battleOver = true; }

        void Update()
        {
            if (health == null || config == null) return;

            // จบการดวลแล้วชีพจรต้องหยุด ไม่งั้นขอบแดงยังเต้นคาหน้าจอ FLATLINE
            if (battleOver)
            {
                if (vignette != null)
                {
                    var col = vignette.color;
                    col.a = Mathf.MoveTowards(col.a, 0f, Time.unscaledDeltaTime * 1.5f);
                    vignette.color = col;
                }
                if (audioDirector != null) audioDirector.SetHeartbeat(0f, config.heartbeatMinRate);
                return;
            }

            float frac = health.PlayerFraction;
            float severity = frac < config.redlineStartFraction
                ? 1f - (frac / config.redlineStartFraction)
                : 0f;

            float rate = Mathf.Lerp(config.heartbeatMinRate, config.heartbeatMaxRate, severity);
            phase += Time.deltaTime * rate;

            if (vignette != null)
            {
                // คลื่นสองยอดเลียนเสียงหัวใจ "ตุบ-ตับ"
                float wave = Mathf.Max(Pulse(phase, 0f), Pulse(phase, 0.18f) * 0.7f);
                var c = vignette.color;
                c.a = severity <= 0f ? 0f : Mathf.Lerp(0.10f, 0.6f, severity) * wave;
                vignette.color = c;
            }

            if (audioDirector != null)
                audioDirector.SetHeartbeat(severity, rate);
        }

        static float Pulse(float p, float offset)
        {
            float t = Mathf.Repeat(p - offset, 1f);
            return Mathf.Exp(-t * 6f);
        }
    }
}
