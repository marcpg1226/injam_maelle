using System.Collections;
using UnityEngine;

public class CheckpointRespawn : MonoBehaviour
{
    IEnumerator Start()
    {
        if (!CheckpointData.HasCheckpoint)
        {
            Debug.Log("CheckpointRespawn: no checkpoint stored, starting normally.");
            yield break;
        }

        Debug.Log("CheckpointRespawn: applying checkpoint at " + CheckpointData.Position);

        // Re-apply for 3 frames in case another script moves the player after Start
        for (int i = 0; i < 3; i++)
        {
            Apply();
            yield return null;
        }

        Debug.Log("CheckpointRespawn: player final position " + transform.position);
    }

    private void Apply()
    {
        Vector2 p = CheckpointData.Position;
        transform.position = new Vector3(p.x, p.y, transform.position.z);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = p;
            rb.linearVelocity = Vector2.zero;   // Unity 6; older versions use rb.velocity
        }
        Physics2D.SyncTransforms();
    }
}