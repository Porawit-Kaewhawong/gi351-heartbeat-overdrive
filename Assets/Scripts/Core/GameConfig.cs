using UnityEngine;

namespace HBO
{
    /// <summary>Climax Shift จะเร่งอะไรตอนใกล้จบแมตช์</summary>
    public enum ClimaxMode
    {
        /// <summary>เร่ง BPM — กดดันที่สุด แต่เพลงที่ BPM คงที่จะหลุดจังหวะ</summary>
        Tempo,
        /// <summary>ตรึง BPM ไว้ที่ baseBpm แล้วปล่อยวงถี่ขึ้นแทน — เพลงตรงจังหวะตลอด</summary>
        Density,
    }

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
        [Header("Tempo (Climax Shift)")]
        [Tooltip("Tempo = เร่ง BPM ตอนใกล้จบ (เพลงที่ BPM คงที่จะค่อยๆ หลุดจังหวะ)\n" +
                 "Density = ตรึง BPM ไว้แล้วปล่อยวงถี่ขึ้นแทน (เพลงตรงจังหวะตลอดแมตช์)")]
        public ClimaxMode climaxMode = ClimaxMode.Tempo;
        [Tooltip("BPM เริ่มต้นของการดวล")]
        public float baseBpm = 90f;
        [Tooltip("โหมด Tempo: BPM ที่บวกเพิ่มสูงสุดเมื่อขบวนศัตรูใกล้หมด")]
        public float maxBpmBonus = 60f;
        [Tooltip("ปล่อยวง Pulse ทุกๆ กี่บีต (ค่าตอนเริ่มเกม)")]
        public int beatsPerPulse = 2;
        [Tooltip("โหมด Density: ตอนใกล้จบปล่อยวงทุกกี่บีต (1 = ทุกบีต = ถี่สุด)")]
        public int minBeatsPerPulse = 1;
        [Tooltip("วง Pulse ใช้เวลาวิ่งเข้าเป้ากี่บีต (ยิ่ง BPM สูง ยิ่งวิ่งเร็ว)")]
        public float approachBeats = 3f;

        /// <summary>
        /// ปล่อยวง Pulse ทุกกี่บีต ณ ความคืบหน้าของขบวนนี้ (1 = ยังไม่โดนเลย, 0 = ล้มหมด)
        /// โหมด Density จะไล่จาก beatsPerPulse ลงไปหา minBeatsPerPulse — ค่าลดลงอย่างเดียว
        /// กริดใหม่จึงเป็น superset ของกริดเดิม วงที่ปล่อยออกมาไม่มีทางเลื่อนออกจากบีต
        /// </summary>
        public int BeatsPerPulseAt(float lineupFraction)
        {
            if (climaxMode != ClimaxMode.Density) return Mathf.Max(1, beatsPerPulse);
            int n = Mathf.RoundToInt(Mathf.Lerp(beatsPerPulse, minBeatsPerPulse, 1f - lineupFraction));
            return Mathf.Max(1, n);
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
