using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class FlowFieldManager : MonoBehaviour
{
    public static FlowFieldManager Instance { get; private set; }

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Grid")]
    [SerializeField] private int width = 150;
    [SerializeField] private int height = 150;
    [SerializeField] private float cellSize = 1f;

    [Header("Terrain")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float raycastHeight = 10f;
    [SerializeField] private float raycastDistance = 30f;
    [SerializeField, Range(0f, 89f)] private float maxSlopeAngle = 45f;
    [SerializeField] private float maxStepHeight = 0.75f;

    [Header("Obstacles")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float obstacleCheckHeight = 2f;

    [Header("Rebuild")]
    [SerializeField, Min(0f)] private float rebuildInterval = 0.1f;
    [SerializeField, Min(0f)] private float targetUpdateDistance = 2f;

    private int totalCellCount;

    private bool[] walkable;
    private float[] heights;
    private Vector3[] worldPositions;
    private Vector3[] groundNormals;
    private byte[] connectionMasks;

    private NativeArray<float> nativeHeights;
    private NativeArray<float3> nativeWorldPositions;
    private NativeArray<byte> nativeConnectionMasks;
    private NativeArray<byte> nativeWalkable;

    private NativeArray<int> nativeCosts;
    private NativeArray<int> nativeCostVersions;
    private NativeArray<int> nativeProcessedVersions;

    private NativeArray<int> nativeReachableCells;
    private NativeArray<int> nativeReachableCount;

    private NativeArray<int> nativeHeapIndices;
    private NativeArray<int> nativeHeapCosts;

    private NativeArray<float3> directions;
    private NativeArray<float3> buildingDirections;

    private NativeArray<int> directionVersions;
    private NativeArray<int> buildingDirectionVersions;

    private int currentVersion;
    private int publishedVersion;
    private int heapCapacity;

    private JobHandle integrationJob;
    private JobHandle directionJob;

    private bool integrationScheduled;
    private bool directionScheduled;

    private Vector2Int targetCellPosition;
    private bool hasTargetCell;
    private bool hasValidFlowField;

    private Vector3 lastRebuildTargetPosition;
    private bool hasLastRebuildTargetPosition;

    private bool rebuildRequested;
    private float nextRebuildTime;

    private static readonly int[] NeighborX =
    {
        -1, 1, 0, 0,
        -1, -1, 1, 1
    };

    private static readonly int[] NeighborZ =
    {
        0, 0, -1, 1,
        -1, 1, -1, 1
    };

    private static readonly byte[] NeighborBits =
    {
        1 << 0,
        1 << 1,
        1 << 2,
        1 << 3,
        1 << 4,
        1 << 5,
        1 << 6,
        1 << 7
    };

    public Transform Target => target;
    public float CellSize => cellSize;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        totalCellCount = width * height;

        AllocateManagedArrays();
        AllocateNativeArrays();
        CreateGrid();
    }

    private void Start()
    {
        RebuildFlowField();
    }

    private void Update()
    {
        UpdateTarget();
        ProcessJobs();
        ProcessRebuildRequest();
    }

    private void OnDestroy()
    {
        CompleteJobs();
        DisposeNativeArrays();

        if (Instance == this)
            Instance = null;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        hasTargetCell = false;
        hasValidFlowField = false;
        hasLastRebuildTargetPosition = false;
        rebuildRequested = false;

        RebuildFlowField();
    }

    private void UpdateTarget()
    {
        if (target == null)
            return;

        Vector3 targetPosition = target.position;

        if (!hasLastRebuildTargetPosition)
        {
            rebuildRequested = true;
            return;
        }

        float distanceSqr =
            (targetPosition - lastRebuildTargetPosition).sqrMagnitude;

        float updateDistanceSqr =
            targetUpdateDistance * targetUpdateDistance;

        if (distanceSqr < updateDistanceSqr)
            return;

        rebuildRequested = true;
    }

    private void ProcessJobs()
    {
        if (integrationScheduled &&
            integrationJob.IsCompleted)
        {
            integrationJob.Complete();
            integrationScheduled = false;

            ScheduleDirectionJob();
        }

        if (directionScheduled &&
            directionJob.IsCompleted)
        {
            directionJob.Complete();
            directionScheduled = false;

            PublishDirectionField();
        }
    }

    private void ProcessRebuildRequest()
    {
        if (!rebuildRequested)
            return;

        if (integrationScheduled ||
            directionScheduled)
            return;

        if (Time.time < nextRebuildTime)
            return;

        if (target == null)
        {
            rebuildRequested = false;
            return;
        }

        if (!TryGetCellCoordinates(
                target.position,
                out int targetX,
                out int targetZ))
        {
            return;
        }

        int targetIndex =
            GetIndex(targetX, targetZ);

        if (!walkable[targetIndex])
            return;

        targetCellPosition =
            new Vector2Int(targetX, targetZ);

        hasTargetCell = true;

        rebuildRequested = false;

        lastRebuildTargetPosition =
            target.position;

        hasLastRebuildTargetPosition = true;

        nextRebuildTime =
            Time.time + rebuildInterval;

        ScheduleIntegrationJob(targetIndex);
    }

    public void RebuildFlowField()
    {
        if (target == null ||
            walkable == null)
        {
            return;
        }

        rebuildRequested = true;

        ProcessRebuildRequest();
    }

    private void ScheduleIntegrationJob(int targetIndex)
    {
        int buildVersion =
            GetNextVersion();

        nativeReachableCount[0] = 0;

        IntegrationJob integration =
            new IntegrationJob
            {
                Width = width,
                CellSize = cellSize,
                TargetIndex = targetIndex,
                Version = buildVersion,

                Heights = nativeHeights,
                ConnectionMasks = nativeConnectionMasks,

                Costs = nativeCosts,
                CostVersions = nativeCostVersions,
                ProcessedVersions = nativeProcessedVersions,

                ReachableCells = nativeReachableCells,
                ReachableCount = nativeReachableCount,

                HeapIndices = nativeHeapIndices,
                HeapCosts = nativeHeapCosts
            };

        integrationJob =
            integration.Schedule();

        integrationScheduled = true;
    }

    private void ScheduleDirectionJob()
    {
        int buildVersion =
            currentVersion;

        DirectionJob direction =
            new DirectionJob
            {
                Width = width,
                Version = buildVersion,

                WorldPositions = nativeWorldPositions,
                ConnectionMasks = nativeConnectionMasks,

                Costs = nativeCosts,
                CostVersions = nativeCostVersions,

                Directions = buildingDirections,
                DirectionVersions = buildingDirectionVersions
            };

        directionJob =
            direction.Schedule(
                totalCellCount,
                64);

        directionScheduled = true;
    }

    private void PublishDirectionField()
    {
        SwapDirectionBuffers(currentVersion);

        hasValidFlowField = true;
    }

    private void CompleteJobs()
    {
        if (integrationScheduled)
        {
            integrationJob.Complete();
            integrationScheduled = false;
        }

        if (directionScheduled)
        {
            directionJob.Complete();
            directionScheduled = false;
        }
    }

    private int GetNextVersion()
    {
        if (currentVersion >= int.MaxValue - 1)
        {
            ResetVersions();
            currentVersion = 1;
        }
        else
        {
            currentVersion++;
        }

        return currentVersion;
    }

    private void SwapDirectionBuffers(int version)
    {
        NativeArray<float3> tempDirections = directions;
        directions = buildingDirections;
        buildingDirections = tempDirections;

        NativeArray<int> tempVersions = directionVersions;
        directionVersions = buildingDirectionVersions;
        buildingDirectionVersions = tempVersions;

        publishedVersion = version;
    }

    private void AllocateManagedArrays()
    {
        walkable = new bool[totalCellCount];
        heights = new float[totalCellCount];
        worldPositions = new Vector3[totalCellCount];
        groundNormals = new Vector3[totalCellCount];
        connectionMasks = new byte[totalCellCount];
    }

    private void AllocateNativeArrays()
    {
        nativeWalkable = new NativeArray<byte>(totalCellCount, Allocator.Persistent);
        nativeHeights = new NativeArray<float>(totalCellCount, Allocator.Persistent);
        nativeWorldPositions = new NativeArray<float3>(totalCellCount, Allocator.Persistent);
        nativeConnectionMasks = new NativeArray<byte>(totalCellCount, Allocator.Persistent);

        nativeCosts = new NativeArray<int>(totalCellCount, Allocator.Persistent);
        nativeCostVersions = new NativeArray<int>(totalCellCount, Allocator.Persistent);
        nativeProcessedVersions = new NativeArray<int>(totalCellCount, Allocator.Persistent);

        nativeReachableCells = new NativeArray<int>(totalCellCount, Allocator.Persistent);
        nativeReachableCount = new NativeArray<int>(1, Allocator.Persistent);

        heapCapacity = Mathf.Max(totalCellCount * 8, 64);

        nativeHeapIndices = new NativeArray<int>(heapCapacity, Allocator.Persistent);
        nativeHeapCosts = new NativeArray<int>(heapCapacity, Allocator.Persistent);

        directions = new NativeArray<float3>(totalCellCount, Allocator.Persistent);
        buildingDirections = new NativeArray<float3>(totalCellCount, Allocator.Persistent);

        directionVersions = new NativeArray<int>(totalCellCount, Allocator.Persistent);
        buildingDirectionVersions = new NativeArray<int>(totalCellCount, Allocator.Persistent);
    }

    private void DisposeNativeArrays()
    {
        if (nativeWalkable.IsCreated)
            nativeWalkable.Dispose();

        if (nativeHeights.IsCreated)
            nativeHeights.Dispose();

        if (nativeWorldPositions.IsCreated)
            nativeWorldPositions.Dispose();

        if (nativeConnectionMasks.IsCreated)
            nativeConnectionMasks.Dispose();

        if (nativeCosts.IsCreated)
            nativeCosts.Dispose();

        if (nativeCostVersions.IsCreated)
            nativeCostVersions.Dispose();

        if (nativeProcessedVersions.IsCreated)
            nativeProcessedVersions.Dispose();

        if (nativeReachableCells.IsCreated)
            nativeReachableCells.Dispose();

        if (nativeReachableCount.IsCreated)
            nativeReachableCount.Dispose();

        if (nativeHeapIndices.IsCreated)
            nativeHeapIndices.Dispose();

        if (nativeHeapCosts.IsCreated)
            nativeHeapCosts.Dispose();

        if (directions.IsCreated)
            directions.Dispose();

        if (buildingDirections.IsCreated)
            buildingDirections.Dispose();

        if (directionVersions.IsCreated)
            directionVersions.Dispose();

        if (buildingDirectionVersions.IsCreated)
            buildingDirectionVersions.Dispose();
    }

    private void ResetVersions()
    {
        for (int i = 0; i < totalCellCount; i++)
        {
            nativeCostVersions[i] = 0;
            nativeProcessedVersions[i] = 0;
            directionVersions[i] = 0;
            buildingDirectionVersions[i] = 0;
        }
    }

    private void CreateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                int index = GetIndex(x, z);
                Vector3 cellPosition = GetGridPosition(x, z);

                worldPositions[index] = cellPosition;

                bool hasGround =
                    TryGetGround(cellPosition, out RaycastHit hit);

                if (!hasGround)
                {
                    walkable[index] = false;
                    heights[index] = 0f;
                    groundNormals[index] = Vector3.up;
                    continue;
                }

                float slopeAngle =
                    Vector3.Angle(hit.normal, Vector3.up);

                bool validSlope =
                    slopeAngle <= maxSlopeAngle;

                bool blocked =
                    IsBlocked(hit.point);

                walkable[index] =
                    validSlope && !blocked;

                heights[index] = hit.point.y;
                worldPositions[index] = hit.point;
                groundNormals[index] = hit.normal;
            }
        }

        BuildConnections();

        for (int i = 0; i < totalCellCount; i++)
        {
            nativeWalkable[i] =
                walkable[i] ? (byte)1 : (byte)0;

            nativeHeights[i] = heights[i];
            nativeWorldPositions[i] = worldPositions[i];
            nativeConnectionMasks[i] = connectionMasks[i];
        }
    }

    private void BuildConnections()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                int index = GetIndex(x, z);

                if (!walkable[index])
                {
                    connectionMasks[index] = 0;
                    continue;
                }

                byte mask = 0;

                for (int direction = 0; direction < 8; direction++)
                {
                    int neighborX =
                        x + NeighborX[direction];

                    int neighborZ =
                        z + NeighborZ[direction];

                    if (neighborX < 0 ||
                        neighborX >= width ||
                        neighborZ < 0 ||
                        neighborZ >= height)
                    {
                        continue;
                    }

                    int neighborIndex =
                        GetIndex(neighborX, neighborZ);

                    if (!CanMoveBetween(index, neighborIndex))
                        continue;

                    mask |= NeighborBits[direction];
                }

                connectionMasks[index] = mask;
            }
        }
    }

    private bool CanMoveBetween(int fromIndex, int toIndex)
    {
        if (!walkable[fromIndex] ||
            !walkable[toIndex])
        {
            return false;
        }

        float heightDifference =
            Mathf.Abs(
                heights[fromIndex] -
                heights[toIndex]);

        if (heightDifference > maxStepHeight)
            return false;

        int fromX = fromIndex % width;
        int fromZ = fromIndex / width;

        int toX = toIndex % width;
        int toZ = toIndex / width;

        bool diagonal =
            fromX != toX &&
            fromZ != toZ;

        if (!diagonal)
            return true;

        int dx = toX - fromX;
        int dz = toZ - fromZ;

        int horizontalIndex =
            GetIndex(fromX + dx, fromZ);

        int verticalIndex =
            GetIndex(fromX, fromZ + dz);

        if (!walkable[horizontalIndex] ||
            !walkable[verticalIndex])
        {
            return false;
        }

        if (Mathf.Abs(
                heights[fromIndex] -
                heights[horizontalIndex]) >
            maxStepHeight)
        {
            return false;
        }

        if (Mathf.Abs(
                heights[fromIndex] -
                heights[verticalIndex]) >
            maxStepHeight)
        {
            return false;
        }

        return true;
    }

    private bool TryGetCellCoordinates(
        Vector3 worldPosition,
        out int x,
        out int z)
    {
        Vector3 localPosition =
            worldPosition - transform.position;

        x =
            Mathf.FloorToInt(
                localPosition.x / cellSize);

        z =
            Mathf.FloorToInt(
                localPosition.z / cellSize);

        if (x < 0 ||
            x >= width ||
            z < 0 ||
            z >= height)
        {
            x = 0;
            z = 0;
            return false;
        }

        return true;
    }

    private int GetIndex(int x, int z)
    {
        return x + z * width;
    }

    private Vector3 GetGridPosition(int x, int z)
    {
        return transform.position +
               new Vector3(
                   (x + 0.5f) * cellSize,
                   0f,
                   (z + 0.5f) * cellSize);
    }

    private bool TryGetGround(
        Vector3 position,
        out RaycastHit hit)
    {
        Vector3 origin =
            position +
            Vector3.up * raycastHeight;

        return Physics.Raycast(
            origin,
            Vector3.down,
            out hit,
            raycastDistance,
            groundMask,
            QueryTriggerInteraction.Ignore);
    }

    private bool IsBlocked(Vector3 groundPosition)
    {
        Vector3 center =
            groundPosition +
            Vector3.up *
            (obstacleCheckHeight * 0.5f);

        Vector3 halfExtents =
            new Vector3(
                cellSize * 0.5f,
                obstacleCheckHeight * 0.5f,
                cellSize * 0.5f);

        return Physics.CheckBox(
            center,
            halfExtents,
            Quaternion.identity,
            obstacleMask,
            QueryTriggerInteraction.Ignore);
    }

    public Vector3 GetDirection(Vector3 worldPosition)
    {
        if (!hasValidFlowField)
            return Vector3.zero;

        if (!TryGetCellCoordinates(
                worldPosition,
                out int x,
                out int z))
        {
            return Vector3.zero;
        }

        int index = GetIndex(x, z);

        if (directionVersions[index] != publishedVersion)
            return Vector3.zero;

        float3 direction = directions[index];

        if (math.lengthsq(direction) < 0.0001f)
            return Vector3.zero;

        return new Vector3(
            direction.x,
            direction.y,
            direction.z);
    }

    public bool TryGetGround(
        Vector3 worldPosition,
        out Vector3 groundPosition,
        out Vector3 groundNormal)
    {
        if (TryGetGround(
                worldPosition,
                out RaycastHit hit))
        {
            groundPosition = hit.point;
            groundNormal = hit.normal;
            return true;
        }

        groundPosition = worldPosition;
        groundNormal = Vector3.up;

        return false;
    }

    public bool TryGetCell(
        Vector3 worldPosition,
        out FlowFieldCell cell)
    {
        if (!TryGetCellCoordinates(
                worldPosition,
                out int x,
                out int z))
        {
            cell = null;
            return false;
        }

        int index = GetIndex(x, z);

        cell =
            new FlowFieldCell(
                x,
                z,
                worldPositions[index],
                heights[index],
                groundNormals[index],
                walkable[index]);

        cell.Cost =
            nativeCostVersions[index] == publishedVersion
                ? nativeCosts[index]
                : int.MaxValue;

        cell.Direction =
            GetDirection(worldPosition);

        return true;
    }

    public bool IsWalkable(Vector3 worldPosition)
    {
        if (!TryGetCellCoordinates(
                worldPosition,
                out int x,
                out int z))
        {
            return false;
        }

        return walkable[GetIndex(x, z)];
    }

    public bool TryGetRecoveryDirection(
        Vector3 worldPosition,
        out Vector3 direction)
    {
        direction = Vector3.zero;

        if (!TryGetCellCoordinates(
                worldPosition,
                out int centerX,
                out int centerZ))
        {
            return false;
        }

        int centerIndex =
            GetIndex(centerX, centerZ);

        if (walkable[centerIndex])
        {
            direction =
                GetDirection(worldPosition);

            return direction.sqrMagnitude >
                   0.0001f;
        }

        int bestIndex = -1;
        float bestDistanceSqr = float.MaxValue;
        int maxSearchRadius = 6;

        for (int radius = 1;
             radius <= maxSearchRadius;
             radius++)
        {
            int minX =
                Mathf.Max(
                    0,
                    centerX - radius);

            int maxX =
                Mathf.Min(
                    width - 1,
                    centerX + radius);

            int minZ =
                Mathf.Max(
                    0,
                    centerZ - radius);

            int maxZ =
                Mathf.Min(
                    height - 1,
                    centerZ + radius);

            for (int x = minX;
                 x <= maxX;
                 x++)
            {
                for (int z = minZ;
                     z <= maxZ;
                     z++)
                {
                    if (x != minX &&
                        x != maxX &&
                        z != minZ &&
                        z != maxZ)
                    {
                        continue;
                    }

                    int index =
                        GetIndex(x, z);

                    if (!walkable[index])
                        continue;

                    Vector3 offset =
                        worldPositions[index] -
                        worldPosition;

                    offset.y = 0f;

                    float distanceSqr =
                        offset.sqrMagnitude;

                    if (distanceSqr >= bestDistanceSqr)
                        continue;

                    bestDistanceSqr = distanceSqr;
                    bestIndex = index;
                }
            }

            if (bestIndex >= 0)
                break;
        }

        if (bestIndex < 0)
            return false;

        direction =
            worldPositions[bestIndex] -
            worldPosition;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return false;

        direction.Normalize();

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (walkable == null)
            return;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                int index = GetIndex(x, z);

                Gizmos.color =
                    walkable[index]
                        ? Color.white
                        : Color.red;

                Gizmos.DrawWireCube(
                    worldPositions[index],
                    new Vector3(
                        cellSize,
                        0.05f,
                        cellSize));

                if (!walkable[index])
                    continue;

                Vector3 direction =
                    GetDirection(
                        worldPositions[index]);

                if (direction.sqrMagnitude < 0.001f)
                    continue;

                Gizmos.DrawLine(
                    worldPositions[index],
                    worldPositions[index] +
                    direction *
                    cellSize *
                    0.4f);
            }
        }
    }

    [BurstCompile]
    private struct IntegrationJob : IJob
    {
        public int Width;
        public float CellSize;
        public int TargetIndex;
        public int Version;

        [ReadOnly]
        public NativeArray<float> Heights;

        [ReadOnly]
        public NativeArray<byte> ConnectionMasks;

        public NativeArray<int> Costs;
        public NativeArray<int> CostVersions;
        public NativeArray<int> ProcessedVersions;

        public NativeArray<int> ReachableCells;
        public NativeArray<int> ReachableCount;

        public NativeArray<int> HeapIndices;
        public NativeArray<int> HeapCosts;

        private int heapCount;

        public void Execute()
        {
            heapCount = 0;

            SetCost(
                TargetIndex,
                0);

            HeapPush(
                TargetIndex,
                0);

            int reachableCount = 0;

            while (HeapPop(
                       out int currentIndex,
                       out int currentCost))
            {
                if (ProcessedVersions[currentIndex] ==
                    Version)
                {
                    continue;
                }

                if (!HasCost(currentIndex) ||
                    currentCost != Costs[currentIndex])
                {
                    continue;
                }

                ProcessedVersions[currentIndex] = Version;

                ReachableCells[
                    reachableCount++] =
                    currentIndex;

                ProcessCell(
                    currentIndex,
                    currentCost);
            }

            ReachableCount[0] =
                reachableCount;
        }

        private void ProcessCell(
            int currentIndex,
            int currentCost)
        {
            int x =
                currentIndex % Width;

            int z =
                currentIndex / Width;

            byte mask =
                ConnectionMasks[currentIndex];

            for (int direction = 0;
                 direction < 8;
                 direction++)
            {
                byte bit =
                    GetNeighborBit(direction);

                if ((mask & bit) == 0)
                    continue;

                int neighborX =
                    x +
                    GetNeighborX(direction);

                int neighborZ =
                    z +
                    GetNeighborZ(direction);

                int neighborIndex =
                    neighborX +
                    neighborZ * Width;

                int movementCost =
                    GetMovementCost(
                        currentIndex,
                        neighborIndex,
                        direction);

                int newCost =
                    currentCost +
                    movementCost;

                if (HasCost(neighborIndex) &&
                    newCost >= Costs[neighborIndex])
                {
                    continue;
                }

                SetCost(
                    neighborIndex,
                    newCost);

                HeapPush(
                    neighborIndex,
                    newCost);
            }
        }

        private int GetMovementCost(
            int fromIndex,
            int toIndex,
            int direction)
        {
            int baseCost =
                direction >= 4
                    ? 14
                    : 10;

            float heightDifference =
                math.abs(
                    Heights[fromIndex] -
                    Heights[toIndex]);

            float slopePenalty =
                heightDifference /
                math.max(
                    CellSize,
                    0.001f);

            return baseCost +
                   (int)math.round(
                       slopePenalty * 2f);
        }

        private bool HasCost(int index)
        {
            return CostVersions[index] == Version;
        }

        private void SetCost(
            int index,
            int cost)
        {
            CostVersions[index] = Version;
            Costs[index] = cost;
        }

        private void HeapPush(
            int index,
            int cost)
        {
            if (heapCount >= HeapIndices.Length)
                return;

            int position = heapCount++;

            while (position > 0)
            {
                int parent =
                    (position - 1) >> 1;

                if (HeapCosts[parent] <= cost)
                    break;

                HeapIndices[position] =
                    HeapIndices[parent];

                HeapCosts[position] =
                    HeapCosts[parent];

                position = parent;
            }

            HeapIndices[position] = index;
            HeapCosts[position] = cost;
        }

        private bool HeapPop(
            out int index,
            out int cost)
        {
            if (heapCount <= 0)
            {
                index = -1;
                cost = 0;
                return false;
            }

            index = HeapIndices[0];
            cost = HeapCosts[0];

            heapCount--;

            if (heapCount <= 0)
                return true;

            int lastIndex =
                HeapIndices[heapCount];

            int lastCost =
                HeapCosts[heapCount];

            int position = 0;

            while (true)
            {
                int left =
                    position * 2 + 1;

                if (left >= heapCount)
                    break;

                int right = left + 1;
                int smallest = left;

                if (right < heapCount &&
                    HeapCosts[right] <
                    HeapCosts[left])
                {
                    smallest = right;
                }

                if (HeapCosts[smallest] >= lastCost)
                    break;

                HeapIndices[position] =
                    HeapIndices[smallest];

                HeapCosts[position] =
                    HeapCosts[smallest];

                position = smallest;
            }

            HeapIndices[position] = lastIndex;
            HeapCosts[position] = lastCost;

            return true;
        }

        private static int GetNeighborX(int direction)
        {
            switch (direction)
            {
                case 0: return -1;
                case 1: return 1;
                default: return direction >= 6 ? 1 : 0;
            }
        }

        private static int GetNeighborZ(int direction)
        {
            switch (direction)
            {
                case 2: return -1;
                case 3: return 1;
                case 4:
                case 6: return -1;
                case 5:
                case 7: return 1;
                default: return 0;
            }
        }

        private static byte GetNeighborBit(int direction)
        {
            return (byte)(1 << direction);
        }
    }

    [BurstCompile]
    private struct DirectionJob : IJobParallelFor
    {
        public int Width;
        public int Version;

        [ReadOnly]
        public NativeArray<float3> WorldPositions;

        [ReadOnly]
        public NativeArray<byte> ConnectionMasks;

        [ReadOnly]
        public NativeArray<int> Costs;

        [ReadOnly]
        public NativeArray<int> CostVersions;

        public NativeArray<float3> Directions;
        public NativeArray<int> DirectionVersions;

        public void Execute(int index)
        {
            if (CostVersions[index] != Version)
            {
                Directions[index] = float3.zero;
                DirectionVersions[index] = 0;
                return;
            }

            int x = index % Width;
            int z = index / Width;

            byte mask = ConnectionMasks[index];

            int currentCost = Costs[index];
            int bestCost = currentCost;
            int bestIndex = -1;

            for (int direction = 0; direction < 8; direction++)
            {
                byte bit = (byte)(1 << direction);

                if ((mask & bit) == 0)
                    continue;

                int neighborX =
                    x + GetNeighborX(direction);

                int neighborZ =
                    z + GetNeighborZ(direction);

                int neighborIndex =
                    neighborX +
                    neighborZ * Width;

                if (CostVersions[neighborIndex] != Version)
                    continue;

                int neighborCost =
                    Costs[neighborIndex];

                if (neighborCost >= bestCost)
                    continue;

                bestCost = neighborCost;
                bestIndex = neighborIndex;
            }

            if (bestIndex < 0)
            {
                Directions[index] = float3.zero;
                DirectionVersions[index] = Version;
                return;
            }

            float3 directionVector =
                WorldPositions[bestIndex] -
                WorldPositions[index];

            directionVector.y = 0f;

            if (math.lengthsq(directionVector) < 0.0001f)
            {
                Directions[index] = float3.zero;
                DirectionVersions[index] = Version;
                return;
            }

            Directions[index] =
                math.normalize(directionVector);

            DirectionVersions[index] =
                Version;
        }

        private static int GetNeighborX(int direction)
        {
            switch (direction)
            {
                case 0: return -1;
                case 1: return 1;
                default: return direction >= 6 ? 1 : 0;
            }
        }

        private static int GetNeighborZ(int direction)
        {
            switch (direction)
            {
                case 2: return -1;
                case 3: return 1;
                case 4:
                case 6: return -1;
                case 5:
                case 7: return 1;
                default: return 0;
            }
        }
    }
}