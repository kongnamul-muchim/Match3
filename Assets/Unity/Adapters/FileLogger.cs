using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Match3.Unity
{
    /// <summary>Unity Debug.Log 안 찍힐 때를 대비한 파일 로거</summary>
    public static class FileLogger
    {
        private static string _logPath;
        private static StringBuilder _buffer = new StringBuilder();
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            string logDir = Path.Combine(Application.dataPath, "..", "Logs");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _logPath = Path.Combine(logDir, $"match3_debug_{timestamp}.md");
            File.WriteAllText(_logPath, "# Match3 Debug Log\n\n");
            Log("=== FileLogger initialized ===");
        }

        public static void Log(string message)
        {
            if (!_initialized) Init();

            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            lock (_lock)
            {
                _buffer.AppendLine($"- {line}");
                if (_buffer.Length > 5000)
                    FlushSync();
            }
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

        /// <summary>강제 플러시 (타이머 또는 중요 시점에 호출)</summary>
        public static void Flush()
        {
            lock (_lock)
            {
                FlushSync();
            }
        }

        private static void FlushSync()
        {
            if (_buffer.Length == 0) return;
            try
            {
                File.AppendAllText(_logPath, _buffer.ToString());
                _buffer.Clear();
            }
            catch (Exception e)
            {
                Debug.LogError($"[FileLogger] Write fail: {e.Message}");
            }
        }

        public static string GetLogPath() => _logPath;
    }

    /// <summary>주기적으로 Flush 해주는 MonoBehaviour (씬에 하나만)</summary>
    public class FileLoggerFlusher : MonoBehaviour
    {
        private float _timer = 0f;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= 3f)
            {
                _timer = 0f;
                FileLogger.Flush();
            }
        }

        private void OnDestroy()
        {
            FileLogger.Flush();
        }
    }
}
