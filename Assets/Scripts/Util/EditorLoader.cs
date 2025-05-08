using UnityEngine;
using UnityEngine.SceneManagement;

public static class EditorLoader : object
{
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void InitialLoad()
    {
        if(SceneManager.GetActiveScene() != SceneManager.GetSceneByBuildIndex(0))
        {
            SceneManager.LoadScene(0);

        }
    }
#endif
}
