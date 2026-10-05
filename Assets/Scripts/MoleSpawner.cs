using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoleSpawner : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Mole[] moles;

    private readonly List<Mole> activeMoles = new List<Mole>();
    private Coroutine spawnRoutine;
    private GameManager.GameDifficulty difficulty;
    private float startVisibleTime, endVisibleTime, startGap, endGap;
    private int minWave = 1, maxWave = 1;
    private float doubleWaveChance, rewardChance;
    private int lastIndex = -1;
    private bool gridReady;

    public void SetDifficulty(GameManager.GameDifficulty value)
    {
        difficulty = value;
        EnsureNineGrid();
        ConfigureActiveSlots();
        switch (difficulty)
        {
            case GameManager.GameDifficulty.Easy:
                startVisibleTime = 1.25f; endVisibleTime = 0.78f;
                startGap = 0.62f; endGap = 0.34f;
                minWave = maxWave = 1; doubleWaveChance = rewardChance = 0f;
                break;
            case GameManager.GameDifficulty.Hard:
                startVisibleTime = 1.10f; endVisibleTime = 0.68f;
                startGap = 0.55f; endGap = 0.32f;
                minWave = 2; maxWave = 3; doubleWaveChance = 0.78f;
                rewardChance = 0.18f;
                break;
            default:
                startVisibleTime = 1.35f; endVisibleTime = 0.90f;
                startGap = 0.75f; endGap = 0.48f;
                minWave = 1; maxWave = 2; doubleWaveChance = 0.60f;
                rewardChance = 0.20f;
                break;
        }
    }

    public void BeginSpawning()
    {
        StopSpawning();
        EnsureNineGrid();
        ConfigureActiveSlots();
        foreach (Mole mole in moles)
            if (mole != null) mole.Configure(gameManager, this);
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = null;
        HideAll();
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(0.35f);
        while (gameManager.IsRoundActive)
        {
            activeMoles.RemoveAll(m => m == null || !m.IsVisible);
            int desired = minWave;
            if (maxWave > minWave && Random.value < doubleWaveChance)
                desired = Random.Range(minWave + 1, maxWave + 1);

            List<Mole> available = GetAvailableMoles();
            desired = Mathf.Min(desired, available.Count);
            MoleType[] waveTypes = BuildWaveTypes(desired);
            HashSet<int> usedColumns = new HashSet<int>();
            for (int i = 0; i < desired; i++)
            {
                int pick = ChooseAvailableIndex(available, usedColumns);
                Mole mole = available[pick];
                int globalIndex = System.Array.IndexOf(moles, mole);
                if (globalIndex >= 0) usedColumns.Add(globalIndex % 3);
                available.RemoveAt(pick);
                MoleType type = waveTypes[i];
                float visible = Mathf.Lerp(startVisibleTime, endVisibleTime, gameManager.Progress01);
                mole.Show(type, visible);
                activeMoles.Add(mole);
            }
            float gap = Mathf.Lerp(startGap, endGap, gameManager.Progress01);
            yield return new WaitForSeconds(Mathf.Lerp(startVisibleTime, endVisibleTime, gameManager.Progress01) + gap);
        }
        HideAll();
        spawnRoutine = null;
    }

    private MoleType[] BuildWaveTypes(int count)
    {
        MoleType[] types = new MoleType[count];
        for (int i = 0; i < count; i++) types[i] = MoleType.Normal;
        if (count == 0 || difficulty == GameManager.GameDifficulty.Easy) return types;

        if (difficulty == GameManager.GameDifficulty.Normal)
        {
            // 双目标波次更容易出现奖励鼠，让玩家真正进行目标选择。
            if (count > 1 && Random.value < 0.58f) types[Random.Range(0, count)] = MoleType.Reward;
            else if (Random.value < rewardChance) types[0] = MoleType.Reward;
            return types;
        }

        // 困难模式首只永远可以得分；炸弹只会与普通鼠或奖励鼠一同出现。
        types[0] = Random.value < 0.30f ? MoleType.Reward : MoleType.Normal;
        for (int i = 1; i < count; i++)
        {
            float roll = Random.value;
            types[i] = roll < 0.38f ? MoleType.Bomb : roll < 0.68f ? MoleType.Reward : MoleType.Normal;
        }
        return types;
    }

    private List<Mole> GetAvailableMoles()
    {
        List<Mole> result = new List<Mole>();
        foreach (Mole mole in moles)
            if (mole != null && mole.gameObject.activeInHierarchy && !mole.IsVisible) result.Add(mole);
        return result;
    }

    private int ChooseAvailableIndex(List<Mole> available, HashSet<int> usedColumns)
    {
        if (available.Count <= 1) return 0;
        List<int> validPicks = new List<int>();
        for (int i = 0; i < available.Count; i++)
        {
            int index = System.Array.IndexOf(moles, available[i]);
            if (index >= 0 && !usedColumns.Contains(index % 3)) validPicks.Add(i);
        }
        int pick = validPicks.Count > 0
            ? validPicks[Random.Range(0, validPicks.Count)]
            : Random.Range(0, available.Count);
        int globalIndex = System.Array.IndexOf(moles, available[pick]);
        if (globalIndex == lastIndex && validPicks.Count > 1)
        {
            int position = validPicks.IndexOf(pick);
            pick = validPicks[(position + 1) % validPicks.Count];
        }
        lastIndex = System.Array.IndexOf(moles, available[pick]);
        return pick;
    }

    public void NotifyMoleFinished(Mole mole, bool wasHit, MoleType type)
    {
        activeMoles.Remove(mole);
        if (!wasHit) gameManager.RegisterMoleEscaped(type);
    }

    private void EnsureNineGrid()
    {
        if (gridReady || moles == null || moles.Length == 0) return;
        List<Mole> list = new List<Mole>(moles);
        if (list.Count == 6)
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject clone = Instantiate(list[i].transform.parent.gameObject, list[i].transform.parent.parent);
                clone.name = $"HoleSlot_Middle_{i + 1}";
                list.Add(clone.GetComponentInChildren<Mole>(true));
            }
            moles = new[] { list[0], list[1], list[2], list[6], list[7], list[8], list[3], list[4], list[5] };
        }
        gridReady = true;
    }

    private void ConfigureActiveSlots()
    {
        if (moles == null) return;
        Vector2[] positions = difficulty == GameManager.GameDifficulty.Easy
            ? new[] {
                new Vector2(-5.1f, 1.25f), new Vector2(0f, 1.25f), new Vector2(5.1f, 1.25f),
                new Vector2(-5.1f, -0.45f), new Vector2(0f, -0.45f), new Vector2(5.1f, -0.45f),
                new Vector2(-5.1f, -2.15f), new Vector2(0f, -2.15f), new Vector2(5.1f, -2.15f)
            }
            : new[] {
                new Vector2(-5.1f, 1.85f), new Vector2(0f, 1.85f), new Vector2(5.1f, 1.85f),
                new Vector2(-5.1f, -0.75f), new Vector2(0f, -0.75f), new Vector2(5.1f, -0.75f),
                new Vector2(-5.1f, -3.35f), new Vector2(0f, -3.35f), new Vector2(5.1f, -3.35f)
            };
        for (int i = 0; i < moles.Length; i++)
        {
            if (i < positions.Length)
                moles[i].transform.parent.localPosition = positions[i];
            moles[i].transform.parent.localScale = difficulty == GameManager.GameDifficulty.Easy
                ? Vector3.one
                : Vector3.one * 0.88f;
            bool enabled = difficulty != GameManager.GameDifficulty.Easy || i < 3 || i >= 6;
            moles[i].transform.parent.gameObject.SetActive(enabled);
        }
    }

    private void HideAll()
    {
        activeMoles.Clear();
        if (moles == null) return;
        foreach (Mole mole in moles) if (mole != null) mole.HideImmediate();
    }
}
