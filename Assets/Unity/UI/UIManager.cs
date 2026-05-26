using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Match3.Core;

namespace Match3.Unity
{
    /// <summary>점수/콤보/게임오버/힌트/타이머 UI 관리</summary>
    public class UIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _comboText;
        [SerializeField] private Text _timerText;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private Text _finalScoreText;
        [SerializeField] private Button _restartButton;

        [Header("Hint")]
        [SerializeField] private Button _hintButton;
        [SerializeField] private Text _hintText;
        [SerializeField] private Text _hintCountText;

        [Header("Settings")]
        [SerializeField] private float _gameTimeSeconds = 90f;
        [SerializeField] private int _maxHints = 5;

        private GameController _gameController;
        private float _timeRemaining;
        private int _hintCount;
        private bool _hintActive;
        private bool _isGameOver;
        private bool _timerPaused;

        private void Awake()
        {
            if (_scoreText == null)
                _scoreText = CreateText("ScoreText", new Vector2(-Screen.width * 0.4f, Screen.height * 0.45f),
                    "Score: 0", 36, TextAnchor.UpperLeft);

            if (_timerText == null)
            {
                _timerText = CreateText("TimerText", new Vector2(Screen.width * 0.4f, Screen.height * 0.45f),
                    "90", 40, TextAnchor.UpperRight);
                _timerText.color = Color.white;
            }

            if (_comboText == null)
            {
                _comboText = CreateText("ComboText", new Vector2(0, Screen.height * 0.35f),
                    "", 28, TextAnchor.UpperCenter);
                _comboText.gameObject.SetActive(false);
            }

            if (_gameOverPanel == null)
                CreateGameOverPanel();

            if (_hintButton == null)
                CreateHintButton();

            if (_hintText == null)
            {
                _hintText = CreateText("HintText", new Vector2(0, -Screen.height * 0.4f),
                    "", 22, TextAnchor.LowerCenter);
                _hintText.gameObject.SetActive(false);
            }
        }

        public void Initialize(GameController controller)
        {
            _gameController = controller;

            _gameController.Score.OnScoreChanged += OnScoreChanged;
            _gameController.OnChainCombo += OnChainCombo;
            _gameController.OnGameOver += OnCoreGameOver;

            if (_hintButton != null)
                _hintButton.onClick.AddListener(OnHintButtonClicked);

            var input = _gameController.Input as UnityInputHandler;
            if (input != null)
                input.OnTileSwapped += (a, b) => { if (_hintActive) HideHint(); };

            ResetGame();
        }

        private void ResetGame()
        {
            _timeRemaining = _gameTimeSeconds;
            _hintCount = _maxHints;
            _hintActive = false;
            _isGameOver = false;
            _timerPaused = false;

            UpdateHintButton();
            UpdateTimerText();
            UpdateScoreText(0, 0);
        }

        private void Update()
        {
            if (_isGameOver || _gameController == null) return;

            // Idle이 아닐 때는 타이머 일시정지 (애니메이션 중)
            _timerPaused = !_gameController.State.CanInput;

            if (!_timerPaused)
            {
                _timeRemaining -= Time.deltaTime;
                UpdateTimerText();

                if (_timeRemaining <= 0f)
                {
                    _timeRemaining = 0f;
                    TimeUpGameOver();
                }
            }
        }

        private void OnDestroy()
        {
            if (_gameController != null)
            {
                _gameController.Score.OnScoreChanged -= OnScoreChanged;
                _gameController.OnChainCombo -= OnChainCombo;
                _gameController.OnGameOver -= OnCoreGameOver;

                var input = _gameController.Input as UnityInputHandler;
                if (input != null)
                    input.OnTileSwapped -= (a, b) => { if (_hintActive) HideHint(); };
            }
        }

        // ── 점수 / 콤보 ──

        private void OnScoreChanged(int newScore, int delta)
        {
            UpdateScoreText(newScore, delta);
        }

        private void OnChainCombo(int chainCount)
        {
            if (_comboText == null) return;
            _comboText.text = $"{chainCount}x COMBO!";
            _comboText.gameObject.SetActive(true);
            CancelInvoke(nameof(HideCombo));
            Invoke(nameof(HideCombo), 1.5f);
        }

        private void HideCombo()
        {
            if (_comboText != null)
                _comboText.gameObject.SetActive(false);
        }

        // ── 게임오버 ──

