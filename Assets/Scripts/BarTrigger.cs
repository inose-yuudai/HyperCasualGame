using UnityEngine;

public class BarTrigger : MonoBehaviour
{
    [SerializeField]
    private PlayerBarCombat _combat;

    private void OnTriggerEnter(Collider other)
    {
        _combat.ProcessHit(other);
    }
}
