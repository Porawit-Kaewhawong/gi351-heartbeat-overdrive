using System;
using UnityEngine;

namespace HBO
{
    public enum Judgement { Perfect, Great, Miss }

    /// <summary>
    /// รับอินพุตปุ่มเดียว (Space / คลิกซ้าย) แล้วตัดสินจากวง Pulse ที่เวลาใกล้ที่สุด
    /// - กดในหน้าต่าง Perfect/Great = โจมตี
    /// - กดนอกหน้าต่างทั้งหมด = Miss (กันการกดรัวมั่ว)
    /// - ปล่อยวงหลุดเกินเวลา = Miss อัตโนมัติ
    /// </summary>
    public class InputJudge : MonoBehaviour
    {
        public GameConfig config;
        public PulseSpawner spawner;

        public event Action<Judgement, PulseRing> OnJudged;

        bool active;
        double lastInputTime = -999;

        public void Activate()
        {
            active = true;
            // กินอินพุตเฟรมเดียวกับปุ่มเริ่มเกม จะได้ไม่โดนตัดสินเป็น Miss ทันที
            lastInputTime = Conductor.Now;
            DiscardUnreachableRings();
        }

        /// <summary>
        /// ทิ้งวงที่ผ่านไปแล้วหรือใกล้เกินจนกดไม่ทัน ณ วินาทีที่เพิ่งเปิดตัดสิน — ทิ้งเงียบๆ ไม่นับ Miss
        /// เพราะช่วงที่ปิดตัดสินอยู่ (สลับมอนสเตอร์) ผู้เล่นกดอะไรไม่ได้อยู่แล้ว จะลงโทษไม่ได้
        /// </summary>
        void DiscardUnreachableRings()
        {
            double cutoff = Conductor.Now + config.greatWindow;
            for (int i = spawner.Active.Count - 1; i >= 0; i--)
            {
                var r = spawner.Active[i];
                if (r == null) { spawner.Active.RemoveAt(i); continue; }
                if (r.HitTime < cutoff) spawner.Remove(r);
            }
        }

        public void Deactivate() { active = false; }

        void Update()
        {
            if (!active) return;
            AutoMissSweep();

            bool pressed = Input.GetKeyDown(KeyCode.Space);
            if (!pressed) return;

            double now = Conductor.Now;
            if (now - lastInputTime < config.inputCooldown) return;
            lastInputTime = now;

            PulseRing best = null;
            double bestAbs = double.MaxValue;
            foreach (var r in spawner.Active)
            {
                if (r == null || r.Consumed) continue;
                double d = Math.Abs(now - r.HitTime);
                if (d < bestAbs) { bestAbs = d; best = r; }
            }

            if (best == null) return; // ยังไม่มีวงในสนามเลย ไม่ลงโทษ

            if (bestAbs > config.greatWindow)
            {
                // กดผิดจังหวะชัดเจน = Miss (วงยังอยู่ ให้โอกาสกดวงเดิมใหม่)
                OnJudged?.Invoke(Judgement.Miss, null);
                return;
            }

            best.Consumed = true;
            Judgement j = bestAbs <= config.perfectWindow ? Judgement.Perfect : Judgement.Great;
            OnJudged?.Invoke(j, best);
            spawner.Remove(best);
        }

        void AutoMissSweep()
        {
            double now = Conductor.Now;
            for (int i = spawner.Active.Count - 1; i >= 0; i--)
            {
                var r = spawner.Active[i];
                if (r == null) { spawner.Active.RemoveAt(i); continue; }
                if (!r.Consumed && now - r.HitTime > config.lateMissGrace)
                {
                    r.Consumed = true;
                    OnJudged?.Invoke(Judgement.Miss, r);
                    spawner.Remove(r);
                }
            }
        }
    }
}
