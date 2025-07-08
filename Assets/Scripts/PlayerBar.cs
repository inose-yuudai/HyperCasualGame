using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class PlayerBar : MonoBehaviour
{
    private enum BarState
    {
        Idle,
        Aiming,
        Rotating
    }

    public UnityEvent<int> OnDeploymentsChanged;

    [Header("参照")]
    [SerializeField]
    private Transform _barModelTransform;

    // --- その他の設定項目（変更なし） ---
    #region Inspector設定項目
    [Header("展開回数の制御")]
    [SerializeField]
    private int _maxDeployments = 5;

    [Header("長さの制御")]
    [SerializeField]
    private float _minLength = 2f;

    [SerializeField]
    private float _maxLength = 10f;

    [Header("回転の制御")]
    [SerializeField]
    private float _baseRotationSpeed = 360f;

    [Header("方向転換の制御")]
    [SerializeField]
    private int _minReversals = 10;

    [SerializeField]
    private int _maxReversalsForLongestBar = 5;

    [Header("時間減衰の制御")]
    [SerializeField]
    private float _maxRotationTime = 15f;

    [SerializeField]
    private float _minRotationTimeForLongestBar = 5f;

    [SerializeField]
    private AnimationCurve _decayCurve = AnimationCurve.Linear(0, 1, 1, 0.8f);

    [Header("戦闘の制御")]
    [SerializeField]
    private int _damage = 1;

    [SerializeField]
    private float _knockbackForce = 10f;

    [Header("ノックバック角度")]
    [SerializeField, Range(0f, 1f)]
    private float _knockbackUpwardRatio = 0.5f;
    #endregion

    private BarState _currentState;
    private int _deploymentsRemaining;
    private float _currentLength;
    private float _currentRotationSpeed;
    private float _rotationDirection = 1f;
    private int _maxReversalCount;
    private int _reversalsRemaining;
    private float _timeInRotationState;
    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
        _barModelTransform.gameObject.SetActive(false);
        _currentState = BarState.Idle;
        _deploymentsRemaining = _maxDeployments;
        OnDeploymentsChanged.Invoke(_deploymentsRemaining);
    }

    public int GetInitialDeployments() => _deploymentsRemaining;

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
        }
    }

    private void HandleIdleState()
    {
        if (_deploymentsRemaining <= 0)
            return;
        var pointer = Pointer.current;
        if (pointer != null && pointer.press.wasPressedThisFrame)
        {
            _currentState = BarState.Aiming;
            // 照準開始時にBarをプレビューとして表示する
            _barModelTransform.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 照準状態のロジックを、ドラッグ中のプレビュー表示と、離した時の確定処理に分割
    /// </summary>
    private void HandleAimingState()
    {
        var pointer = Pointer.current;
        if (pointer == null)
            return;

        // --- ドラッグ中のリアルタイムプレビュー処理 ---
        if (pointer.press.isPressed)
        {
            var groundPlane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = _mainCamera.ScreenPointToRay(pointer.position.ReadValue());
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 worldDragPosition = ray.GetPoint(enter);
                Vector3 barVector = worldDragPosition - transform.position;
                barVector.y = 0;

                // 現在の長さを計算し、見た目に反映
                float previewLength = Mathf.Clamp(barVector.magnitude, _minLength, _maxLength);
                _barModelTransform.localScale = new Vector3(
                    previewLength,
                    _barModelTransform.localScale.y,
                    _barModelTransform.localScale.z
                );
                _barModelTransform.localPosition = new Vector3(previewLength * 0.5f, 0f, 0f);

                // 現在の角度を計算し、見た目に反映
                if (barVector != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(barVector.normalized, Vector3.up);
                }
            }
        }

        // --- ドラッグを離した時の確定処理 ---
        if (pointer.press.wasReleasedThisFrame)
        {
            // 最終的な長さを確定
            float finalLength = _barModelTransform.localScale.x;

            if (finalLength < _minLength)
            {
                // 長さが足りなければキャンセル
                _barModelTransform.gameObject.SetActive(false);
                _currentState = BarState.Idle;
                return;
            }

            // 展開回数を消費
            _deploymentsRemaining--;
            OnDeploymentsChanged.Invoke(_deploymentsRemaining);

            // 回転に必要なパラメータを設定
            _currentLength = finalLength;
            float lengthRatio = Mathf.InverseLerp(_minLength, _maxLength, _currentLength);
            _maxReversalCount = (int)
                Mathf.Lerp(_minReversals, _maxReversalsForLongestBar, lengthRatio);
            _reversalsRemaining = _maxReversalCount;
            _timeInRotationState = 0f;
            _rotationDirection = 1f;

            // 回転状態に移行
            _currentState = BarState.Rotating;
        }
    }

    // HandleRotatingState と ProcessHit は変更ありません
    #region 変更のないメソッド
    private void HandleRotatingState()
    {
        _timeInRotationState += Time.deltaTime;
        float lengthRatio = Mathf.InverseLerp(_minLength, _maxLength, _currentLength);
        float actualRotationTime = Mathf.Lerp(
            _maxRotationTime,
            _minRotationTimeForLongestBar,
            lengthRatio
        );
        float normalizedTime = Mathf.Clamp01(_timeInRotationState / actualRotationTime);
        float timeMultiplier = _decayCurve.Evaluate(normalizedTime);
        float reversalMultiplier =
            (_maxReversalCount > 0) ? (float)_reversalsRemaining / _maxReversalCount : 0;
        float lengthBasedSpeed = _baseRotationSpeed / _currentLength;
        _currentRotationSpeed =
            lengthBasedSpeed * timeMultiplier * reversalMultiplier * _rotationDirection;
        transform.Rotate(Vector3.up, _currentRotationSpeed * Time.deltaTime);
        var pointer = Pointer.current;
        if (pointer != null && pointer.press.wasPressedThisFrame)
        {
            if (_reversalsRemaining > 0)
            {
                _rotationDirection *= -1f;
                _reversalsRemaining--;
            }
        }
        if (normalizedTime >= 1f || _reversalsRemaining <= 0)
        {
            if (Mathf.Abs(_currentRotationSpeed) < 1f)
            {
                _barModelTransform.gameObject.SetActive(false);
                _currentState = BarState.Idle;
            }
        }
    }

    public void ProcessHit(Collider other)
    {
        if (_currentState != BarState.Rotating)
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
                Debug.DrawRay(other.transform.position, knockbackDirection * 5f, Color.red, 2f);
                enemy.TakeDamage(_damage, knockbackDirection, _knockbackForce);
            }
        }
    }
    #endregion
}
