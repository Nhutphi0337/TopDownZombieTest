using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }

    public Scene CurrentScene { get; private set; }

    public bool IsLoading { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(
        string sceneName,
        LoadSceneMode loadMode = LoadSceneMode.Additive,
        bool setActive = true,
        Action<Scene> onLoaded = null)
    {
        if (IsLoading)
        {
            Debug.LogWarning(
                $"Cannot load scene '{sceneName}' because another scene is loading.");

            return;
        }

        StartCoroutine(
            LoadSceneRoutine(
                sceneName,
                loadMode,
                setActive,
                onLoaded));
    }

    public void UnloadCurrentScene(
        Action<Scene> onUnloaded = null)
    {
        if (!CurrentScene.IsValid())
        {
            Debug.LogWarning("There is no current scene to unload.");
            return;
        }

        UnloadScene(CurrentScene, onUnloaded);
    }

    public void UnloadScene(
        Scene scene,
        Action<Scene> onUnloaded = null)
    {
        if (!scene.IsValid())
        {
            Debug.LogWarning("Cannot unload an invalid scene.");
            return;
        }

        if (!scene.isLoaded)
        {
            Debug.LogWarning(
                $"Scene '{scene.name}' is not loaded.");

            return;
        }

        if (IsLoading)
        {
            Debug.LogWarning(
                $"Cannot unload scene '{scene.name}' because another scene operation is in progress.");

            return;
        }

        StartCoroutine(
            UnloadSceneRoutine(
                scene,
                onUnloaded));
    }

    private IEnumerator LoadSceneRoutine(
        string sceneName,
        LoadSceneMode loadMode,
        bool setActive,
        Action<Scene> onLoaded)
    {
        IsLoading = true;

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(
                sceneName,
                loadMode);

        if (operation == null)
        {
            Debug.LogError(
                $"Failed to start loading scene '{sceneName}'.");

            IsLoading = false;
            yield break;
        }

        yield return operation;

        Scene loadedScene =
            SceneManager.GetSceneByName(sceneName);

        if (!loadedScene.IsValid() || !loadedScene.isLoaded)
        {
            Debug.LogError(
                $"Scene '{sceneName}' was not loaded correctly.");

            IsLoading = false;
            yield break;
        }

        CurrentScene = loadedScene;

        if (setActive)
        {
            bool success =
                SceneManager.SetActiveScene(loadedScene);

            if (!success)
            {
                Debug.LogWarning(
                    $"Failed to set '{sceneName}' as the active scene.");
            }
        }

        IsLoading = false;

        onLoaded?.Invoke(loadedScene);
    }

    private IEnumerator UnloadSceneRoutine(
        Scene scene,
        Action<Scene> onUnloaded)
    {
        IsLoading = true;

        AsyncOperation operation =
            SceneManager.UnloadSceneAsync(scene);

        if (operation == null)
        {
            Debug.LogError(
                $"Failed to start unloading scene '{scene.name}'.");

            IsLoading = false;
            yield break;
        }

        yield return operation;

        if (CurrentScene == scene)
        {
            CurrentScene = default;
        }

        IsLoading = false;

        onUnloaded?.Invoke(scene);
    }
}