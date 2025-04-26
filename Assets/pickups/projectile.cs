using UnityEngine;

public class projectile : MonoBehaviour
{
    [SerializeField] 
    private float gravity;

    [SerializeField] 
    private float frictionFactor;
    
    private Vector3 _velocity;
    
    private bool _settled;
    
    void FixedUpdate()
    {
        if (!_settled)
        {
            _velocity += gravity * Vector3.down;
            transform.Translate(_velocity);
        
            var pos = transform.position;
            var height = Terrain.activeTerrain.SampleHeight(pos);
            var normal = Terrain.activeTerrain.terrainData.GetInterpolatedNormal(pos.x, pos.z);

            if (height >= transform.position.y)
            {
                //reflect the vector across the normal and invert Y
                pos.y = height + 1.0f;
                transform.position = pos;
                _velocity = (_velocity - 2 * Vector3.Dot(_velocity, normal) * normal) * frictionFactor;
                if (_velocity.magnitude <= 0.05)
                {
                    _settled = true;
                }
            }
        }
    }

    public void SetVelocity(Vector3 v)
    {
        _velocity = v;
    }
}
