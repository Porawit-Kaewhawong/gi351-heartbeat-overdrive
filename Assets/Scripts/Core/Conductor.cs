using System;
using UnityEngine;

namespace HBO
{
    /// <summary>
    /// นาฬิกาจังหวะของเกม อิง AudioSettings.dspTime เพื่อความเที่ยงตรงระดับเสียง
    /// (อย่าใช้ Time.time ตัดสินจังหวะ เพราะโดน framerate / timeScale รบกวน)
    /// BPM ถูกเร่งตาม HP ศัตรูที่ลดลง = Climax Shift
    /// </summary>
    public class Conductor : MonoBehaviour
    {
        public GameConfig config;
        public HealthSystem health;

        public float CurrentBpm { get; private set; }
        public double BeatInterval => 60.0 / CurrentBpm;

        /// <summary>เวลากลางของระบบจังหวะทั้งเกม</summary>
        public static double Now => AudioSettings.dspTime;

        /// <summary>เวลา dspTime ของบีตแรก — AudioDirector ใช้ตั้งเวลาเริ่มเพลงให้ตรงบีตเป๊ะ</summary>
        public double FirstBeatTime { get; private set; }

        /// <summary>
        /// เวลา dspTime ที่ "แท้จริง" ของบีตที่กำลังยิง OnBeat อยู่
        /// ใช้ค่านี้ตั้งเวลาแทน Now เสมอ เพราะ OnBeat ถูกยิงตอนต้นเฟรมถัดจากบีตจริง
        /// จึงช้ากว่าบีตจริงได้ถึงหนึ่งเฟรม (16-33 ms = ครึ่งหนึ่งของหน้าต่าง Perfect)
        /// </summary>
        public double CurrentBeatTime { get; private set; }

        /// <summary>ยิงทุกบีต พร้อม index ของบีต</summary>
        public event Action<int> OnBeat;

        double nextBeatTime;
        int beatIndex;
        bool running;

        public void StartConducting()
        {
            CurrentBpm = config.baseBpm;
            beatIndex = 0;
            nextBeatTime = Now + 1.0; // หน่วงหนึ่งวินาทีก่อนบีตแรก ให้ผู้เล่นตั้งตัว
            FirstBeatTime = nextBeatTime;
            running = true;
        }

        public void StopConducting() { running = false; }

        void Update()
        {
            if (!running) return;

            // Climax Shift: อิงความคืบหน้าของทั้งขบวน จังหวะจะได้เร่งขึ้นต่อเนื่องตลอดแมตช์
            // ไม่ใช่ตกกลับลงมาทุกครั้งที่มอนสเตอร์ตัวใหม่โผล่
            // โหมด Density ตรึง BPM ไว้ ให้ PulseSpawner ไปเร่งความถี่ของวงแทน เพลงจะได้ไม่หลุด
            float progress = health != null ? health.LineupFraction : 1f;
            CurrentBpm = config.climaxMode == ClimaxMode.Density
                ? config.baseBpm
                : config.baseBpm + (1f - progress) * config.maxBpmBonus;

            while (Now >= nextBeatTime)
            {
                CurrentBeatTime = nextBeatTime;
                OnBeat?.Invoke(beatIndex);
                beatIndex++;
                nextBeatTime += BeatInterval;
            }
        }
    }
}
