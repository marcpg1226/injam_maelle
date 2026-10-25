using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private Sprite activatedSprite;
    [SerializeField] private AudioClip activateSound;

    private SpriteRenderer sr;
    private bool activated;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerProtection>() == null) return;

        Vector2 spot = respawnPoint != null
            ? (Vector2)respawnPoint.position
            : (Vector2)other.transform.position;
        CheckpointData.Set(spot);
        Debug.Log("Checkpoint: saved " + spot + " from " + name, this);

        if (activated) return;
        activated = true;

        if (activatedSprite != null && sr != null) sr.sprite = activatedSprite;
        if (activateSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(activateSound, 1, true);
    }
}