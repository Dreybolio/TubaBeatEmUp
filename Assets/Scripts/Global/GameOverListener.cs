using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.GraphicsBuffer;

public class GameOverListener : Singleton<GameOverListener>
{
    private Character[] charactersListenForDeath;
    public UnityEvent OnGameOver;
    private int _numCharacters;
    private int _numDeaths = 0;

    private void IncrementDeaths()
    {
        _numDeaths++;
        if (_numDeaths == _numCharacters)
        {
            // Everyone has died.
            StartCoroutine(C_GameOver());
        }
    }
    private void DecrementDeaths()
    {
        _numDeaths--;
        if (_numDeaths < 0)
            _numDeaths = 0;
    }
    public void SearchForTargets()
    {
        charactersListenForDeath = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        _numCharacters = charactersListenForDeath.Length;
        foreach (var c in charactersListenForDeath)
        {
            c.OnDeath += IncrementDeaths;
            c.OnRevive += DecrementDeaths;
        }
    }

    private IEnumerator C_GameOver()
    {
        yield return new WaitForSeconds(2f);
        OnGameOver?.Invoke();
    }
}
