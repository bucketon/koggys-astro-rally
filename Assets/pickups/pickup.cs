using System;
using UnityEngine;
using UnityEngine.Serialization;

public class pickup : MonoBehaviour
{
    [Serializable]
    public struct PickupData
    {
        public float acceleration;
        public float topSpeed;
        public float charge;
        public float turn;
        public float weight;
        public float maxCharge;
        public float speedDecay;
        public float glideThreshold;
        public float glide;
        public float glideAcceleration;
    }
    
    public PickupData pickupData;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerCharacterController>().Pickup(pickupData);
            Destroy(gameObject);
        }
    }
}
