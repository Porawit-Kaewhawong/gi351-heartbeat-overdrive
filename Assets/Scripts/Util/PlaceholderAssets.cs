using UnityEngine;

namespace HBO
{
    /// <summary>
    /// สร้างสไปรต์ placeholder ตอนรัน (วงกลม / วงแหวน / ขอบจอแดง / ฉากหลัง)
    /// ทีมอาร์ตแทนที่ด้วยไฟล์จริงได้ทุกจุด: แค่ลากสไปรต์ใส่ช่องใน Inspector
    /// สคริปต์จะใช้ placeholder เฉพาะเมื่อช่องสไปรต์ว่างเท่านั้น
    /// </summary>
    public static class PlaceholderAssets
    {
        static Sprite pulseRing;
        /// <summary>วงแหวนของวง Pulse — สร้างครั้งเดียวแล้วใช้ซ้ำทุกวง (วงเกิดทุกบีต ห้ามสร้างใหม่ทุกครั้ง)</summary>
        public static Sprite SharedPulseRing
        {
            get { if (pulseRing == null) pulseRing = Ring(256, 0.09f, Color.white); return pulseRing; }
        }

        static Sprite glow;
        /// <summary>แสงฟุ้งกลมๆ ไล่จากกลางออกขอบ — ใช้เป็นแสงหลังจุดกดจังหวะและเอฟเฟกต์ Perfect</summary>
        public static Sprite SharedGlow
        {
            get { if (glow == null) glow = RadialGlow(256); return glow; }
        }

        public static Sprite Circle(int size, Color color)
        {
            var tex = NewTex(size);
            float r = size * 0.5f - 1f;
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                float a = Mathf.Clamp01(r - d + 1f); // ขอบนุ่ม 1px
                px[y * size + x] = new Color(color.r, color.g, color.b, color.a * a);
            }
            return Finish(tex, px, size);
        }

        public static Sprite Ring(int size, float thickness01, Color color)
        {
            var tex = NewTex(size);
            float rOut = size * 0.5f - 1f;
            float th = size * thickness01;
            float rMid = rOut - th * 0.5f;
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                float a = Mathf.Clamp01(th * 0.5f - Mathf.Abs(d - rMid) + 1f);
                px[y * size + x] = new Color(color.r, color.g, color.b, color.a * a);
            }
            return Finish(tex, px, size);
        }

        /// <summary>ภาพขอบจอ: กลางจอโปร่งใส ขอบทึบ (สีขาว ไว้ tint ด้วย Image.color)</summary>
        public static Sprite Vignette(int size)
        {
            var tex = NewTex(size);
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float maxD = size * 0.5f;
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / maxD; // 0 กลาง -> ~1.4 มุมจอ
                float a = Mathf.Clamp01((d - 0.55f) / 0.65f);
                a = a * a; // ไล่แบบนุ่ม
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            return Finish(tex, px, size);
        }

        /// <summary>ตรงข้ามกับ Vignette: กลางสว่างทึบ ไล่จางออกขอบ</summary>
        public static Sprite RadialGlow(int size)
        {
            var tex = NewTex(size);
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float maxD = size * 0.5f;
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), c) / maxD);
                float a = 1f - d;
                a = a * a; // ไล่แบบนุ่ม ไม่ให้เห็นขอบวง
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            return Finish(tex, px, size);
        }

        /// <summary>
        /// ฉากหลังไล่สีบนลงล่าง — ต้องเป็นสี่เหลี่ยมจัตุรัส เพราะ pixelsPerUnit มีค่าเดียว
        /// ถ้าทำเป็นแถบผอม (เช่น 2xN) สไปรต์จะกว้างแค่เศษ unit แล้วสเกลให้เต็มจอไม่ได้
        /// </summary>
        public static Sprite VerticalGradient(int size, Color top, Color bottom)
        {
            var tex = NewTex(size);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                Color c = Color.Lerp(bottom, top, size > 1 ? (float)y / (size - 1) : 0f);
                for (int x = 0; x < size; x++) px[y * size + x] = c;
            }
            return Finish(tex, px, size);
        }

        static Texture2D NewTex(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        static Sprite Finish(Texture2D tex, Color[] px, int size)
        {
            tex.SetPixels(px);
            tex.Apply();
            // pixelsPerUnit = size ทำให้สไปรต์กว้าง 1 world unit พอดี
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
