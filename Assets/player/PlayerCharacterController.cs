using System;
using System.Numerics;
using Mirror;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerCharacterController : NetworkBehaviour
{
    [Header("Game Stats")]
    
    [SerializeField] private float acceleration;

    [SerializeField] private float topSpeed;

    [SerializeField] private float charge;

    [SerializeField] private float turn;

    [SerializeField] private float weight;

    [SerializeField] public float maxCharge;

    [SerializeField] private float speedDecay;

    [SerializeField] private float glideThreshold;

    [SerializeField] private float glide;
    
    [SerializeField] private float glideAcceleration;

    [Header("Constants")] 
    
    [SerializeField] private float defaultHeight;
    
    [SerializeField] private float fovFactor;

    [SerializeField] private float glideDecayFactor;

    [SerializeField] private float gravity;

    [SerializeField] private float brakeGravity;

    [SerializeField] private float fallFactor;

    [Header("Dependencies")] 

    [SerializeField] private GameObject ship;
    
    //-----------------------------------------------------------------------------

    private Vector3 _velocity;

    private float _chargeLevel;

    private float _connectedHeight;

    private float _takeOffTime = -1f;

    private float _lift;

    private float _glideEnergy;
    
    //-------------------------------------------------------------------------------------------
    
    private InputAction _moveAction;
    private InputAction _brakeAction;
    private CinemachineCamera _followCam;
    private CinemachineThirdPersonFollow _thirdPersonFollow;

    private float _startFov;
    private float _fovMomentum;

    private float _startCameraDistance;

    private HUDController _hc;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!isLocalPlayer)
        {
            return;
        }
        _moveAction = InputSystem.actions.FindAction("Move");
        _brakeAction = InputSystem.actions.FindAction("Jump");

        _followCam = FindFirstObjectByType<CinemachineCamera>();
        _thirdPersonFollow = FindFirstObjectByType<CinemachineThirdPersonFollow>();

        _followCam.Follow = ship.transform;
        
        _startFov = _followCam.Lens.FieldOfView;
        _startCameraDistance = _thirdPersonFollow.CameraDistance;

        _hc = FindAnyObjectByType<HUDController>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!isLocalPlayer)
        {
            return;
        }
        if (_hc.isPaused)
        {
            return;
        }
        
        bool brake = _brakeAction.ReadValue<float>() != 0f;
        Vector2 movementVector = _moveAction.ReadValue<Vector2>();

        var speedMultiple = 1f;
        if (_velocity.magnitude > topSpeed)
        {
            speedMultiple = _velocity.magnitude / topSpeed;
        }
        
        if (_takeOffTime <= 0f)
        {
            //We drivin

            var turnAmount = movementVector.x * turn;
            
            if (brake)
            {
                if (_velocity.magnitude <= 0.1f)
                {
                    turnAmount *= 1.4f;
                }
                if (_velocity.magnitude > 0)
                {
                    _velocity = Mathf.Max(0, _velocity.magnitude - weight * speedMultiple) * _velocity.normalized;
                }

                _chargeLevel = Mathf.Min(_chargeLevel + charge, maxCharge);

                _connectedHeight = 0;

                _thirdPersonFollow.CameraDistance = 0.5f;
            }
            else
            {
                turnAmount *= 0.6f;
                if (_chargeLevel > 0)
                {
                    var boost = Mathf.Max((_chargeLevel - (maxCharge / 2f)) * 2f, 1);
                    _velocity += acceleration * boost * ship.transform.forward;
                    _chargeLevel = 0;
                }
                else
                {
                    _velocity += acceleration * ship.transform.forward;
                }

                if (_velocity.magnitude > topSpeed)
                {
                    _velocity = Mathf.Max(topSpeed, _velocity.magnitude - speedDecay * speedMultiple) * _velocity.normalized;
                }

                _connectedHeight = defaultHeight;
                _thirdPersonFollow.CameraDistance = _startCameraDistance;//todo: smooth this out
            }
            
            ship.transform.localEulerAngles += new Vector3(0, turnAmount, 0);

            PlaceOnSurface();
            
            // calculate ramps
            _lift += CalculateLift();
            if (_lift >= glideThreshold)
            {
                _takeOffTime = Time.time;
                _glideEnergy = glide;
                Debug.Log("Take off");
            }
            _lift *= glideDecayFactor;

            _velocity.y = 0f;
            
            //update HUD
            _hc.UpdateChargeDisplay(_chargeLevel / maxCharge);
        }
        else
        {
            //we flyin
            
            var verticalAngle = Vector3.Dot(ship.transform.forward, Vector3.up);
            var maxVerticalAngle = 0.4f;
            var factor = Mathf.Clamp(maxVerticalAngle - Mathf.Abs(verticalAngle), 0f, maxVerticalAngle);
            if (verticalAngle < 0f && movementVector.y < 0f || verticalAngle > 0f && movementVector.y > 0f)
            {
                factor = 1;
            }
            
            ship.transform.localEulerAngles += new Vector3(movementVector.y * factor, 
                                                           movementVector.x * turn, 
                                                           0);
            
            _velocity += glideAcceleration * ship.transform.forward;
            _chargeLevel = 0;

            if (_velocity.magnitude > topSpeed)
            {
                _velocity = Mathf.Max(topSpeed, _velocity.magnitude - speedDecay * speedMultiple) * _velocity.normalized;
            }
            
            _thirdPersonFollow.CameraDistance = _startCameraDistance;//todo: smooth this out
            
            _glideEnergy -= 1;
            var force = gravity * (brake ? brakeGravity : 1f);
            if (_glideEnergy < 0f)
            {
                force *= Mathf.Abs(_glideEnergy) * fallFactor;
            }
            _velocity += force * Vector3.down;

            if (CheckLanding())
            {
                _takeOffTime = -1f;
                ship.transform.localEulerAngles = new Vector3(0f, ship.transform.localEulerAngles.y, 0f);
                Debug.Log("Landing");
            }
            
            //update HUD
            _hc.UpdateChargeDisplay(_glideEnergy / glide);
        }

        _hc.UpdateSpeedometerText(_velocity.magnitude.ToString("00.0"));
        
        //FOV effect on boost/charge
        _fovMomentum = (_velocity.magnitude * fovFactor) + _startFov;
        _followCam.Lens.FieldOfView += _fovMomentum - _followCam.Lens.FieldOfView;
            
        //move the ship
        transform.Translate(_velocity);
    }

    private bool CheckLanding()
    {
        if (Time.time - _takeOffTime < 0.2f)
        {
            return false;
        }
        var pos = transform.position;
        var height = Terrain.activeTerrain.SampleHeight(pos);
        
        return height >= transform.position.y;
    }
    
    private void PlaceOnSurface()
    {
        var pos = transform.position;
        pos.y = Terrain.activeTerrain.SampleHeight(transform.position) + _connectedHeight;
        var terrainSize = Terrain.activeTerrain.terrainData.size;
        var x = pos.x / terrainSize.x;
        var z = pos.z / terrainSize.z;
        var normal = Terrain.activeTerrain.terrainData.GetInterpolatedNormal(x, z);
        transform.position = pos;
        transform.rotation = Quaternion.FromToRotation(transform.up, normal) * transform.rotation;
        transform.rotation = new Quaternion(transform.rotation.x, 0, transform.rotation.z, transform.rotation.w);
    }

    private float CalculateLift()
    {
        var height = Terrain.activeTerrain.SampleHeight(transform.position);
        var nextHeight = Terrain.activeTerrain.SampleHeight(transform.position + _velocity);
        
        return nextHeight - height;
    }

    public void Pickup(pickup.PickupData pd)
    {
        acceleration += pd.acceleration;
        topSpeed += pd.topSpeed;
        charge += pd.charge;
        turn += pd.turn;
        weight += pd.weight;
        maxCharge += pd.maxCharge;
        speedDecay += pd.speedDecay;
        glideThreshold += pd.glideThreshold; 
        glide += pd.glide; 
        glideAcceleration += pd.glideAcceleration;
    }
}
