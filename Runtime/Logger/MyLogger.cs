using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public enum MyLogLevel
{
    OFF,
    FATAL,
    ERROR,
    WARN,
    INFO
}

public static class MyLogger
{
    public static MyLogLevel Level = MyLogLevel.INFO;

    public static bool LogToFile = true;
    public static int IntervalSeconds = 10;

    public static string LogFileDirectory
    {
        get
        {
            return _logFileDirectory;
        }

        set
        {
            _logFileDirectory = value;
            if (!Directory.Exists(_logFileDirectory)) { Directory.CreateDirectory(_logFileDirectory); }
            InitFiles(_logFileDirectory);
        }
    }
    private static string _logFileDirectory;

    private static string _logFilePrepend = "LOG_";

    private static float _expirationDays = 2.0f;

    private static string _logQueue = "";

    private static bool _isInitialized = false;

    private static void InitFiles(string path)
    {
        if (_isInitialized)
        {
            Debug.LogError($"MyLogger :: ERROR = already initialized!");
            return;
        }

        if (!string.IsNullOrEmpty(path))
        {
            _isInitialized = true;

            Application.logMessageReceived += QueueLog;

            // remove old files
            var existingFiles = Directory.GetFiles(path);
            foreach (var filepath in existingFiles)
            {
                var createdDate = File.GetCreationTime(filepath);
                if ((createdDate.AddDays(_expirationDays) - DateTime.Now).TotalDays > _expirationDays) // DateTime.Now > createdDate.AddDays(_expirationDays))
                {
                    MyLogger.LogWarning($"MyLogger :: Log File Expired (created={createdDate}): {filepath}");
                    File.Delete(filepath);
                }
            }

            // set up timer to write to file every X seconds..
            var timer = new System.Threading.Timer((e) =>
            {
                WriteToFile();
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(IntervalSeconds));
        }
    }

    public static void Log(object message)
    {
        if (Level < MyLogLevel.INFO) { return; }

        Debug.Log(message);
    }


    public static void LogWarningFormat(string format, params object[] args)
    {
        if (Level < MyLogLevel.WARN) { return; }

        Debug.LogWarningFormat(format, args);
    }

    public static void LogWarningFormat(UnityEngine.Object context, string format, params object[] args)
    {
        if (Level < MyLogLevel.WARN) { return; }

        Debug.LogWarningFormat(context, format, args);
    }

    public static void LogWarning(object message)
    {
        if (Level < MyLogLevel.WARN) { return; }

        Debug.LogWarning(message);
    }


    public static void LogError(object message)
    {
        if (Level < MyLogLevel.ERROR) { return; }

        Debug.LogError(message);
    }

    public static void LogError(object message, UnityEngine.Object context)
    {
        if (Level < MyLogLevel.ERROR) { return; }

        Debug.LogError(message, context);
    }

    public static void LogErrorFormat(string format, params object[] args)
    {
        if (Level < MyLogLevel.ERROR) { return; }

        Debug.LogErrorFormat(format, args);
    }

    public static void LogErrorFormat(UnityEngine.Object context, string format, params object[] args)
    {
        if (Level < MyLogLevel.ERROR) { return; }

        Debug.LogErrorFormat(context, format, args);
    }



    public static void LogException(Exception ex)
    {
        if (Level < MyLogLevel.FATAL) { return; }

        Debug.LogException(ex);
    }

    private static void QueueLog(string condition, string stackTrace, LogType type)
    {
        if (LogToFile)
        {
            _logQueue += $"[{DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt")}] {type} :: {condition}\n ${stackTrace}" + "\n";
        }
    }

    private static async void WriteToFile()
    {
        if (string.IsNullOrEmpty(_logQueue)) { return; }

        try
        {
            var dataToWrite = "" + _logQueue;

            _logQueue = "";

            await Task.Run(() =>
            {
                // create new file for today, if needed
                var logFileToday = Path.Combine(_logFileDirectory, $"{_logFilePrepend}{DateTime.Now.ToString("MM_dd_yyyy")}.log");
                File.AppendAllText(logFileToday, dataToWrite);
            });
        }
        catch (Exception ex)
        {
            Debug.LogError("MyLogger :: Could Not Write Log File");
            Debug.LogException(ex);
        }

    }
}