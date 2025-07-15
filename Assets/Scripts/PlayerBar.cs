using UnityEngine;
using UnityEngine.Events;
using System;

[RequireComponent(typeof(PlayerBarInput), typeof(PlayerBarVisuals), typeof(PlayerBarCombat))]
public class PlayerBar : MonoBehaviour
{
    // --- イベント ---
    public event Action OnFirstBarDeployed;
    public UnityEvent<int> OnDeploymentsChanged;
    public UnityEvent<int> OnFeverCountChanged;

    // --- 状態定義 ---
    private enum BarState
    {
        Idle,
        Aiming,
        Rotating,
        Fever
    }

    private BarState _currentState;

    // --- Inspector設定 ---
    [Header("ゲームルールの設定")]
    [SerializeField]
    private int _maxDeployments = 5;

    [SerializeField]
    private int _maxFeverCount = 2;

    [SerializeField]
    private float _minLength = 1f;

    [SerializeField]
    private float _maxLength = 10f;

    [Header("回転の制御")]
    [SerializeField]
    private float _baseRotationSpeed = 800f;

    [SerializeField]
    private float _feverRotationSpeed = 2000f;

    [SerializeField]
    private int _feverRotations = 5;

    [Header("収縮と方向転換の制御")]
    [SerializeField]
    private float _shrinkRate = 0.5f;

    [SerializeField]
    private float _lengthToShrinkRateBonus = 0.05f;

    [SerializeField]
    private float _lengthPenaltyPerReversal = 0.5f;

    [SerializeField]
    private int _maxReversals = 10;

    [SerializeField, Min(0.1f)]
    private float _reversalCooldown = 0.2f;

    [Header("効果音")]
    [SerializeField]
    private AudioSource _stretchingSoundSource;

    // --- 内部変数 ---
    private int _deploymentsRemaining;
    private int _feverUsesRemaining;
    private float _currentLength;
    private float _rotationDirection;
    private int _reversalsRemaining;
    private float _currentShrinkRate;
    private float _timeInRotationState;
    private float _totalRotationInFever;
    private bool _firstBarHasBeenDeployed = false;
    private bool _isTutorialMode = false;

    // --- コンポーネント参照 ---
    private PlayerBarInput _input;
    private PlayerBarVisuals _visuals;
    private PlayerBarCombat _combat;

    private void Awake()
    {
        _input = GetComponent<PlayerBarInput>();
        _visuals = GetComponent<PlayerBarVisuals>();
        _combat = GetComponent<PlayerBarCombat>();
    }

    private void OnEnable()
    {
        _input.OnDragStart += HandleDragStart;
        _input.OnDragUpdate += HandleDragUpdate;
        _input.OnDragEnd += HandleDragEnd;
        _input.OnTap += HandleTap;
    }

    private void OnDisable()
    {
        _input.OnDragStart -= HandleDragStart;
        _input.OnDragUpdate -= HandleDragUpdate;
        _input.OnDragEnd -= HandleDragEnd;
        _input.OnTap -= HandleTap;
    }

    private void Start()
    {
        _currentState = BarState.Idle;
        _visuals.SetActive(false);

        _deploymentsRemaining = _maxDeployments;
        OnDeploymentsChanged.Invoke(_deploymentsRemaining);

        _feverUsesRemaining = _maxFeverCount;
        OnFeverCountChanged.Invoke(_feverUsesRemaining);
    }

    private void Update()
    {
        switch (_currentState)
        {
            case BarState.Rotating:
                UpdateRotatingState();
                break;
            case BarState.Fever:
                UpdateFeverState();
                break;
        }
    }

    // --- 入力イベントのハンドラ ---

    private void HandleDragStart(Vector3 position)
    {
        if (_currentState != BarState.Idle)
            return;
        if (!_isTutorialMode && _deploymentsRemaining <= 0)
            return;

        if (_stretchingSoundSource != null && !_stretchingSoundSource.isPlaying)
        {
            _stretchingSoundSource.Play();
        }

        _currentState = BarState.Aiming;
        _visuals.SetActive(true);
        _currentLength = 0.1f;
        _visuals.UpdateTransform(_currentLength, transform.rotation);
    }

