using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RandomObjectSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public List<GameObject> prefabs = new List<GameObject>();

    [Header("Parent")]
    public Transform parentTransform;

    [Header("Spawn Amount")]
    public int totalToSpawn = 10; // will spawn in pairs (so 2 per step)

    [Header("Random Position Offset")]
    public Vector3 minOffset = new Vector3(-1f, 0f, -1f);
    public Vector3 maxOffset = new Vector3(1f, 0f, 1f);

    [Header("Timing")]
    public float delayBetweenPairs = 0.2f;

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        if (prefabs.Count == 0)
        {
            Debug.LogWarning("No prefabs assigned.");
            yield break;
        }

        int spawned = 0;

        while (spawned < totalToSpawn)
        {
            // Spawn two objects at once
            SpawnOne();
            spawned++;

            if (spawned < totalToSpawn)
            {
                SpawnOne();
                spawned++;
            }

            // Wait 0.2 sec between pairs
            yield return new WaitForSeconds(delayBetweenPairs);
        }
    }

    void SpawnOne()
    {
        // Pick random prefab
        GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];

        // Random offset
        Vector3 randomOffset = new Vector3(
            Random.Range(minOffset.x, maxOffset.x),
            Random.Range(minOffset.y, maxOffset.y),
            Random.Range(minOffset.z, maxOffset.z)
        );

        Vector3 spawnPos = parentTransform.position + randomOffset;

        // Random rotation
        Quaternion randomRot = Random.rotation;

        // Spawn
        Instantiate(prefab, spawnPos, randomRot, parentTransform);
    }
}
