using System;
using UnityEngine;

namespace HBO
{
    /// <summary>
    /// เก็บ HP ของผู้เล่นและของมอนสเตอร์ตัวที่กำลังสู้อยู่ + คุมลำดับขบวนมอนสเตอร์
    /// ตีตัวหนึ่งตาย → ยิง OnEnemyDefeated ให้ GameManager เล่นอนิเมชันเฟด แล้วค่อยเรียก
    /// AdvanceToNextEnemy() เพื่อเอาตัวถัดไปขึ้นมา ตายครบทั้งขบวนถึงจะชนะ
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        public GameConfig config;

        static readonly EnemyDef Fallback = new EnemyDef { name = "ENEMY", maxHp = 120 };

        public int PlayerHp { get; private set; }
        public int EnemyHp { get; private set; }
        /// <summary>ลำดับมอนสเตอร์ตัวที่กำลังสู้อยู่ (เริ่มที่ 0)</summary>
        public int EnemyIndex { get; private set; }
        public int EnemyCount => config != null && config.enemies != null && config.enemies.Length > 0
            ? config.enemies.Length : 1;
        public EnemyDef CurrentEnemy => EnemyAt(EnemyIndex);

        public float PlayerFraction => config.playerMaxHp > 0 ? Mathf.Clamp01((float)PlayerHp / config.playerMaxHp) : 0f;
        /// <summary>สัดส่วน HP ของมอนสเตอร์ตัวปัจจุบัน — ใช้กับหลอดเลือดบน HUD</summary>
        public float EnemyFraction
        {
            get { int max = CurrentEnemy.maxHp; return max > 0 ? Mathf.Clamp01((float)EnemyHp / max) : 0f; }
        }

        /// <summary>
        /// ความคืบหน้าของทั้งขบวน (1 = ยังไม่โดนเลย, 0 = ตายหมด)
        /// Climax Shift ใช้ค่านี้ จังหวะจะได้เร่งขึ้นเรื่อยๆ ตลอดแมตช์ แทนที่จะรีเซ็ตทุกครั้งที่เปลี่ยนตัว
        /// </summary>
        public float LineupFraction
        {
            get
            {
                int total = 0, remaining = EnemyHp;
                for (int i = 0; i < EnemyCount; i++)
                {
                    total += EnemyAt(i).maxHp;
                    if (i > EnemyIndex) remaining += EnemyAt(i).maxHp;
                }
                return total > 0 ? Mathf.Clamp01((float)remaining / total) : 0f;
            }
        }

        public event Action OnChanged;
        /// <summary>มอนสเตอร์ตัวนี้ตายแล้วแต่ยังเหลือตัวถัดไป (ส่ง index ของตัวที่เพิ่งตาย)</summary>
        public event Action<int> OnEnemyDefeated;
        /// <summary>true = ผู้เล่นชนะ</summary>
        public event Action<bool> OnBattleEnded;

        bool ended;
        bool awaitingNextEnemy;

        public EnemyDef EnemyAt(int index)
        {
            if (config == null || config.enemies == null || config.enemies.Length == 0) return Fallback;
            return config.enemies[Mathf.Clamp(index, 0, config.enemies.Length - 1)] ?? Fallback;
        }

        public void ResetAll()
        {
            PlayerHp = config.playerMaxHp;
            EnemyIndex = 0;
            EnemyHp = Mathf.Max(1, CurrentEnemy.maxHp);
            ended = false;
            awaitingNextEnemy = false;
            OnChanged?.Invoke();
        }

        public void DamageEnemy(int amount)
        {
            if (ended || awaitingNextEnemy) return;
            EnemyHp = Mathf.Max(0, EnemyHp - amount);
            OnChanged?.Invoke();
            if (EnemyHp > 0) return;

            if (EnemyIndex + 1 < EnemyCount)
            {
                awaitingNextEnemy = true;
                OnEnemyDefeated?.Invoke(EnemyIndex);
            }
            else End(true);
        }

        /// <summary>เรียกหลังอนิเมชันเฟดจบ เพื่อเอามอนสเตอร์ตัวถัดไปขึ้นสังเวียน</summary>
        public void AdvanceToNextEnemy()
        {
            if (!awaitingNextEnemy) return;
            awaitingNextEnemy = false;
            EnemyIndex++;
            EnemyHp = Mathf.Max(1, CurrentEnemy.maxHp);
            OnChanged?.Invoke();
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
