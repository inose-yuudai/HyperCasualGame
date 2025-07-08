using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(Animator))]
public class Enemy : MonoBehaviour
{
    // 状態を3つに簡略化
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

    [SerializeField, Tooltip("デバッグ用にインスペクターに表示される現在のHP")]
    private int _currentHealth;

    // Dieトリガーのみ使用
    private static readonly int k_animatorTriggerDie = Animator.StringToHash("Die");

    public int ScoreValue => (_enemyData != null) ? _enemyData.scoreValue : 0;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _renderer = GetComponentInChildren<Renderer>();
        _currentState = EnemyState.Spawning;

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
        // 移動状態の時のみ、移動処理を実行
        if (_currentState == EnemyState.Moving)
        {
            MoveTowardsTarget();
        }
    }

    private void ApplyData()
    {
        _currentMoveSpeed = _enemyData.moveSpeed;
        _currentHealth = _enemyData.health;
    }

    public void Initialize(Transform target)
    {
        _targetPlayer = target;
        SetState(EnemyState.Moving);
    }

    public void TakeDamage(int damage, Vector3 knockbackDirection, float knockbackForce)
    {
        if (_currentState == EnemyState.Dying)
            return;
        _currentHealth -= damage;

        if (_currentHealth > 0)
        {
            Debug.Log("ダメージを与えたが、まだ生きています。残りHP: " + _currentHealth);
            if (_renderer != null)
            {
                _renderer.material.color = _enemyData.damagedColor;
            }
        }
        else
        {
            DieAndGetBlownAway(knockbackDirection, knockbackForce);
        }
    }

    private void SetState(EnemyState newState)
    {
        if (_currentState == newState)
            return;
        _currentState = newState;
        // MoveSpeedパラメータの更新処理を削除
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
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.fixedDeltaTime * 10f
            );
        }
    }

    private void DieAndGetBlownAway(Vector3 knockbackDirection, float knockbackForce)
    {
        SetState(EnemyState.Dying);
        OnDefeated?.Invoke(this);
        GetComponent<Collider>().enabled = false;

        // Dieアニメーションを再生
        _animator.SetTrigger(k_animatorTriggerDie);

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode.Impulse);
        _rigidbody.AddTorque(transform.up * knockbackForce * 0.1f, ForceMode.Impulse);

        Destroy(gameObject, 3f);
    }
}
