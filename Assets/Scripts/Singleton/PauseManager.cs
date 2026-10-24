using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{

    public static PauseManager Instance { get; private set; }
    public bool IsPaused { get; private set; } = true;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;
        if (IsPaused) Resume();
        else Pause();
    }

    public void KeyBind() 
    { 
        UIManager.Instance.OnMenuButton();
    }

    public void Pause() 
    {
        Time.timeScale = 0f;
        transform.GetChild(0).gameObject.SetActive(true);
    }

    public void Resume()
    {
        Time.timeScale = 1f;
        transform.GetChild(0).gameObject.SetActive(false);
    }

    public void LoadMenuScene()
    {
        Time.timeScale = 1f;
        //TransitionManager.Instance.LoadScene(TransitionManager.MAIN_MENU_SCENE);
        transform.GetChild(0).gameObject.SetActive(false);
        AudioManager.Instance.PlayMusic(GameManager.Instance.MainMenu);
        SceneManager.LoadScene("Menu");
    }

    public void RestartScene()
    {
        transform.GetChild(0).gameObject.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Time.timeScale = 1f;
    }
    public void ExitGame() => Application.Quit();
}
