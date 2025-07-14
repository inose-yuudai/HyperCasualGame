using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(Animator))]
public class Enemy : MonoBehaviour
{
    // --- 既存のコード (変更なし) ---
    #region 既存の変数・メソッド
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
    private Color _originalColor; // ◀◀◀ 元の色を保存するために追加

    [SerializeField]
    private int _currentHealth;

    private static readonly int k_animatorTriggerDie = Animator.StringToHash("Die");
    public int ScoreValue => (_enemyData != null) ? _enemyData.scoreValue : 0;
    #endregion

    // --- 🔽🔽🔽 ここから追加 🔽🔽🔽 ---

    // 凍結中に受けた吹っ飛ばし情報を保存する構造体
    private struct PendingKnockback
    {
        public Vector3 Direction;
        public float Force;
    }

    private PendingKnockback? _pendingKnockback; // null許容型で保存情報があるか判断

    public bool IsFrozen { get; private set; }

    // --- 🔼🔼🔼 追加ここまで 🔼🔼🔼 ---

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _renderer = GetComponentInChildren<Renderer>();
        _currentState = EnemyState.Spawning;

        if (_renderer != null)
        {
            //_originalColor = _renderer.material.color; // ◀◀◀ 元の色をAwakeで取得
        }

        if (_enemyData == null)
        {
            Debug.LogError("EnemyDataがアタッチされていません！", this);
            enabled = false;
            return;
        }
        ApplyData();
    }

    // --- FixedUpdate, Initialize, ApplyData は変更なし ---
    #region 変更のないメソッド (一部)
    private void FixedUpdate()
    {
        if (_currentState == EnemyState.Moving && !IsFrozen) // ◀◀◀ 凍結中は移動しないように追記
        {
            MoveTowardsTarget();
        }
    }

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
    #endregion

    public void TakeDamage(int damage, Vector3 knockbackDirection, float knockbackForce)
    {
        if (_currentState == EnemyState.Dying)
            return;

        _currentHealth -= damage;

        if (_currentHealth > 0)
        {
            // 生きている場合はダメージ色に（凍結してなければ）
            if (_renderer != null && !IsFrozen)
                _renderer.material.color = _enemyData.damagedColor;
            // TODO: ダメージ色から元の色に戻す処理も必要に応じて追加
        }
        else
        {
            // HPが0以下になったらやられる処理へ
            DieAndGetBlownAway(knockbackDirection, knockbackForce);
        }
    }

    private void DieAndGetBlownAway(Vector3 knockbackDirection, float knockbackForce)
    {
        if (_currentState == EnemyState.Dying)
            return; // 既に死んでいる場合は何もしない

        SetState(EnemyState.Dying);
        OnDefeated?.Invoke(this);
        GetComponent<Collider>().enabled = false;

        if (_animator != null)
            _animator.SetTrigger(k_animatorTriggerDie);

        // --- 🔽🔽🔽 ここから修正 🔽🔽🔽 ---

        if (IsFrozen)
        {
            // 凍結中の場合、吹っ飛ばし情報を保存するだけ
            _pendingKnockback = new PendingKnockback
            {
                Direction = knockbackDirection,
                Force = knockbackForce
            };
        }
        else
        {
            // 通常時はすぐに吹っ飛ばす
            ApplyKnockback(knockbackDirection, knockbackForce);
        }

        // --- 🔼🔼🔼 修正ここまで 🔼🔼🔼 ---

        Destroy(gameObject, 3f);
    }

    private void ApplyKnockback(Vector3 direction, float force)
    {
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.AddForce(direction.normalized * force, ForceMode.Impulse);
        _rigidbody.AddTorque(transform.up * force * 0.1f, ForceMode.Impulse);
    }

    // --- 🔽🔽🔽 ここからメソッドを新規追加 🔽🔽🔽 ---

    public void Freeze()
    {
        if (IsFrozen)
            return;
        IsFrozen = true;

        if (_rigidbody != null)
        {
            // isKinematicをtrueにすることで、物理的な力を無視する状態にする
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
            _renderer.material.color = _originalColor; // 元の色に戻す
        }

        // 保存されていた吹っ飛ばし情報があれば、ここで適用する
        if (_pendingKnockback.HasValue)
        {
            ApplyKnockback(_pendingKnockback.Value.Direction, _pendingKnockback.Value.Force);
            _pendingKnockback = null; // 適用後はクリア
        }
    }

    // --- 🔼🔼🔼 追加ここまで 🔼🔼🔼 ---

    // --- OnCollisionEnter, DieByCollision, SetState は変更なし ---
    #region 変更のないメソッド (残り)
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
       // OnDefeated?.Invoke(this);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
    #endregion
}
