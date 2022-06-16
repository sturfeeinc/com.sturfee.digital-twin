
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;

public static class InstalledPackages
{
    [InitializeOnLoadMethod]
    public static void InitializeOnLoad()
    {
        var listRequest = Client.List(true);
        while (!listRequest.IsCompleted)
            Thread.Sleep(100);

        if (listRequest.Error != null)
        {
            Debug.Log("Error: " + listRequest.Error.message);
            return;
        }

        var packages = listRequest.Result;
        var text = new StringBuilder("Packages:\n");
        foreach (var package in packages)
        {
            if (package.source == PackageSource.Registry)
                text.AppendLine($"{package.name}: {package.version} [{package.resolvedPath}]");
        }

        Debug.Log(text.ToString());
    }
}
