using UnityEngine;

using UnityEngine.Events;

using System.Collections;

using DG.Tweening;

using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public UnityEvent<int> OnComboUpdated;

    [Header("チュートリアル設定")]
    [SerializeField]
    private bool _isTutorial = true;

    [SerializeField]
    private GameObject _tutorialHand;

    [SerializeField]
    private float _tutorialStartDelay = 1f;

    [SerializeField]
    private float _tutorialAnimationDuration = 1.5f;

    [Header("参照")]
    [SerializeField]
    private EnemySpawner _enemySpawner;

    [SerializeField]
    private PlayerBar _playerBar;

    [Header("コンボ設定")]
    [SerializeField]
    private float _comboResetTime = 2f;

    private int _currentCombo;

    private float _comboTimer;

    private Sequence _tutorialSequence;

    private bool _tutorialBarReleased = false; // チュートリアルでBarが発射されたかどうか

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
    }

    private void Start()
    {
        if (_isTutorial)
        {
            StartCoroutine(TutorialSequence());
        }
        else
        {
            _enemySpawner.StartWave();
        }
    }

    private void Update()
    {
        // コンボタイマーの処理

        if (_currentCombo > 0)
        {
            _comboTimer -= Time.deltaTime;

            if (_comboTimer <= 0)
            {
                ResetCombo();
            }
        }
    }

    private IEnumerator TutorialSequence()
    {
        // PlayerBarは有効にしておく（プレイヤーの入力を受け付けるため）

        if (_playerBar != null)
        {
            _playerBar.enabled = true;

            // チュートリアル完了時のコールバックを設定

            _playerBar.OnTutorialBarReleased = OnTutorialBarReleased;
        }

        yield return new WaitForSeconds(_tutorialStartDelay);

        if (_tutorialHand != null && _playerBar != null)
        {
            // 手とプレビューを表示

            _tutorialHand.SetActive(true);

            _playerBar.ShowPreview();

            RectTransform handRect = _tutorialHand.GetComponent<RectTransform>();

            CanvasGroup handCanvasGroup = _tutorialHand.GetComponent<CanvasGroup>();

            // アニメーションシーケンスを作成

            _tutorialSequence = DOTween.Sequence();

            _tutorialSequence.AppendCallback(() =>
            {
                // ループの開始時にリセット

                handRect.anchoredPosition = Vector2.zero;

                handCanvasGroup.alpha = 1f;

                _playerBar.ResetPreview();
            });

            // 指を右下に動かす

            _tutorialSequence.Append(
                handRect
                    .DOAnchorPos(new Vector2(300, -200), _tutorialAnimationDuration)
                    .SetEase(Ease.OutCubic)
            );

            _tutorialSequence.Join(_playerBar.AnimatePreview(5f, _tutorialAnimationDuration));

            _tutorialSequence.AppendInterval(0.5f);

            _tutorialSequence.Append(handCanvasGroup.DOFade(0, 0.5f));

            _tutorialSequence.AppendInterval(0.5f);

            _tutorialSequence.SetLoops(-1);
        }
    }

    /// <summary>

    /// プレイヤーがチュートリアル中にドラッグを開始した時に呼ばれる

    /// </summary>

    public void OnTutorialDragStart()
    {
        if (!_isTutorial)

            return;

        // アニメーションを停止して手を消す

        if (_tutorialSequence != null)
        {
            _tutorialSequence.Kill();
        }

        if (_tutorialHand != null)
        {
            _tutorialHand.SetActive(false);
        }

        // プレビューのBarは消さない（プレイヤーが操作するBarとして使用される）
    }

    /// <summary>

    /// チュートリアル中にBarが発射された時に呼ばれる

    /// </summary>

    private void OnTutorialBarReleased()
    {
        if (!_isTutorial || _tutorialBarReleased)

            return;

        _tutorialBarReleased = true;

        _isTutorial = false;

        // 敵のスポーンを開始

        _enemySpawner.StartWave();

        // コールバックを解除

        if (_playerBar != null)
        {
            _playerBar.OnTutorialBarReleased = null;
        }
    }

    public void OnEnemyDefeated()
    {
        _currentCombo++;

        _comboTimer = _comboResetTime;

        OnComboUpdated.Invoke(_currentCombo);
    }

    private void ResetCombo()
    {
        if (_currentCombo == 0)

            return;

        _currentCombo = 0;

        OnComboUpdated.Invoke(_currentCombo);
    }
}
