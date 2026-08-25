using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ZombieSpawnManager : MonoBehaviour
{
    [Serializable]
    private class ZombieSpawnEntry
    {
        [SerializeField] private ZombieDef zombie;
        [SerializeField] private int count = 1;
        [SerializeField] private float spawnInterval = 0.5f;

        public ZombieDef Zombie => zombie;
        public int Count => count;
        public float SpawnInterval => spawnInterval;
    }

    [Serializable]
    private class Wave
    {
        [SerializeField] private ZombieSpawnEntry[] zombies;
        [SerializeField] private float timeToNextWave = 5f;

        public ZombieSpawnEntry[] Zombies => zombies;
        public float TimeToNextWave => timeToNextWave;
    }

    [Header("Waves")]
    [SerializeField] private Wave[] waves;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Spawn Validation")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float minimumSpawnDistance = 8f;
    [SerializeField] private LayerMask visibilityBlockingLayers;
    [SerializeField] private float retryDelay = 0.1f;

    private readonly List<Transform> shuffledSpawnPoints = new();

    private IPooler pooler;
    private int nextSpawnPointIndex;

    public int TotalSpawned { get; private set; }
    public int TotalDead { get; private set; }
    public int AliveCount => TotalSpawned - TotalDead;

    public List<Zombie> CurrentZombies { get; private set; }    

    public event Action<Zombie> OnZombieSpawn;
    public event Action OnClear;

    private Coroutine spawnCou;

    private bool levelDone;
    public void Init(IPooler pooler)
    {
        this.pooler = pooler;
        CurrentZombies = new List<Zombie>();
        levelDone = false;
    }

    public void SetDone(bool done)
    {
        levelDone = done;
    }
    public void StartLevel()
    {
        TotalSpawned = 0;
        TotalDead = 0;

        spawnCou = StartCoroutine(SpawnLevelRoutine());
    }

    public void RegisterZombieDeath(Zombie zombie)
    {
        TotalDead++;
        if(AliveCount <= 0)
        {
            OnClear?.Invoke();
        }
    }

    private IEnumerator SpawnLevelRoutine()
    {
        for (int i = 0; i < waves.Length; i++)
        {
            PrepareSpawnPoints();

            yield return SpawnWaveRoutine(waves[i]);

            if (i < waves.Length - 1)
                yield return new WaitForSeconds(waves[i].TimeToNextWave);
        }
    }

    private IEnumerator SpawnWaveRoutine(Wave wave)
    {
        if (wave.Zombies == null || wave.Zombies.Length == 0)
            yield break;

        int remainingEntries = wave.Zombies.Length;

        foreach (ZombieSpawnEntry entry in wave.Zombies)
        {
            StartCoroutine(SpawnEntryRoutine(entry, () => remainingEntries--));
        }

        yield return new WaitUntil(() => remainingEntries == 0);
    }

    private IEnumerator SpawnEntryRoutine(
        ZombieSpawnEntry entry,
        Action onComplete)
    {
        if (entry.Zombie == null)
        {
            Debug.LogError("Zombie Spawn Entry has no ZombieDef.");
            onComplete?.Invoke();
            yield break;
        }

        if (entry.Count <= 0)
        {
            onComplete?.Invoke();
            yield break;
        }

        for (int i = 0; i < entry.Count; i++)
        {
            yield return SpawnZombieRoutine(entry.Zombie);

            if (i < entry.Count - 1)
                yield return new WaitForSeconds(entry.SpawnInterval);
        }

        onComplete?.Invoke();
    }

    private IEnumerator SpawnZombieRoutine(ZombieDef zombieDef)
    {
        Transform spawnPoint = null;

        while (spawnPoint == null)
        {
            spawnPoint = GetValidSpawnPoint();

            if (spawnPoint == null)
                yield return new WaitForSeconds(retryDelay);
        }

        if(levelDone)
            yield break;

        IPoolable zombiePool = pooler.Spawn(
            zombieDef.ZombiePrefab.gameObject,
            spawnPoint.position,
            spawnPoint.rotation);

        Zombie zombie = zombiePool as Zombie;

        if (zombie == null)
            yield break;

        zombie.Init(zombieDef);

        TotalSpawned++;

        CurrentZombies.Add(zombie);

        OnZombieSpawn?.Invoke(zombie);
    }

    private Transform GetValidSpawnPoint()
    {
        if (shuffledSpawnPoints.Count == 0)
        {
            Debug.LogError("ZombieSpawnManager has no spawn points.");
            return null;
        }

        int checkedPoints = 0;

        while (checkedPoints < shuffledSpawnPoints.Count)
        {
            Transform spawnPoint = shuffledSpawnPoints[nextSpawnPointIndex];

            AdvanceSpawnPoint();
            checkedPoints++;

            if (IsValidSpawnPoint(spawnPoint))
                return spawnPoint;
        }

        return null;
    }

    private bool IsValidSpawnPoint(Transform spawnPoint)
    {
        if (spawnPoint == null)
            return false;

        if (IsTooCloseToPlayer(spawnPoint))
            return false;

        if (IsVisibleToPlayer(spawnPoint))
            return false;

        return true;
    }

    private bool IsTooCloseToPlayer(Transform spawnPoint)
    {
        if (player == null)
            return false;

        Vector3 offset = spawnPoint.position - player.position;
        offset.y = 0f;

        return offset.sqrMagnitude <
               minimumSpawnDistance * minimumSpawnDistance;
    }

    private bool IsVisibleToPlayer(Transform spawnPoint)
    {
        if (playerCamera == null)
            return false;

        Vector3 viewportPosition =
            playerCamera.WorldToViewportPoint(spawnPoint.position);

        if (viewportPosition.z <= 0f)
            return false;

        if (viewportPosition.x < 0f ||
            viewportPosition.x > 1f ||
            viewportPosition.y < 0f ||
            viewportPosition.y > 1f)
            return false;

        Vector3 cameraPosition = playerCamera.transform.position;
        Vector3 direction = spawnPoint.position - cameraPosition;
        float distance = direction.magnitude;

        if (Physics.Raycast(
                cameraPosition,
                direction.normalized,
                out _,
                distance,
                visibilityBlockingLayers))
            return false;

        return true;
    }

    private void AdvanceSpawnPoint()
    {
        nextSpawnPointIndex++;

        if (nextSpawnPointIndex >= shuffledSpawnPoints.Count)
        {
            ShuffleSpawnPoints();
            nextSpawnPointIndex = 0;
        }
    }

    private void PrepareSpawnPoints()
    {
        shuffledSpawnPoints.Clear();

        if (spawnPoints == null)
            return;

        shuffledSpawnPoints.AddRange(spawnPoints);
        ShuffleSpawnPoints();

        nextSpawnPointIndex = 0;
    }

    private void ShuffleSpawnPoints()
    {
        for (int i = shuffledSpawnPoints.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);

            Transform temp = shuffledSpawnPoints[i];
            shuffledSpawnPoints[i] = shuffledSpawnPoints[randomIndex];
            shuffledSpawnPoints[randomIndex] = temp;
        }
    }

    public void SetLevelFail()
    {
        levelDone = true;
        DestroyAllZombies();
    }
    public void DestroyAllZombies()
    {
        for(int i = 0; i < CurrentZombies.Count; i++)
        {
            Destroy(CurrentZombies[i].gameObject);
        }
        CurrentZombies = null;
    }
}