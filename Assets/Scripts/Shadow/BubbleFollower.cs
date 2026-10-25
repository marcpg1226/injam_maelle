using UnityEngine;

public class BubbleFollower : MonoBehaviour
{
    public Transform target;
    public Vector3 worldOffset = new Vector3(0f, 1f, 0f);

    void LateUpdate()
    {
        Camera cam = Camera.main;   // the camera needs the MainCamera tag
        if (target == null || cam == null) return;
        transform.position = cam.WorldToScreenPoint(target.position + worldOffset);
    }
}