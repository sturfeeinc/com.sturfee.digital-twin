using SturfeeVPS.Core;
using SturfeeVPS.SDK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceGpsProvider : BaseGpsProvider
{
    [SerializeField]
    private GeoLocation _spaceLocation;

    private bool _tilesLoaded = false;

    public BaseGpsProvider BaseGps;

    private void Start()
    {
        BaseGps = GetComponent<BaseGpsProvider>();
    }

    private GeoLocation SpaceLocation
    {
        get 
        {
            // wait till base gps is ready before using space's location
            //if(BaseGps.GetProviderStatus() != ProviderStatus.Ready)
            //{
            //    return null;
            //}

            // Use space's location only to load tiles
            // After tiles are loaded use the regular gps 
            if(_tilesLoaded)
            {
                return null;
            }

            return _spaceLocation; 
        }
    }

    public override void OnRegister()
    {
        BaseGps.OnRegister();        
        SturfeeEventManager.OnTilesLoaded += OnTilesLoaded;
    }

    private void OnTilesLoaded()
    {
        _tilesLoaded = true;
    }

    public override GeoLocation GetFineLocation(out bool includesElevation)
    {
        if (SpaceLocation == null)
        {
            return BaseGps.GetFineLocation(out includesElevation);
        }

        includesElevation = false;
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

    public override void OnUnregister()
    {
        BaseGps.OnUnregister();
        
        SturfeeEventManager.OnTilesLoaded -= OnTilesLoaded;
    }

    public void SetSpaceLocation(GeoLocation spaceLocation)
    {
        _spaceLocation = spaceLocation;
    }

    public override GeoLocation GetApproximateLocation(out bool includesElevation)
    {
       return BaseGps.GetApproximateLocation(out includesElevation);
    }
}
