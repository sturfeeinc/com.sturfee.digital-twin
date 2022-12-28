using System.Collections;
using System.Collections.Generic;
using Mirror;
using SturfeeVPS.Core;
using SturfeeVPS.Networking;
using SturfeeVPS.SDK;
using UnityEngine;

namespace Sturfee.DigitalTwin.Demo
{
    public class NetworkPlayerController : NetworkBehaviour
    {
        private GeoNetworkTransform _geoNetworkTransform;
        private NetworkPlayerInfo _networkPlayerInfo;

        private void Start()
        {
            _networkPlayerInfo = GetComponent<NetworkPlayerInfo>();
            _geoNetworkTransform = GetComponent<GeoNetworkTransform>();
        }

        private void Update()
        {
            if (GameManager.CurrentInstance.IsLoading)
                return;

            if (!isClient)
                return;

            if (isLocalPlayer)
            {
                float heightFromGround = 0;
                var poseProvider = XrSessionManager.GetSession().GetProvider<IPoseProvider>();
                if (poseProvider != null && poseProvider.GetProviderStatus() == ProviderStatus.Ready)
                {
                    heightFromGround = poseProvider.GetHeightFromGround();
                }

                Vector3 pos = XrCamera.Pose.Position;
                pos.y -= heightFromGround;

                transform.position = pos;
                transform.eulerAngles = new Vector3(0, XrCamera.Pose.Rotation.eulerAngles.y, 0);    // only use yaw                
            }

            // if local player and other player are not in same space then use terrrain elevation
            _geoNetworkTransform.SnapToTerrain = !isLocalPlayer &&
                (PlayerInfoManager.Instance.PlayerInfo.SpaceMode != _networkPlayerInfo.PlayerInfo.SpaceMode); 
        }
    }
}