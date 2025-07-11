using UnityEngine;

using UnityEngine.InputSystem;

using UnityEngine.Events;

using DG.Tweening;

using System;

public class PlayerBar : MonoBehaviour
{
    private enum BarState
    {
        Idle,

        Aiming,

        Rotating,

        Fever
    }

    // イベント

    public UnityEvent<int> OnDeploymentsChanged;

    public UnityEvent<int> OnFeverCountChanged;

    // チュートリアル用のコールバック

    public Action OnTutorialBarReleased;

    [Header("参照")]
    [SerializeField]
    private Transform _barModelTransform;

    [Header("展開回数の制御")]
    [SerializeField, Tooltip("Barを展開できる最大回数")]
    private int _maxDeployments = 5;

    #region Inspector設定項目

    [Header("フィーバー制御")]
    [SerializeField]
    private int _maxFeverCount = 2;

    [SerializeField]
    private float _feverRotationSpeed = 2000f;

    [SerializeField]
    private int _feverRotations = 5;

    [Header("長さの制御")]
    [SerializeField]
    private float _minLength = 1f;

    [SerializeField]
    private float _maxLength = 10f;

    [Header("回転の制御")]
    [SerializeField]
    private float _baseRotationSpeed = 800f;

    [Header("収縮と方向転換の制御")]
    [SerializeField]
    private float _shrinkRate = 0.5f;

    [SerializeField]
    private float _lengthToShrinkRateBonus = 0.05f;

    [SerializeField]
    private float _lengthPenaltyPerReversal = 0.5f;

    [SerializeField]
    private int _maxReversals = 10;

    [Header("戦闘の制御")]
    [SerializeField]
    private int _damage = 1;

    [SerializeField]
    private float _knockbackForce = 10f;

    [Header("ノックバック角度")]
    [SerializeField, Range(0f, 1f)]
    private float _knockbackUpwardRatio = 0.5f;

    #endregion



    // 内部変数

    private BarState _currentState;

    private int _deploymentsRemaining;

    private float _currentLength;

    private float _rotationDirection = 1f;

    private Camera _mainCamera;

    private int _reversalsRemaining;

    private float _currentShrinkRate;

    private int _feverUsesRemaining;

    private float _totalRotationInFever;

    private bool _isPreviewActive = false; // プレビューが表示されているかどうか

    private void Start()
    {
        _mainCamera = Camera.main;

        _barModelTransform.gameObject.SetActive(false);

        _currentState = BarState.Idle;

        _deploymentsRemaining = _maxDeployments;

        OnDeploymentsChanged.Invoke(_deploymentsRemaining);

        _feverUsesRemaining = _maxFeverCount;

        OnFeverCountChanged.Invoke(_feverUsesRemaining);
    }

    public int GetInitialDeployments() => _deploymentsRemaining;

    public int GetInitialFeverCount() => _maxFeverCount;

    private void Update()
    {
        switch (_currentState)
        {
            case BarState.Idle:

                HandleIdleState();

                break;

            case BarState.Aiming:

                HandleAimingState();

                break;

            case BarState.Rotating:

                HandleRotatingState();

                break;

            case BarState.Fever:

                HandleFeverState();

                break;
        }
    }

    private void HandleIdleState()
    {
        if (_deploymentsRemaining <= 0 && _feverUsesRemaining <= 0)

            return;

        var pointer = Pointer.current;

        if (pointer != null && pointer.press.wasPressedThisFrame)
        {
            if (_deploymentsRemaining > 0)
            {
                // チュートリアル中でプレビューが表示されている場合は、GameManagerに通知

                if (_isPreviewActive && GameManager.Instance != null)
                {
                    GameManager.Instance.OnTutorialDragStart();

                    _isPreviewActive = false;
                }

                _currentState = BarState.Aiming;

                _barModelTransform.gameObject.SetActive(true);
            }
        }
    }

    /// <summary>

    /// チュートリアル用に、Barのプレビューを表示状態にする

    /// </summary>

    public void ShowPreview()
    {
        _isPreviewActive = true;

        _barModelTransform.gameObject.SetActive(true);

        // 全てのRendererを取得して半透明にする

        foreach (var renderer in _barModelTransform.GetComponentsInChildren<Renderer>())
        {
            // マテリアルのインスタンスを作成して元のマテリアルを保護

            var mat = renderer.material;

            var color = mat.color;

            mat.color = new Color(color.r, color.g, color.b, 0.5f);
        }
    }

    /// <summary>

    /// チュートリアル用に、Barのプレビューをアニメーションさせる

    /// </summary>

    public Tween AnimatePreview(float length, float duration)
    {
        // 回転方向を右下に設定

        transform.rotation = Quaternion.Euler(0, 45, 0);

        return _barModelTransform
            .DOScaleX(length, duration)
            .SetEase(Ease.OutCubic)
            .OnUpdate(() =>
            {
                float currentPreviewLength = _barModelTransform.localScale.x;

                _barModelTransform.localPosition = new Vector3(currentPreviewLength * 0.5f, 0, 0);
            });
    }

    /// <summary>

    /// チュートリアル用に、Barのプレビューを非表示にする

    /// </summary>

    public Tween HidePreview(float duration)
    {
        _isPreviewActive = false;

        var renderers = _barModelTransform.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)

