using UnityEngine;

namespace HBO
{
    /// <summary>
    /// มอนสเตอร์หนึ่งตัวในขบวน — ทีมอาร์ตใส่สไปรต์ต่อตัวได้จาก Inspector
    /// </summary>
    [System.Serializable]
    public class EnemyDef
    {
        public string name = "MONSTER";
        public int maxHp = 55;
        [Tooltip("ท่ายืน — เว้นว่าง = ใช้วงกลม placeholder (ถ้าใส่อาร์ตจริง ตั้งสีเป็นขาวไม่ให้ tint ทับ)")]
        public Sprite sprite;
        [Tooltip("ท่าโจมตีของตัวนี้ ใส่ได้หลายใบ เวลาสวนกลับจะสุ่มมาหนึ่งใบ แล้วคืนท่ายืนเมื่อจบ")]
        public Sprite[] attackSprites;
        public Color bodyColor = new Color(1f, 0.45f, 0.4f);
        [Tooltip("ขนาดตัวในซีน — ตัวท้ายขบวนควรใหญ่กว่าเพื่อให้ดูเป็นบอส")]
        public float scale = 1.8f;
    }

    /// <summary>
    /// ค่าบาลานซ์ทั้งหมดของเกม รวมไว้ที่เดียว ปรับได้จาก Inspector บน GameSystems
    /// </summary>
    public class GameConfig : MonoBehaviour
    {
        [Header("Climax Shift (วงถี่ขึ้นเมื่อขบวนศัตรูใกล้ล้ม)")]
        [Tooltip("BPM ของการดวล — คงที่ตลอดเกมโดยตั้งใจ เพลงจึงไม่มีวันหลุดจังหวะ\n" +
                 "ความกดดันมาจากความถี่ของวง ไม่ใช่ความเร็วของบีต")]
        public float baseBpm = 90f;
        [Tooltip("ตัวตั้ง: ตอนเริ่มเกมปล่อยวง Pulse ทุกๆ กี่บีต")]
        public float beatsPerPulse = 2f;
        [Tooltip("เลือดศัตรูลดไปทุกๆ กี่ส่วน ถึงจะเร่งหนึ่งขั้น (0.25 = ทุก 25%)")]
        [Range(0.05f, 1f)] public float climaxStepFraction = 0.25f;
        [Tooltip("เร่งหนึ่งขั้น = ลด beatsPerPulse ลงเท่าไหร่")]
        public float beatsPerPulseStep = 0.25f;
        [Tooltip("พื้น: beatsPerPulse ลงได้ต่ำสุดเท่านี้ (1 = ปล่อยวงทุกบีต)")]
        public float minBeatsPerPulse = 1f;
        [Tooltip("วง Pulse ใช้เวลาวิ่งเข้าเป้ากี่บีต (มากขึ้น = อ่านง่ายขึ้น)")]
        public float approachBeats = 3f;

        /// <summary>
        /// ปล่อยวง Pulse ทุกกี่บีต ณ ความคืบหน้าของขบวนนี้ (lineupFraction: 1 = ยังไม่โดนเลย, 0 = ล้มหมด)
        ///
        /// ไล่เป็นขั้นบันได: เลือดลดครบทุก climaxStepFraction ก็ลด beatsPerPulse ลง beatsPerPulseStep
        /// ค่าเริ่มต้น = ทุก 25% ลด 0.25 → 2.00 / 1.75 / 1.50 / 1.25 (ขั้นสุดท้ายคือตอนเลือดหมดพอดี)
        /// BPM ไม่ขยับเลยตลอดกระบวนการนี้ เพลงจึงล็อกกับเกมได้ 100%
        /// </summary>
        public float BeatsPerPulseAt(float lineupFraction)
        {
            float lost = 1f - Mathf.Clamp01(lineupFraction);
            int steps = climaxStepFraction > 0f ? Mathf.FloorToInt(lost / climaxStepFraction) : 0;
            return Mathf.Max(minBeatsPerPulse, beatsPerPulse - steps * beatsPerPulseStep);
        }

        [Header("Judgement Windows (วินาที)")]
        public float perfectWindow = 0.065f;
        public float greatWindow = 0.13f;
        [Tooltip("ปล่อยวงเลยเวลาไปเท่านี้ = Miss อัตโนมัติ")]
        public float lateMissGrace = 0.15f;
        [Tooltip("กันการกดรัว: เว้นช่วงขั้นต่ำระหว่างการกดสองครั้ง")]
        public float inputCooldown = 0.2f;

        [Header("Enemy Lineup (ตีตัวหนึ่งตาย ตัวถัดไปโผล่)")]
        [Tooltip("ขบวนมอนสเตอร์เรียงตามลำดับที่ออกมา — HP รวมทั้งขบวนคือความยาวของหนึ่งแมตช์")]
        public EnemyDef[] enemies =
        {
            new EnemyDef { name = "STALKER", maxHp = 40, bodyColor = new Color(1f, 0.62f, 0.35f), scale = 1.5f },
            new EnemyDef { name = "BRUTE",   maxHp = 55, bodyColor = new Color(0.85f, 0.45f, 1f), scale = 1.75f },
            new EnemyDef { name = "WARDEN",  maxHp = 75, bodyColor = new Color(1f, 0.32f, 0.34f), scale = 2.05f },
        };
        [Tooltip("เวลาที่มอนสเตอร์ค่อยๆ จางหายตอนถูกตีตาย (วินาที)")]
        public float enemyFadeOutTime = 0.55f;
        [Tooltip("เวลาที่ตัวถัดไปค่อยๆ ปรากฏ (วินาที)")]
        public float enemyFadeInTime = 0.4f;

        [Header("Health / Damage")]
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
