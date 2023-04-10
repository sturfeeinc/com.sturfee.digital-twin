using Sturfee.DigitalTwin;
using SturfeeVPS.Core;
using SturfeeVPS.Core.Constants;
using SturfeeVPS.SDK;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DtObjectsCreator : MonoBehaviour
{
    [MenuItem("GameObject/SturfeeXR/SturfeeXRSession-VR", false, 2)]
    public static void CreateSturfeeXrSessionAR()
    {
        var prefab = Resources.Load<SturfeeXrSession>($"Prefabs/{DtConstants.SturfeeXrSessionVR}");
        if (prefab == null)
        {
            SturfeeDebug.LogError($"Cannot instantiate {DtConstants.SturfeeXrSessionVR}. Prefab not found");
            return;
        }


        var go = PrefabUtility.InstantiatePrefab(prefab);
        go.name = DtConstants.SturfeeXrSessionVR;
    }

    [MenuItem("GameObject/SturfeeXR/SturfeeXRSession-AR+VR", false, 2)]
    public static void CreateSturfeeXrSessionARVR()
    {
        var prefab = Resources.Load<SturfeeXrSession>($"Prefabs/{DtConstants.SturfeeXrSessionARVR}");
        if (prefab == null)
        {
            SturfeeDebug.LogError($"Cannot instantiate {DtConstants.SturfeeXrSessionARVR}. Prefab not found");
            return;
        }


        var go = PrefabUtility.InstantiatePrefab(prefab);
        go.name = DtConstants.SturfeeXrSessionARVR;
    }
}
