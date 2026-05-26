using UnityEngine;
using Match3.Core;

namespace Match3.Unity
{
    /// <summary>
    /// 게임 진입점. DI 설정 + 게임 시작.
    /// Canvas에 이 컴포넌트를 붙이면 자동으로 실행됨.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class Match3Bootstrapper : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UnityBoardRenderer _boardRenderer;
        [SerializeField] private UnityInputHandler _inputHandler;
        [SerializeField] private UIManager _uiManager;

        private GameController _gameController;

        private void Awake()
        {
            // ── 파일 로거 초기화 (Debug.Log 안 찍힐 때 대비) ──
            FileLogger.Init();
            var flusherGO = new GameObject("FileLoggerFlusher");
            flusherGO.AddComponent<FileLoggerFlusher>();
            FileLogger.Log("=== Match3Bootstrapper Awake ===");

            // ── 씬 기본 설정 ──
            SetupMainCamera();
            SetupCanvas();

            // ── 어댑터 생성 ──
            if (_boardRenderer == null)
            {
                var go = new GameObject("BoardRenderer");
                go.transform.SetParent(transform.parent);
                _boardRenderer = go.AddComponent<UnityBoardRenderer>();
                FileLogger.Log($"BoardRenderer created: {_boardRenderer != null}");
            }

            if (_inputHandler == null)
            {
                var go = new GameObject("InputHandler");
                go.transform.SetParent(transform.parent);
                _inputHandler = go.AddComponent<UnityInputHandler>();
                FileLogger.Log($"InputHandler created: {_inputHandler != null}");
            }

            if (_uiManager == null)
            {
                _uiManager = GetComponent<UIManager>();
                if (_uiManager == null)
                    _uiManager = gameObject.AddComponent<UIManager>();
            }

            // ── LeaderboardClient 생성 ──
            var leaderboard = GetComponent<LeaderboardClient>();
            if (leaderboard == null)
                leaderboard = gameObject.AddComponent<LeaderboardClient>();

            // ── Core GameController 생성 + DI ──
            _gameController = new GameController();
            _gameController.Renderer = _boardRenderer;
            _gameController.Input = _inputHandler;
            FileLogger.Log("GameController created, DI set");

            // ── Input 이벤트 연결 ──
            _inputHandler.OnTileSwapped += (a, b) =>
            {
                FileLogger.Log($"[Bootstrapper] OnTileSwapped! from=({a.Row},{a.Col}) to=({b.Row},{b.Col})");
                FileLogger.Log($"[Bootstrapper]   Renderer={(_gameController.Renderer != null)} Input={(_gameController.Input != null)} State={_gameController.State.CurrentState}");
                _gameController.OnPlayerSwap(a, b);
            };

            // ── UI 초기화 ──
            _uiManager.Initialize(_gameController);

            // ── 게임 시작! ──
            FileLogger.Log($"[Bootstrapper] Before StartGame — Renderer={(_gameController.Renderer != null)} Input={(_gameController.Input != null)}");
            _gameController.StartGame();
            FileLogger.Log($"[Bootstrapper] After StartGame — State={_gameController.State.CurrentState} Score={_gameController.Score.Score}");

            FileLogger.Log("=== Match3Bootstrapper complete ===");
            FileLogger.Flush();
        }

        private void OnDestroy()
        {
            if (_inputHandler != null)
                _inputHandler.OnTileSwapped -= (a, b) => _gameController?.OnPlayerSwap(a, b);
        }

        private void SetupMainCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                go.tag = "MainCamera";
                cam = go.GetComponent<Camera>();

                cam.orthographic = true;
                cam.orthographicSize = 6f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.12f, 0.12f, 0.15f);
                cam.transform.position = new Vector3(3.5f, 3.5f, -10f);
            }
        }

        private void SetupCanvas()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // GraphicRaycaster 필수! 없으면 버튼 클릭 안 됨
            if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // EventSystem 필수! UI 입력 처리
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
    }
}
