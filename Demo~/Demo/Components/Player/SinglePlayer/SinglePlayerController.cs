using SturfeeVPS.Core;
using SturfeeVPS.SDK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sturfee.DigitalTwin.Demo
{
    public class SinglePlayerController : MonoBehaviour
    {
        private void Update()
        {
            float heightFromGround = 0;
            var poseProvider = XrSessionManager.GetSession().GetProvider<IPoseProvider>();
            if(poseProvider != null && poseProvider.GetProviderStatus() == ProviderStatus.Ready)
            {
                heightFromGround = poseProvider.GetHeightFromGround();
            }

            Vector3 pos = XrCamera.Pose.Position;
            pos.y -= heightFromGround;

            transform.position = pos;
            transform.eulerAngles = new Vector3(0, XrCamera.Pose.Rotation.eulerAngles.y, 0);    // only use yaw            
        }
    }
}