using System.Collections.Generic;
using UnityEngine;

public class ShadowManager : MonoBehaviour
{
    private struct Snapshot
    {
        public Vector2 position;
        public bool flipX;
    }

    [Header("Referencias")]
    public Transform target;
    public SpriteRenderer targetSprite;

    [Header("Ajustes")]
    [SerializeField] private float delaySeconds = 3f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 6f;
    [SerializeField] private float gravity = 30f;
    [SerializeField] private LayerMask groundMask;   // only your ground/platform layers

    private readonly Queue<Snapshot> buffer = new Queue<Snapshot>();
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;
    private int delayFrames;
    private bool chasing;
    private float verticalSpeed;
    public AudioClip DeatchAudio;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        sr.enabled = false;
    }

    void OnEnable() => FakeSafeZone.Broke += OnZoneBroke;
    void OnDisable() => FakeSafeZone.Broke -= OnZoneBroke;

    void Start()
    {
        delayFrames = Mathf.RoundToInt(delaySeconds / Time.fixedDeltaTime);
    }

    private void OnZoneBroke(FakeSafeZone zone)
    {
        if (!chasing) StartChase();
    }

    void FixedUpdate()
    {
        if (chasing)
        {
            Chase();
            return;
        }

        buffer.Enqueue(new Snapshot
        {
            position = target.position,
            flipX = targetSprite.flipX
        });

        if (buffer.Count > delayFrames)
        {
            Snapshot s = buffer.Dequeue();

            // Wait outside a fake zone only while the player is inside it
            bool blocked = false;
            foreach (FakeSafeZone zone in FakeSafeZone.All)
            {
                if (!zone.IsBroken && zone.PlayerInside && zone.Contains(s.position))
                {
                    blocked = true;
                    zone.ShadowWaiting(Time.fixedDeltaTime);
                }
                else
                {
                    zone.ShadowNotWaiting();
                }
            }

            if (chasing || blocked) return;   // chasing: a zone just broke this step

            rb.MovePosition(s.position);
            sr.flipX = s.flipX;
            sr.enabled = true;
        }
    }

    private void StartChase()
    {
        chasing = true;
        verticalSpeed = 0f;
        buffer.Clear();
        sr.enabled = true;
    }

    private void Chase()
    {
        float dt = Time.fixedDeltaTime;
        Vector2 pos = rb.position;

        // Horizontal: walk toward the player's X only
        float x = Mathf.MoveTowards(pos.x, target.position.x, chaseSpeed * dt);
        sr.flipX = target.position.x < pos.x;   // flip this if your art faces the other way

        // Vertical: fall until the feet reach the ground
        verticalSpeed -= gravity * dt;
        float dy = verticalSpeed * dt;
        float y = pos.y + dy;

        if (dy < 0f)
        {
            Bounds b = col.bounds;
            float feetOffset = pos.y - b.min.y;               // pivot to feet distance
            Vector2 feet = new Vector2(b.center.x, b.min.y);
            RaycastHit2D hit = Physics2D.Raycast(
                feet + Vector2.up * 0.05f, Vector2.down, -dy + 0.05f, groundMask);

            if (hit.collider != null)
            {
                y = hit.point.y + feetOffset;
                verticalSpeed = 0f;
            }
        }

        rb.MovePosition(new Vector2(x, y));
    }

    void OnTriggerEnter2D(Collider2D other) => TryKill(other);
    void OnTriggerStay2D(Collider2D other) => TryKill(other);

    private void TryKill(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (PlayerProtection.Instance != null && PlayerProtection.Instance.IsProtected) return;

        AudioManager.Instance.PlaySFX(DeatchAudio, 1, true);
        GameManager.Instance.RestartLevel();
    }
}