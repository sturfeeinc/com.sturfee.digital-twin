using Newtonsoft.Json;
using Sturfee.DigitalTwin.Spaces;
using Sturfee.XRCS;
using SturfeeVPS.Core;
using SturfeeVPS.SDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Sturfee.DigitalTwin.Demo
{
    public enum GameMode
    {
        Single,
        Multiplayer
    }

    public class SpacesManager : SimpleSingleton<SpacesManager>
    {
        public SpaceMode StartSpaceMode;
        public GameMode GameMode = GameMode.Multiplayer;

        public XrSceneData CurrentSpace => _currentSpace;
        [SerializeField]
        private XrSceneData _currentSpace;

        private ISpacesProvider _spacesProvider;

        private void Start()
        {
            _spacesProvider = new WebSpacesProvider();
        }

        public async Task<List<XrSceneData>> FindSpaces(FindSpacesFilter filter)
        {
            var spaces = await _spacesProvider.FindSpaces(filter, null);
            return spaces;
        }

        public async Task<XrSceneData> CreateSpace(double latitude, double longitude)
        {
            try
            {
                var space =await _spacesProvider.CreateSpace(AuthManager.Instance.CurrentUser, latitude, longitude);
                return space;
            }
            catch(Exception e)
            {
                Debug.LogError(e.Message);
                throw;
            }
        }

        public async Task PublishSpace(XrSceneData space)
        {
            await _spacesProvider.SaveSpace(space);
        }

        public void SetCurrentSpace(XrSceneData space) 
        {
            _currentSpace = space;

            if (_currentSpace != null)
            {                
                MyLogger.Log($"SpacesManager :: Set LocManager Reference Location from Space: {_currentSpace.Location.Latitude}, {_currentSpace.Location.Longitude}");

                var location = new GeoLocation {
                    Latitude = _currentSpace.Location.Latitude,
                    Longitude = _currentSpace.Location.Longitude,
                    Altitude = _currentSpace.Location.Altitude
                };

                // VR
                LocManager.Instance.SetReferenceLocation(location.Latitude, location.Longitude, location.Altitude);

                // AR
                PositioningUtils.Init(location);
            }
        }

    }
}
