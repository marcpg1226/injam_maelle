using System.Transactions;
using UnityEngine;
using UnityEngine.SceneManagement;
public class MenuManager : MonoBehaviour
{
    
    
    public void LoadGameScene()
    {
        AudioManager.Instance.PlayMusic(GameManager.Instance.MainMenu);
        Time.timeScale = 1f;
        //TransitionManager.Instance.LoadScene(TransitionManager.GAME_SCENE);
        AudioManager.Instance.PlayMusic(GameManager.Instance.GameMusic);
        SceneManager.LoadScene("Level1");
    }

    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Time.timeScale = 1f;
    }

    

    public void ExitGame() => Application.Quit();
}