    private void HandleDragUpdate(Vector3 dragVector)
    {
        if (_currentState != BarState.Aiming)
            return;

        float dragDistance = dragVector.magnitude;
        _currentLength = Mathf.Clamp(dragDistance, 0.1f, _maxLength);

        Quaternion newRotation = transform.rotation;
        if (dragVector.sqrMagnitude > 0.01f)
        {
            newRotation = Quaternion.LookRotation(dragVector.normalized, Vector3.up);
        }
        transform.rotation = newRotation;
        _visuals.UpdateTransform(_currentLength, newRotation);
    }

    private void HandleDragEnd()
    {
        if (_currentState != BarState.Aiming)
            return;

        if (_stretchingSoundSource != null && _stretchingSoundSource.isPlaying)
        {
            _stretchingSoundSource.Stop();
        }

        if (_currentLength < _minLength)
        {
            TransitionToIdle();
            return;
        }

        if (!_firstBarHasBeenDeployed)
        {
            OnFirstBarDeployed?.Invoke();
            _firstBarHasBeenDeployed = true;
        }

        if (!_isTutorialMode)
        {
            _deploymentsRemaining--;
            OnDeploymentsChanged.Invoke(_deploymentsRemaining);
        }

        _rotationDirection = 1f;
        _currentShrinkRate = _shrinkRate + (_currentLength * _lengthToShrinkRateBonus);
        _reversalsRemaining = _maxReversals;
        _timeInRotationState = 0f;
        _currentState = BarState.Rotating;
        _combat.SetCombatActive(true);
    }

    private void HandleTap()
    {
        if (_currentState == BarState.Rotating && _timeInRotationState > _reversalCooldown)
        {
            if (_reversalsRemaining > 0)
            {
                _rotationDirection *= -1f;
                _reversalsRemaining--;
                _currentLength = Mathf.Max(_minLength, _currentLength - _lengthPenaltyPerReversal);
            }
        }
    }

    // --- 状態更新ロジック ---

    private void UpdateRotatingState()
    {
        _timeInRotationState += Time.deltaTime;
        _currentLength -= _currentShrinkRate * Time.deltaTime;

        _visuals.CheckBlinking(_currentLength);

        if (_currentLength <= _minLength)
        {
            TransitionToIdle();
            return;
        }

        float rotationSpeed = (_baseRotationSpeed / _currentLength) * _rotationDirection;
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        _visuals.UpdateTransform(_currentLength, transform.rotation);
    }

    private void UpdateFeverState()
    {
        float rotationThisFrame = _feverRotationSpeed * Time.deltaTime;
        transform.Rotate(Vector3.up, rotationThisFrame);
        _totalRotationInFever += Mathf.Abs(rotationThisFrame);

        if (_totalRotationInFever >= _feverRotations * 360f)
        {
            TransitionToIdle();
        }
    }

    private void TransitionToIdle()
    {
        _currentState = BarState.Idle;
        _visuals.SetActive(false);
        _visuals.StopBlinking();
        _combat.SetCombatActive(false);
    }

    // --- Public API ---

    public void ActivateFever()
    {
        if (_currentState != BarState.Idle || _feverUsesRemaining <= 0)
            return;

        if (!_firstBarHasBeenDeployed)
        {
            OnFirstBarDeployed?.Invoke();
            _firstBarHasBeenDeployed = true;
        }

        _feverUsesRemaining--;
        OnFeverCountChanged.Invoke(_feverUsesRemaining);

        _totalRotationInFever = 0f;
        _currentState = BarState.Fever;
        _visuals.SetActive(true);
        _visuals.UpdateTransform(_maxLength, transform.rotation);
        _combat.SetCombatActive(true);
    }

    public void SetTutorialMode(bool isTutorial) => _isTutorialMode = isTutorial;

    public int GetInitialDeployments() => _deploymentsRemaining;

    public int GetInitialFeverCount() => _feverUsesRemaining;
}
