using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SafeZone : MonoBehaviour
{
    public static readonly List<SafeZone> All = new List<SafeZone>();

    private Collider2D col;
    private bool playerInside;

    public bool PlayerInside => playerInside;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void Awake() => col = GetComponent<Collider2D>();

    void OnEnable() => All.Add(this);

    void OnDisable()
    {
        All.Remove(this);
        Release();
    }

    // Used by the shadow
    public bool Contains(Vector2 point) => col.OverlapPoint(point);

    void OnTriggerEnter2D(Collider2D other)
    {
        if (playerInside || !other.CompareTag("Player")) return;
        playerInside = true;
        PlayerProtection.Instance.Enter();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) Release();
    }

    private void Release()
    {
        if (!playerInside) return;
        playerInside = false;
        if (PlayerProtection.Instance != null)
            PlayerProtection.Instance.Exit();
    }
}