using Sturfee.XRCS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Sturfee.DigitalTwin.Demo
{
    public class SpaceItemManager : MonoBehaviour
    {
        [SerializeField]
        private SpaceItem _spaceItemPrefab;
        [SerializeField]
        private Transform _itemsParent;

#if CLIENT
        private void Start()
        {
            if (SpacesListManager.Instance.FetchedSpaces.Count > 0)
            {
                ShowSpaceLists(SpacesListManager.Instance.FetchedSpaces);
            }
            else
            {
                FindSpaces();
            }
        }
#endif

        public async void FindSpaces()
        {
            //FullScreenLoader.Instance.ShowLoader();
            LoadScreenManager.Instance.ShowLoadingScreen();
            var filter = new FindSpacesFilter
            {
                IsPublic = true,
                Privacy = ProjectPrivacy.Private,
                SortOption = SortSpacesOption.Date,
                Tag = SharedSpaceTag.Featured
            };

            // If user is logged in
            if (AuthManager.Instance.CurrentUser != null)
            {
                // Show only current user created spaces
                List<Guid> userIds = new List<Guid>();
                userIds.Add(AuthManager.Instance.CurrentUser.Id);

                filter.UserIds = userIds;
                filter.IsPublic = false;
                filter.Privacy = ProjectPrivacy.Private;
                filter.Tag = SharedSpaceTag.None;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            var spaces = await SpacesManager.Instance.FindSpaces(filter);
            MyLogger.Log($" Timer :: SpaceItemManager :: Fetchspaces time : {stopwatch.ElapsedMilliseconds} ms");
            stopwatch.Stop();
            SpacesListManager.Instance.UpdateFetchedSpaces(spaces);
            ShowSpaceLists(spaces);

            //FullScreenLoader.Instance.HideLoader();
            LoadScreenManager.Instance.HideLoadingScreen();
        }

        private void ShowSpaceLists(List<XrSceneData> spaces)
        {
            ClearAll();

            foreach (var space in spaces)
            {
                var spaceItem = Instantiate(_spaceItemPrefab, _itemsParent);
                spaceItem.SetData(space);
            }
        }

        private void ClearAll()
        {
            foreach (var spaceItem in _itemsParent.GetComponentsInChildren<SpaceItem>())
            {
                Destroy(spaceItem.gameObject);
            }
        }

    }
}