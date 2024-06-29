using UnityEngine;

/**
 *	Handles the bullet hole decals:
 *	- Sets a random frame
 *	- Optionally rotates the decal
 *	
 *	(c) 2015, Jean Moreno
**/

[RequireComponent(typeof(MeshFilter))]
public class WFX_BulletHoleDecal : MonoBehaviour
{
	static private Vector2[] quadUVs = new Vector2[]{new Vector2(0,0), new Vector2(0,1), new Vector2(1,0), new Vector2(1,1)};
	
	public float lifetime = 10f;
	public Vector2 frames;
	public bool randomRotation = false;
	public bool deactivate = false;
	
	void OnEnable()
	{
		//Random UVs
		int random = Random.Range(0, (int)(frames.x*frames.y));
		int fx = (int)(random%frames.x);
		int fy = (int)(random/frames.y);
		//Set new UVs
		Vector2[] meshUvs = new Vector2[4];
		for(int i = 0; i < 4; i++)
		{
			meshUvs[i].x = (quadUVs[i].x + fx) * (1.0f/frames.x);
			meshUvs[i].y = (quadUVs[i].y + fy) * (1.0f/frames.y);
		}
		GetComponent<MeshFilter>().mesh.uv = meshUvs;
		
		//Random rotate
		if(randomRotation)
			transform.Rotate(0f,0f,Random.Range(0f,360f), Space.Self);
	}
}
