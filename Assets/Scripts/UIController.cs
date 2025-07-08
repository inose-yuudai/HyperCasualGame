// UIController.cs の全文
using UnityEngine;
using TMPro;

public class UIController : MonoBehaviour
{
    [Header("UI要素の参照")]
    [SerializeField]
    private TextMeshProUGUI _enemyCountText;

    [SerializeField]
    private TextMeshProUGUI _deploymentsRemainingText;

    [Header("参照するコンポーネント")]
    [SerializeField]
    private EnemySpawner _enemySpawner;

    [SerializeField]
    private PlayerBar _playerBar;

    private void Start()
    {
        if (_enemySpawner != null)
        {
            _enemySpawner.OnEnemyCountUpdated.AddListener(UpdateEnemyCountDisplay);
        }
        if (_playerBar != null)
        {
            _playerBar.OnDeploymentsChanged.AddListener(UpdateDeploymentsRemaining);
            // 初期表示
            UpdateDeploymentsRemaining(_playerBar.GetInitialDeployments());
        }

        // Spawnerの準備ができていれば初期表示
        if (_enemySpawner != null)
        {
            UpdateEnemyCountDisplay();
        }
    }

    private void OnDestroy()
    {
        _enemySpawner?.OnEnemyCountUpdated.RemoveListener(UpdateEnemyCountDisplay);
        _playerBar?.OnDeploymentsChanged.RemoveListener(UpdateDeploymentsRemaining);
    }

    /// <summary>
    /// 敵の数（倒した数 / 全体）の表示を更新します
    /// </summary>
    private void UpdateEnemyCountDisplay()
    {
        if (_enemyCountText != null && _enemySpawner != null)
        {
            int defeated = _enemySpawner.DefeatedEnemiesCount;
            int total = _enemySpawner.TotalEnemiesToSpawn;
            _enemyCountText.text = $"Defeated: {defeated} / {total}";
        }
    }

    public void UpdateDeploymentsRemaining(int count)
    {
        if (_deploymentsRemainingText != null)
        {
            _deploymentsRemainingText.text = $"Shots Left: {count}";
        }
    }
}
