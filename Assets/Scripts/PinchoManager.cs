using UnityEngine;
using UnityEngine.Tilemaps;

public class PinchoManager : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) 
        {
            GameManager.Instance.RestartLevel();
        }
    }
}
