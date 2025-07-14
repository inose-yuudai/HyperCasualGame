using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public enum ItemType
{
    TimeStop,
    InstantFever,
    MagnetField
}

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }
    public UnityEvent<ItemType, int> OnItemCountChanged;

    [Header("アイテム初期数")]
    [SerializeField]
    private int _initialTimeStopCount = 3;

    [SerializeField]
    private int _initialInstantFeverCount = 2;

    [SerializeField]
    private int _initialMagnetFieldCount = 2;

    [Header("効果の持続時間")]
    [SerializeField]
    private float _timeStopDuration = 5f;

    [SerializeField]
    private float _magnetFieldDuration = 7f;

    [Header("マグネット設定")]
    [SerializeField]
    private float _magnetFieldRadius = 10f;

    [SerializeField]
    private float _magnetFieldForce = 15f;

    [SerializeField, Tooltip("敵がこれ以上近づかなくなる安全な半径")]
    private float _magnetFieldSafeRadius = 1.5f;

    [Header("エフェクト（オプション）")]
    [SerializeField]
    private GameObject _timeStopEffectPrefab;

    [SerializeField]
    private GameObject _magnetFieldEffectPrefab;

    [Header("参照")]
    [SerializeField]
    private PlayerBar _playerBar;

    [SerializeField]
    private Transform _playerTransform;

    private Dictionary<ItemType, int> _itemCounts = new Dictionary<ItemType, int>();
    private bool _isTimeStopActive = false;
    private bool _isMagnetFieldActive = false;
    private List<Enemy> _frozenEnemies = new List<Enemy>();
    private GameObject _currentMagnetFieldEffect;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        _itemCounts[ItemType.TimeStop] = _initialTimeStopCount;
        _itemCounts[ItemType.InstantFever] = _initialInstantFeverCount;
        _itemCounts[ItemType.MagnetField] = _initialMagnetFieldCount;
    }

    private void Start()
    {
        OnItemCountChanged?.Invoke(ItemType.TimeStop, _itemCounts[ItemType.TimeStop]);
        OnItemCountChanged?.Invoke(ItemType.InstantFever, _itemCounts[ItemType.InstantFever]);
        OnItemCountChanged?.Invoke(ItemType.MagnetField, _itemCounts[ItemType.MagnetField]);

        if (_playerBar == null)
        {
            _playerBar = FindObjectOfType<PlayerBar>();
        }
        if (_playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }
    }

    public void UseItem(ItemType itemType)
    {
        if (!HasItem(itemType))
            return;

        switch (itemType)
        {
            case ItemType.TimeStop:
                UseTimeStop();
                break;
            case ItemType.InstantFever:
                UseInstantFever();
                break;
            case ItemType.MagnetField:
                UseMagnetField();
                break;
        }

        _itemCounts[itemType]--;
        OnItemCountChanged?.Invoke(itemType, _itemCounts[itemType]);
    }

    private bool HasItem(ItemType itemType)
    {
        return _itemCounts.ContainsKey(itemType) && _itemCounts[itemType] > 0;
    }

    private void UseTimeStop()
    {
        if (_isTimeStopActive)
            return;
        StartCoroutine(TimeStopCoroutine());
    }

    private IEnumerator TimeStopCoroutine()
    {
        _isTimeStopActive = true;
        GameObject effect = null;
        if (_timeStopEffectPrefab != null && _playerTransform != null)
        {
            effect = Instantiate(
                _timeStopEffectPrefab,
                _playerTransform.position,
                Quaternion.identity
            );
        }

        Enemy[] allEnemies = FindObjectsOfType<Enemy>();
        _frozenEnemies.Clear();
        foreach (Enemy enemy in allEnemies)
        {
            if (enemy != null && !enemy.IsFrozen)
            {
                enemy.Freeze();
                _frozenEnemies.Add(enemy);
            }
        }

        // 時間停止中に新しい敵がスポーンした場合も凍結させる
        // EnemySpawner側に `public static event Action<Enemy> OnEnemySpawned;` のようなイベントがあると仮定
        EnemySpawner.OnEnemySpawned += OnNewEnemySpawnedDuringTimeStop;

        yield return new WaitForSeconds(_timeStopDuration);

        EnemySpawner.OnEnemySpawned -= OnNewEnemySpawnedDuringTimeStop;

        foreach (Enemy enemy in _frozenEnemies)
        {
            if (enemy != null)
            {
                enemy.Unfreeze();
            }
        }

        _frozenEnemies.Clear();
        _isTimeStopActive = false;

        if (effect != null)
        {
            Destroy(effect);
        }
    }

    private void OnNewEnemySpawnedDuringTimeStop(Enemy enemy)
    {
        if (_isTimeStopActive && enemy != null)
        {
            enemy.Freeze();
            _frozenEnemies.Add(enemy);
        }
    }

    private void UseInstantFever()
    {
        if (_playerBar != null)
        {
            _playerBar.ActivateFever();
        }
    }

    private void UseMagnetField()
    {
        if (_isMagnetFieldActive)
            return;
        StartCoroutine(MagnetFieldCoroutine());
    }

    private IEnumerator MagnetFieldCoroutine()
    {
        _isMagnetFieldActive = true;

        if (_magnetFieldEffectPrefab != null && _playerTransform != null)
        {
            _currentMagnetFieldEffect = Instantiate(
                _magnetFieldEffectPrefab,
                _playerTransform.position,
                Quaternion.identity,
                _playerTransform
            );
            _currentMagnetFieldEffect.transform.localScale = Vector3.one * _magnetFieldRadius * 2f;
        }

        float elapsedTime = 0f;
        while (elapsedTime < _magnetFieldDuration)
        {
            if (_playerTransform != null)
            {
                Collider[] colliders = Physics.OverlapSphere(
                    _playerTransform.position,
                    _magnetFieldRadius,
                    LayerMask.GetMask("Enemy")
                );
                foreach (Collider col in colliders)
                {
                    Enemy enemy = col.GetComponent<Enemy>();
                    if (enemy != null && enemy.enabled)
                    {
                        Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
                        if (enemyRb != null && !enemyRb.isKinematic)
                        {
                            Vector3 direction =
                                _playerTransform.position - enemy.transform.position;
                            float distance = direction.magnitude;

                            if (distance > _magnetFieldSafeRadius)
                            {
                                float forceMagnitude =
                                    _magnetFieldForce * (1f - distance / _magnetFieldRadius);
                                enemyRb.AddForce(
                                    direction.normalized * forceMagnitude,
                                    ForceMode.Force
                                );
                            }
                            else
                            {
                                enemyRb.linearVelocity = Vector3.zero;
                            }
                        }
                    }
                }
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (_currentMagnetFieldEffect != null)
        {
            Destroy(_currentMagnetFieldEffect);
        }
        _isMagnetFieldActive = false;
    }

    public void AddItem(ItemType itemType, int count = 1)
    {
        if (_itemCounts.ContainsKey(itemType))
        {
            _itemCounts[itemType] += count;
            OnItemCountChanged?.Invoke(itemType, _itemCounts[itemType]);
        }
    }

    public int GetItemCount(ItemType itemType)
    {
        return _itemCounts.ContainsKey(itemType) ? _itemCounts[itemType] : 0;
    }
}
