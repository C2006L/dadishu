using UnityEngine;
using UnityEngine.UI;

public enum MoleType { Normal, Reward, Bomb }

public class GameManager : MonoBehaviour
{
    public enum GameState { Waiting, Playing, GameOver }
    public enum GameDifficulty { Easy, Normal, Hard }

    [Header("Game Rules")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private int normalScore = 10;
    [SerializeField] private int rewardScore = 30;
    [SerializeField] private int bombPenalty = 20;

    [Header("Scene References")]
    [SerializeField] private MoleSpawner spawner;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text timeText;
    [SerializeField] private Text statusText;
    [SerializeField] private Text finalScoreText;
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject gameOverPanel;

    private int score, combo, bestCombo, normalHits, rewardHits, bombHits, clickAttempts, correctHits;
    private float bestMultiplier = 1f;
    private float timeLeft;
    private GameState state = GameState.Waiting;
    private GameDifficulty selectedDifficulty = GameDifficulty.Normal;
    private UIThemeController uiTheme;
    private GardenMenuController menu;
    private bool paused;

    public bool IsPlaying => state == GameState.Playing && !paused;
    public bool IsRoundActive => state == GameState.Playing;
    public bool IsPaused => paused;
    public int Score => score;
    public int Combo => combo;
    public int BestCombo => bestCombo;
    public float ComboMultiplier => combo >= 10 ? 2f : combo >= 5 ? 1.5f : 1f;
    public float Progress01 => gameDuration <= 0f ? 1f : 1f - Mathf.Clamp01(timeLeft / gameDuration);
    public string DifficultyName => GetDifficultyName(selectedDifficulty);

    private void Awake()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null && canvas.GetComponent<UIThemeController>() == null)
            canvas.gameObject.AddComponent<UIThemeController>();
        uiTheme = canvas != null ? canvas.GetComponent<UIThemeController>() : null;
    }

    private void Start()
    {
        menu = FindObjectOfType<GardenMenuController>();
        timeLeft = gameDuration;
        Time.timeScale = 1f;
        state = GameState.Waiting;
        startPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        statusText.text = "选择你的挑战";
        if (uiTheme != null) uiTheme.ShowGameplayHud(false);
        spawner.StopSpawning();
        if (audioManager != null) audioManager.PlayBackgroundMusic();
        UpdateHud();
    }

    private void Update()
    {
        if (state == GameState.Playing && Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        if (!IsPlaying) return;
        timeLeft = Mathf.Max(0f, timeLeft - Time.deltaTime);
        UpdateHud();
        if (timeLeft <= 0f) EndGame();
    }

    public void StartGame() => StartNormalMode();
    public void StartEasyMode() => StartGameWithDifficulty(GameDifficulty.Easy);
    public void StartNormalMode() => StartGameWithDifficulty(GameDifficulty.Normal);
    public void StartHardMode() => StartGameWithDifficulty(GameDifficulty.Hard);

    private void StartGameWithDifficulty(GameDifficulty difficulty)
    {
        ResumeGame(true);
        selectedDifficulty = difficulty;
        spawner.SetDifficulty(difficulty);
        score = combo = bestCombo = normalHits = rewardHits = bombHits = clickAttempts = correctHits = 0;
        bestMultiplier = 1f;
        timeLeft = gameDuration;
        state = GameState.Playing;
        startPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        statusText.text = difficulty == GameDifficulty.Easy ? "找准目标，一击命中！" :
            difficulty == GameDifficulty.Normal ? "优先击打金色奖励鼠！" : "小心炸弹鼠！";
        if (uiTheme != null) uiTheme.ShowGameplayHud(true);
        spawner.BeginSpawning();
        UpdateHud();
    }

    public void RestartGame() => StartGameWithDifficulty(selectedDifficulty);

    public void ReturnToMenu()
    {
        ResumeGame(true);
        state = GameState.Waiting;
        score = combo = 0;
        timeLeft = gameDuration;
        spawner.StopSpawning();
        gameOverPanel.SetActive(false);
        startPanel.SetActive(true);
        statusText.text = "选择你的挑战";
        if (uiTheme != null) uiTheme.ShowGameplayHud(false);
        UpdateHud();
    }

    public int RegisterMoleHit(MoleType type)
    {
        if (!IsPlaying) return 0;
        clickAttempts++;
        int delta;
        if (type == MoleType.Bomb)
        {
            bombHits++;
            combo = 0;
            delta = -Mathf.Min(score, bombPenalty);
            menu?.PulseComboBreak("误击炸弹鼠");
        }
        else
        {
            float previousMultiplier = ComboMultiplier;
            combo++;
            bestCombo = Mathf.Max(bestCombo, combo);
            correctHits++;
            if (type == MoleType.Reward) rewardHits++; else normalHits++;
            int baseScore = type == MoleType.Reward ? rewardScore : normalScore;
            delta = Mathf.RoundToInt(baseScore * ComboMultiplier);
            bestMultiplier = Mathf.Max(bestMultiplier, ComboMultiplier);
            if (ComboMultiplier > previousMultiplier)
                menu?.PulseComboMilestone(combo, ComboMultiplier);
        }
        score = Mathf.Max(0, score + delta);
        if (audioManager != null) audioManager.PlayMoleSound(type);
        UpdateHud();
        return delta;
    }

    public void RegisterMiss()
    {
        if (!IsPlaying) return;
        clickAttempts++;
        if (combo == 0) return;
        combo = 0;
        menu?.PulseComboBreak("点空了");
        UpdateHud();
    }

    public void RegisterWaveMissed()
    {
        if (!IsPlaying || combo == 0) return;
        combo = 0;
        menu?.PulseComboBreak("整批地鼠逃脱");
        UpdateHud();
    }

    public void PauseGame()
    {
        if (state != GameState.Playing || paused) return;
        paused = true;
        Time.timeScale = 0f;
        menu?.ShowPausePanel(true);
    }

    public void ResumeGame() => ResumeGame(true);
    private void ResumeGame(bool hidePanel)
    {
        paused = false;
        Time.timeScale = 1f;
        if (hidePanel) menu?.ShowPausePanel(false);
    }

    public void TogglePause() { if (paused) ResumeGame(); else PauseGame(); }

    private void EndGame()
    {
        if (state == GameState.GameOver) return;
        state = GameState.GameOver;
        paused = false;
        Time.timeScale = 1f;
        timeLeft = 0f;
        spawner.StopSpawning();
        statusText.text = "挑战结束";
        finalScoreText.text = $"{DifficultyName}  ·  {score} 分";
        float accuracy = clickAttempts <= 0 ? 0f : correctHits * 100f / clickAttempts;
        menu?.SetFinalStats($"最高连击  {bestCombo}     最高倍率  ×{bestMultiplier:0.#}\n命中率  {accuracy:0}%\n普通鼠 {normalHits}  ·  奖励鼠 {rewardHits}  ·  炸弹鼠 {bombHits}");
        if (uiTheme != null) uiTheme.ShowGameplayHud(false);
        gameOverPanel.SetActive(true);
        UpdateHud();
    }

    private void UpdateHud()
    {
        if (scoreText != null) scoreText.text = $"得分  {score}";
        if (timeText != null) timeText.text = $"剩余  {Mathf.CeilToInt(timeLeft)} 秒";
        menu?.UpdateCombo(combo, ComboMultiplier);
    }

    private static string GetDifficultyName(GameDifficulty difficulty)
    {
        switch (difficulty)
        {
            case GameDifficulty.Easy: return "简单模式";
            case GameDifficulty.Hard: return "困难模式";
            default: return "中等模式";
        }
    }
}
