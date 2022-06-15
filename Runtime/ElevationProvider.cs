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
        public float GetTerrainElevation(Vector3 pos)
        {
            RaycastHit hit;
            Ray ray;
            float elevation = 0.0f;


            // 1st raycast to get terrain elevation
            pos.y += 1000;       // TODO: what if terrain is higher than 1000m ?
            ray = new Ray(pos, Vector3.down);
            //Debug.DrawRay(ray.origin, ray.direction, Color.yellow, 1.0f);
            if (Physics.Raycast(ray, out hit, 1000, LayerMask.GetMask(SturfeeLayers.Terrain, $"{XrLayers.Terrain}")))
            {
                elevation = hit.point.y;
            }

            // 2nd raycast to start slightly above terrain 
            pos.y = elevation + 10;
            //ray = new Ray(pos, Vector3.down);
            Debug.DrawRay(ray.origin, ray.direction, Color.green, 1.0f);
            if (Physics.Raycast(ray, out hit))
            {
                elevation = hit.point.y;
            }

            return elevation;
        }

        public float GetTerrainElevation(double latitude, double longitude)
        {
            var location = new GeoLocation { Latitude = latitude, Longitude = longitude };
            var local = Converters.GeoToUnityPosition(location);

            return GetTerrainElevation(local);
        }

        public float GetTerrainElevation(GeoLocation location)
        {
            return GetTerrainElevation(location.Latitude, location.Longitude);
        }
    }
}
