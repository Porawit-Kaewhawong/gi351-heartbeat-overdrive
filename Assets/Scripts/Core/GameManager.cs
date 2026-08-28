using UnityEngine;
using UnityEngine.SceneManagement;

namespace HBO
{
    public enum GameState { Ready, Playing, Won, Lost }

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

        void Start()
        {
            health.ResetAll();
            hud.ShowReady();
            judge.OnJudged += HandleJudgement;
            health.OnBattleEnded += HandleBattleEnd;
            stateChangedAt = Time.unscaledTime;
        }

        void Update()
        {
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
            perfectCombo = 0;
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
                    int bonus = Mathf.Min(perfectCombo / config.comboBonusEvery, config.comboBonusCap);
                    health.DamageEnemy(config.perfectDamage + bonus);
                    feedback.OnPerfect();
                    if (enemyVisual != null) enemyVisual.FlashHurt();
                    if (playerVisual != null) playerVisual.Lunge();
                    break;
                }
                case Judgement.Great:
                    perfectCombo = 0;
                    health.DamageEnemy(config.greatDamage);
                    feedback.OnGreat();
                    if (enemyVisual != null) enemyVisual.FlashHurt();
                    if (playerVisual != null) playerVisual.Lunge();
                    break;
                default: // Miss: ผู้เล่นชะงัก + ศัตรูสวนกลับทันที
                    perfectCombo = 0;
                    health.DamagePlayer(config.enemyCounterDamage);
                    feedback.OnMiss();
                    if (playerVisual != null) playerVisual.FlashHurt();
                    if (enemyVisual != null) enemyVisual.Lunge();
                    break;
            }
            hud.ShowJudgement(j, perfectCombo);
        }

        void HandleBattleEnd(bool playerWon)
        {
            State = playerWon ? GameState.Won : GameState.Lost;
            stateChangedAt = Time.unscaledTime;
            conductor.StopConducting();
            spawner.End();
            judge.Deactivate();
            audioDirector.StopMusic(playerWon);
            hud.ShowResult(playerWon);
        }

        void Retry()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
