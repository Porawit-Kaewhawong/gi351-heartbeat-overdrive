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
            {
                string bpm = Mathf.RoundToInt(conductor.CurrentBpm) + " BPM";
                bpmText.text = health != null && health.EnemyCount > 1
                    ? string.Format("{0}  {1}/{2}   ·   {3}",
                        health.CurrentEnemy.name, health.EnemyIndex + 1, health.EnemyCount, bpm)
                    : bpm;
            }

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

        /// <summary>ป้ายกลางจอตอนตีมอนสเตอร์ตัวหนึ่งตาย (ค้างนานกว่าป้ายตัดสินปกติ)</summary>
        public void ShowEnemyDown(string enemyName)
        {
            if (judgementText == null) return;
            judgementTimer = 1.1f;
            judgementText.text = enemyName + " DOWN!";
            judgementText.color = new Color(0.5f, 1f, 0.6f);
        }

        public void ShowResult(bool playerWon, BattleStats stats)
        {
            if (resultPanel != null) resultPanel.SetActive(true);
            if (resultText == null) return;

            // ใช้ rich text ย่อบรรทัดสถิติ เพราะ Text ตัวนี้ตั้งไว้ 76pt สำหรับพาดหัว
            int monsterTotal = health != null ? health.EnemyCount : stats.monstersDown;
            resultText.text = string.Format(
                "{0}\n<size=38>MONSTERS DOWN {1}/{2}\n" +
                "<color=#FFD93B>PERFECT {3}</color>   <color=#66E5FF>GREAT {4}</color>   <color=#FF6B6B>MISS {5}</color>\n" +
                "BEST COMBO {6}   ACCURACY {7:0}%   TIME {8:0.0}s</size>\n\n<size=40>PRESS SPACE TO RETRY</size>",
                playerWon ? "YOU WIN!" : "FLATLINE...",
                stats.monstersDown, monsterTotal,
                stats.perfect, stats.great, stats.miss,
                stats.bestCombo, stats.Accuracy * 100f, stats.seconds);
        }
    }
}
