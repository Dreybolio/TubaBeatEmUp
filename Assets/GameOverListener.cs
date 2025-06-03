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
    public void SearchForTargets()
    {
        charactersListenForDeath = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        _numCharacters = charactersListenForDeath.Length;
        foreach (var c in charactersListenForDeath)
        {
            c.OnDeath += IncrementDeaths;
        }
    }

    private IEnumerator C_GameOver()
    {
        yield return new WaitForSeconds(2f);
        OnGameOver?.Invoke();
    }
}
