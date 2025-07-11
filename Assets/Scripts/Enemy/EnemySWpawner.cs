// EnemySpawner.cs の全文
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System;

public class EnemySpawner : MonoBehaviour
{
    public event Action OnWaveCleared;
    public UnityEvent OnEnemyCountUpdated;
    public int TotalEnemiesToSpawn { get; private set; }
    public int DefeatedEnemiesCount { get; private set; }

    [Header("生成する敵")]
    [SerializeField]
    private GameObject[] _enemyPrefabs;

    [Header("ターゲット")]
    [SerializeField]
    private Transform _playerTransform;

    [Header("ウェーブ設定")]
    [SerializeField]
    private int _enemiesToSpawn = 10;

    [SerializeField]
    private float _spawnInterval = 1f;

    [SerializeField]
    private float _spawnRadius = 15f;

    private int _aliveEnemiesCount;
    private bool _isSpawning;

 

    public void StartWave()
    {
        if (_isSpawning)
            return;

        // ★★★ ここで総数を設定し、即座にイベントを発行する ★★★
        TotalEnemiesToSpawn = _enemiesToSpawn;
        OnEnemyCountUpdated.Invoke(); // UIに「準備できたよ」と通知
        // ★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★

        StartCoroutine(SpawnWaveCoroutine());
    }

    private IEnumerator SpawnWaveCoroutine()
    {
        _isSpawning = true;
        _aliveEnemiesCount = 0;
        DefeatedEnemiesCount = 0;
        // OnEnemyCountUpdated.Invoke(); // StartWaveに移動したため、ここは不要

        for (int i = 0; i < _enemiesToSpawn; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, _enemyPrefabs.Length);
            GameObject prefabToSpawn = _enemyPrefabs[randomIndex];
            Vector2 randomCirclePos = UnityEngine.Random.insideUnitCircle.normalized * _spawnRadius;
            Vector3 spawnPosition =
                _playerTransform.position + new Vector3(randomCirclePos.x, 0, randomCirclePos.y);
            GameObject enemyObj = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            Enemy newEnemy = enemyObj.GetComponent<Enemy>();
            if (newEnemy != null)
            {
                newEnemy.OnDefeated += HandleEnemyDefeated;
                newEnemy.Initialize(_playerTransform);
                _aliveEnemiesCount++;
            }
            yield return new WaitForSeconds(_spawnInterval);
        }
        _isSpawning = false;
    }

    // EnemySpawner.cs の HandleEnemyDefeated メソッド内

    private void HandleEnemyDefeated(Enemy defeatedEnemy)
    {
        _aliveEnemiesCount--;
        DefeatedEnemiesCount++;
        defeatedEnemy.OnDefeated -= HandleEnemyDefeated;

        // ★★★ GameManagerに敵を倒したことを通知 ★★★
        GameManager.Instance?.OnEnemyDefeated();

        OnEnemyCountUpdated.Invoke();

        if (!_isSpawning && _aliveEnemiesCount <= 0)
        {
            Debug.Log("Wave Cleared!");
            OnWaveCleared?.Invoke();
        }
    }
}
