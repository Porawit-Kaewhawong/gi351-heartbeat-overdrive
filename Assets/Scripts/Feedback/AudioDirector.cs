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
        [Tooltip("BPM จริงของไฟล์ musicLoop — ต้องใส่ให้ตรงกับไฟล์")]
        public float musicBpm = 90f;
        [Tooltip("ปรับความเร็วเพลงให้ตรงกับ baseBpm เผื่อหาเพลงที่ BPM ไม่ตรงมา\n" +
                 "pitch = baseBpm / musicBpm และคงที่ตลอดเกม (BPM เกมไม่ขยับแล้ว) จึงไม่มีวันดริฟต์\n" +
                 "ถ้าสองค่าต่างกันมาก เพลงจะเพี้ยนคีย์ — หาเพลง BPM ตรงมาตั้งแต่แรกดีกว่า")]
        public bool syncMusicPitch = false;
        [Tooltip("เคาะเสียงบีตเบาๆ ซ้อนบนเพลงด้วย — ตัวชี้จังหวะที่ตรงเป๊ะเสมอ")]
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
            musicSource.pitch = MusicPitch();
            // PlayScheduled อิง dspTime เดียวกับ Conductor เพลงจึงเริ่มตรงบีตแรกเป๊ะ
            // (Play() ธรรมดาจะเริ่มตอนต้นเฟรมถัดไป คลาดได้หลายสิบมิลลิวินาที)
            musicSource.PlayScheduled(conductor.FirstBeatTime);

            if (syncMusicPitch && Mathf.Abs(MusicPitch() - 1f) > 0.06f)
                Debug.Log(string.Format(
                    "[HBO] เพลงถูกปรับความเร็วเป็น {0:0.00} เท่า (musicBpm {1:0} → baseBpm {2:0}) " +
                    "ห่างเกินหนึ่งเสียงแล้ว คีย์เพลงจะเพี้ยน — หาเพลงที่ BPM ตรงกับ baseBpm มาใช้ดีกว่า",
                    MusicPitch(), musicBpm, config.baseBpm));
        }

        /// <summary>ความเร็วเพลงที่ทำให้ตรงกับ baseBpm — คงที่ตลอดเกมเพราะ BPM เกมไม่ขยับ</summary>
        float MusicPitch()
        {
            if (!syncMusicPitch || musicBpm <= 0f || config == null) return 1f;
            return config.baseBpm / musicBpm;
        }

        void Update()
        {
            // เผื่อจูน baseBpm สดๆ ตอน Play mode เพลงจะได้ตามไปด้วย
            if (musicOn && musicLoop != null && syncMusicPitch) musicSource.pitch = MusicPitch();
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
