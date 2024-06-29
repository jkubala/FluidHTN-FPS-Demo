using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectPooler : MonoBehaviour
{
	[SerializeField] GameObject GOToPool;
	[SerializeField] int amountToPreSpawn = 5;

	List<GameObject> pooledGOs = new();

	void Start()
	{
		for (int i = 0; i < amountToPreSpawn; i++)
		{
			GameObject preSpawnedGO = Instantiate(GOToPool, transform);
			pooledGOs.Add(preSpawnedGO);
			preSpawnedGO.SetActive(false);
		}
	}

	public GameObject GetPooledGO()
	{
		foreach (GameObject pooledGO in pooledGOs)
		{
			if (!pooledGO.activeInHierarchy)
			{
				return pooledGO;
			}
		}

		// If no pool objects are free, create a new GO
		return CreateNewGO();
	}


	GameObject CreateNewGO()
	{
		GameObject tempGO = Instantiate(GOToPool, transform);
		pooledGOs.Add(tempGO);
		return tempGO;
	}
}
