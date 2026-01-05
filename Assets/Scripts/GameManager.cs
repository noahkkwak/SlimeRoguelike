using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Title, Lobby, Stage, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public GameState currentState;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 씬 전환 및 게임 상태 변경 함수
    public void ChangeState(GameState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case GameState.Title:
                SceneManager.LoadScene("Scene_Title");
                break;
            case GameState.Lobby:
                SceneManager.LoadScene("Scene_Lobby");
                break;
            case GameState.Stage:
                SceneManager.LoadScene("Scene_Stage");
                break;
        }
    }
}