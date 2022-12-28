using System;
using UnityEngine;

public class MyLogHandler : ILogHandler
{
    public bool enable;
    private static ILogHandler unityLogHandler = Debug.unityLogger.logHandler;

    public MyLogHandler(bool enable = true)
    {
        this.enable = enable;
    }

    public void LogException(Exception exception, UnityEngine.Object context)
    {
        if (enable)
        {
            unityLogHandler.LogException(exception, context);
        }
    }

    public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (enable)
        {
            unityLogHandler.LogFormat(logType, context, format, args);
        }
#endif
    }
}