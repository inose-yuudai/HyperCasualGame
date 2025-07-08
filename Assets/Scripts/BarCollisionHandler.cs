using UnityEngine;

/// <summary>
/// BarModelの当たり判定を検知し、親のPlayerBarに通知するだけのクラス
/// </summary>
public class BarCollisionHandler : MonoBehaviour
{
    private PlayerBar _playerBar;

    private void Awake()
    {
        // 自分の親オブジェクトからPlayerBarスクリプトを探して取得
        _playerBar = GetComponentInParent<PlayerBar>();
        if (_playerBar == null)
        {
            Debug.LogError("親にPlayerBarが見つかりません！階層構造を確認してください。", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // PlayerBarに、接触した相手の情報を渡す
        _playerBar.ProcessHit(other);
    }
}
