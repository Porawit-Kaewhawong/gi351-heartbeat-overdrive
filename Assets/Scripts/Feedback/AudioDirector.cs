using UnityEngine;

namespace HBO
{
    /// <summary>
    /// คุมเสียงทั้งหมดของเกม — เริ่มต้นใช้เสียงสังเคราะห์ placeholder
    /// ทีมเสียง: ลาก AudioClip จริงใส่ช่องใน Inspector เพื่อแทนที่ได้ทันที
    /// หมายเหตุ: ถ้าใส่เพลงจริง ควรทำเพลงที่ BPM เดียวกับ baseBpm (ค่าเริ่มต้น 90)
    /// </summary>
    public class AudioDirector : MonoBehaviour
    {
        public GameConfig config;
        public Conductor conductor;

        [Header("ใส่ไฟล์เสียงจริงเพื่อแทน placeholder (เว้นว่าง = ใช้เสียงสังเคราะห์)")]
        public AudioClip musicLoop;
        public AudioClip perfectSfx;
        public AudioClip greatSfx;
        public AudioClip missSfx;
        public AudioClip heartbeatSfx;
        public AudioClip winSfx;
        public AudioClip loseSfx;

        [Header("ซิงก์เพลงกับจังหวะเกม")]
        [Tooltip("BPM จริงของไฟล์ musicLoop — ต้องใส่ให้ตรง ไม่งั้นซิงก์ไม่ได้ (ปกติควรเท่ากับ baseBpm)")]
        public float musicBpm = 90f;
        [Tooltip("เร่ง/ลดความเร็วเพลงตาม BPM ของเกม (Climax Shift) เพื่อให้ล็อกจังหวะกันตลอด — แลกกับเสียงเพลงที่คีย์สูงขึ้น")]
        public bool syncMusicPitch = false;
        [Tooltip("เพดาน pitch กันเพลงเสียงแหลมจนเพี้ยน (1.15 ≈ สูงขึ้นราว 2 เสียง)")]
        [Range(1f, 2f)] public float maxMusicPitch = 1.15f;
        [Tooltip("เคาะเสียงบีตเบาๆ ซ้อนบนเพลงด้วย — ตัวชี้จังหวะที่ตรงเป๊ะเสมอแม้เพลงจะดริฟต์")]
        public bool beatTickWithMusic = true;

        AudioSource musicSource;
        AudioSource sfxSource;
        AudioSource heartSource;

        double nextHeartTime;
        bool musicOn;

        void Awake()
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            sfxSource = gameObject.AddComponent<AudioSource>();
            heartSource = gameObject.AddComponent<AudioSource>();

            if (perfectSfx == null) perfectSfx = ProceduralAudio.Chime(880f, 1320f);
            if (greatSfx == null) greatSfx = ProceduralAudio.Chime(660f, 880f);
            if (missSfx == null) missSfx = ProceduralAudio.Buzz(140f);
            if (heartbeatSfx == null) heartbeatSfx = ProceduralAudio.Thump();
            if (winSfx == null) winSfx = ProceduralAudio.Chime(660f, 990f, 0.5f);
            if (loseSfx == null) loseSfx = ProceduralAudio.Buzz(90f, 0.6f);
        }

        void OnEnable() { if (conductor != null) conductor.OnBeat += HandleBeat; }
        void OnDisable() { if (conductor != null) conductor.OnBeat -= HandleBeat; }

        public void StartMusic()
        {
            musicOn = true;
            if (musicLoop == null) return;

            musicSource.clip = musicLoop;
            musicSource.volume = 1f;
            musicSource.pitch = 1f;
            // PlayScheduled อิง dspTime เดียวกับ Conductor เพลงจึงเริ่มตรงบีตแรกเป๊ะ
            // (Play() ธรรมดาจะเริ่มตอนต้นเฟรมถัดไป คลาดได้หลายสิบมิลลิวินาที)
            musicSource.PlayScheduled(conductor.FirstBeatTime);
        }

        void Update()
        {
            if (!musicOn || musicLoop == null || !syncMusicPitch) return;
            if (conductor == null || musicBpm <= 0f || conductor.CurrentBpm <= 0f) return;
            musicSource.pitch = Mathf.Min(conductor.CurrentBpm / musicBpm, maxMusicPitch);
        }

        public void StopMusic(bool playerWon)
        {
            musicOn = false;
            musicSource.Stop();
            sfxSource.PlayOneShot(playerWon ? winSfx : loseSfx, 0.9f);
        }

        void HandleBeat(int beatIndex)
        {
            if (!musicOn) return;
            // เสียงเคาะบีตคือตัวชี้จังหวะที่ตรง 100% เสมอ ต่างจากเพลงที่อาจดริฟต์
            // ไม่มีเพลง = เคาะดัง (เน้นบีตที่ 4), มีเพลงแล้ว = เคาะเบาๆ ซ้อนไว้เป็นไกด์
            if (musicLoop == null)
                sfxSource.PlayOneShot(ProceduralAudio.Tick, beatIndex % 4 == 0 ? 0.8f : 0.45f);
            else if (beatTickWithMusic)
                sfxSource.PlayOneShot(ProceduralAudio.Tick, beatIndex % 4 == 0 ? 0.3f : 0.16f);
        }

        public void PlayPerfect() { sfxSource.PlayOneShot(perfectSfx, 1f); }
        public void PlayGreat() { sfxSource.PlayOneShot(greatSfx, 0.8f); }
        public void PlayMiss() { sfxSource.PlayOneShot(missSfx, 1f); }

        /// <summary>Redline: ยิ่ง severity สูง หัวใจยิ่งดัง/ถี่ เพลงยิ่งถูกกดเบา</summary>
        public void SetHeartbeat(float severity, float rate)
        {
            musicSource.volume = Mathf.Lerp(1f, config.musicDuckAtRedline, severity);

            if (severity > 0f && AudioSettings.dspTime >= nextHeartTime)
            {
                heartSource.PlayOneShot(heartbeatSfx, Mathf.Lerp(0.3f, 1f, severity));
                nextHeartTime = AudioSettings.dspTime + 1.0 / Mathf.Max(0.1f, rate);
            }
        }
    }
}
