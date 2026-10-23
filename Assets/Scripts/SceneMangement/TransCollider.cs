using UnityEngine;

public class TransCollider : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !collision.gameObject.TryGetComponent<ShadowManager>(out var player))
        {
            GameManager.Instance.NextLevel();
        }
    }
}
