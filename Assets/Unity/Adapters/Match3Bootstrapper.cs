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
            // ── 씬 기본 설정 ──
            SetupMainCamera();
            SetupCanvas();

            // ── 어댑터 생성 ──
            if (_boardRenderer == null)
            {
                var go = new GameObject("BoardRenderer");
                go.transform.SetParent(transform.parent);
                _boardRenderer = go.AddComponent<UnityBoardRenderer>();
            }

            if (_inputHandler == null)
            {
                var go = new GameObject("InputHandler");
                go.transform.SetParent(transform.parent);
                _inputHandler = go.AddComponent<UnityInputHandler>();
            }

            if (_uiManager == null)
            {
                _uiManager = GetComponent<UIManager>();
                if (_uiManager == null)
                    _uiManager = gameObject.AddComponent<UIManager>();
            }

            // ── Core GameController 생성 + DI ──
            _gameController = new GameController();
            _gameController.Renderer = _boardRenderer;
            _gameController.Input = _inputHandler;

            // ── Input 이벤트 연결 ──
            _inputHandler.OnTileSwapped += (a, b) => _gameController.OnPlayerSwap(a, b);

            // ── UI 초기화 ──
            _uiManager.Initialize(_gameController);

            // ── 게임 시작! ──
            _gameController.StartGame();

            Debug.Log("[Match3Bootstrapper] Game started!");
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

            // CanvasScaling을 위한 GraphicRaycaster는 Canvas에 기본 포함됨
        }
    }
}
