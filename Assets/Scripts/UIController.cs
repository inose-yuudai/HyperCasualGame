using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIController : MonoBehaviour
{
    [Header("アイテムUI")]
    [SerializeField]
    private Button _timeStopButton;

    [SerializeField]
    private TextMeshProUGUI _timeStopCountText;

    [SerializeField]
    private Button _invincibilityButton;

    [SerializeField]
    private TextMeshProUGUI _invincibilityCountText;

    [Header("UI要素の参照")]
    [SerializeField]
    private TextMeshProUGUI _enemyCountText;

    [SerializeField]
    private TextMeshProUGUI _deploymentsRemainingText;

    [SerializeField]
    private TextMeshProUGUI _playerHealthText;

    [SerializeField]
    private Slider _playerHealthSlider;

    [SerializeField]
    private Slider _progressBarSlider;

    [SerializeField]
    private TextMeshProUGUI _progressText;

    [SerializeField]
    private Button _feverButton;

    [SerializeField]
    private TextMeshProUGUI _feverCountText;

    [SerializeField]
    private GameObject _comboGroup;

    [SerializeField]
    private TextMeshProUGUI _comboNumberText;

    [SerializeField]
    private TextMeshProUGUI _comboLabelText;

    [SerializeField]
    private CanvasGroup _comboGroupCanvasGroup;

    [Header("コンボアニメーション設定")]
    [SerializeField]
    private float _comboAnimationDuration = 0.3f;

    [SerializeField]
    private float _comboFadeOutDuration = 0.5f;

    [Header("参照するコンポーネント")]
    [SerializeField]
    private EnemySpawner _enemySpawner;

    [SerializeField]
    private PlayerBar _playerBar;

    [SerializeField]
    private PlayerHealth _playerHealth;

    private bool _isProgressBarInitialized = false;
    private bool _isHealthBarInitialized = false;

    private void Start()
    {
        // if (GameManager.Instance != null)
        // {
        //     GameManager.Instance.OnComboUpdated.AddListener(UpdateComboDisplay);
        //     GameManager.Instance.OnItemCountChanged.AddListener(UpdateItemCountDisplay);
        // }

        // _timeStopButton?.onClick.AddListener(OnTimeStopButtonPressed);
        // _invincibilityButton?.onClick.AddListener(OnInvincibilityButtonPressed);

        _enemySpawner?.OnEnemyCountUpdated.AddListener(UpdateEnemyCountDisplay);
        _playerHealth?.OnHealthChanged.AddListener(UpdatePlayerHealthDisplay);
        if (_playerBar != null)
        {
            _playerBar.OnDeploymentsChanged.AddListener(UpdateDeploymentsRemaining);
            _playerBar.OnFeverCountChanged.AddListener(UpdateFeverCount);
        }
        _feverButton?.onClick.AddListener(OnFeverButtonPressed);

        if (_playerBar != null)
        {
            UpdateDeploymentsRemaining(_playerBar.GetInitialDeployments());
            UpdateFeverCount(_playerBar.GetInitialFeverCount());
        }
        if (_playerHealth != null)
        {
            UpdatePlayerHealthDisplay(_playerHealth.GetInitialHealth());
        }

        if (_comboGroup != null)
            _comboGroup.SetActive(false);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnComboUpdated.RemoveListener(UpdateComboDisplay);
          //  GameManager.Instance.OnItemCountChanged.RemoveListener(UpdateItemCountDisplay);
        }
        // _timeStopButton?.onClick.RemoveListener(OnTimeStopButtonPressed);
        // _invincibilityButton?.onClick.RemoveListener(OnInvincibilityButtonPressed);
        _enemySpawner?.OnEnemyCountUpdated.RemoveListener(UpdateEnemyCountDisplay);
        _playerHealth?.OnHealthChanged.RemoveListener(UpdatePlayerHealthDisplay);
        if (_playerBar != null)
        {
            _playerBar.OnDeploymentsChanged.RemoveListener(UpdateDeploymentsRemaining);
            _playerBar.OnFeverCountChanged.RemoveListener(UpdateFeverCount);
        }
        _feverButton?.onClick.RemoveListener(OnFeverButtonPressed);
    }

    // private void OnTimeStopButtonPressed() => GameManager.Instance?.UseTimeStop();

    // private void OnInvincibilityButtonPressed() => GameManager.Instance?.UseInvincibility();

    // private void UpdateItemCountDisplay(ItemType itemType, int count)
    // {
    //     switch (itemType)
    //     {
    //         case ItemType.TimeStop:
    //             if (_timeStopCountText != null)
    //                 _timeStopCountText.text = $"{count}";
    //             if (_timeStopButton != null)
    //                 _timeStopButton.interactable = (count > 0);
    //             break;
    //         case ItemType.Invincibility:
    //             if (_invincibilityCountText != null)
    //                 _invincibilityCountText.text = $"{count}";
    //             if (_invincibilityButton != null)
    //                 _invincibilityButton.interactable = (count > 0);
    //             break;
    //     }
    // }

    private void UpdateEnemyCountDisplay()
    {
        if (_enemySpawner == null)
            return;
        if (!_isProgressBarInitialized && _enemySpawner.TotalEnemiesToSpawn > 0)
        {
            if (_progressBarSlider != null)
            {
                _progressBarSlider.minValue = 0;
                _progressBarSlider.maxValue = _enemySpawner.TotalEnemiesToSpawn;
            }
            _isProgressBarInitialized = true;
        }
        UpdateProgressBar();
    }

    private void UpdateProgressBar()
    {
        if (
            _progressBarSlider != null
            && _enemySpawner != null
            && _enemySpawner.TotalEnemiesToSpawn > 0
        )
        {
            _progressBarSlider.value = _enemySpawner.DefeatedEnemiesCount;
            if (_progressText != null)
            {
                float progressPercentage =
                    (float)_enemySpawner.DefeatedEnemiesCount
                    / _enemySpawner.TotalEnemiesToSpawn
                    * 100f;
                _progressText.text = $"{Mathf.RoundToInt(progressPercentage)}%";
            }
        }
    }

    private void UpdatePlayerHealthDisplay(int health)
    {
        if (!_isHealthBarInitialized && _playerHealth != null)
        {
            if (_playerHealthSlider != null)
            {
                _playerHealthSlider.maxValue = _playerHealth.MaxHealth;
            }
            _isHealthBarInitialized = true;
        }
        if (_playerHealthText != null)
        {
            _playerHealthText.text = $"{health}";
        }
        if (_playerHealthSlider != null)
        {
            _playerHealthSlider.value = health;
        }
    }

    private void UpdateDeploymentsRemaining(int count)
    {
        if (_deploymentsRemainingText != null)
        {
            _deploymentsRemainingText.text = $"{count}";
        }
    }

    private void UpdateFeverCount(int count)
    {
        if (_feverCountText != null)
        {
            _feverCountText.text = $"{count}";
        }
        if (_feverButton != null)
        {
            _feverButton.interactable = (count > 0);
        }
    }

    private void OnFeverButtonPressed() => _playerBar?.ActivateFever();

    private void UpdateComboDisplay(int comboCount)
    {
        if (
            _comboGroup == null
            || _comboNumberText == null
            || _comboLabelText == null
            || _comboGroupCanvasGroup == null
        )
            return;
        _comboNumberText.transform.DOKill();
        _comboGroupCanvasGroup.DOKill();
        if (comboCount > 1)
        {
            _comboNumberText.text = $"{comboCount}";
            _comboLabelText.text = "COMBO";
            _comboGroupCanvasGroup.alpha = 1f;
            _comboGroup.SetActive(true);
            _comboNumberText.transform.localScale = Vector3.one * 1.8f;
            _comboNumberText.transform.DOScale(1f, _comboAnimationDuration).SetEase(Ease.OutBack);
        }
        else if (_comboGroup.activeSelf)
        {
            _comboGroupCanvasGroup
                .DOFade(0, _comboFadeOutDuration)
                .OnComplete(() => _comboGroup.SetActive(false));
        }
    }
}
