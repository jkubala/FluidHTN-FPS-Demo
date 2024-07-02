using System.Collections;
using System.Collections.Generic;
using FPSDemo.FPSController;
using UnityEngine;

public class GameObjectPooler : MonoBehaviour
{
	[SerializeField] GameObject GOToPool;
	[SerializeField] int amountToPreSpawn = 5;

	Queue<GameObject> pooledGOs = new();
    List<GameObject> inUseGOs = new();

	void Start()
	{
		for (int i = 0; i < amountToPreSpawn; i++)
        {
            GameObject preSpawnedGO = CreateNewGO();
            pooledGOs.Enqueue(preSpawnedGO);
			preSpawnedGO.SetActive(false);
		}
	}

	public GameObject GetPooledGO()
	{
        if (pooledGOs.Count > 0)
        {
			var pooledGO = pooledGOs.Dequeue();
			pooledGO.SetActive(true);
            inUseGOs.Add(pooledGO);
			return pooledGO;
        }

		// If no pool objects are free, create a new GO
		var go = CreateNewGO();
        inUseGOs.Add(go);
        return go;
    }


	GameObject CreateNewGO()
	{
		var go = Instantiate(GOToPool, transform);
        
        var pooledGo = go.GetComponent<PooledGameObject>();
        if (pooledGo != null)
        {
            pooledGo.SetPool(this);
        }

        return go;
    }

    public void ReturnInstance(GameObject go)
    {
        if (inUseGOs.Contains(go))
        {
            inUseGOs.Remove(go);
			pooledGOs.Enqueue(go);
			go.SetActive(false);
        }
    }
}
