using System;
using UnityEngine;

public enum GameState
{
    Exploration,
    Inspection,
    Journal
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Current State")]
    [SerializeField] private GameState currentState = GameState.Exploration;
    public GameState CurrentState => currentState;

    public static event Action<GameState> OnStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SetState(GameState.Exploration);
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
                Time.timeScale = 0f; // Freeze time for clean isolation (W1-08)
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;

            case GameState.Journal:
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
