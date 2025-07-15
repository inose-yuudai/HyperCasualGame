using UnityEngine;
using UnityEngine.InputSystem;
using System;
using UnityEngine.EventSystems; // ★ 1. この行を追加

public class PlayerBarInput : MonoBehaviour
{
    public event Action<Vector3> OnDragStart;
    public event Action<Vector3> OnDragUpdate;
    public event Action OnDragEnd;
    public event Action OnTap;

    [SerializeField]
    private float _dragSensitivity = 2f;

    private Camera _mainCamera;
    private Plane _groundPlane;
    private Vector3 _dragStartPosition;
    private bool _isDragging = false;
    private AudioManager _audioManager;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _groundPlane = new Plane(Vector3.up, Vector3.zero);
        _audioManager = FindObjectOfType<AudioManager>();
    }

    private void Update()
    {
        var pointer = Pointer.current;
        if (pointer == null)
            return;
        // ★ 1. UI上の要素がクリックされている場合は、PlayerBarInputの処理をスキップ
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // クリックが開始されたフレームの処理
        if (pointer.press.wasPressedThisFrame)
        {
            // まず、タップイベントを発行します。
            // これにより、回転中の方向転換クリックが常に検知されます。
            OnTap?.Invoke();
            _audioManager?.PlaySFX(SFXType.CahnegeRotation);

            // 次に、ドラッグ開始の準備をします。
            if (GetPointerPositionOnGround(out _dragStartPosition))
            {
                _isDragging = true;
                // OnDragStartイベントも発行します。
                // PlayerBarコントローラー側が現在の状態で「今はドラッグ開始は無視する」と判断してくれるので問題ありません。
                OnDragStart?.Invoke(_dragStartPosition);
            }
        }

        // isPressed（押され続けている）かつ isDraggingがtrueならドラッグ中と判断
        if (_isDragging && pointer.press.isPressed)
        {
            if (GetPointerPositionOnGround(out Vector3 currentPos))
            {
                Vector3 dragVector = currentPos - _dragStartPosition;
                dragVector.y = 0;
                OnDragUpdate?.Invoke(dragVector * _dragSensitivity);
            }
        }

        // クリックが離されたフレームの処理
        if (pointer.press.wasReleasedThisFrame)
        {
            // isDraggingがtrueの時だけ（＝ドラッグ操作が行われていた場合のみ）ドラッグ終了を通知
            if (_isDragging)
            {
                OnDragEnd?.Invoke();
                _isDragging = false;
            }

        }
    }

    private bool GetPointerPositionOnGround(out Vector3 position)
    {
        Ray ray = _mainCamera.ScreenPointToRay(Pointer.current.position.ReadValue());
        if (_groundPlane.Raycast(ray, out float enter))
        {
            position = ray.GetPoint(enter);
            return true;
        }
        position = Vector3.zero;
        return false;
    }
}
