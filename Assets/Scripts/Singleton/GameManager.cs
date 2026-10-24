
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameObject playerPrefab;
    public GameObject shadowPrefab;
    public AudioClip MainMenu;
    public AudioClip Finale;
    public AudioClip GameMusic;


    private void Awake()
    {
        
        if (Instance != null)
        {
            Destroy(gameObject);
            
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() 
    {
        if (SceneManager.GetActiveScene().name == "Menu") AudioManager.Instance.PlayMusic(MainMenu);
    }

    public void NextLevel() 
    {

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
