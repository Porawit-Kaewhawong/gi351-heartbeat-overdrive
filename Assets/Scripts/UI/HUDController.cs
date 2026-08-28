using UnityEngine;
using UnityEngine.UI;

namespace HBO
{
    /// <summary>
    /// คุม HUD ทั้งหมด: หลอดเลือดสองฝั่ง คอมโบ ป้ายตัดสิน BPM และหน้าจอเริ่ม/จบ
    /// หลอดเลือดใช้การขยับ anchor ของ Image ลูก (ไม่พึ่ง fillAmount จึงไม่ต้องมีสไปรต์)
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        public HealthSystem health;
        public Conductor conductor;

        [Header("หลอดเลือด (Image ลูกที่เป็นแถบสี)")]
        public Image playerHpFill;
        public Image enemyHpFill;

        [Header("ตัวหนังสือ")]
        public Text comboText;
        public Text judgementText;
        public Text bpmText;

        [Header("แผงหน้าจอ")]
        public GameObject readyPanel;
        public GameObject resultPanel;
        public Text resultText;

        float judgementTimer;

        void Start()
        {
            if (health != null) health.OnChanged += RefreshBars;
            RefreshBars();
            if (comboText != null) comboText.text = "";
            if (judgementText != null) judgementText.text = "";
        }

        void RefreshBars()
        {
            if (health == null) return;
            SetFill(playerHpFill, health.PlayerFraction, false);
            SetFill(enemyHpFill, health.EnemyFraction, true);
        }

        static void SetFill(Image img, float frac, bool fromRight)
        {
            if (img == null) return;
            var rt = img.rectTransform;
            if (fromRight)
            {
                rt.anchorMin = new Vector2(1f - frac, 0f);
                rt.anchorMax = Vector2.one;
            }
            else
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = new Vector2(frac, 1f);
            }
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void Update()
        {
            if (bpmText != null && conductor != null && conductor.CurrentBpm > 0f)
                bpmText.text = Mathf.RoundToInt(conductor.CurrentBpm) + " BPM";

            if (judgementText != null && judgementTimer > 0f)
            {
                judgementTimer -= Time.deltaTime;
                var c = judgementText.color;
                c.a = Mathf.Clamp01(judgementTimer / 0.3f);
                judgementText.color = c;
            }
        }

        public void ShowReady()
        {
            if (readyPanel != null) readyPanel.SetActive(true);
            if (resultPanel != null) resultPanel.SetActive(false);
        }

        public void ShowBattle()
        {
            if (readyPanel != null) readyPanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
        }

        public void ShowJudgement(Judgement j, int combo)
        {
            if (judgementText != null)
            {
                judgementTimer = 0.6f;
                switch (j)
                {
                    case Judgement.Perfect:
                        judgementText.text = "PERFECT!";
                        judgementText.color = new Color(1f, 0.85f, 0.2f);
                        break;
                    case Judgement.Great:
                        judgementText.text = "GREAT";
                        judgementText.color = new Color(0.4f, 0.9f, 1f);
                        break;
                    default:
                        judgementText.text = "MISS";
                        judgementText.color = new Color(1f, 0.3f, 0.3f);
                        break;
                }
            }
            if (comboText != null)
                comboText.text = combo >= 2 ? combo + " COMBO" : "";
        }

        public void ShowResult(bool playerWon)
        {
            if (resultPanel != null) resultPanel.SetActive(true);
            if (resultText != null)
                resultText.text = playerWon
                    ? "YOU WIN!\n\nPRESS SPACE TO RETRY"
                    : "FLATLINE...\n\nPRESS SPACE TO RETRY";
        }
    }
}
