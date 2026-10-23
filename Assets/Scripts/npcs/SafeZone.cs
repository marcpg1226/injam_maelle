using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SafeZone : MonoBehaviour
{
    private bool playerInside;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (playerInside || !other.CompareTag("Player")) return;
        playerInside = true;
        PlayerProtection.Instance.Enter();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Release();
    }

    void OnDisable() => Release();

    private void Release()
    {
        if (!playerInside) return;
        playerInside = false;
        if (PlayerProtection.Instance != null)
            PlayerProtection.Instance.Exit();
    }
}