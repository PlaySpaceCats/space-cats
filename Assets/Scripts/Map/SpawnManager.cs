using System.Collections.Generic;
using System.Linq;
using Tanks.Utilities;
using UnityEngine;

public class SpawnManager : Singleton<SpawnManager>
{
    private readonly List<SpawnPoint> _spawnPoints = new List<SpawnPoint>();

    protected override void Awake()
    {
        base.Awake();
        LazyLoadSpawnPoints();
    }

    private void Start()
    {
        LazyLoadSpawnPoints();
    }

	private void LazyLoadSpawnPoints()
    {
        if (_spawnPoints.Count > 0)
        {
            return;
        }

        var foundSpawnPoints = GetComponentsInChildren<SpawnPoint>();
        _spawnPoints.AddRange(foundSpawnPoints);
    }

	public int GetRandomEmptySpawnPointIndex()
    {
        LazyLoadSpawnPoints();

        var emptySpawnPoints = _spawnPoints.Where(sp => sp.IsEmptyZone).ToList();

        if (emptySpawnPoints.Count == 0) return 0;

        var emptySpawnPoint = emptySpawnPoints[Random.Range(0, emptySpawnPoints.Count)];

        emptySpawnPoint.SetDirty();

        return _spawnPoints.IndexOf(emptySpawnPoint);
    }

    private SpawnPoint GetSpawnPointByIndex(int i)
    {
        LazyLoadSpawnPoints();
        return _spawnPoints[i];
    }

    public Transform GetSpawnPointTransformByIndex(int i)
    {
        return GetSpawnPointByIndex(i).SpawnPointTransform;
    }

	public void CleanupSpawnPoints()
	{
	    foreach (var point in _spawnPoints)
	    {
	        point.Cleanup();
	    }
	}
}