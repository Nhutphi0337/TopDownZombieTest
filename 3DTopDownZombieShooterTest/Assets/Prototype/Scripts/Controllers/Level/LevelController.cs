using UnityEngine;
public class LevelController : MonoBehaviour
{
    public string currentLevelSceneName; //For testing.
    public string nextLevelSceneName; //For testing.

    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private FlowFieldManager flowFieldManager;
    [SerializeField] private ZombieSpawnManager zombieSpawnManager;

    [Header("Loot")]
    [SerializeField] private LevelLootManager levelLootManager;

    [Header("Runtime")]
    [SerializeField] private Player currentPlayer;

    private GameManager gameManager;

    private void Awake()
    {
    }
    private void Start()
    {
        
    }
    public void Init(GameManager gameManager)
    {
        this.gameManager = gameManager;
        SetUp();
        StartLevel();
    }
    public void SetUp()
    {
        zombieSpawnManager.Init(gameManager.Pooler);
        zombieSpawnManager.OnZombieSpawn += OnZombieSpawn;
        zombieSpawnManager.OnClear += OnLevelClear;

        levelLootManager.Init(gameManager.Pooler);

        if (gameManager.playerPrefab == null)
            return;

        currentPlayer = Instantiate(gameManager.playerPrefab, null);
        currentPlayer.gameObject.SetActive(false);
        currentPlayer.transform.position = playerSpawnPoint.position;

        currentPlayer.OnDead += OnLevelFail;

        currentPlayer.gameObject.SetActive(true);

        gameManager.FollowCamera.Follow = currentPlayer.transform;

        gameManager.UIManager.LevelDoneUI.SetNextLevelSceneName(nextLevelSceneName);
        gameManager.UIManager.SetPlayerCallBacks(currentPlayer);
        gameManager.UIManager.PrepareUIsOnStartingLevel();

    }
    public void StartLevel()
    {
        flowFieldManager.SetTarget(currentPlayer.transform);
        currentPlayer.Init();
        zombieSpawnManager.StartLevel();
    }

    private void OnDisable()
    {
        if (currentPlayer)
        {
            currentPlayer.OnDead -= OnLevelFail;
            gameManager.UIManager.UnSetPlayerCallBacks(currentPlayer);
            Destroy(currentPlayer.gameObject);
        }
        zombieSpawnManager.OnZombieSpawn -= OnZombieSpawn;
        zombieSpawnManager.OnClear -= OnLevelClear;

        gameManager.UIManager.PrepareUIsOnDoneLevel();
    }

    public void OnZombieSpawn(Zombie zombie)
    {
        zombie.OnDead -= levelLootManager.OnZombieDead;
        zombie.OnDead += levelLootManager.OnZombieDead;

        zombie.OnDead -= zombieSpawnManager.RegisterZombieDeath;
        zombie.OnDead += zombieSpawnManager.RegisterZombieDeath;
    }

    public void OnLevelClear()
    {
        levelLootManager.DestroyAllDrops();
        gameManager.UIManager.PrepareUIsOnDoneLevel();
        gameManager.UIManager.LevelDoneUI.SetWin();
    }

    public void OnLevelFail()
    {
        levelLootManager.DestroyAllDrops();
        zombieSpawnManager.SetLevelFail();
        gameManager.UIManager.PrepareUIsOnDoneLevel();
        gameManager.UIManager.LevelDoneUI.SetLose();
    }
}