using Sturfee.XRCS.Config;
using SturfeeVPS.Core;
using SturfeeVPS.SDK;
using SturfeeVPS.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Sturfee.DigitalTwin.Tiles
{
    public class DigitalTwinTilesProvider : BaseTilesProvider
    {
        [SerializeField][ReadOnly]
        private ProviderStatus _providerStatus;

        private GameObject _tiles;

        public override async void OnRegister()
        {
            base.OnRegister();

            if (_tiles == null)
            {
                var location = Converters.UnityToGeoLocation(Vector3.zero);
                LocManager.Instance.SetReferenceLocation(new XrGeoLocationData { Latitude = location.Latitude, Longitude = location.Longitude });
                // FOR DEBUG
                // Debug.Log($"VR Location: Latitude: {location.Latitude}, Longitude: {location.Longitude}");
                try
                {                    
                    _tiles = await GetTiles(location);
                }
                catch(Exception ex)
                {
                    Debug.LogException(ex);
                    string localizedError = SturfeeLocalizationProvider.Instance.GetString(ErrorMessages.TileLoadingError.Item1, ErrorMessages.TileLoadingError.Item2);
                    TriggerTileLoadingFailEvent(localizedError);
                }
            }
            
            TriggerTileLoadEvent();
        }

        public override float GetElevation(GeoLocation location)
        {
            RaycastHit hit;

            Vector3 unityPos = Converters.GeoToUnityPosition(location);
            unityPos.y += 100;

            Ray ray = new Ray(unityPos, Vector3.down);
            Debug.DrawRay(ray.origin, ray.direction * 10000, Color.red, 2000);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask($"{XrLayers.Terrain}")))
            {
                float elevation = hit.point.y;
                //SturfeeDebug.Log("Elevation : " + elevation);
                return elevation;
            }

            return 0;
        }

        public override ProviderStatus GetProviderStatus()
        {
            return _providerStatus;
        }

        public override async Task<GameObject> GetTiles(GeoLocation location, float radius = 0, CancellationToken cancellationToken = default)
        {
            _providerStatus = ProviderStatus.Initializing;
            var tiles = await DtTileLoader.Instance.LoadTilesAt(location.Latitude, location.Longitude);
            tiles.transform.parent = transform;
            _providerStatus = ProviderStatus.Ready;
            return tiles ;
        }

        public override Task<GameObject> GetTiles(CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }
}
