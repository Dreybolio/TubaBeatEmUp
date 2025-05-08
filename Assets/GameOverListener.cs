using UnityEngine;
using UnityEngine.Events;

public class GameOverListener : Singleton<GameOverListener>
{
    [SerializeField] private Character[] charactersListenForDeath;
    public UnityEvent OnGameOver;
    private int _numCharacters;
    private int _numDeaths = 0;

    private void Start()
    {
        _numCharacters = charactersListenForDeath.Length;
        foreach (var c in charactersListenForDeath)
        {
            c.OnDeath += IncrementDeaths;
        }
    }

    private void IncrementDeaths()
    {
        _numDeaths++;
        if (_numDeaths == _numCharacters)
        {
            // Everyone has died.
            OnGameOver?.Invoke();
        }
    }

}
