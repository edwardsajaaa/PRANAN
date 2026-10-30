using System;
using UnityEngine;

public enum GameState
{
    Exploration,
    Inspection,
    Journal,
    Dialogue,
    Ending,
    Menu
}

public class GameStateManager : MonoBehaviour
{
    private static GameStateManager instance;
    public static GameStateManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameStateManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GameStateManager");
                    instance = go.AddComponent<GameStateManager>();
                }
            }
            return instance;
        }
    }

    [Header("Current State")]
    [SerializeField] private GameState currentState = GameState.Exploration;
    public GameState CurrentState => currentState;

    public static event Action<GameState> OnStateChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        if (currentState != GameState.Menu)
        {
            SetState(GameState.Exploration);
        }
    }

    public void SetState(GameState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case GameState.Exploration:
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;

            case GameState.Inspection:
            case GameState.Journal:
            case GameState.Dialogue:
            case GameState.Ending:
            case GameState.Menu:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }

        OnStateChanged?.Invoke(currentState);
    }

    public bool CanPlayerMove()
    {
        return currentState == GameState.Exploration;
    }

    public bool CanLookAround()
    {
        return currentState == GameState.Exploration;
    }
}
