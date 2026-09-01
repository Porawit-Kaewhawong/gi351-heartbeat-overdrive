using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HBO
{
    public enum GameState { Ready, Playing, Won, Lost }

    /// <summary>สรุปผลการดวลหนึ่งรอบ ใช้แสดงบนหน้า Result</summary>
    public struct BattleStats
    {
        public int perfect, great, miss, bestCombo, monstersDown;
        public float seconds;

        public int Total => perfect + great + miss;
        public float Accuracy => Total > 0 ? (float)(perfect + great) / Total : 0f;
    }

    /// <summary>
    /// สมองของเกม: คุมสถานะ Ready -> Playing -> Won/Lost -> Retry
    /// และแปลงผลตัดสินจังหวะ (Perfect/Great/Miss) เป็นดาเมจ + ฟีดแบ็ก
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public GameConfig config;
        public Conductor conductor;
        public PulseSpawner spawner;
        public InputJudge judge;
        public HealthSystem health;
        public HUDController hud;
        public AudioDirector audioDirector;
        public FeedbackDirector feedback;
        public CharacterVisual playerVisual;
        public CharacterVisual enemyVisual;

        public GameState State { get; private set; } = GameState.Ready;

        int perfectCombo;
        float stateChangedAt;
        BattleStats stats;
        float battleStartedAt;

        void Start()
        {
            health.ResetAll();
            ApplyEnemyLook();
            hud.ShowReady();
            judge.OnJudged += HandleJudgement;
            health.OnEnemyDefeated += HandleEnemyDefeated;
            health.OnBattleEnded += HandleBattleEnd;
            stateChangedAt = Time.unscaledTime;
        }

        /// <summary>ยัดหน้าตาของมอนสเตอร์ตัวปัจจุบันลง CharacterVisual ฝั่งศัตรู</summary>
        void ApplyEnemyLook()
        {
            if (enemyVisual == null) return;
            var def = health.CurrentEnemy;
            enemyVisual.Apply(def.sprite, def.attackSprites, def.bodyColor, def.scale);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { Quit(); return; }

            bool pressed = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);

            if (State == GameState.Ready && pressed)
            {
                BeginBattle();
            }
            else if (State == GameState.Won || State == GameState.Lost)
            {
                // กันกดรัวท้ายเกมแล้วเผลอ Retry ทันที
                bool cooled = Time.unscaledTime - stateChangedAt > 0.8f;
                if (cooled && (pressed || Input.GetKeyDown(KeyCode.R)))
                    Retry();
            }
        }

        void BeginBattle()
        {
            State = GameState.Playing;
            stateChangedAt = Time.unscaledTime;
            battleStartedAt = Time.unscaledTime;
            perfectCombo = 0;
            stats = new BattleStats();
            hud.ShowBattle();
            conductor.StartConducting();
            spawner.Begin();
            judge.Activate();
            audioDirector.StartMusic();
        }

        void HandleJudgement(Judgement j, PulseRing ring)
        {
            if (State != GameState.Playing) return;

            switch (j)
            {
                case Judgement.Perfect:
                {
                    perfectCombo++;
                    stats.perfect++;
                    if (perfectCombo > stats.bestCombo) stats.bestCombo = perfectCombo;
                    int bonus = Mathf.Min(perfectCombo / config.comboBonusEvery, config.comboBonusCap);
                    health.DamageEnemy(config.perfectDamage + bonus);
                    feedback.OnPerfect();
                    if (enemyVisual != null) enemyVisual.FlashHurt();
                    if (playerVisual != null) playerVisual.Lunge();
                    break;
                }
                case Judgement.Great:
                    perfectCombo = 0;
                    stats.great++;
                    health.DamageEnemy(config.greatDamage);
                    feedback.OnGreat();
                    if (enemyVisual != null) enemyVisual.FlashHurt();
                    if (playerVisual != null) playerVisual.Lunge();
                    break;
                default: // Miss: ผู้เล่นชะงัก + ศัตรูสวนกลับทันที
                    perfectCombo = 0;
                    stats.miss++;
                    health.DamagePlayer(config.enemyCounterDamage);
                    feedback.OnMiss();
                    if (playerVisual != null) playerVisual.FlashHurt();
                    if (enemyVisual != null) enemyVisual.Lunge();
                    break;
            }
            hud.ShowJudgement(j, perfectCombo);
        }

        /// <summary>
        /// มอนสเตอร์ตัวหนึ่งตายแต่ยังเหลือตัวถัดไป: เฟดตัวเก่าออก เฟดตัวใหม่เข้า
        ///
        /// สำคัญ: **ห้ามหยุด PulseSpawner ตรงนี้** วง Pulse ต้องไหลตามบีตต่อเนื่องตลอดการสลับตัว
        /// ไม่งั้นผู้เล่นจะไม่มีตัวจับจังหวะทางสายตาเลยเกือบวินาที เหลือแต่เพลง พอวงกลับมา
        /// ก็จะรู้สึกว่าเพลงกับเกมไม่ตรงกัน (แถมยังต้องรออีก approachBeats บีตกว่าวงแรกจะถึงเป้า)
        /// ปิดแค่ InputJudge พอ แล้วตอน Activate() มันจะทิ้งวงที่เลยเวลาไปให้เองโดยไม่นับ Miss
        /// </summary>
        void HandleEnemyDefeated(int index)
        {
            stats.monstersDown++;
            StartCoroutine(SwapEnemyRoutine());
        }

        IEnumerator SwapEnemyRoutine()
        {
            judge.Deactivate();
            hud.ShowEnemyDown(health.CurrentEnemy.name);

            if (enemyVisual != null) yield return enemyVisual.FadeOutRoutine(config.enemyFadeOutTime);
            else yield return new WaitForSecondsRealtime(config.enemyFadeOutTime);

            health.AdvanceToNextEnemy();
            ApplyEnemyLook();

            if (enemyVisual != null) yield return enemyVisual.FadeInRoutine(config.enemyFadeInTime);

            if (State != GameState.Playing) yield break;
            judge.Activate();
        }

        void HandleBattleEnd(bool playerWon)
        {
            State = playerWon ? GameState.Won : GameState.Lost;
            stateChangedAt = Time.unscaledTime;
            conductor.StopConducting();
            spawner.End();
            judge.Deactivate();
            audioDirector.StopMusic(playerWon);
            stats.seconds = Time.unscaledTime - battleStartedAt;

            if (playerWon)
            {
                stats.monstersDown++;
                StartCoroutine(WinOutroRoutine());
            }
            else hud.ShowResult(false, stats);
        }

        // ตัวสุดท้ายก็ต้องเฟดหายเหมือนกัน แล้วค่อยขึ้นหน้าผล
        IEnumerator WinOutroRoutine()
        {
            if (enemyVisual != null) yield return enemyVisual.FadeOutRoutine(config.enemyFadeOutTime);
            hud.ShowResult(true, stats);
        }

        void Retry()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void Quit()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
