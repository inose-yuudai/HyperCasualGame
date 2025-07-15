// EnemyData.cs の全文
using UnityEngine;

/// <summary>
/// 敵のステータスを定義するデータコンテナ。
/// ScriptableObjectにより、データアセットとしてプロジェクトに保存できる。
/// </summary>
[CreateAssetMenu(fileName = "EnemyData_New", menuName = "MyGame/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("基本ステータス")]
    [Tooltip("敵の名前")]
    public string enemyName = "Enemy";

    [Tooltip("移動速度")]
    public float moveSpeed = 2f;

    [Tooltip("体力")]
    public int health = 1;

    [Tooltip("倒した際に得られるスコア")]
    public int scoreValue = 10;

    [Header("見た目")]
    [ColorUsage(true, true), Tooltip("ダメージを受けた時の色（HDR対応）")]
    public Color damagedColor = Color.yellow; // この行を追加

    [Tooltip("敵のモデルやマテリアルなど見た目に関わる設定（必要に応じて）")]
    public Material material;
}
