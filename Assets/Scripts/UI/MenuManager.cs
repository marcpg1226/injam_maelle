using System.Transactions;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MenuManager : MonoBehaviour
{
    public void LoadGameScene()
    {
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
    }

    public void ExitGame() => Application.Quit();
}