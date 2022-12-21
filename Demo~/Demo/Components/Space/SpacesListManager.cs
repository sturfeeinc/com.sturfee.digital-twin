using SturfeeVPS.SDK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpacesListManager : SimpleSingleton<SpacesListManager>
{
    public List<XrSceneData> FetchedSpaces => _fetchedSpaces;
    [SerializeField]
    private List<XrSceneData> _fetchedSpaces = new List<XrSceneData>();

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    public void UpdateFetchedSpaces(List<XrSceneData> spaces)
    {
        if(spaces == null)
        {            
            return;
        }

        _fetchedSpaces = spaces;
    }

    public void Clear()
    {
        _fetchedSpaces?.Clear();
    }
}
