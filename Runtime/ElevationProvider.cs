using Sturfee.XRCS.Config;
using SturfeeVPS.Core;
using SturfeeVPS.SDK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sturfee.DigitalTwin
{
    public class ElevationProvider : Singleton<ElevationProvider>
    {
        public float GetTerrainElevation(Vector3 pos, float stepOffset = 1, int layerMask = -1)
        {
            RaycastHit hit;
            Ray ray;
            float elevation = -1000.0f;
            if(layerMask == -1)
            {
                Debug.Log(" layermask is -1");
                layerMask = LayerMask.GetMask(SturfeeLayers.Terrain, $"{XrLayers.Terrain}");
            }            

            // 1st raycast to get terrain elevation
            pos.y += 1000;       // TODO: what if terrain is higher than 1000m ?
            ray = new Ray(pos, Vector3.down);
            Debug.DrawRay(ray.origin, ray.direction * 10000, Color.yellow, 1.0f);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
            {
                elevation = hit.point.y;
                //Debug.Log($" Elevation 1 : {elevation} name : {hit.transform.name}");
            }

            // 2nd raycast to start slightly above terrain ( similar to step offset of character controller)
            pos.y = elevation + stepOffset ;     // TODO: Should be made configurable from inspector
            ray = new Ray(pos, Vector3.down);
            Debug.DrawRay(ray.origin, ray.direction * 1000, Color.green, 1.0f);
            if (Physics.Raycast(ray, out hit))
            {
                elevation = hit.point.y;
                //Debug.Log($" Elevation 2 : {elevation} name : {hit.transform.name}");
            }

            return elevation;
        }

        public float GetTerrainElevation(double latitude, double longitude, float stepOffset = 1, int layerMask = -1)
        {
            var location = new GeoLocation { Latitude = latitude, Longitude = longitude };
            var local = Converters.GeoToUnityPosition(location);

            return GetTerrainElevation(local);
        }

        public float GetTerrainElevation(GeoLocation location, float stepOffset = 1, int layerMask = -1)
        {
            return GetTerrainElevation(location.Latitude, location.Longitude);
        }
    }
}
