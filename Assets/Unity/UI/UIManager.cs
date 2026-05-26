using UnityEngine;
using UnityEngine.UI;
using Match3.Core;

namespace Match3.Unity
{
    /// <summary>점수/콤보/게임오버 UI 관리</summary>
    public class UIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _comboText;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private Text _finalScoreText;
        [SerializeField] private Button _restartButton;

        private GameController _gameController;

        private void Awake()
        {
            // UI 컴포넌트가 없으면 런타임에 생성
            if (_scoreText == null)
                _scoreText = CreateText("ScoreText", new Vector2(-Screen.width * 0.4f, Screen.height * 0.45f),
                    "Score: 0", 36, TextAnchor.UpperLeft);

            if (_comboText == null)
            {
                _comboText = CreateText("ComboText", new Vector2(0, Screen.height * 0.35f),
                    "", 28, TextAnchor.UpperCenter);
                _comboText.gameObject.SetActive(false);
            }

            if (_gameOverPanel == null)
                CreateGameOverPanel();
        }

        public void Initialize(GameController controller)
        {
            _gameController = controller;

            // 이벤트 구독
            _gameController.Score.OnScoreChanged += OnScoreChanged;
            _gameController.OnChainCombo += OnChainCombo;
            _gameController.OnGameOver += OnGameOver;

            // 초기화
            UpdateScoreText(0, 0);
        }

        private void OnDestroy()
        {
            if (_gameController != null)
            {
                _gameController.Score.OnScoreChanged -= OnScoreChanged;
                _gameController.OnChainCombo -= OnChainCombo;
                _gameController.OnGameOver -= OnGameOver;
            }
        }

        private void OnScoreChanged(int newScore, int delta)
        {
            UpdateScoreText(newScore, delta);
        }

        private void OnChainCombo(int chainCount)
        {
            if (_comboText == null) return;

            _comboText.text = $"{chainCount}x COMBO!";
            _comboText.gameObject.SetActive(true);

            // 1.5초 후 자동 숨김
            CancelInvoke(nameof(HideCombo));
            Invoke(nameof(HideCombo), 1.5f);
        }

        private void OnGameOver()
        {
            if (_gameOverPanel == null) return;

            _gameOverPanel.SetActive(true);
            if (_finalScoreText != null)
                _finalScoreText.text = $"Final Score: {_gameController.Score.Score}";
        }

        public void RestartGame()
        {
            if (_gameOverPanel != null)
                _gameOverPanel.SetActive(false);

            if (_gameController != null)
                _gameController.StartGame();
        }

        // ── 내부 ──

        private void UpdateScoreText(int score, int delta)
        {
            if (_scoreText != null)
                _scoreText.text = $"Score: {score}";
        }

        private void HideCombo()
        {
            if (_comboText != null)
                _comboText.gameObject.SetActive(false);
        }

        private Text CreateText(string name, Vector2 anchoredPos, string text, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(transform, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(400, 60);

            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.alignment = anchor;
            txt.color = Color.white;

            return txt;
        }

        private void CreateGameOverPanel()
        {
            _gameOverPanel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _gameOverPanel.transform.SetParent(transform, false);

            var rect = _gameOverPanel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = _gameOverPanel.GetComponent<Image>();
            img.color = new Color(0, 0, 0, 0.7f);

            _gameOverPanel.SetActive(false);

            // Final Score Text
            var scoreGo = new GameObject("FinalScoreText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            scoreGo.transform.SetParent(_gameOverPanel.transform, false);
            var scoreRect = scoreGo.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.5f, 0.5f);
            scoreRect.anchorMax = new Vector2(0.5f, 0.5f);
            scoreRect.anchoredPosition = new Vector2(0, 30);
            scoreRect.sizeDelta = new Vector2(400, 60);

            _finalScoreText = scoreGo.GetComponent<Text>();
            _finalScoreText.text = "Game Over!";
            _finalScoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _finalScoreText.fontSize = 48;
            _finalScoreText.alignment = TextAnchor.MiddleCenter;
            _finalScoreText.color = Color.white;

            // Restart Button
            var btnGo = new GameObject("RestartButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(_gameOverPanel.transform, false);
            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = new Vector2(0, -40);
            btnRect.sizeDelta = new Vector2(200, 50);

            var btnImg = btnGo.GetComponent<Image>();
            btnImg.color = new Color(0.2f, 0.6f, 0.2f);

            var btnTextGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnTextRect = btnTextGo.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;

            var btnText = btnTextGo.GetComponent<Text>();
            btnText.text = "Restart";
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 28;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;

            _restartButton = btnGo.GetComponent<Button>();
            _restartButton.onClick.AddListener(RestartGame);
        }
    }
}
