using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using HBO;

/// <summary>
/// สร้างซีนหลักของเกมอัตโนมัติ: เมนู Tools > Heartbeat Overdrive > Setup Main Scene
/// ได้ซีน greybox ที่กด Play เล่นได้ทันที พร้อมต่อสายทุกระบบให้ครบ
/// รันซ้ำได้ (จะสร้างซีนใหม่ทับ Assets/Scenes/Main.unity)
/// </summary>
public static class SceneSetup
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Tools/Heartbeat Overdrive/Setup Main Scene")]
    public static void CreateMainScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ---------- กล้อง ----------
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.06f, 0.10f);
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        // ---------- ระบบเกม ----------
        var sys = new GameObject("GameSystems");
        var config = sys.AddComponent<GameConfig>();
        var health = sys.AddComponent<HealthSystem>();
        var conductor = sys.AddComponent<Conductor>();
        var spawner = sys.AddComponent<PulseSpawner>();
        var judge = sys.AddComponent<InputJudge>();
        var audio = sys.AddComponent<AudioDirector>();
        var feedback = sys.AddComponent<FeedbackDirector>();
        var redline = sys.AddComponent<RedlineEffect>();
        var gm = sys.AddComponent<GameManager>();

        // ---------- ตัวละคร + วงเป้า ----------
        var playerGo = new GameObject("Player");
        playerGo.transform.position = new Vector3(-3.2f, -1.2f, 0f);
        playerGo.transform.localScale = Vector3.one * 1.5f;
        var playerVis = playerGo.AddComponent<CharacterVisual>();
        playerVis.bodyColor = new Color(0.35f, 0.85f, 1f);
        playerVis.lungeDirection = 1f;

        var enemyGo = new GameObject("Enemy");
        enemyGo.transform.position = new Vector3(3.2f, -1.2f, 0f);
        enemyGo.transform.localScale = Vector3.one * 1.8f;
        var enemyVis = enemyGo.AddComponent<CharacterVisual>();
        enemyVis.bodyColor = new Color(1f, 0.45f, 0.4f);
        enemyVis.lungeDirection = -1f;

        var targetGo = new GameObject("TimingTarget");
        targetGo.transform.position = new Vector3(0f, 1.4f, 0f);
        targetGo.transform.localScale = Vector3.one * 1.3f;
        var targetSr = targetGo.AddComponent<SpriteRenderer>();
        targetSr.sortingOrder = 5;
        targetGo.AddComponent<TargetRing>();

        // ---------- Canvas / HUD ----------
        var canvasGo = new GameObject("Canvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // ขอบจอแดง (Redline) — อยู่ล่างสุดของ HUD
        var vignetteRt = NewUI("RedVignette", canvasGo.transform);
        Stretch(vignetteRt);
        var vignetteImg = vignetteRt.gameObject.AddComponent<Image>();
        vignetteImg.color = new Color(0.9f, 0.05f, 0.05f, 0f);
        vignetteImg.raycastTarget = false;

        // หลอดเลือด
        var playerFill = MakeHpBar(canvasGo.transform, font, "PlayerHpBar", "YOU",
            anchoredLeft: true, fillColor: new Color(0.3f, 0.9f, 1f));
        var enemyFill = MakeHpBar(canvasGo.transform, font, "EnemyHpBar", "ENEMY",
            anchoredLeft: false, fillColor: new Color(1f, 0.4f, 0.35f));

        // ตัวหนังสือกลางจอ
        var bpmText = NewText("BpmText", canvasGo.transform, "", 34, font,
            new Color(1f, 1f, 1f, 0.75f));
        Anchor(bpmText.rectTransform, 0.5f, 1f, new Vector2(0f, -140f), new Vector2(400f, 50f));

        var judgementText = NewText("JudgementText", canvasGo.transform, "", 84, font, Color.white);
        judgementText.fontStyle = FontStyle.Bold;
        Anchor(judgementText.rectTransform, 0.5f, 0.5f, new Vector2(0f, 60f), new Vector2(800f, 110f));

        var comboText = NewText("ComboText", canvasGo.transform, "", 44, font,
            new Color(1f, 0.85f, 0.2f));
        Anchor(comboText.rectTransform, 0.5f, 0.5f, new Vector2(0f, -30f), new Vector2(600f, 60f));

        // แผงหน้าจอเริ่มเกม
        var readyPanel = MakePanel(canvasGo.transform, "ReadyPanel");
        var title = NewText("Title", readyPanel.transform, "HEARTBEAT OVERDRIVE", 92, font,
            new Color(1f, 0.3f, 0.35f));
        title.fontStyle = FontStyle.Bold;
        Anchor(title.rectTransform, 0.5f, 0.5f, new Vector2(0f, 120f), new Vector2(1400f, 120f));
        var hint = NewText("Hint", readyPanel.transform,
            "ONE BUTTON. PERFECT TIMING.\n\nPRESS SPACE TO START", 40, font, Color.white);
        Anchor(hint.rectTransform, 0.5f, 0.5f, new Vector2(0f, -100f), new Vector2(1200f, 220f));

        // แผงหน้าจอจบเกม
        var resultPanel = MakePanel(canvasGo.transform, "ResultPanel");
        var resultText = NewText("ResultText", resultPanel.transform, "", 76, font, Color.white);
        resultText.fontStyle = FontStyle.Bold;
        Anchor(resultText.rectTransform, 0.5f, 0.5f, Vector2.zero, new Vector2(1400f, 400f));
        resultPanel.SetActive(false);

        // HUD controller
        var hud = canvasGo.AddComponent<HUDController>();
        hud.health = health;
        hud.conductor = conductor;
        hud.playerHpFill = playerFill;
        hud.enemyHpFill = enemyFill;
        hud.comboText = comboText;
        hud.judgementText = judgementText;
        hud.bpmText = bpmText;
        hud.readyPanel = readyPanel;
        hud.resultPanel = resultPanel;
        hud.resultText = resultText;

        // ---------- ต่อสายระบบทั้งหมด ----------
        health.config = config;

        conductor.config = config;
        conductor.health = health;

        spawner.config = config;
        spawner.conductor = conductor;
        spawner.target = targetGo.transform;

        judge.config = config;
        judge.spawner = spawner;

        audio.config = config;
        audio.conductor = conductor;

        feedback.config = config;
        feedback.targetCamera = cam;
        feedback.audioDirector = audio;

        redline.config = config;
        redline.health = health;
        redline.audioDirector = audio;
        redline.vignette = vignetteImg;

        gm.config = config;
        gm.conductor = conductor;
        gm.spawner = spawner;
        gm.judge = judge;
        gm.health = health;
        gm.hud = hud;
        gm.audioDirector = audio;
        gm.feedback = feedback;
        gm.playerVisual = playerVis;
        gm.enemyVisual = enemyVis;

        // ---------- เซฟซีน + ใส่ Build Settings ----------
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        Debug.Log("[Heartbeat Overdrive] Setup เสร็จแล้ว! กด Play ได้เลย (ซีน: " + ScenePath + ")");
    }

    // ================= UI helpers =================

    static RectTransform NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Anchor(RectTransform rt, float ax, float ay, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(ax, ay);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static Text NewText(string name, Transform parent, string content, int size, Font font, Color color)
    {
        var rt = NewUI(name, parent);
        var text = rt.gameObject.AddComponent<Text>();
        text.text = content;
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    static GameObject MakePanel(Transform parent, string name)
    {
        var rt = NewUI(name, parent);
        Stretch(rt);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.82f);
        img.raycastTarget = false;
        return rt.gameObject;
    }

    static Image MakeHpBar(Transform parent, Font font, string name, string label,
        bool anchoredLeft, Color fillColor)
    {
        // กรอบนอก
        var bar = NewUI(name, parent);
        float x = anchoredLeft ? 0f : 1f;
        bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(x, 1f);
        bar.anchoredPosition = new Vector2(anchoredLeft ? 40f : -40f, -50f);
        bar.sizeDelta = new Vector2(640f, 44f);
        var bg = bar.gameObject.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
        bg.raycastTarget = false;

        // แถบสี (HUDController ขยับ anchor ของอันนี้ตาม HP)
        var fillRt = NewUI("Fill", bar);
        Stretch(fillRt);
        var fill = fillRt.gameObject.AddComponent<Image>();
        fill.color = fillColor;
        fill.raycastTarget = false;

        // ป้ายชื่อ
        var text = NewText("Label", bar, label, 26, font, Color.white);
        var trt = text.rectTransform;
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 0f);
        trt.anchoredPosition = new Vector2(0f, 4f);
        trt.sizeDelta = new Vector2(0f, 30f);
        text.alignment = anchoredLeft ? TextAnchor.LowerLeft : TextAnchor.LowerRight;

        return fill;
    }
}
