using UnityEngine;

public class PlayerBarCombat : MonoBehaviour
{
    [Header("戦闘の制御")]
    [SerializeField]
    private int _damage = 1;

    [SerializeField]
    private float _knockbackForce = 10f;

    [SerializeField, Range(0f, 1f)]
    private float _knockbackUpwardRatio = 0.5f;

    private bool _isCombatActive = false;

    public void SetCombatActive(bool isActive)
    {
        _isCombatActive = isActive;
    }

    public void ProcessHit(Collider other)
    {
        if (!_isCombatActive)
            return;

        if (other.gameObject.CompareTag("Enemy"))
        {
            Debug.Log( "Enemy hit detected: " + other.gameObject.name, this);
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

    // `BarModel`側にColliderとRigidbodyを付けて、このメソッドを呼ぶトリガーとしてください。
    // もし当たり判定用のオブジェクトを別に設ける場合は、そのオブジェクトからこのメソッドを呼び出します。
    // ここでは、別のコンポーネントから`ProcessHit`を呼び出す設計とします。
}
