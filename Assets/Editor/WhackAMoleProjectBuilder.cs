using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WhackAMoleProjectBuilder
{
    private const string ScenePath = "Assets/Scenes/WhackAMole.unity";
    private const string MolePrefabPath = "Assets/Prefabs/Mole.prefab";
    private const string HolePrefabPath = "Assets/Prefabs/Hole.prefab";

    [MenuItem("Tools/Whack A Mole/Rebuild Complete Project")]
    public static void BuildAll()
    {
        ConfigureProject();
        ConfigureLayers();
        ConfigureTexture("Assets/Sprites/Background.png", false, 100f);
        ConfigureTexture("Assets/Sprites/Hole.png", true, 100f);
        ConfigureTexture("Assets/Sprites/Mole.png", true, 100f);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Background.png");
        Sprite holeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hole.png");
        Sprite moleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Mole.png");
        AudioClip music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BackgroundMusic.wav");
        AudioClip hit = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Hit.wav");

        if (backgroundSprite == null || holeSprite == null || moleSprite == null || music == null || hit == null)
            throw new FileNotFoundException("Required game assets could not be imported.");

        CreatePrefabs(holeSprite, moleSprite);
        CreateScene(backgroundSprite, music, hit);
        BuildWindowsPlayer();
        Debug.Log("WHACK_A_MOLE_BUILD_SUCCESS");
    }

    private static void ConfigureProject()
    {
        PlayerSettings.companyName = "ShandongUniversityStudentProject";
        PlayerSettings.productName = "Whack A Mole";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = false;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, "com.student.whackamole");
    }

    private static void ConfigureLayers()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        layers.GetArrayElementAtIndex(8).stringValue = "Mole";
        tagManager.ApplyModifiedProperties();
    }

    private static void ConfigureTexture(string path, bool alpha, float pixelsPerUnit)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.alphaIsTransparency = alpha;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    private static void CreatePrefabs(Sprite holeSprite, Sprite moleSprite)
    {
        GameObject hole = new GameObject("Hole");
        SpriteRenderer holeRenderer = hole.AddComponent<SpriteRenderer>();
        holeRenderer.sprite = holeSprite;
        holeRenderer.sortingOrder = 0;
        PrefabUtility.SaveAsPrefabAsset(hole, HolePrefabPath);
        Object.DestroyImmediate(hole);

        GameObject mole = new GameObject("Mole");
        mole.layer = 8;
        SpriteRenderer moleRenderer = mole.AddComponent<SpriteRenderer>();
        moleRenderer.sprite = moleSprite;
        moleRenderer.sortingOrder = 1;
        moleRenderer.enabled = false;
        BoxCollider2D collider = mole.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(moleSprite.bounds.size.x * 0.72f, moleSprite.bounds.size.y * 0.78f);
        collider.offset = new Vector2(0f, moleSprite.bounds.size.y * 0.03f);
        collider.enabled = false;
        mole.AddComponent<Mole>();
        PrefabUtility.SaveAsPrefabAsset(mole, MolePrefabPath);
        Object.DestroyImmediate(mole);
    }

    private static void CreateScene(Sprite backgroundSprite, AudioClip music, AudioClip hit)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera gameCamera = cameraObject.AddComponent<Camera>();
        gameCamera.orthographic = true;
        gameCamera.orthographicSize = 5.625f;
        gameCamera.clearFlags = CameraClearFlags.SolidColor;
        gameCamera.backgroundColor = new Color(0.3f, 0.72f, 0.95f);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.AddComponent<AudioListener>();

        GameObject background = new GameObject("Background");
        SpriteRenderer backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = backgroundSprite;
        backgroundRenderer.sortingOrder = -10;
        float backgroundScale = Mathf.Max(17.78f / backgroundSprite.bounds.size.x, 11.25f / backgroundSprite.bounds.size.y);
        background.transform.localScale = Vector3.one * backgroundScale;

        GameObject systems = new GameObject("Systems");
        GameManager gameManager = new GameObject("GameManager").AddComponent<GameManager>();
        gameManager.transform.SetParent(systems.transform);
        MoleSpawner spawner = new GameObject("MoleSpawner").AddComponent<MoleSpawner>();
        spawner.transform.SetParent(systems.transform);
        InputManager inputManager = new GameObject("InputManager").AddComponent<InputManager>();
        inputManager.transform.SetParent(systems.transform);
        AudioManager audioManager = new GameObject("AudioManager").AddComponent<AudioManager>();
        audioManager.transform.SetParent(systems.transform);
        AudioSource musicSource = audioManager.gameObject.AddComponent<AudioSource>();
        AudioSource effectsSource = audioManager.gameObject.AddComponent<AudioSource>();

        GameObject gameArea = new GameObject("GameArea");
        GameObject holePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HolePrefabPath);
        GameObject molePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MolePrefabPath);
        Vector2[] positions =
        {
            new Vector2(-5.1f, 1.95f), new Vector2(0f, 1.95f), new Vector2(5.1f, 1.95f),
            new Vector2(-5.1f, -0.90f), new Vector2(0f, -0.90f), new Vector2(5.1f, -0.90f),
            new Vector2(-5.1f, -3.75f), new Vector2(0f, -3.75f), new Vector2(5.1f, -3.75f)
        };
        Mole[] moles = new Mole[positions.Length];

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject slot = new GameObject($"HoleSlot_{i + 1:00}");
            slot.transform.SetParent(gameArea.transform);
            slot.transform.position = positions[i];

            GameObject hole = (GameObject)PrefabUtility.InstantiatePrefab(holePrefab, scene);
            hole.name = $"Hole_{i + 1:00}";
            hole.transform.SetParent(slot.transform);
            hole.transform.localPosition = Vector3.zero;
            hole.transform.localScale = Vector3.one * 0.205f;

            GameObject mole = (GameObject)PrefabUtility.InstantiatePrefab(molePrefab, scene);
            mole.name = $"Mole_{i + 1:00}";
            mole.transform.SetParent(slot.transform);
            mole.transform.localPosition = new Vector3(0f, 0.36f, 0f);
            mole.transform.localScale = Vector3.one * 0.155f;
            moles[i] = mole.GetComponent<Mole>();
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Canvas canvas = CreateCanvas();
        Text scoreText = CreateText(canvas.transform, "ScoreText", font, "得分：0", 42, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(36f, -30f), new Vector2(430f, 75f));
        Text timeText = CreateText(canvas.transform, "TimeText", font, "剩余：60 秒", 42, TextAnchor.MiddleRight,
            new Vector2(1f, 1f), new Vector2(-36f, -30f), new Vector2(430f, 75f));
        Text statusText = CreateText(canvas.transform, "StatusText", font, "准备好了吗？", 42, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(600f, 75f));
        AddOutline(scoreText); AddOutline(timeText); AddOutline(statusText);

        GameObject startPanel = CreatePanel(canvas.transform, "StartPanel", new Vector2(760f, 430f));
        CreateText(startPanel.transform, "Title", font, "欢乐打地鼠", 72, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 105f), new Vector2(700f, 90f));
        CreateText(startPanel.transform, "Instructions", font, "点击地鼠，每次获得 10 分\n在 60 秒内挑战更高分！", 32, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(680f, 110f));
        Button startButton = CreateButton(startPanel.transform, "StartButton", font, "开始游戏", new Vector2(0f, -115f));

        GameObject gameOverPanel = CreatePanel(canvas.transform, "GameOverPanel", new Vector2(700f, 400f));
        CreateText(gameOverPanel.transform, "GameOverTitle", font, "游戏结束", 64, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 105f), new Vector2(650f, 80f));
        Text finalScoreText = CreateText(gameOverPanel.transform, "FinalScoreText", font, "最终得分：0", 44, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 15f), new Vector2(650f, 80f));
        Button restartButton = CreateButton(gameOverPanel.transform, "RestartButton", font, "再玩一次", new Vector2(0f, -105f));
        gameOverPanel.SetActive(false);

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();

        SetObjectReference(gameManager, "spawner", spawner);
        SetObjectReference(gameManager, "audioManager", audioManager);
        SetObjectReference(gameManager, "scoreText", scoreText);
        SetObjectReference(gameManager, "timeText", timeText);
        SetObjectReference(gameManager, "statusText", statusText);
        SetObjectReference(gameManager, "finalScoreText", finalScoreText);
        SetObjectReference(gameManager, "startPanel", startPanel);
        SetObjectReference(gameManager, "gameOverPanel", gameOverPanel);

        SetObjectReference(spawner, "gameManager", gameManager);
        SetArrayReferences(spawner, "moles", moles);
        SetObjectReference(inputManager, "gameCamera", gameCamera);
        SerializedObject inputSerialized = new SerializedObject(inputManager);
        inputSerialized.FindProperty("moleLayer").intValue = 1 << 8;
        inputSerialized.ApplyModifiedProperties();

        SetObjectReference(audioManager, "musicSource", musicSource);
        SetObjectReference(audioManager, "effectsSource", effectsSource);
        SetObjectReference(audioManager, "backgroundMusic", music);
        SetObjectReference(audioManager, "hitSound", hit);

        UnityEventTools.AddPersistentListener(startButton.onClick, gameManager.StartGame);
        UnityEventTools.AddPersistentListener(restartButton.onClick, gameManager.RestartGame);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        ValidateScene(moles, scoreText, timeText, startButton, restartButton);
        AssetDatabase.SaveAssets();
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        canvasObject.AddComponent<UIThemeController>();
        return canvas;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.055f, 0.10f, 0.16f, 0.92f);
        return panel;
    }

    private static Text CreateText(Transform parent, string name, Font font, string value, int fontSize,
        TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text text = obj.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, Font font, string label, Vector2 position)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(340f, 92f);
        rect.anchoredPosition = position;
        Image image = obj.GetComponent<Image>();
        image.color = new Color(1f, 0.57f, 0.12f, 1f);
        Button button = obj.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.72f, 0.26f, 1f);
        colors.pressedColor = new Color(0.88f, 0.38f, 0.06f, 1f);
        button.colors = colors;
        Text text = CreateText(obj.transform, "Label", font, label, 34, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), Vector2.zero, rect.sizeDelta);
        text.color = new Color(0.08f, 0.08f, 0.08f);
        return button;
    }

    private static void AddOutline(Text text)
    {
        Outline outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArrayReferences(Object target, string propertyName, Mole[] values)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty array = serialized.FindProperty(propertyName);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ValidateScene(Mole[] moles, Text scoreText, Text timeText, Button startButton, Button restartButton)
    {
        if (moles.Length < 9) throw new System.InvalidOperationException("Nine mole slots are required.");
        if (scoreText == null || timeText == null || startButton == null || restartButton == null)
            throw new System.InvalidOperationException("Required UI references are missing.");
        Debug.Log($"Scene validation passed: {moles.Length} holes, HUD, start and restart controls are present.");
    }

    private static void BuildWindowsPlayer()
    {
        Directory.CreateDirectory("Build");
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Build/WhackAMole.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException($"Windows build failed: {report.summary.result}");
        Debug.Log($"Windows build succeeded: {report.summary.totalSize} bytes");
    }
}
