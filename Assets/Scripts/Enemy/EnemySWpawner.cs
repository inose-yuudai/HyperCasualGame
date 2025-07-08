using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System;

public class EnemySpawner : MonoBehaviour
{
    // --- イベント ---
    public event Action OnWaveCleared;
    public UnityEvent OnEnemyCountUpdated;

    // --- 公開プロパティ ---
    public int TotalEnemiesToSpawn { get; private set; }
    public int DefeatedEnemiesCount { get; private set; }

    // --- 設定項目 ---
    [Header("生成する敵")]
    // 単一のPrefabから、Prefabの配列（リスト）に変更
    [SerializeField, Tooltip("ランダムに生成する敵のPrefabリスト")]
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

    // --- 内部状態 ---
    private int _aliveEnemiesCount;
    private bool _isSpawning;

    private void Start()
    {
        // Prefabリストが空でないか、PlayerTransformが設定されているかを確認
        if (_playerTransform != null && _enemyPrefabs != null && _enemyPrefabs.Length > 0)
        {
            StartWave();
        }
        else
        {
            Debug.LogError("Player TransformまたはEnemy Prefabsが設定されていません。", this);
        }
    }

    public void StartWave()
    {
        if (_isSpawning)
            return;
        StartCoroutine(SpawnWaveCoroutine());
    }

    private IEnumerator SpawnWaveCoroutine()
    {
        _isSpawning = true;
        _aliveEnemiesCount = 0;
        DefeatedEnemiesCount = 0;
        TotalEnemiesToSpawn = _enemiesToSpawn;
        OnEnemyCountUpdated.Invoke();

        for (int i = 0; i < _enemiesToSpawn; i++)
        {
            // --- ここからが新しいロジック ---
            // 1. 生成する敵をPrefabリストからランダムに選択
            int randomIndex = UnityEngine.Random.Range(0, _enemyPrefabs.Length);
            GameObject prefabToSpawn = _enemyPrefabs[randomIndex];
            // --- ここまで ---

            Vector2 randomCirclePos = UnityEngine.Random.insideUnitCircle.normalized * _spawnRadius;
            Vector3 spawnPosition =
                _playerTransform.position + new Vector3(randomCirclePos.x, 0, randomCirclePos.y);

            // 2. 選択したPrefabを生成
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

    private void HandleEnemyDefeated(Enemy defeatedEnemy)
    {
        _aliveEnemiesCount--;
        DefeatedEnemiesCount++;
        defeatedEnemy.OnDefeated -= HandleEnemyDefeated;
        OnEnemyCountUpdated.Invoke();

        if (!_isSpawning && _aliveEnemiesCount <= 0)
        {
            Debug.Log("Wave Cleared!");
            OnWaveCleared?.Invoke();
        }
    }
}
