using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class objectSpawner : NetworkBehaviour
{
    [SerializeField] 
    private float minTime;
    
    [SerializeField] 
    private float maxTime;
    
    [SerializeField]
    private List<GameObject> objects;

    private float _nextSpawnTime;

    // Update is called once per frame
    void Update()
    {
        if (!isServer)
        {
            return;
        }
        if (Time.time > _nextSpawnTime)
        {
            _nextSpawnTime = Time.time + Random.Range(minTime, maxTime);
            SpawnObject();
        }
    }

    private void SpawnObject()
    {
        var terrainSize = Terrain.activeTerrain.terrainData.size;
        var randomXY = new Vector3(Random.Range(0, terrainSize.x) + Terrain.activeTerrain.GetPosition().x, 
                                   Random.Range(0, terrainSize.y) + Terrain.activeTerrain.GetPosition().y,
                                   Random.Range(0, terrainSize.z) + Terrain.activeTerrain.GetPosition().z);
        
        SpawnObject(new Vector3(randomXY.x, 100f, randomXY.z));
    }

    private void SpawnObject(Vector3 loc)
    {
        var objectPrefab = objects[Random.Range(0, objects.Count)];
        var obj = Instantiate(objectPrefab, loc, Quaternion.identity);
        NetworkServer.Spawn(obj);
    }
}
