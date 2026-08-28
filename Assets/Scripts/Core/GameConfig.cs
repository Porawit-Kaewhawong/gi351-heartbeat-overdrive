using UnityEngine;

namespace HBO
{
    /// <summary>
    /// ค่าบาลานซ์ทั้งหมดของเกม รวมไว้ที่เดียว ปรับได้จาก Inspector บน GameSystems
    /// </summary>
    public class GameConfig : MonoBehaviour
    {
        [Header("Tempo (Climax Shift)")]
        [Tooltip("BPM เริ่มต้นของการดวล")]
        public float baseBpm = 90f;
        [Tooltip("BPM ที่บวกเพิ่มสูงสุดเมื่อ HP ศัตรูใกล้หมด (Climax Shift)")]
        public float maxBpmBonus = 60f;
        [Tooltip("ปล่อยวง Pulse ทุกๆ กี่บีต")]
        public int beatsPerPulse = 2;
        [Tooltip("วง Pulse ใช้เวลาวิ่งเข้าเป้ากี่บีต (ยิ่ง BPM สูง ยิ่งวิ่งเร็ว)")]
        public float approachBeats = 3f;

        [Header("Judgement Windows (วินาที)")]
        public float perfectWindow = 0.065f;
        public float greatWindow = 0.13f;
        [Tooltip("ปล่อยวงเลยเวลาไปเท่านี้ = Miss อัตโนมัติ")]
        public float lateMissGrace = 0.15f;
        [Tooltip("กันการกดรัว: เว้นช่วงขั้นต่ำระหว่างการกดสองครั้ง")]
        public float inputCooldown = 0.2f;

        [Header("Health / Damage")]
        public int enemyMaxHp = 120;
        public int playerMaxHp = 100;
        public int perfectDamage = 7;
        public int greatDamage = 3;
        [Tooltip("ทุกๆ N คอมโบ Perfect ติดกัน ได้ดาเมจโบนัส +1")]
        public int comboBonusEvery = 5;
        public int comboBonusCap = 3;
        [Tooltip("ดาเมจที่ศัตรูสวนกลับเมื่อผู้เล่น Miss")]
        public int enemyCounterDamage = 10;

        [Header("Redline Pressure")]
        [Tooltip("เริ่มเอฟเฟกต์ขอบแดง+เสียงหัวใจ เมื่อ HP ผู้เล่นต่ำกว่าสัดส่วนนี้")]
        [Range(0f, 1f)] public float redlineStartFraction = 0.5f;
        [Tooltip("อัตราหัวใจเต้น (ครั้ง/วินาที) ตอนเพิ่งเข้า Redline")]
        public float heartbeatMinRate = 1.0f;
        [Tooltip("อัตราหัวใจเต้นตอนใกล้ตาย")]
        public float heartbeatMaxRate = 2.4f;
        [Tooltip("ตอนวิกฤตสุด เพลงจะถูกกดให้เหลือดังเท่านี้ (0-1)")]
        [Range(0f, 1f)] public float musicDuckAtRedline = 0.35f;

        [Header("Feedback")]
        public float hitStopDuration = 0.05f;
        public float shakePerfect = 0.25f;
        public float shakeMiss = 0.15f;
    }
}
