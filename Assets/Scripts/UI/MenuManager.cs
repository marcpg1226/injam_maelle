using System.Transactions;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MenuManager : MonoBehaviour
{
    
    public AudioClip Game;
    public void LoadGameScene()
    {
        AudioManager.Instance.PlayMusic(GameManager.Instance.MainMenu);
        Time.timeScale = 1f;
        //TransitionManager.Instance.LoadScene(TransitionManager.GAME_SCENE);
        SceneManager.LoadScene("Level1");
    }

    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Time.timeScale = 1f;
    }

    public void LoadMenuScene()
    {
        Time.timeScale = 1f;
        //TransitionManager.Instance.LoadScene(TransitionManager.MAIN_MENU_SCENE);
        SceneManager.LoadScene("Menu");
        AudioManager.Instance.PlayMusic(GameManager.Instance.MainMenu);
    }

    public void ExitGame() => Application.Quit();
}