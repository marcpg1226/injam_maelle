using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float distanceLeft = 3f;     // how far it walks left from where you placed it
    [SerializeField] private float distanceRight = 3f;    // how far it walks right
    [SerializeField] private float waitAtEnds = 0f;       // seconds to pause before turning around
    [SerializeField] private bool startMovingRight = true;
    [SerializeField] private bool spriteFacesRight = true; // untick if your art faces left by default

    [Header("Kill")]
    [SerializeField] private AudioClip killSound;
    [SerializeField] private bool safeZonesProtect = false; // tick if safe zones should also protect from this enemy

    private SpriteRenderer sr;
    private float leftX;
    private float rightX;
    private int direction;
    private float waitTimer;
    private bool killed;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void Start()
    {
        leftX = transform.position.x - distanceLeft;
        rightX = transform.position.x + distanceRight;
        direction = startMovingRight ? 1 : -1;
        UpdateFlip();
    }

    void Update()
    {
        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        Vector3 pos = transform.position;
        pos.x += direction * speed * Time.deltaTime;

        if (pos.x >= rightX)
        {
            pos.x = rightX;
            Turn(-1);
        }
        else if (pos.x <= leftX)
        {
            pos.x = leftX;
            Turn(1);
        }

        transform.position = pos;
    }

    private void Turn(int newDirection)
    {
        direction = newDirection;
        waitTimer = waitAtEnds;
        UpdateFlip();
    }

    private void UpdateFlip()
    {
        if (sr == null) return;
        sr.flipX = spriteFacesRight ? direction < 0 : direction > 0;
    }

    // Works whether the enemy's collider is a trigger or not
    void OnTriggerEnter2D(Collider2D other) => TryKill(other);
    void OnTriggerStay2D(Collider2D other) => TryKill(other);
    void OnCollisionEnter2D(Collision2D c) => TryKill(c.collider);
    void OnCollisionStay2D(Collision2D c) => TryKill(c.collider);

    private void TryKill(Collider2D other)
    {
        if (killed || !other.CompareTag("Player")) return;
        if (safeZonesProtect && PlayerProtection.Instance != null && PlayerProtection.Instance.IsProtected) return;

        killed = true;   // stops it from calling RestartLevel several times in one frame
        if (killSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(killSound, 1, true);
        GameManager.Instance.RestartLevel();
    }

    // Shows the patrol range in the Scene view when the enemy is selected
    void OnDrawGizmosSelected()
    {
        float l, r;
        if (Application.isPlaying) { l = leftX; r = rightX; }
        else
        {
            l = transform.position.x - distanceLeft;
            r = transform.position.x + distanceRight;
        }

        Gizmos.color = Color.red;
        Vector3 a = new Vector3(l, transform.position.y, 0f);
        Vector3 b = new Vector3(r, transform.position.y, 0f);
        Gizmos.DrawLine(a, b);
        Gizmos.DrawWireSphere(a, 0.15f);
        Gizmos.DrawWireSphere(b, 0.15f);
    }
}