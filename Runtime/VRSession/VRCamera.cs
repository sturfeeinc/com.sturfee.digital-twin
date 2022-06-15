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
            

    public XRPose FirstPersonCamera
    {
        get
        {
            if (_firstPersonCamera == null)
                return null;

            return new XRPose
            {
                Position = _firstPersonCamera.transform.position,
                Rotation = _firstPersonCamera.transform.rotation,
                GeoLocation = LocManager.Instance.GetObjectLocation(_firstPersonCamera.transform)
            };
        }
    }

    public XRPose ThirdPersonCamera
    {
        get
        {
            if (_thirdPersonCamera == null)
                return null;

            return new XRPose
            {
                Position = _thirdPersonCamera.transform.position,
                Rotation = _thirdPersonCamera.transform.rotation,
                GeoLocation = LocManager.Instance.GetObjectLocation(_thirdPersonCamera.transform)
            };
        }
    }

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

