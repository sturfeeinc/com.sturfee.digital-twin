using Sturfee.DigitalTwin.CMS;
using Sturfee.DigitalTwin.Spaces;
using Sturfee.DigitalTwin.Tiles;
using Sturfee.XRCS;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sturfee.DigitalTwin
{
    public enum ServiceType
    {
        Web,
        Local,
        Test
    }

    public class ServiceManager : SimpleSingleton<ServiceManager>
    {
        public bool Initilalized = false;
        public void Init(ServiceType serviceType = ServiceType.Web)
        {
            MyLogger.Log($"Registering providers");

            switch (serviceType)
            {
                case ServiceType.Local: throw new Exception("Local provider for services not yet available");
                case ServiceType.Test: throw new Exception("Test providers for services not yet available");
                case ServiceType.Web:

                    //Cache
                    IOC.Register<ICacheProvider<CachedDtTile>>(new DtCacheProvider()); // get using IOC.Resolve<ICacheProvider<CachedDtTile>>();


                    // Auth
                    IOC.Register<IAuthProvider>(new WebAuthProvider()); // get using IOC.Resolve<IUserProvider>();

                    // Spaces
                    IOC.Register<ISpacesProvider>(new WebSpacesProvider()); //get using IOC.Resolve<ISpacesProvider>();

                    // Tiles
                    IOC.Register<ITileProvider>(new DtTileProvider());  // get using IOC.Resolve<ITileProvider>

                    // CMS
                    IOC.Register<ICMSProvider>(new WebCMSProvider()); //get using IOC.Resolve<ICMSProvider>();

                    // XRCS
                    IOC.Register<IProjectProvider>(new WebProjectProvider()); // get using IOC.Resolve<IProjectProvider>();
                    IOC.Register<IThumbnailProvider>(new WebThumbnailProvider()); //get using IOC.Resolve<IThumbnailProvider>();

                    // Enhnacements

                    break;
            }

            Initilalized = true;
        }
    }
}