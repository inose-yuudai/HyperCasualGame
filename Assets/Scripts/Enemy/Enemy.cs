using System;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(Animator))]
public class Enemy : MonoBehaviour
{
    // --- 既存の変数 ---
    #region 既存の変数
    private enum EnemyState
    {
        Spawning,
        Moving,
        Dying
    }

    public event Action<Enemy> OnDefeated;

    [Header("データ")]
    [SerializeField]
    private EnemyData _enemyData;

    private EnemyState _currentState;
    private Transform _targetPlayer;
    private Rigidbody _rigidbody;
    private Animator _animator;
    private Renderer _renderer;
    private float _currentMoveSpeed;
    private Color _originalColor;

    [SerializeField]
    private int _currentHealth;

    public AudioManager _audioManager;
    private static readonly int k_animatorTriggerDie = Animator.StringToHash("Die");
    private static readonly int k_animatorTriggerHit = Animator.StringToHash("Hit"); // ★ 1. HitトリガーのIDを追加

    public int ScoreValue => (_enemyData != null) ? _enemyData.scoreValue : 0;

    private struct PendingKnockback
    {
        public Vector3 Direction;
        public float Force;
    }

    private PendingKnockback? _pendingKnockback;
    public bool IsFrozen { get; private set; }
    #endregion

    [Header("戦闘設定")]
    [SerializeField, Tooltip("ダメージを受けた際の硬直時間（秒）")]
    private float k_hitStunDuration = 0.2f;
    private bool _isHitStunned = false;
    private Coroutine _hitStunCoroutine;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _renderer = GetComponentInChildren<Renderer>();
        _currentState = EnemyState.Spawning;
        _audioManager = FindObjectOfType<AudioManager>();

        if (_enemyData == null)
        {
            Debug.LogError("EnemyDataがアタッチされていません！", this);
            enabled = false;
            return;
        }
        ApplyData();
    }

    private void FixedUpdate()
    {
        if (_currentState == EnemyState.Moving && !IsFrozen && !_isHitStunned)
        {
            MoveTowardsTarget();
        }
    }

    public void TakeDamage(int damage, Vector3 knockbackDirection, float knockbackForce)
    {
        if (_currentState == EnemyState.Dying)
            return;

        _currentHealth -= damage;

        if (_currentHealth > 0)
        {
            if (_audioManager != null)
            {
                _audioManager.PlaySFX(SFXType.EnemyDamage);
            }
            TriggerHitStun();
        }
        else
        {
            DieAndGetBlownAway(knockbackDirection, knockbackForce);
            _audioManager?.PlaySFX(SFXType.EnemyDeath);
        }
    }

    private void TriggerHitStun()
    {
        // ★ 2. アニメーショントリガーをここで起動する
        if (_animator != null)
        {
            _animator.SetTrigger(k_animatorTriggerHit);
        }

        if (_hitStunCoroutine != null)
        {
            StopCoroutine(_hitStunCoroutine);
        }
        _hitStunCoroutine = StartCoroutine(HitStunCoroutine());
    }

    private IEnumerator HitStunCoroutine()
    {
        _isHitStunned = true;
        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector3.zero;
        }

        yield return new WaitForSeconds(k_hitStunDuration);

        if (_renderer != null && !IsFrozen)
        {
            _renderer.material.color = _originalColor;
        }

        _isHitStunned = false;
        _hitStunCoroutine = null;
    }

    #region 変更のないメソッド
    public void Initialize(Transform target)
    {
        _targetPlayer = target;
        SetState(EnemyState.Moving);
        Vector3 direction = _targetPlayer.position - transform.position;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void ApplyData()
    {
        _currentMoveSpeed = _enemyData.moveSpeed;
        _currentHealth = _enemyData.health;
    }

    private void DieAndGetBlownAway(Vector3 knockbackDirection, float knockbackForce)
    {
        if (_currentState == EnemyState.Dying)
            return;

        SetState(EnemyState.Dying);
        OnDefeated?.Invoke(this);
        GetComponent<Collider>().enabled = false;

        if (_animator != null)
            _animator.SetTrigger(k_animatorTriggerDie);

        if (IsFrozen)
        {
            _pendingKnockback = new PendingKnockback
            {
                Direction = knockbackDirection,
                Force = knockbackForce
            };
        }
        else
        {
            ApplyKnockback(knockbackDirection, knockbackForce);
        }

        Destroy(gameObject, 3f);
    }

    private void ApplyKnockback(Vector3 direction, float force)
    {
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.AddForce(direction.normalized * force, ForceMode.Impulse);
        _rigidbody.AddTorque(transform.up * force * 0.1f, ForceMode.Impulse);
    }

    public void Freeze()
    {
        if (IsFrozen)
            return;
        IsFrozen = true;

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
        }
        if (_animator != null)
        {
            _animator.speed = 0f;
        }
        if (_renderer != null)
        {
            _renderer.material.color = new Color(0.5f, 0.7f, 1f); // 凍結色
        }
    }

    public void Unfreeze()
    {
        if (!IsFrozen)
            return;
        IsFrozen = false;

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false;
        }
        if (_animator != null)
        {
            _animator.speed = 1f;
        }
        if (_renderer != null)
        {
            _renderer.material.color = _originalColor;
        }

        if (_pendingKnockback.HasValue)
        {
            ApplyKnockback(_pendingKnockback.Value.Direction, _pendingKnockback.Value.Force);
            _pendingKnockback = null;
        }
    }

    private void SetState(EnemyState newState)
    {
        if (_currentState == newState)
            return;
        _currentState = newState;
    }

    private void MoveTowardsTarget()
    {
        if (_targetPlayer == null)
            return;
        Vector3 direction = _targetPlayer.position - transform.position;
        direction.y = 0;
        direction.Normalize();
        _rigidbody.linearVelocity = direction * _currentMoveSpeed;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                Time.fixedDeltaTime * 10f
            );
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Enemy collided with Player: " + collision.gameObject.name);
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1);
            }
            DieByCollision();
        }
    }

    private void DieByCollision()
    {
        SetState(EnemyState.Dying);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
    #endregion
}
