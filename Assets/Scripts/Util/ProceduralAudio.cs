using UnityEngine;

namespace HBO
{
    /// <summary>
    /// สังเคราะห์เสียง placeholder ในโค้ด เพื่อให้เกม "มีเสียง" ตั้งแต่วันแรก
    /// ทีมเสียงเอาไฟล์จริงมาใส่ใน AudioDirector (Inspector) เพื่อแทนที่ได้เลย
    /// </summary>
    public static class ProceduralAudio
    {
        const int Rate = 44100;

        static AudioClip tick;
        public static AudioClip Tick
        {
            get { if (tick == null) tick = Chime(1200f, 1200f, 0.05f); return tick; }
        }

        /// <summary>เสียงกริ๊งสองโน้ต (ใช้กับ Perfect/Great/Win)</summary>
        public static AudioClip Chime(float f1, float f2, float duration = 0.18f)
        {
            int n = (int)(Rate * duration);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Exp(-t * 18f);
                float f = t < duration * 0.5f ? f1 : f2;
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.5f;
            }
            return Make("chime", data);
        }

        /// <summary>เสียงบัซต่ำ (ใช้กับ Miss/Lose)</summary>
        public static AudioClip Buzz(float freq, float duration = 0.25f)
        {
            int n = (int)(Rate * duration);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Exp(-t * 10f);
                float square = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t));
                data[i] = square * env * 0.25f;
            }
            return Make("buzz", data);
        }

        /// <summary>เสียง "ตุบ" ของหัวใจหนึ่งครั้ง (โทนต่ำ ไล่ลง)</summary>
        public static AudioClip Thump(float duration = 0.22f)
        {
            int n = (int)(Rate * duration);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Exp(-t * 22f);
                float freq = Mathf.Lerp(120f, 45f, t / duration);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.9f;
            }
            return Make("thump", data);
        }

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