        private void OnCoreGameOver()
        {
            // 일반 게임오버 (더 이상 이동 불가)
            if (!_isGameOver) ShowGameOverPanel("No more moves!");
        }

        private void TimeUpGameOver()
        {
            if (_isGameOver) return;
            _isGameOver = true;

            _gameController.ForceGameOver();
            ShowGameOverPanel("Time's Up!");
        }

        private void ShowGameOverPanel(string reason)
        {
            if (_gameOverPanel == null) return;
            _gameOverPanel.SetActive(true);
            if (_finalScoreText != null)
                _finalScoreText.text = $"{reason}\nFinal Score: {_gameController.Score.Score}";
        }

        public void RestartGame()
        {
            if (_gameOverPanel != null)
                _gameOverPanel.SetActive(false);

            if (_hintActive) HideHint();

            if (_gameController != null)
            {
                _gameController.StartGame();
                ResetGame();
            }
        }

        // ── 힌트 ──

        private void OnHintButtonClicked()
        {
            if (_gameController == null || !_gameController.State.CanInput) return;
            if (_hintCount <= 0) return;

            if (_gameController.TryGetHint(out var a, out var b, out var hintResult))
            {
                _hintCount--;
                _hintActive = true;
                UpdateHintButton();

                _hintText.text = hintResult.Description;
                _hintText.gameObject.SetActive(true);

                var renderer = _gameController.Renderer as UnityBoardRenderer;
                renderer?.ShowHint(new List<TilePosition> { a, b });

                CancelInvoke(nameof(HideHint));
                Invoke(nameof(HideHint), 3f);
            }
        }

        private void HideHint()
        {
            _hintActive = false;
            _hintText.gameObject.SetActive(false);

            var renderer = _gameController?.Renderer as UnityBoardRenderer;
            renderer?.ClearHighlights();
        }

        private void UpdateHintButton()
        {
            if (_hintButton == null) return;

            bool available = _hintCount > 0;
            _hintButton.interactable = available;

            var text = _hintButton.GetComponentInChildren<Text>();
            if (text != null)
                text.text = available ? $"💡 Hint ({_hintCount})" : "💡 Hint (0)";

            var img = _hintButton.GetComponent<Image>();
            if (img != null)
                img.color = available ? new Color(0.2f, 0.6f, 0.8f) : new Color(0.3f, 0.3f, 0.3f);
        }

        // ── 타이머 ──

        private void UpdateTimerText()
        {
            if (_timerText == null) return;

            int seconds = Mathf.CeilToInt(_timeRemaining);
            int min = seconds / 60;
            int sec = seconds % 60;
            _timerText.text = $"{min}:{sec:D2}";

            // 10초 미만이면 빨간색
            if (seconds <= 10)
                _timerText.color = Color.Lerp(Color.red, Color.white,
                    Mathf.PingPong(Time.time * 4f, 1f));
            else
                _timerText.color = Color.white;
        }

        // ── UI 생성 ──

        private void UpdateScoreText(int score, int delta)
        {
            if (_scoreText != null)
                _scoreText.text = $"Score: {score}";
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

        private void CreateHintButton()
        {
            var btnGo = new GameObject("HintButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(transform, false);

            var rect = btnGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-80, 80);
            rect.sizeDelta = new Vector2(160, 50);

            var img = btnGo.GetComponent<Image>();
            img.color = new Color(0.2f, 0.6f, 0.8f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(btnGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var btnText = textGo.GetComponent<Text>();
            btnText.text = "💡 Hint (5)";
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 22;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;

            _hintButton = btnGo.GetComponent<Button>();
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

            var scoreGo = new GameObject("FinalScoreText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            scoreGo.transform.SetParent(_gameOverPanel.transform, false);
            var scoreRect = scoreGo.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.5f, 0.5f);
            scoreRect.anchorMax = new Vector2(0.5f, 0.5f);
            scoreRect.anchoredPosition = new Vector2(0, 30);
            scoreRect.sizeDelta = new Vector2(400, 80);

            _finalScoreText = scoreGo.GetComponent<Text>();
            _finalScoreText.text = "Game Over!";
            _finalScoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _finalScoreText.fontSize = 36;
            _finalScoreText.alignment = TextAnchor.MiddleCenter;
            _finalScoreText.color = Color.white;

            var btnGo = new GameObject("RestartButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(_gameOverPanel.transform, false);
            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = new Vector2(0, -50);
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
