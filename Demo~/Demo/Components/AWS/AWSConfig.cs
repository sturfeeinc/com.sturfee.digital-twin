using System.Collections;
using System.Collections.Generic;
using Amazon;
using UnityEngine;

namespace Sturfee.DigitalTwin.Demo
{
    public class AWSConfig 
    {
        public static RegionEndpoint Region = RegionEndpoint.USEast1;
        public static readonly string IdentityPoolId = "IDENTITY_POOL_ID";
        public static readonly string AliasId = "GAMELIFT_ALIAS_ID"; 
    }
}