            return null;

        var sequence = DOTween.Sequence();

        foreach (var renderer in renderers)
        {
            var mat = renderer.material;

            sequence.Join(mat.DOFade(0, duration));
        }

        sequence.OnComplete(() =>
        {
            _barModelTransform.gameObject.SetActive(false);

            // 透明度を元に戻しておく

            foreach (var renderer in renderers)
            {
                var mat = renderer.material;

                mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 1f);
            }
        });

        return sequence;
    }

    /// <summary>

    /// チュートリアルのループ開始時にプレビューをリセットする

    /// </summary>

    public void ResetPreview()
    {
        UpdateBarTransform(0);

        var renderers = _barModelTransform.GetComponentsInChildren<Renderer>();

        foreach (var renderer in renderers)
        {
            var mat = renderer.material;

            mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.5f);
        }
    }

    private void HandleAimingState()
    {
        var pointer = Pointer.current;

        if (pointer == null)

            return;

        // 透明度を通常に戻す（プレビューから通常のBarに切り替わった場合）

        if (_isPreviewActive)
        {
            _isPreviewActive = false;

            foreach (var renderer in _barModelTransform.GetComponentsInChildren<Renderer>())
            {
                var mat = renderer.material;

                var color = mat.color;

                mat.color = new Color(color.r, color.g, color.b, 1f);
            }
        }

        if (pointer.press.isPressed)
        {
            var groundPlane = new Plane(Vector3.up, Vector3.zero);

            Ray ray = _mainCamera.ScreenPointToRay(pointer.position.ReadValue());

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 barVector = ray.GetPoint(enter) - transform.position;

                barVector.y = 0;

                float previewLength = Mathf.Clamp(barVector.magnitude, _minLength, _maxLength);

                UpdateBarTransform(previewLength);

                if (barVector != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(barVector.normalized, Vector3.up);
                }
            }
        }

        if (pointer.press.wasReleasedThisFrame)
        {
            float finalLength = _barModelTransform.localScale.x;

            if (finalLength <= _minLength)
            {
                // 長さが足りなければキャンセル

                _barModelTransform.gameObject.SetActive(false);

                _currentState = BarState.Idle;

                return;
            }

            // Barの生成が確定

            _deploymentsRemaining--;

            OnDeploymentsChanged.Invoke(_deploymentsRemaining);

            _currentLength = finalLength;

            _rotationDirection = 1f;

            _currentShrinkRate = _shrinkRate + (_currentLength * _lengthToShrinkRateBonus);

            _reversalsRemaining = _maxReversals;

            _currentState = BarState.Rotating;

            // チュートリアル用のコールバックを呼び出す

            OnTutorialBarReleased?.Invoke();
        }
    }

    private void HandleRotatingState()
    {
        _currentLength -= _currentShrinkRate * Time.deltaTime;

        if (_currentLength <= _minLength)
        {
            _barModelTransform.gameObject.SetActive(false);

            _currentState = BarState.Idle;

            return;
        }

        UpdateBarTransform(_currentLength);

        float currentRotationSpeed = (_baseRotationSpeed / _currentLength) * _rotationDirection;

        transform.Rotate(Vector3.up, currentRotationSpeed * Time.deltaTime);

        var pointer = Pointer.current;

        if (pointer != null && pointer.press.wasPressedThisFrame)
        {
            if (_reversalsRemaining > 0)
            {
                _rotationDirection *= -1f;

                _reversalsRemaining--;

                _currentLength -= _lengthPenaltyPerReversal;
            }
        }
    }

    private void UpdateBarTransform(float length)
    {
        _barModelTransform.localScale = new Vector3(
            length,
            _barModelTransform.localScale.y,
            _barModelTransform.localScale.z
        );

        _barModelTransform.localPosition = new Vector3(length * 0.5f, 0f, 0f);
    }

    public void ActivateFever()
    {
        if (_currentState != BarState.Idle || _feverUsesRemaining <= 0)

            return;

        _feverUsesRemaining--;

        OnFeverCountChanged.Invoke(_feverUsesRemaining);

        _totalRotationInFever = 0f;

        _currentState = BarState.Fever;

        UpdateBarTransform(_maxLength);

        _barModelTransform.gameObject.SetActive(true);
    }

    private void HandleFeverState()
    {
        float rotationThisFrame = _feverRotationSpeed * Time.deltaTime;

        transform.Rotate(Vector3.up, rotationThisFrame);

        _totalRotationInFever += Mathf.Abs(rotationThisFrame);

        if (_totalRotationInFever >= _feverRotations * 360f)
        {
            _barModelTransform.gameObject.SetActive(false);

            _currentState = BarState.Idle;
        }
    }

    public void ProcessHit(Collider other)
    {
        if (_currentState != BarState.Rotating && _currentState != BarState.Fever)

            return;

        if (other.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy = other.gameObject.GetComponent<Enemy>();

            if (enemy != null)
            {
                Vector3 horizontalDirection = other.transform.position - transform.position;

                horizontalDirection.y = 0;

                Vector3 knockbackDirection = (
                    horizontalDirection.normalized + Vector3.up * _knockbackUpwardRatio
                ).normalized;

                enemy.TakeDamage(_damage, knockbackDirection, _knockbackForce);
            }
        }
    }
}
