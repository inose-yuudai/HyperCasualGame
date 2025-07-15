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
    private bool _tutorialInteractionStarted = false;

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
            if (_playerBar != null)
            {
                _playerBar.SetTutorialMode(true);
                _playerBar.OnFirstBarDeployed += EndTutorial;
                _playerBar.enabled = true; // 最初から有効にする
            }
            StartCoroutine(TutorialSequence());
        }
        else
        {
            _enemySpawner.StartWave();
        }
    }

    private void Update()
    {
        // チュートリアルアニメーション中にクリックしたら、アニメーションを停止
        if (_isTutorial && !_tutorialInteractionStarted && _tutorialSequence != null && _tutorialSequence.IsActive())
        {
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                _tutorialInteractionStarted = true;
                StopTutorialAnimation();
            }
        }

        if (_currentCombo > 0)
        {
            _comboTimer -= Time.deltaTime;
            if (_comboTimer <= 0)
            {
                ResetCombo();
            }
        }
    }

    private void EndTutorial()
    {
        if (_playerBar != null)
        {
            _playerBar.OnFirstBarDeployed -= EndTutorial;
            _playerBar.SetTutorialMode(false);
        }
        StopTutorialAnimation();
        _isTutorial = false;
        _enemySpawner.StartWave();
    }

    private IEnumerator TutorialSequence()
    {
        yield return new WaitForSeconds(_tutorialStartDelay);

        if (_tutorialHand != null && _playerBar != null && !_tutorialInteractionStarted)
        {
            _tutorialHand.SetActive(true);

            RectTransform handRect = _tutorialHand.GetComponent<RectTransform>();
            CanvasGroup handCanvasGroup = _tutorialHand.GetComponent<CanvasGroup>();

            _tutorialSequence = DOTween.Sequence();

            // ドラッグ&ドロップのアニメーション
            _tutorialSequence.AppendCallback(() =>
            {
                handRect.anchoredPosition = Vector2.zero;
                handCanvasGroup.alpha = 1f;
            });

            // ドラッグ開始
            _tutorialSequence.Append(handCanvasGroup.DOFade(1f, 0.3f));

            // ドラッグ中（長くドラッグ）
            _tutorialSequence.Append(
                handRect
                    .DOAnchorPos(new Vector2(300, -200), _tutorialAnimationDuration)
                    .SetEase(Ease.InOutSine)
            );

            // ドロップ（離す）
            _tutorialSequence.AppendInterval(0.3f);
            _tutorialSequence.Append(handCanvasGroup.DOFade(0.5f, 0.2f));
            _tutorialSequence.AppendInterval(0.5f);

            // 短くドラッグする例も見せる
            _tutorialSequence.AppendCallback(() =>
            {
                handRect.anchoredPosition = Vector2.zero;
                handCanvasGroup.alpha = 1f;
            });

            _tutorialSequence.Append(
                handRect
                    .DOAnchorPos(new Vector2(150, -100), _tutorialAnimationDuration * 0.5f)
                    .SetEase(Ease.InOutSine)
            );

            _tutorialSequence.AppendInterval(0.3f);
            _tutorialSequence.Append(handCanvasGroup.DOFade(0, 0.5f));
            _tutorialSequence.AppendInterval(1f);

            _tutorialSequence.SetLoops(-1);
        }
    }

    private void StopTutorialAnimation()
    {
        if (_tutorialSequence != null && _tutorialSequence.IsActive())
        {
            _tutorialSequence.Kill();
        }
        if (_tutorialHand != null)
            _tutorialHand.SetActive(false);
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