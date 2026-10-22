using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Scriptable Objects/PlayerData")]
public class PlayerData : ScriptableObject
{
    public float maxSpeed;
    public float acceleration;
    public float deceleration;
    public float detectionRadius;
    public LayerMask groundMask;
    public float jumpForce;
    public float wallJumpForce;
    public float wallRayLength;
    public float afterWallJumpTime = 0.5f;
    public float wallSlideSpeed = 2f;
    public float wallCoyoteTime = 0.2f;
}
