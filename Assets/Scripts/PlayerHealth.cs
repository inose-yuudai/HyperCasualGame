using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent OnDied;

    [Header("ステータス")]
    [SerializeField]
    private int _maxHealth = 5;
    public int MaxHealth => _maxHealth;

    private int _currentHealth;
    private bool _isInvincible = false; // ★ 無敵状態かどうかのフラグ

    public AudioManager _audioManager;

    public Animator _animator;

    private void Start()
    {
        _currentHealth = _maxHealth;
        OnHealthChanged.Invoke(_currentHealth);
        _audioManager = FindObjectOfType<AudioManager>();
    }

    /// <summary>
    /// ダメージを受ける処理
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        Debug.Log($"プレイヤーがダメージを受けた: {damageAmount}", this);
        if (_audioManager != null)
        {
            _audioManager.PlaySFX(SFXType.PlayerDamage);
        }

        // ★ 無敵状態ならダメージを受けない
        if (_currentHealth <= 0 || _isInvincible)
            return;

        _currentHealth -= damageAmount;
        OnHealthChanged.Invoke(_currentHealth);


        if (_currentHealth <= 0)
        {
            if (_audioManager != null)
            {
                _audioManager.PlaySFX(SFXType.PlayerDeath);
            }
            _animator.SetTrigger("Die");
            Debug.Log("プレイヤーが死亡しました", this);
            OnDied.Invoke();

        }
    }

    /// <summary>
    /// 無敵状態を設定または解除する
    /// </summary>
    public void SetInvincible(bool invincible)
    {
        _isInvincible = invincible;
        // TODO: 無敵状態の見た目の変化（点滅など）をここに追加すると良い
        Debug.Log($"プレイヤーの無敵状態: {invincible}");
    }

    public int GetInitialHealth() => _maxHealth;
}
