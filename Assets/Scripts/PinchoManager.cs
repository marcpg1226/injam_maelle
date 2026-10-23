using UnityEngine;
using UnityEngine.Tilemaps;

public class PinchoManager : MonoBehaviour
{
    public AudioClip pinchoClip;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) 
        {
            AudioManager.Instance.PlaySFX(pinchoClip);
            GameManager.Instance.RestartLevel();
        }
    }
}
