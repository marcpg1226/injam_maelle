using System.Collections.Generic;
using UnityEngine;

public class ShadowManager : MonoBehaviour
{
    private struct Snapshot
    {
        public Vector2 position;
        public bool flipX;
        public float time;
    }

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private static readonly int SpawnHash = Animator.StringToHash("Spawn");

    [Header("Referencias")]
    public Transform target;
    public SpriteRenderer targetSprite;   // only used to copy which way the player faces
    [SerializeField] private ShadowBubble bubble;

    [Header("Ajustes")]
    [SerializeField] private float delaySeconds = 3f;
    [SerializeField] private float catchUpMargin = 5;

    [Header("Spawn")]
    [SerializeField] private float spawnDuration = 1f;   // set to the length of your Spawn clip

    [Header("Chase")]
    [SerializeField] private float chaseMultiplier = 1.5f;
    [SerializeField] private float chaseGapSeconds = 1f;

    private readonly Queue<Snapshot> buffer = new Queue<Snapshot>();
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private Collider2D[] cols;
    private int delayFrames;
    private bool chasing;
    private bool waitingAtSafeZone;
    private bool appeared;
    private bool spawning;
    private bool facingFlipX;
    private float spawnEndTime;
    private float stepAccumulator;
    private Vector2 shownPos;
    public AudioClip DeatchAudio;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

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

        if (anim == null)
            Debug.LogError("ShadowManager: the shadow has no Animator.", this);
        else if (!HasParam("Speed") || !HasParam("VerticalSpeed") || !HasParam("Spawn"))
            Debug.LogError("ShadowManager: the Animator needs the parameters Speed (float), VerticalSpeed (float) and Spawn (trigger).", this);
    }

    private bool HasParam(string paramName)
    {
        foreach (AnimatorControllerParameter p in anim.parameters)
            if (p.name == paramName) return true;
        return false;
    }

    private void SetCollidersEnabled(bool on)
    {
        foreach (Collider2D c in cols)
            if (c != null) c.enabled = on;
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
        appeared = true;
        sr.enabled = true;
        if (!spawning) SetCollidersEnabled(true);
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

    private void BeginSpawn()
    {
        spawning = true;
        spawnEndTime = Time.time + spawnDuration;
        if (bubble != null) bubble.Hide();
        if (anim != null) anim.SetTrigger(SpawnHash);
    }

    void FixedUpdate()
    {
        bool playerFlipped = targetSprite.flipX ^ (targetSprite.transform.lossyScale.x < 0f);

        buffer.Enqueue(new Snapshot
        {
            position = target.position,
            flipX = target.lossyScale.x < 0f,
            time = Time.fixedTime
        });

        // While the spawn animation plays, the shadow stays put and harmless
        if (spawning)
        {
            if (Time.time < spawnEndTime) return;
            spawning = false;
            SetCollidersEnabled(true);
        }

        Vector2 startPos = shownPos;
        Replay();
        UpdateAnimator((shownPos - startPos) / Time.fixedDeltaTime);
    }

    // Runs after the Animator, so a Flip X curve in a clip can't overwrite the direction
    void LateUpdate()
    {
        if (sr.enabled) sr.flipX = facingFlipX;
    }

    private void UpdateAnimator(Vector2 velocity)
    {
        if (anim == null) return;
        anim.SetFloat(SpeedHash, Mathf.Abs(velocity.x));
        anim.SetFloat(VerticalSpeedHash, velocity.y);
    }

    private void Replay()
    {
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
                        if (fakeBlocked && appeared) bubble.SetProgress(progress);
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
            Debug.Log("facing flip: " + s.flipX);
            facingFlipX = s.flipX;
            shownPos = s.position;

            if (!appeared)
            {
                // First appearance: teleport there, play the spawn animation
                appeared = true;
                rb.position = s.position;
                transform.position = s.position;
                sr.enabled = true;
                BeginSpawn();
                return;
            }

            rb.MovePosition(s.position);
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
        if (!other.CompareTag("Player")) return;
        if (!sr.enabled || spawning) return;
        if (PlayerProtection.Instance != null && PlayerProtection.Instance.IsProtected) return;

        Debug.Log("Shadow kill! shadow pos " + transform.position +
                  ", visible: " + sr.enabled +
                  ", time since load: " + Time.timeSinceLevelLoad);

        AudioManager.Instance.PlaySFX(DeatchAudio, 1, true);
        GameManager.Instance.RestartLevel();
    }
}