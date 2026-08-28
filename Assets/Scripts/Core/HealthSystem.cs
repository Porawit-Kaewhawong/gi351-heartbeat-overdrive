using System;
using UnityEngine;

namespace HBO
{
    /// <summary>
    /// เก็บ HP สองฝั่ง + ยิงอีเวนต์เมื่อค่าเปลี่ยนหรือจบการดวล
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        public GameConfig config;

        public int PlayerHp { get; private set; }
        public int EnemyHp { get; private set; }
        public float PlayerFraction => config.playerMaxHp > 0 ? Mathf.Clamp01((float)PlayerHp / config.playerMaxHp) : 0f;
        public float EnemyFraction => config.enemyMaxHp > 0 ? Mathf.Clamp01((float)EnemyHp / config.enemyMaxHp) : 0f;

        public event Action OnChanged;
        /// <summary>true = ผู้เล่นชนะ</summary>
        public event Action<bool> OnBattleEnded;

        bool ended;

        public void ResetAll()
        {
            PlayerHp = config.playerMaxHp;
            EnemyHp = config.enemyMaxHp;
            ended = false;
            OnChanged?.Invoke();
        }

        public void DamageEnemy(int amount)
        {
            if (ended) return;
            EnemyHp = Mathf.Max(0, EnemyHp - amount);
            OnChanged?.Invoke();
            if (EnemyHp <= 0) End(true);
        }

        public void DamagePlayer(int amount)
        {
            if (ended) return;
            PlayerHp = Mathf.Max(0, PlayerHp - amount);
            OnChanged?.Invoke();
            if (PlayerHp <= 0) End(false);
        }

        void End(bool playerWon)
        {
            ended = true;
            OnBattleEnded?.Invoke(playerWon);
        }
    }
}
