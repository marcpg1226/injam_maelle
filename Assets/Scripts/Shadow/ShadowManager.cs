using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ShadowManager : MonoBehaviour
{
    private struct Snapshot
    {
        public Vector2 position;
        public Vector3 scale;
        public bool flipX;
        public bool isWalking;
        public bool isAereal;
    }

    [Header("Referencias")]
    public Transform target;            // el player
    public SpriteRenderer targetSprite; // sprite del player
    public Animator targetAnimator;

    [Header("Ajustes")]
    [SerializeField] private float delaySeconds = 3f;

    private readonly Queue<Snapshot> buffer = new Queue<Snapshot>();
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator animator;
    private int delayFrames;
    private Vector2 playerMove;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();   // Body Type: Kinematic
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        sr.enabled = false;                 // invisible hasta que empiece a perseguir
    }

    void Start()
    {
        delayFrames = Mathf.RoundToInt(delaySeconds / Time.fixedDeltaTime);
    }

    void FixedUpdate()
    {
        // 1. Grabar el estado actual del player
        buffer.Enqueue(new Snapshot
        {
            position = target.position,
            scale = target.localScale,
            flipX = targetSprite.flipX,
            isWalking = targetAnimator.GetBool("isWalking"),
            isAereal = targetAnimator.GetBool("isAereal")
        });

        // 2. Cuando ya hay suficiente retraso acumulado, reproducir
        if (buffer.Count > delayFrames)
        {
            Snapshot s = buffer.Dequeue();
            rb.MovePosition(s.position);
            transform.localScale = s.scale;
            sr.flipX = s.flipX;
            animator.SetBool("isWalking", s.isWalking);
            animator.SetBool("isAereal", s.isAereal);

            sr.enabled = true;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
           GameManager.Instance.RestartLevel();
        }
    }
}