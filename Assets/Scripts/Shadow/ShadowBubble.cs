using TMPro;
using UnityEngine;

public class ShadowBubble : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField]
    private string[] lines =
    {
        "You don't matter to them",
        "They don't care about you",
        "You're his second dish",
        "Never the first"
    };

    private int currentLine = -1;

    private bool EnsureLabel()
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        if (label == null)
            Debug.LogError("ShadowBubble: no TextMeshPro text found. Assign Label or add a TMP text as a child.", this);
        return label != null;
    }

    public void SetProgress(float p)
    {
        Debug.Log("ShadowBubble showing, object: " + gameObject.name + " in scene: " + gameObject.scene.name);
        if (!EnsureLabel()) return;

        int i = Mathf.Min(Mathf.FloorToInt(p * lines.Length), lines.Length - 1);
        if (i != currentLine)
        {
            currentLine = i;
            label.text = lines[i];
        }
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    public void Say(string text, float duration)
    {
        if (!EnsureLabel()) return;

        label.text = text;
        currentLine = -1;
        gameObject.SetActive(true);
        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), duration);
    }

    public void Hide()
    {
        currentLine = -1;
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }
}