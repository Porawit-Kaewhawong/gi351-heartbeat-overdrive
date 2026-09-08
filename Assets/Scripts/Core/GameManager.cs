using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HBO
{
    public enum GameState { Ready, Playing, Won, Lost }

    /// <summary>สรุปผลการดวลหนึ่งรอบ ใช้แสดงบนหน้า Result</summary>
    public struct BattleStats
    {
        public int perfect, great, miss, bestCombo, monstersDown, guarded;
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

        // ---------- Overdrive: กลไกที่ Perfect เท่านั้นเข้าถึงได้ ----------
        float overdriveMeter;      // 0-100
        int overdriveBeatsLeft;

        public bool InOverdrive => overdriveBeatsLeft > 0;
        /// <summary>ค่าที่เอาไปวาดหลอด: ตอนสะสมคือเกจ ตอนใช้งานคือเวลาที่เหลือ</summary>
        public float OverdriveFraction => InOverdrive
            ? (float)overdriveBeatsLeft / Mathf.Max(1, config.overdriveBeats)
            : Mathf.Clamp01(overdriveMeter / 100f);

        /// <summary>Overdrive คูณดาเมจ — เป็นเหตุผลหลักที่ Perfect ต่างจาก Great</summary>
        int Scaled(int damage)
        {
            if (!InOverdrive) return damage;
            return Mathf.Max(1, Mathf.RoundToInt(damage * config.overdriveDamageMultiplier));
        }

        void AddOverdrive(float amount)
        {
            if (InOverdrive || amount <= 0f) return;   // ระหว่างใช้งานอยู่ไม่สะสมเพิ่ม
            overdriveMeter = Mathf.Clamp(overdriveMeter + amount, 0f, 100f);
            if (overdriveMeter >= 100f) EnterOverdrive();
        }

        void EnterOverdrive()
        {
            overdriveBeatsLeft = Mathf.Max(1, config.overdriveBeats);
            overdriveMeter = 100f;
            feedback.OnOverdrive();
            hud.ShowOverdriveStart();
            HitBurst.Spawn(TargetPos(), new Color(1f, 0.4f, 0.35f, 0.95f), 1.2f, 6f, 0.5f);
        }

        void BreakOverdrive()
        {
            overdriveBeatsLeft = 0;
            overdriveMeter = 0f;
        }

        /// <summary>Overdrive นับถอยหลังเป็นบีต ไม่ใช่วินาที จะได้อยู่ในกริดจังหวะเดียวกับเกม</summary>
        void HandleBeat(int beatIndex)
        {
            if (overdriveBeatsLeft <= 0) return;
            overdriveBeatsLeft--;
            if (overdriveBeatsLeft <= 0) overdriveMeter = 0f;
        }

        Vector3 TargetPos()
        {
            return spawner != null && spawner.target != null ? spawner.target.position : Vector3.zero;
        }

        /// <summary>ตำแหน่งของโน้ตที่เพิ่งถูกตี — โน้ตแต่ละตัวอยู่คนละที่ เอฟเฟกต์ต้องไปโผล่ตรงนั้น</summary>
        Vector3 HitPos(PulseRing ring)
        {
            return ring != null ? ring.transform.position : TargetPos();
        }

        /// <summary>Perfect ระหว่าง Overdrive ฟื้นเลือดให้ — เป็นรางวัลอีกชั้นที่ Great ไม่มีทางได้</summary>
        void HealDuringOverdrive()
        {
            if (!InOverdrive || config.overdriveHealPerPerfect <= 0) return;

            int healed = health.HealPlayer(config.overdriveHealPerPerfect);
            if (healed <= 0) return; // เลือดเต็มอยู่แล้ว ไม่ต้องขึ้นเอฟเฟกต์ให้เก้อ

            Vector3 at = playerVisual != null ? playerVisual.transform.position : TargetPos();
            HitBurst.Spawn(at, new Color(0.45f, 1f, 0.6f, 0.9f), 0.6f, 2.6f, 0.42f);
            hud.ShowHeal(healed);
        }

        void Start()
        {
            health.ResetAll();
            ApplyEnemyLook();
            hud.ShowReady();
            judge.OnJudged += HandleJudgement;
            health.OnEnemyDefeated += HandleEnemyDefeated;
            health.OnBattleEnded += HandleBattleEnd;
            if (conductor != null) conductor.OnBeat += HandleBeat;
            stateChangedAt = Time.unscaledTime;

            // ฉากหลังสร้างจากโค้ด ไม่ต้องมีอยู่ในซีนก่อน (ทีมอาร์ตแทนด้วยภาพจริงได้ทีหลัง)
            Backdrop.Create(feedback != null ? feedback.targetCamera : Camera.main,
                conductor, spawner != null ? spawner.target : null);
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

            hud.SetOverdrive(OverdriveFraction, InOverdrive);

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
            overdriveMeter = 0f;
            overdriveBeatsLeft = 0;
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
                    health.DamageEnemy(Scaled(config.perfectDamage + bonus));
                    feedback.OnPerfect();
                    // เอฟเฟกต์ระเบิดวงเฉพาะ Perfect — Great ไม่มี ให้แยกออกจากกันด้วยตา
                    // ระเบิดที่ตำแหน่งของโน้ตที่เพิ่งตี ไม่ใช่กลางจอ เพราะแต่ละโน้ตอยู่คนละที่แล้ว
                    HitBurst.Spawn(HitPos(ring),
                        InOverdrive ? new Color(1f, 0.45f, 0.35f, 0.95f) : new Color(1f, 0.85f, 0.2f, 0.9f),
                        1.1f, InOverdrive ? 4.2f : 3.2f, 0.34f);
                    HealDuringOverdrive();
                    AddOverdrive(config.overdrivePerPerfect);
                    if (enemyVisual != null) enemyVisual.FlashHurt();
                    if (playerVisual != null) playerVisual.PlayAttack();
                    break;
                }
                case Judgement.Great:
                    perfectCombo = 0;
                    stats.great++;
                    health.DamageEnemy(Scaled(config.greatDamage));
                    feedback.OnGreat();
                    AddOverdrive(config.overdrivePerGreat);
                    if (enemyVisual != null) enemyVisual.FlashHurt();
                    if (playerVisual != null) playerVisual.PlayAttack();
                    break;
                default: // Miss: ผู้เล่นชะงัก + ศัตรูสวนกลับทันที
                    perfectCombo = 0;
                    stats.miss++;
                    if (InOverdrive && config.overdriveBlocksCounter)
                    {
                        // Overdrive กันการสวนกลับได้หนึ่งครั้ง แลกกับการหลุดสถานะทันที
                        stats.guarded++;
                        BreakOverdrive();
                        feedback.OnGreat();
                        hud.ShowGuard();
                        return;
                    }
                    overdriveMeter = Mathf.Max(0f, overdriveMeter - config.overdriveLossOnMiss);
                    health.DamagePlayer(config.enemyCounterDamage);
                    feedback.OnMiss();
                    if (playerVisual != null) playerVisual.FlashHurt();
                    if (enemyVisual != null) enemyVisual.PlayAttack();
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
