using SturfeeVPS.SDK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class VRCamera : SceneSingleton<VRCamera>
{
    [SerializeField]
    private Camera _firstPersonCamera;
    [SerializeField]
    private Camera _thirdPersonCamera;

    private CharacterController _characterController;

    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
    }

    public static XRPose Pose
    {
        get
        {
            return new XRPose()
            {
                GeoLocation = Converters.UnityToGeoLocation(CurrentInstance.transform.position),
                Position = CurrentInstance.transform.position,
                Rotation = CurrentInstance.transform.rotation
            };
        }
    }


    public Camera FirstPersonCamera => _firstPersonCamera;
    
    public Camera ThirdPersonCamera => _thirdPersonCamera;
    

    public void Activate()
    {
        _characterController.enabled = true;
    }

    public void DeActivate()
    {
        _characterController.enabled = false;
    }

    public void SetPosition(Vector3 pos)
    {
        transform.position = pos;
    }

    public void SetRotation(Quaternion rot)
    {
        transform.rotation = rot;
    }
} 

