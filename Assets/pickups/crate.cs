using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class crate : MonoBehaviour
{
    [SerializeField] 
    private float upwardForce;
    
    [SerializeField] 
    private float spread;
    
    [SerializeField]
    private List<GameObject> objects;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("hit");
        if (other.CompareTag("Player"))
        {
            for (int i = 0; i < 4; i++)
            {
                var loc = transform.position;
                loc.y += 3f;
                var objectPrefab = objects[Random.Range(0, objects.Count)];
                var pickup = Instantiate(objectPrefab, loc, Quaternion.identity);
                pickup.gameObject.GetComponent<projectile>().SetVelocity(new Vector3(Random.Range(spread * -1f, spread), upwardForce, Random.Range(spread * -1f, spread)));
            }
            Destroy(gameObject);
        }
    }
}
