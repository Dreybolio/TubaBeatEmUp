using System;
using UnityEngine;
using UnityEngine.Events;
using AdvancedSceneManager;
using AdvancedSceneManager.Models;
using Unity.VisualScripting;
using AdvancedSceneManager.Callbacks.Events;

public class Autoexec : MonoBehaviour
{
    public UnityEvent OnStart;
    public UnityEvent<SceneCollection> OnCollectionLoaded;

    private void Awake()
    {
        SceneManager.runtime.collectionOpened += OnCollectionOpened;
    }
    private void Start()
    {
        OnStart?.Invoke();
    }
    void OnCollectionOpened(SceneCollection col)
    {
        // Ensure this only happens once
        SceneManager.runtime.collectionOpened -= OnCollectionOpened;
        OnCollectionLoaded?.Invoke(col);
    }
}