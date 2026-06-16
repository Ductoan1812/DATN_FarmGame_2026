using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NightEnemySpawnManager : MonoBehaviour
{
    private const string SpawnGroupId = "night_enemy";

    [SerializeField] private bool enableNightSpawns = true;
    [SerializeField] private int maxActiveNightEnemies = 5;
    [SerializeField] private int spawnIntervalGameMinutes = 80;
    [SerializeField] private float minSpawnDistance = 5f;
    [SerializeField] private float maxSpawnDistance = 8f;

    private EventBus subscribedBus;
    private int lastSpawnMinute = -99999;
    private bool wasNight;

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        if (subscribedBus != null)
        {
            subscribedBus.Unsubscribe<GameTimeChangedPublish>(OnGameTimeChanged);
            subscribedBus.Unsubscribe<DayChangedPublish>(OnDayChanged);
            subscribedBus = null;
        }
    }

    private void Subscribe()
    {
        if (subscribedBus != null) return;
        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;
        bus.Subscribe<GameTimeChangedPublish>(OnGameTimeChanged);
        bus.Subscribe<DayChangedPublish>(OnDayChanged);
        subscribedBus = bus;
    }

    private void OnGameTimeChanged(GameTimeChangedPublish evt)
    {
        bool isNight = IsNight(evt.hour);
        if (wasNight && !isNight)
            CleanupNightEnemies();
        wasNight = isNight;

        if (!enableNightSpawns || !isNight)
            return;

        int totalMinute = evt.day * 1440 + evt.hour * 60 + evt.minute;
        if (totalMinute - lastSpawnMinute < Mathf.Max(10, spawnIntervalGameMinutes))
            return;

        if (CountActiveNightEnemies() >= maxActiveNightEnemies)
            return;

        if (TrySpawn(evt.day))
            lastSpawnMinute = totalMinute;
    }

    private void OnDayChanged(DayChangedPublish _)
    {
        wasNight = false;
        lastSpawnMinute = -99999;
        CleanupNightEnemies();
    }

    private bool TrySpawn(int day)
    {
        var gm = GameManager.Instance;
        if (gm?.EventBus == null || gm.EntityDataRegistry == null)
            return false;

        var player = FindAnyObjectByType<PlayerControler>();
        if (player == null)
            return false;

        string enemyId = RollEnemyId(day);
        var data = gm.EntityDataRegistry.GetById(enemyId);
        if (data == null)
            return false;

        ObjectType objectType = ResolveObjectType(enemyId);
        if (!TryPickSpawnPosition(player.transform.position, data, out Vector2 spawnPos))
            return false;

        var cell = new Vector3Int(Mathf.FloorToInt(spawnPos.x), Mathf.FloorToInt(spawnPos.y), 0);
        var payload = new SceneSpawnPayload
        {
            sceneName = SceneManager.GetActiveScene().name,
            markerKind = SceneMarkerKind.Enemy,
            objectType = objectType,
            cell = cell,
            spawnGroupId = SpawnGroupId,
            savePolicy = SceneEntitySavePolicy.Temporary,
            initialAmount = 1
        };

        gm.EventBus.Publish(new SpawnRequestPublish(spawnPos, objectType, data, 1, false, payload));
        return true;
    }

    private bool TryPickSpawnPosition(Vector2 playerPosition, EntityData data, out Vector2 spawnPosition)
    {
        var gm = GameManager.Instance;
        var world = gm?.WorldService;
        spawnPosition = default;
        if (world == null || data == null)
            return false;

        for (int i = 0; i < 10; i++)
        {
            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude <= 0.001f)
                direction = Vector2.right;

            Vector2 candidate = playerPosition + direction * UnityEngine.Random.Range(minSpawnDistance, maxSpawnDistance);
            var cell = new Vector2Int(Mathf.FloorToInt(candidate.x), Mathf.FloorToInt(candidate.y));
            if (world.CanPlaceAt(data.placementRule, cell, out _))
            {
                spawnPosition = candidate;
                return true;
            }
        }

        return false;
    }

    private void CleanupNightEnemies()
    {
        var gm = GameManager.Instance;
        var world = gm?.WorldService;
        var bus = gm?.EventBus;
        if (world == null || bus == null)
            return;

        var ids = new System.Collections.Generic.List<string>();
        foreach (var ep in world.GetAllPositions())
        {
            if (ep == null) continue;
            if (string.Equals(ep.spawnGroupId, SpawnGroupId, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(ep.idRuntime))
            {
                ids.Add(ep.idRuntime);
            }
        }

        foreach (string id in ids)
            bus.Publish(new DestroyEntityRequestPublish(id));
    }

    private int CountActiveNightEnemies()
    {
        var world = GameManager.Instance?.WorldService;
        if (world == null)
            return 0;

        int count = 0;
        foreach (var ep in world.GetAllPositions())
        {
            if (ep == null) continue;
            if (string.Equals(ep.spawnGroupId, SpawnGroupId, StringComparison.Ordinal))
                count++;
        }

        return count;
    }

    private static bool IsNight(int hour) => hour >= 20 || hour < 5;

    private static string RollEnemyId(int day)
    {
        float roll = UnityEngine.Random.value;
        if (day >= 21)
            return roll < 0.45f ? "enemy_orc2" : roll < 0.8f ? "enemy_orc3" : "enemy_slime3";
        if (day >= 13)
            return roll < 0.45f ? "enemy_slime3" : roll < 0.85f ? "enemy_orc1" : "enemy_orc2";
        if (day >= 6)
            return roll < 0.5f ? "enemy_slime2" : roll < 0.85f ? "enemy_slime3" : "enemy_orc1";
        return roll < 0.7f ? "enemy_slime1" : "enemy_slime2";
    }

    public static ObjectType ResolveObjectType(string entityDataId)
    {
        return entityDataId switch
        {
            "enemy_slime1" => ObjectType.EnemySlime1,
            "enemy_slime2" => ObjectType.EnemySlime2,
            "enemy_slime3" => ObjectType.EnemySlime3,
            "enemy_orc1" => ObjectType.EnemyOrc1,
            "enemy_orc2" => ObjectType.EnemyOrc2,
            "enemy_orc3" => ObjectType.EnemyOrc3,
            _ => ObjectType.Enemy01,
        };
    }
}
