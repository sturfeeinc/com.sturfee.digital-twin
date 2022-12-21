using Sturfee.XRCS.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public static class ThumbnailUtils
{
    public static async void TryLoadThumbnail(Guid id, Guid? backupId, Action<Texture2D> callback, ImageFileType ext = ImageFileType.png)
    {
        // try to load thumbnail for the Space, else use the Layer (Project) thumbnail
        try
        {
            var image = await LoadThumbnail(id, ext);

            if (backupId.HasValue && image == null)
            {
                image = await LoadThumbnail(backupId.Value, ext);
            }

            callback?.Invoke(image);
        }
        catch (Exception ex)
        {
            MyLogger.LogError(ex);
        }
    }

    public static async Task<Texture2D> LoadThumbnail(Guid id, ImageFileType ext = ImageFileType.png)
    {
        //var thumbPath = $"{Application.persistentDataPath}/{XrConstants.LOCAL_PROJECTS_PATH}/{project.ProjectId}/thumb.jpg";
        var thumbPath = $"{Application.persistentDataPath}/{XrConstants.LOCAL_THUMBNAILS_PATH}/{id}.{ext}";

        var thumbnailProvider = IOC.Resolve<IThumbnailProvider>();
        var image = await thumbnailProvider.GetThumbnail(id, ext);

        return image as Texture2D;
    }
}
