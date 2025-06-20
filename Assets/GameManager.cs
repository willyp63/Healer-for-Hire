using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [SerializeField]
    private float delayBetweenWaves = 1f;

    private int currentWaveIndex = 0;
    private bool isWaveSpawning = false;

    private void Start()
    {
        // Start spawning waves
        StartCoroutine(WaveSpawningLoop());
    }

    public void PauseGame()
    {
        Time.timeScale = 0;
    }

    public void UnpauseGame()
    {
        Time.timeScale = 1;
    }

    private IEnumerator WaveSpawningLoop()
    {
        yield return new WaitForSeconds(0.2f);

        while (currentWaveIndex < EnemyWaveManager.Instance.Waves.Count)
        {
            // Spawn the current wave
            yield return StartCoroutine(SpawnCurrentWave());

            // Wait for all enemies to die
            yield return StartCoroutine(WaitForEnemiesToDie());

            // Pause before next wave
            yield return new WaitForSeconds(delayBetweenWaves);

            // Move to next wave
            currentWaveIndex++;
        }

        // All waves completed
        Debug.Log("All waves completed!");
    }

    private IEnumerator SpawnCurrentWave()
    {
        if (currentWaveIndex >= EnemyWaveManager.Instance.Waves.Count)
            yield break;

        var wave = EnemyWaveManager.Instance.Waves[currentWaveIndex];
        Debug.Log($"Spawning wave {currentWaveIndex + 1} with {wave.characters.Count} enemies");

        isWaveSpawning = true;
        CharacterManager.Instance.SpawnEnemyWave(wave.characters);

        // Wait for the wave spawning to complete
        while (CharacterManager.Instance.IsWaveInProgress)
        {
            yield return null;
        }

        isWaveSpawning = false;
    }

    private IEnumerator WaitForEnemiesToDie()
    {
        Debug.Log("Waiting for all enemies to die...");

        while (true)
        {
            var activeEnemies = CharacterManager.Instance.GetActiveEnemyCharacters();
            bool allEnemiesDead = true;

            foreach (var enemy in activeEnemies)
            {
                if (enemy != null && !enemy.IsDead)
                {
                    allEnemiesDead = false;
                    break;
                }
            }

            if (allEnemiesDead)
            {
                Debug.Log("All enemies defeated!");
                break;
            }

            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
        }
    }

    public void RestartWaves()
    {
        currentWaveIndex = 0;
        StopAllCoroutines();
        StartCoroutine(WaveSpawningLoop());
    }

    public int GetCurrentWaveIndex()
    {
        return currentWaveIndex;
    }

    public int GetTotalWaves()
    {
        return EnemyWaveManager.Instance.Waves.Count;
    }

    public bool IsWaveSpawning()
    {
        return isWaveSpawning;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Time.timeScale == 0)
            {
                UnpauseGame();
            }
            else
            {
                PauseGame();
            }
        }
    }
}
