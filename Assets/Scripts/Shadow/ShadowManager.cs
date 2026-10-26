using System.Collections.Generic;
using UnityEngine;

public class ShadowManager : MonoBehaviour
{
    private struct Snapshot
    {
        public Vector2 position;
        public bool flipX;
        public Sprite sprite;
        public float time;
    }

    [Header("Referencias")]
    public Transform target;
    public SpriteRenderer targetSprite;
    [SerializeField] private ShadowBubble bubble;

    [Header("Ajustes")]
    [SerializeField] private float delaySeconds = 3f;
    [SerializeField] private float catchUpMargin = 5;

    [Header("Chase")]
    [SerializeField] private float chaseMultiplier = 1.5f;
    [SerializeField] private float chaseGapSeconds = 1f;   // never closer than this (in time) behind the player

    private readonly Queue<Snapshot> buffer = new Queue<Snapshot>();
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private Collider2D[] cols;

    private void SetCollidersEnabled(bool on)
    {
        foreach (Collider2D c in cols)
            if (c != null) c.enabled = on;
    }

    private int delayFrames;
    private bool chasing;
    private bool waitingAtSafeZone;
    private float stepAccumulator;
    public AudioClip DeatchAudio;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        if (anim != null) anim.enabled = false;

        cols = GetComponentsInChildren<Collider2D>(true);
        SetCollidersEnabled(false);   // no killing while invisible

        sr.enabled = false;
        if (bubble != null) bubble.Hide();
    }

    void OnEnable() => FakeSafeZone.Broke += OnZoneBroke;
    void OnDisable() => FakeSafeZone.Broke -= OnZoneBroke;

    void Start()
    {
        delayFrames = Mathf.RoundToInt(delaySeconds / Time.fixedDeltaTime);

        if (target == null || targetSprite == null)
            Debug.LogError("ShadowManager: Target or Target Sprite is not assigned.", this);
    }

    private void OnZoneBroke(FakeSafeZone zone)
    {
        if (!chasing) StartChase();
    }

    private void StartChase()
    {
        chasing = true;
        waitingAtSafeZone = false;
        stepAccumulator = 0f;
        CompressStandstill();
        sr.enabled = true;
        SetCollidersEnabled(true);
        if (bubble != null) bubble.Say("Found you!", 2f);
    }

    // Removes the frames where the player stood still, so the shadow doesn't replay them
    private void CompressStandstill()
    {
        if (buffer.Count < 2) return;

        Snapshot[] all = buffer.ToArray();
        buffer.Clear();

        Snapshot last = all[0];
        buffer.Enqueue(last);
        for (int i = 1; i < all.Length; i++)
        {
            if ((all[i].position - last.position).sqrMagnitude < 0.0004f) continue;
            buffer.Enqueue(all[i]);
            last = all[i];
        }
    }

    void FixedUpdate()
    {
        buffer.Enqueue(new Snapshot
        {
            position = target.position,
            flipX = targetSprite.flipX,
            sprite = targetSprite.sprite,
            time = Time.fixedTime
        });

        if (chasing)
            stepAccumulator += chaseMultiplier;
        else
            stepAccumulator += buffer.Count > delayFrames + catchUpMargin ? 2 : 1;

        int steps = Mathf.FloorToInt(stepAccumulator);
        stepAccumulator -= steps;

        for (int i = 0; i < steps; i++)
        {
            if (buffer.Count == 0) break;
            if (!chasing && buffer.Count <= delayFrames) break;

            Snapshot s = buffer.Peek();

            if (!chasing)
            {
                bool blocked = IsBlocked(s.position, out float progress, out bool fakeBlocked);

                if (chasing)
                {
                    // a fake zone broke just now and the buffer was compressed
                    if (buffer.Count == 0) return;
                    s = buffer.Peek();
                }
                else if (blocked)
                {
                    if (bubble != null)
                    {
                        if (fakeBlocked && sr.enabled) bubble.SetProgress(progress);
                        else bubble.Hide();   // real zones show no text
                    }
                    return;   // frozen outside the barrier
                }
            }

            if (chasing)
            {
                if (Time.fixedTime - s.time < chaseGapSeconds) return;   // stay a bit behind

                if (IsInRealZone(s.position))
                {
                    waitingAtSafeZone = true;
                    return;   // wait at the edge of a real safe zone
                }

                if (waitingAtSafeZone)
                {
                    waitingAtSafeZone = false;
                    CompressStandstill();
                    if (buffer.Count == 0) return;
                    s = buffer.Peek();
                    if (Time.fixedTime - s.time < chaseGapSeconds) return;
                }
            }

            buffer.Dequeue();
            if (!sr.enabled)   // first appearance: teleport, don't glide
            {
                rb.position = s.position;
                transform.position = s.position;
            }
            rb.MovePosition(s.position);
            sr.flipX = s.flipX;
            sr.sprite = s.sprite;
            sr.enabled = true;
            SetCollidersEnabled(true);

            if (!chasing && bubble != null) bubble.Hide();
        }
    }

    private bool IsInRealZone(Vector2 pos)
    {
        foreach (SafeZone zone in SafeZone.All)
            if (zone.PlayerInside && zone.Contains(pos)) return true;
        return false;
    }

    private bool IsBlocked(Vector2 pos, out float progress, out bool fakeBlocked)
    {
        bool blocked = false;
        fakeBlocked = false;
        progress = 0f;

        foreach (FakeSafeZone zone in FakeSafeZone.All)
        {
            if (!zone.IsBroken && zone.PlayerInside && zone.Contains(pos))
            {
                blocked = true;
                fakeBlocked = true;
                zone.ShadowWaiting(Time.fixedDeltaTime);
                progress = Mathf.Max(progress, zone.Progress);
            }
            else
            {
                zone.ShadowNotWaiting();
            }
        }

        foreach (SafeZone zone in SafeZone.All)
            if (zone.PlayerInside && zone.Contains(pos)) blocked = true;

        return blocked;
    }

    void OnTriggerEnter2D(Collider2D other) => TryKill(other);
    void OnTriggerStay2D(Collider2D other) => TryKill(other);

    private void TryKill(Collider2D other)
    {
        if (Time.timeSinceLevelLoad < 1f) return;
        if (!other.CompareTag("Player")) return;
        if (PlayerProtection.Instance != null && PlayerProtection.Instance.IsProtected) return;

        AudioManager.Instance.PlaySFX(DeatchAudio, 1, true);
        GameManager.Instance.RestartLevel();
    }
}