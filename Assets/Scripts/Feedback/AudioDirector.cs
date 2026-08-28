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
            if (musicLoop != null)
            {
                musicSource.clip = musicLoop;
                musicSource.volume = 1f;
                musicSource.Play();
            }
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
            // ยังไม่มีเพลงจริง: เคาะติ๊กตามบีตให้พอจับจังหวะได้ (เน้นทุกบีตที่ 4)
            if (musicLoop == null)
                sfxSource.PlayOneShot(ProceduralAudio.Tick, beatIndex % 4 == 0 ? 0.8f : 0.45f);
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
