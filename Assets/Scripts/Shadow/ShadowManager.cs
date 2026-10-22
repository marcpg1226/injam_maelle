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
    public Transform target;            // el player
    public SpriteRenderer targetSprite; // sprite del player

    [Header("Ajustes")]
    [SerializeField] private float delaySeconds = 3f;

    private readonly Queue<Snapshot> buffer = new Queue<Snapshot>();
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private int delayFrames;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();   // Body Type: Kinematic
        sr = GetComponent<SpriteRenderer>();
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
            flipX = targetSprite.flipX
        });

        // 2. Cuando ya hay suficiente retraso acumulado, reproducir
        if (buffer.Count > delayFrames)
        {
            Snapshot s = buffer.Dequeue();
            rb.MovePosition(s.position);
            sr.flipX = s.flipX;
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