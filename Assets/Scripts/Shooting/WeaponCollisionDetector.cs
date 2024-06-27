using System;
using UnityEngine;

public class WeaponCollisionDetector : MonoBehaviour
{
	public Action CollisionEntered;
	public Action CollisionExited;
	int nOfCollisions = 0;
	private void OnTriggerEnter(Collider other)
	{
		CollisionEntered?.Invoke();
		Debug.Log("ENTER" + other.name);
		nOfCollisions++;
	}

	private void OnTriggerExit(Collider other)
	{
		nOfCollisions--;
		if (nOfCollisions <= 0)
		{
			Debug.Log("EXIT" + other.name);
			CollisionExited?.Invoke();
		}
	}
}
