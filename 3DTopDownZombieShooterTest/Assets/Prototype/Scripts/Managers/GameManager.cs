using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [field: SerializeField] public Player playerPrefab { get; private set; }//Just for testing
    [field: SerializeField] public Pooler Pooler { get; private set; }
    [field: SerializeField] public CinemachineVirtualCamera FollowCamera { get; private set; }
    [field: SerializeField] public UIManager UIManager { get; private set; }
    [field: SerializeField] public GameSceneManager SceneManager { get; private set; }
    [field: SerializeField] public AudioManager AudioManager { get; private set; }
    public Scene CurrentLevelScene { get; private set; }
    private void Awake()
    {
        Application.targetFrameRate = 30;
    }
    private void Start()
    {
        UIManager.LevelDoneUI.OnReplayPressed -= ReloadCurrentLevel;
        UIManager.LevelDoneUI.OnReplayPressed += ReloadCurrentLevel;

        UIManager.LevelDoneUI.OnNextLevelPressed -= LoadLevel;
        UIManager.LevelDoneUI.OnNextLevelPressed += LoadLevel;
    }
    public void ReloadCurrentLevel()
    {
        var curLvName = CurrentLevelScene.name;
        LoadLevel(curLvName);
    }
    public void LoadLevel(string sceneName)
    {
        if (CurrentLevelScene.name != null)
        {
            GameSceneManager.Instance.UnloadScene(
                CurrentLevelScene,
                (oldScene) =>
                {
                    OnLevelUnloaded(oldScene);

                    GameSceneManager.Instance.LoadScene(
                        sceneName,
                        LoadSceneMode.Additive,
                        true,
                        OnLevelLoaded);
                }
                );
        }
        else
        {
            GameSceneManager.Instance.LoadScene(
                sceneName,
                LoadSceneMode.Additive,
                true,
                OnLevelLoaded);
        }
    }
    private void OnLevelLoaded(Scene scene)
    {
        CurrentLevelScene = scene;
        Debug.Log($"Level loaded: {scene.name}");

        var lvCtrl = FindFirstObjectByType<LevelController>();
        lvCtrl.Init(this);
    }
    private void OnLevelUnloaded(Scene scene)
    {
        Debug.Log($"Level unloaded: {scene.name}");

    }
}
