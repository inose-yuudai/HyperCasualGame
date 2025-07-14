using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
// PlayerBarVisualsのenumを参照するためにusingを追加
using static PlayerBarVisuals;

public class UIController : MonoBehaviour
{
    // ... (既存のUI要素は変更なし) ...
    #region 既存のUI要素
    [Header("アイテムUI")]
    [SerializeField]
    private Button _timeStopButton;

    [SerializeField]
    private TextMeshProUGUI _timeStopCountText;

    [SerializeField]
    private Button _instantFeverButton;

    [SerializeField]
    private TextMeshProUGUI _instantFeverCountText;

    [SerializeField]
    private Button _magnetFieldButton;

    [SerializeField]
    private TextMeshProUGUI _magnetFieldCountText;

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
    #endregion

    // --- 🔽🔽🔽 ここから追加 🔽🔽🔽 ---
    [Header("設定・デバッグUI")]
    [SerializeField]
    private Button _pivotModeToggleButton; // モード切替ボタン

    [SerializeField]
    private TextMeshProUGUI _pivotModeStatusText; // 現在のモード表示用テキスト

    // --- 🔼🔼🔼 追加ここまで 🔼🔼🔼 ---

    [Header("参照するコンポーネント")]
    [SerializeField]
    private EnemySpawner _enemySpawner;

    [SerializeField]
    private PlayerBar _playerBar;

    [SerializeField]
    private PlayerHealth _playerHealth;

    // --- 🔽🔽🔽 ここから追加 🔽🔽🔽 ---
    [SerializeField]
    private PlayerBarVisuals _playerBarVisuals; // Visualsへの参照を追加

    // --- 🔼🔼🔼 追加ここまで 🔼🔼🔼 ---

    private bool _isProgressBarInitialized = false;
    private bool _isHealthBarInitialized = false;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnComboUpdated.AddListener(UpdateComboDisplay);
        }

        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.OnItemCountChanged.AddListener(UpdateItemCountDisplay);
        }

        _timeStopButton?.onClick.AddListener(OnTimeStopButtonPressed);
        _instantFeverButton?.onClick.AddListener(OnInstantFeverButtonPressed);
        _magnetFieldButton?.onClick.AddListener(OnMagnetFieldButtonPressed);

        _enemySpawner?.OnEnemyCountUpdated.AddListener(UpdateEnemyCountDisplay);
        _playerHealth?.OnHealthChanged.AddListener(UpdatePlayerHealthDisplay);

        if (_playerBar != null)
        {
            _playerBar.OnDeploymentsChanged.AddListener(UpdateDeploymentsRemaining);
            _playerBar.OnFeverCountChanged.AddListener(UpdateFeverCount);
        }
        _feverButton?.onClick.AddListener(OnFeverButtonPressed);

        // --- 🔽🔽🔽 ここから追加 🔽🔽🔽 ---
        _pivotModeToggleButton?.onClick.AddListener(OnPivotModeTogglePressed);
        if (_playerBarVisuals != null)
        {
            _playerBarVisuals.OnPivotModeChanged += UpdatePivotModeButtonDisplay;
            // 初期表示の更新
            UpdatePivotModeButtonDisplay(_playerBarVisuals.CurrentPivotMode);
        }
        // --- 🔼🔼🔼 追加ここまで 🔼🔼🔼 ---


        // --- 初期値の設定 ---
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
        // ... (既存のリスナー解除処理は変更なし) ...
        #region 既存のリスナー解除
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnComboUpdated.RemoveListener(UpdateComboDisplay);
        }

        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.OnItemCountChanged.RemoveListener(UpdateItemCountDisplay);
        }

        _timeStopButton?.onClick.RemoveListener(OnTimeStopButtonPressed);
        _instantFeverButton?.onClick.RemoveListener(OnInstantFeverButtonPressed);
        _magnetFieldButton?.onClick.RemoveListener(OnMagnetFieldButtonPressed);

        _enemySpawner?.OnEnemyCountUpdated.RemoveListener(UpdateEnemyCountDisplay);
        _playerHealth?.OnHealthChanged.RemoveListener(UpdatePlayerHealthDisplay);

        if (_playerBar != null)
        {
            _playerBar.OnDeploymentsChanged.RemoveListener(UpdateDeploymentsRemaining);
            _playerBar.OnFeverCountChanged.RemoveListener(UpdateFeverCount);
        }

        _feverButton?.onClick.RemoveListener(OnFeverButtonPressed);
        #endregion

        // --- 🔽🔽🔽 ここから追加 🔽🔽🔽 ---
        _pivotModeToggleButton?.onClick.RemoveListener(OnPivotModeTogglePressed);
        if (_playerBarVisuals != null)
        {
            _playerBarVisuals.OnPivotModeChanged -= UpdatePivotModeButtonDisplay;
        }
        // --- 🔼🔼🔼 追加ここまで 🔼🔼🔼 ---
    }

    // --- 🔽🔽🔽 ここからメソッドを新規追加 🔽🔽🔽 ---

    /// <summary>
    /// モード切替ボタンが押された時に呼ばれる
    /// </summary>
    private void OnPivotModeTogglePressed()
    {
        _playerBarVisuals?.TogglePivotMode();
    }

    /// <summary>
    /// PlayerBarVisualsからモード変更が通知された時に呼ばれる
    /// </summary>
    private void UpdatePivotModeButtonDisplay(BarPivotMode newMode)
    {
        if (_pivotModeStatusText == null)
            return;

        switch (newMode)
        {
            case BarPivotMode.EndPoint:
                _pivotModeStatusText.text = "endpoint";
                break;
            case BarPivotMode.Center:
                _pivotModeStatusText.text = "center";
                break;
        }
    }

    // --- 🔼🔼🔼 追加ここまで 🔼🔼🔼 ---




    #region 既存のメソッド（変更なし）
                    private void OnTimeStopButtonPressed()
    {
        ItemManager.Instance?.UseItem(ItemType.TimeStop);
    }

    private void OnInstantFeverButtonPressed()
    {
        ItemManager.Instance?.UseItem(ItemType.InstantFever);
    }

    private void OnMagnetFieldButtonPressed()
    {
        ItemManager.Instance?.UseItem(ItemType.MagnetField);
    }

    private void UpdateItemCountDisplay(ItemType itemType, int count)
    {
        switch (itemType)
        {
            case ItemType.TimeStop:
                if (_timeStopCountText != null)
                    _timeStopCountText.text = $"{count}";
                if (_timeStopButton != null)
                    _timeStopButton.interactable = (count > 0);
                break;

            case ItemType.InstantFever:
                if (_instantFeverCountText != null)
                    _instantFeverCountText.text = $"{count}";
                if (_instantFeverButton != null)
                    _instantFeverButton.interactable = (count > 0);
                break;

            case ItemType.MagnetField:
                if (_magnetFieldCountText != null)
                    _magnetFieldCountText.text = $"{count}";
                if (_magnetFieldButton != null)
                    _magnetFieldButton.interactable = (count > 0);
                break;
        }
    }

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

    public void UpdatePlayerHealthDisplay(int health)
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

    private void OnFeverButtonPressed()
    {
        _playerBar?.ActivateFever();
    }

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
    #endregion
}
