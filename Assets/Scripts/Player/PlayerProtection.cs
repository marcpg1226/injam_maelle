using UnityEngine;

public class PlayerProtection : MonoBehaviour
{
    public static PlayerProtection Instance { get; private set; }

    private int zonesInside;
    public bool IsProtected => zonesInside > 0;

    void Awake()
    {
        Instance = this;
    }

    public void Enter() => zonesInside++;
    public void Exit() => zonesInside = Mathf.Max(0, zonesInside - 1);

    // Safety net: if the level restarts or the player is disabled inside a zone
    void OnDisable() => zonesInside = 0;
}