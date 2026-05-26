using System;
using System.Text;
using UnityEngine;

namespace Match3.Unity
{
    /// <summary>Unity Debug.Log 안 찍힐 때를 대비한 로거 (WebGL 안전)</summary>
    public static class FileLogger
    {
        private static StringBuilder _buffer = new StringBuilder();
        private static bool _initialized = false;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            Log("=== FileLogger initialized ===");
        }

        public static void Log(string message)
        {
            if (!_initialized) Init();

            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            Debug.Log(line);
        }

        public static void LogWarning(string message)
        {
            Log($"[WARN] {message}");
        }

        public static void LogError(string message)
        {
            Log($"[ERROR] {message}");
        }

        public static void Flush() { }
        public static string GetLogPath() => "";
    }

    /// <summary>더미 (WebGL에서 안전하게 동작)</summary>
    public class FileLoggerFlusher : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
