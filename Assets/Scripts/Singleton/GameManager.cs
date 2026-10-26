
using System.Collections;
using TMPro;
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
    [SerializeField] private GameObject CanvasBetween;
    [SerializeField] private TMP_Text messageBetween;
    public float timeBetweenScenes = 2f;
    public bool isTransitioning = false;
    public string[] textBetweenScenes = {"PlaceHolder1", "PlaceHolder2", "PlaceHolder3", "PlaceHolder4"};


    private void Awake()
    {
        
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
            
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() 
    {
        if (SceneManager.GetActiveScene().name == "Menu") AudioManager.Instance.PlayMusic(MainMenu);
        textBetweenScenes = new string[SceneManager.sceneCountInBuildSettings - 1];
    }

    public void NextLevel() 
    {

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        if (SceneManager.GetActiveScene().buildIndex + 1 == SceneManager.sceneCountInBuildSettings - 1)
        {
            AudioManager.Instance.PlayMusic(Finale, true, 0.5f);
            return;
        }
        InBetweenScenes();
    }

    public void InBetweenScenes() 
    {
        Time.timeScale = 0;
        isTransitioning = true;
        messageBetween.text = textBetweenScenes[SceneManager.GetActiveScene().buildIndex];
        CanvasBetween.SetActive(true);
        StartCoroutine(BetweenScenesRoutine());
    }

    public void SkipBetweenScenes()
    { 
        StopCoroutine(BetweenScenesRoutine());
        CanvasBetween.SetActive(false);
        Time.timeScale = 1;
        isTransitioning = false;
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator BetweenScenesRoutine() 
    {
        yield return new WaitForSecondsRealtime(timeBetweenScenes);
        CanvasBetween.SetActive(false);
        Time.timeScale = 1;
        isTransitioning = false;
    }
}
