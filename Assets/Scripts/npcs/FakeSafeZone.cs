using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FakeSafeZone : MonoBehaviour
{
    public static readonly List<FakeSafeZone> All = new List<FakeSafeZone>();
    public static event System.Action<FakeSafeZone> Broke;

    [SerializeField] private float breakDelay = 4f;
    [SerializeField] private Sprite brokenSprite;

    [Header("Warning effects")]
    [SerializeField] private SpriteRenderer cracksRenderer;  // child drawn over the barrier
    [SerializeField] private Sprite[] crackStages;           // small cracks -> big cracks
    [SerializeField] private float maxFlickerChance = 0.6f;

    private Collider2D col;
    private SpriteRenderer sr;
    private bool playerInside;
    private float waitTimer;

    public bool IsBroken { get; private set; }
    public bool PlayerInside => playerInside;
    public float Progress => IsBroken ? 1f : Mathf.Clamp01(waitTimer / breakDelay);

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        if (cracksRenderer != null) cracksRenderer.enabled = false;
    }

    void OnEnable() => All.Add(this);

    void OnDisable()
    {
        All.Remove(this);
        ReleasePlayer();
    }

    void Update()
    {
        if (IsBroken) return;

        float p = Progress;

        if (cracksRenderer != null)
        {
            bool show = p > 0.05f && crackStages != null && crackStages.Length > 0;
            cracksRenderer.enabled = show;
            if (show)
            {
                int stage = Mathf.Min(Mathf.FloorToInt(p * crackStages.Length), crackStages.Length - 1);
                cracksRenderer.sprite = crackStages[stage];
                Color c = cracksRenderer.color;
                c.a = Mathf.Clamp01(p * 1.5f);
                cracksRenderer.color = c;
            }
        }

        if (sr != null)
        {
            float chance = p * p * maxFlickerChance;   // rare at first, frantic near the end
            Color c = sr.color;
            c.a = (p > 0.2f && Random.value < chance) ? 0.3f : 1f;
            sr.color = c;
        }
    }

    // --- Called by the shadow ---
    public bool Contains(Vector2 point) => col.OverlapPoint(point);

    public void ShadowWaiting(float dt)
    {
        if (IsBroken) return;
        waitTimer += dt;
        if (waitTimer >= breakDelay) Break();
    }

    public void ShadowNotWaiting() => waitTimer = 0f;

    private void Break()
    {
        if (IsBroken) return;
        IsBroken = true;
        ReleasePlayer();

        if (sr != null)
        {
            if (brokenSprite != null) sr.sprite = brokenSprite;
            Color c = sr.color; c.a = 1f; sr.color = c;
        }
        if (cracksRenderer != null) cracksRenderer.enabled = false;

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
        if (other.CompareTag("Player")) ReleasePlayer();
    }

    private void ReleasePlayer()
    {
        if (!playerInside) return;
        playerInside = false;
        if (PlayerProtection.Instance != null)
            PlayerProtection.Instance.Exit();
    }
}