using SturfeeVPS.Core;
using SturfeeVPS.Providers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceGpsProvider : GpsProviderBase
{
    [SerializeField]
    private GeoLocation _spaceLocation;

    private bool _tilesLoaded = false;

    public GpsProviderBase BaseGps;

    private void Start()
    {
        BaseGps = GetComponent<GpsProviderBase>();
    }

    private GeoLocation SpaceLocation
    {
        get 
        {
            // wait till base gps is ready before using space's location
            if(BaseGps.GetProviderStatus() != ProviderStatus.Ready)
            {
                return null;
            }

            // Use space's location only to load tiles
            // After tiles are loaded use the regular gps 
            if(_tilesLoaded)
            {
                return null;
            }

            return _spaceLocation; 
        }
    }

    public override void Initialize()
    {
        BaseGps.Initialize();        
        SturfeeEventManager.Instance.OnTilesLoaded += OnTilesLoaded;
    }

    private void OnTilesLoaded()
    {
        _tilesLoaded = true;
    }

    public override GeoLocation GetCurrentLocation()
    {
        if (SpaceLocation == null)
        {
            return BaseGps.GetCurrentLocation();
        }

        return SpaceLocation;
    }

    public override ProviderStatus GetProviderStatus()
    {
        if (SpaceLocation == null)
        {
            return BaseGps.GetProviderStatus();
        }

        return ProviderStatus.Ready;        
    }

    public override void Destroy()
    {
        BaseGps.Destroy();
        
        SturfeeEventManager.Instance.OnTilesLoaded -= OnTilesLoaded;
    }

    public void SetSpaceLocation(GeoLocation spaceLocation)
    {
        _spaceLocation = spaceLocation;
    }
}
