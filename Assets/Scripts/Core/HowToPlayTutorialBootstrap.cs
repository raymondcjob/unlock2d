using UnityEngine;
using UnityEngine.SceneManagement;

public static class HowToPlayTutorialBootstrap
{
    private const string HowToPlaySceneName = "HowToPlay";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != HowToPlaySceneName)
        {
            return;
        }

        BoardManager boardManager = Object.FindAnyObjectByType<BoardManager>();
        if (boardManager != null)
        {
            boardManager.enabled = false;
        }

        SaveGameManager saveGameManager = Object.FindAnyObjectByType<SaveGameManager>();
        if (saveGameManager != null)
        {
            saveGameManager.enabled = false;
        }

        if (Object.FindAnyObjectByType<HowToPlayTutorialController>() != null)
        {
            return;
        }

        GameObject controllerObject = new GameObject(nameof(HowToPlayTutorialController));
        controllerObject.AddComponent<HowToPlayTutorialController>();
    }
}
