using UnityEngine;
using DG.Tweening;
using System; // ◀◀◀ Actionを使うために追加

public class PlayerBarVisuals : MonoBehaviour
{
    // --- 🔽🔽🔽 ここから追加 🔽🔽🔽 ---
    public event Action<BarPivotMode> OnPivotModeChanged;
    public BarPivotMode CurrentPivotMode => _pivotMode;

    // --- 🔼🔼🔼 追加ここまで 🔼🔼🔼 ---

    public enum BarPivotMode
    {
        EndPoint,
        Center
    }

    [Header("表示モード")]
    [SerializeField]
    private BarPivotMode _pivotMode = BarPivotMode.EndPoint;

    [Header("参照")]
    [SerializeField]
    private Transform _barModelTransform;

    [Header("点滅の制御")]
    [SerializeField]
    private float _blinkingThreshold = 3.0f;

    [SerializeField]
    private float _blinkingDuration = 0.4f;

    [SerializeField]
    private Color _blinkingColor = Color.red;

    private Renderer _barRenderer;
    private Color _originalBarColor;
    private Tween _blinkingTween;
    private bool _isBlinking = false;

    private void Awake()
    {
        _barRenderer = _barModelTransform.GetComponent<Renderer>();
        if (_barRenderer != null)
        {
            _originalBarColor = _barRenderer.material.color;
        }
        else
        {
            Debug.LogError("BarModelにRendererが見つかりません", this);
        }
    }

    // --- 🔽🔽🔽 ここからメソッドを新規追加 🔽🔽🔽 ---
    /// <summary>
    /// UIボタンなど外部から呼ばれ、ピボットモードを切り替える
    /// </summary>
    public void TogglePivotMode()
    {
        // モードをトグル切り替え
        _pivotMode =
            (_pivotMode == BarPivotMode.EndPoint) ? BarPivotMode.Center : BarPivotMode.EndPoint;

        // モードが変更されたことをイベントで通知
        OnPivotModeChanged?.Invoke(_pivotMode);
    }

    // --- 🔼🔼🔼 追加ここまで 🔼🔼🔼 ---


    #region 変更の少ないメソッド
    public void SetActive(bool isActive)
    {
        _barModelTransform.gameObject.SetActive(isActive);
    }

    public void UpdateTransform(float length, Quaternion rotation)
    {
        _barModelTransform.localScale = new Vector3(
            _barModelTransform.localScale.x,
            _barModelTransform.localScale.y,
            length
        );
        _barModelTransform.localPosition = GetPositionForMode(length);
    }

    public void CheckBlinking(float currentLength)
    {
        if (currentLength <= _blinkingThreshold && !_isBlinking)
        {
            StartBlinking();
        }
        else if (currentLength > _blinkingThreshold && _isBlinking)
        {
            StopBlinking();
        }
    }

    public void StartBlinking()
    {
        if (_barRenderer == null || _isBlinking)
            return;
        _isBlinking = true;
        _blinkingTween?.Kill();
        _blinkingTween = _barRenderer.material
            .DOColor(_blinkingColor, _blinkingDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    public void StopBlinking()
    {
        if (!_isBlinking)
            return;
        if (_barRenderer == null)
            return;

        _isBlinking = false;
        _blinkingTween?.Kill();
        _blinkingTween = null;
        _barRenderer.material.color = _originalBarColor;
    }

    public Tween AnimatePreview(float length, float duration)
    {
        Debug.Log("Animating preview to length: " + length, this);
        return _barModelTransform
            .DOScaleZ(length, duration)
            .SetEase(Ease.OutCubic)
            .OnUpdate(() =>
            {
                float currentPreviewLength = _barModelTransform.localScale.z;
                _barModelTransform.localPosition = GetPositionForMode(currentPreviewLength);
            });
    }

    private Vector3 GetPositionForMode(float currentLength)
    {
        switch (_pivotMode)
        {
            case BarPivotMode.EndPoint:
                return new Vector3(0, 0, currentLength * 0.5f);
            case BarPivotMode.Center:
                return Vector3.zero;
            default:
                return Vector3.zero;
        }
    }

    public void ShowPreview()
    {
        StopBlinking();
        SetActive(true);
        var mat = _barRenderer.material;
        mat.color = new Color(_originalBarColor.r, _originalBarColor.g, _originalBarColor.b, 0.5f);
    }

    public Tween HidePreview(float duration)
    {
        return _barRenderer.material
            .DOFade(0, duration)
            .OnComplete(() =>
            {
                SetActive(false);
                _barRenderer.material.color = _originalBarColor;
            });
    }

    public void ResetPreview()
    {
        StopBlinking();
        UpdateTransform(0, Quaternion.identity);
        var mat = _barRenderer.material;
        mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.5f);
    }
    #endregion
}
