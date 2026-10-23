using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FakeSafeZone : MonoBehaviour
{
    public static readonly List<FakeSafeZone> All = new List<FakeSafeZone>();
    public static event System.Action<FakeSafeZone> Broke;

    [SerializeField] private float breakDelay = 4f;
    [SerializeField] private Sprite brokenSprite;

    private Collider2D col;
    private SpriteRenderer sr;
    private bool playerInside;
    private bool isShadowWaiting;
    private float waitTimer;

    public bool IsBroken { get; private set; }
    public bool PlayerInside => playerInside;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable() => All.Add(this);

    void OnDisable()
    {
        All.Remove(this);
        ReleasePlayer();
    }

    // --- Called by the shadow ---
    public bool Contains(Vector2 point) => col.OverlapPoint(point);

    public void ShadowWaiting(float dt)
    {
        if (IsBroken) return;
        isShadowWaiting = true;
        waitTimer += dt;
        if (waitTimer >= breakDelay) Break();
    }

    public void ShadowNotWaiting()
    {
        isShadowWaiting = false;
        waitTimer = 0f;
    }

    private void Break()
    {
        if (IsBroken) return;
        IsBroken = true;
        ReleasePlayer();
        if (brokenSprite != null && sr != null)
            sr.sprite = brokenSprite;
        Broke?.Invoke(this);
    }

    // --- Player protection ---
    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsBroken || playerInside || !other.CompareTag("Player")) return;
        playerInside = true;
        PlayerProtection.Instance.Enter();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (!IsBroken && isShadowWaiting) Break();
        else ReleasePlayer();
    }

    private void ReleasePlayer()
    {
        if (!playerInside) return;
        playerInside = false;
        if (PlayerProtection.Instance != null)
            PlayerProtection.Instance.Exit();
    }
}