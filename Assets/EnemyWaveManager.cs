using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct EnemyWave
{
    public List<Character> characters;
}

public class EnemyWaveManager : Singleton<EnemyWaveManager>
{
    [SerializeField]
    private List<EnemyWave> waves;
    public List<EnemyWave> Waves => waves;
}
