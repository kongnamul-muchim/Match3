using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Match3.Core;

namespace Match3.Unity
{
    /// <summary>WebGL 안전 폰트 헬퍼</summary>
    public static class FontHelper
    {
        private static Font _cached;

        public static Font GetDefaultFont()
        {
            if (_cached != null) return _cached;

            _cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_cached != null) return _cached;

            _cached = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (_cached != null) return _cached;

            try { _cached = Font.CreateDynamicFontFromOSFont("Arial", 24); } catch { }

            return _cached;
        }
    }
}

namespace Match3.Unity
{
    /// <summary>점수/콤보/게임오버/힌트/타이머/리더보드 UI 관리</summary>
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

        [Header("Leaderboard")]
        [SerializeField] private InputField _nameInput;
        [SerializeField] private Button _submitButton;
        [SerializeField] private Text _leaderboardText;
        [SerializeField] private Text _statusText;

        private GameController _gameController;
        private float _timeRemaining;
        private int _hintCount;
        private bool _hintActive;
        private bool _isGameOver;
        private bool _timerPaused;
        private LeaderboardClient _leaderboard;

        private const float GameTimeSeconds = 90f;
        private const int MaxHints = 5;

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

            // 리더보드 UI
            if (_leaderboardText == null)
                CreateLeaderboardUI();

        }

        public void Initialize(GameController controller)
        {
            _gameController = controller;

            // Bootstrapper가 LeaderboardClient를 생성한 후에 찾음 (Awake 순서 이슈 방지)
            if (_leaderboard == null)
                _leaderboard = GetComponentInParent<LeaderboardClient>();
            if (_leaderboard == null)
                _leaderboard = FindObjectOfType<LeaderboardClient>();

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
            _timeRemaining = GameTimeSeconds;
            _hintCount = MaxHints;
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

        private void UpdateScoreText(int score, int delta)
        {
            if (_scoreText != null)
                _scoreText.text = $"Score: {score}";
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

            ShowLeaderboard();
        }

        public void RestartGame()
        {
            if (_gameOverPanel != null)
                _gameOverPanel.SetActive(false);

            if (_hintActive) HideHint();

            // 리더보드 UI 초기화
            if (_statusText != null) _statusText.text = "";
            if (_nameInput != null) _nameInput.text = "";
            if (_submitButton != null) _submitButton.interactable = true;

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
            _timerText.text = seconds.ToString();
            _timerText.color = seconds <= 10 ? Color.red : Color.white;
        }

        // ── UI 생성 ──

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
            var font = FontHelper.GetDefaultFont();
            if (font != null) txt.font = font;
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
            rect.sizeDelta = new Vector2(140, 50);

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
            btnText.text = "💡 Hint";
            var hintFont = FontHelper.GetDefaultFont();
            if (hintFont != null) btnText.font = hintFont;
            btnText.fontSize = 24;
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

            // Final Score Text (top area)
            var scoreGo = new GameObject("FinalScoreText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            scoreGo.transform.SetParent(_gameOverPanel.transform, false);
            var scoreRect = scoreGo.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.5f, 0.5f);
            scoreRect.anchorMax = new Vector2(0.5f, 0.5f);
            scoreRect.anchoredPosition = new Vector2(0, 120);
            scoreRect.sizeDelta = new Vector2(600, 80);

            _finalScoreText = scoreGo.GetComponent<Text>();
            _finalScoreText.text = "Game Over!";
            var fsFont = FontHelper.GetDefaultFont();
            if (fsFont != null) _finalScoreText.font = fsFont;
            _finalScoreText.fontSize = 36;
            _finalScoreText.alignment = TextAnchor.MiddleCenter;
            _finalScoreText.color = Color.white;

            // (리더보드 UI는 CreateLeaderboardUI에서 생성)
        }

        // ── 리더보드 ──

        private void CreateLeaderboardUI()
        {
            // 방어코드: _gameOverPanel이 null이면 먼저 생성
            if (_gameOverPanel == null)
                CreateGameOverPanel();

            // 닉네임 입력 (InputField와 Text는 같은 GameObject에 AddComponent 금지)
            var inputGo = new GameObject("NameInput", typeof(RectTransform));
            inputGo.transform.SetParent(_gameOverPanel.transform, false);
            var inputRect = inputGo.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.5f, 0.5f);
            inputRect.anchorMax = new Vector2(0.5f, 0.5f);
            inputRect.anchoredPosition = new Vector2(0, 65);
            inputRect.sizeDelta = new Vector2(260, 36);

            var inputImg = inputGo.AddComponent<Image>();
            inputImg.color = new Color(1, 1, 1, 0.9f);

            // InputField 전용 자식 Text 오브젝트
            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(inputGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var textComp = textGo.GetComponent<Text>();
            var defaultFont = FontHelper.GetDefaultFont();
            if (defaultFont != null) textComp.font = defaultFont;
            textComp.fontSize = 22;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.color = Color.black;
            textComp.text = "";

            var inputField = inputGo.AddComponent<InputField>();
            inputField.textComponent = textComp;

            // Placeholder
            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            phGo.transform.SetParent(inputGo.transform, false);
            var phRect = phGo.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;
            var phText = phGo.GetComponent<Text>();
            phText.text = "닉네임 입력";
            if (defaultFont != null) phText.font = defaultFont;
            phText.fontSize = 22;
            phText.alignment = TextAnchor.MiddleCenter;
            phText.color = new Color(0.5f, 0.5f, 0.5f);
            inputField.placeholder = phText;

            _nameInput = inputField;

            // 상태 텍스트
            _statusText = CreateText("StatusText", new Vector2(0, 30), "", 20, TextAnchor.MiddleCenter);
            _statusText.transform.SetParent(_gameOverPanel.transform, false);

            // Submit 버튼
            var subGo = new GameObject("SubmitButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            subGo.transform.SetParent(_gameOverPanel.transform, false);
            var subRect = subGo.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 0.5f);
            subRect.anchorMax = new Vector2(0.5f, 0.5f);
            subRect.anchoredPosition = new Vector2(-70, -5);
            subRect.sizeDelta = new Vector2(140, 44);
            var subImg = subGo.GetComponent<Image>();
            subImg.color = new Color(0.2f, 0.6f, 0.8f);
            var subText = CreateChildText(subGo, "🏆 등록", 24, Color.white);
            _submitButton = subGo.GetComponent<Button>();
            _submitButton.onClick.AddListener(OnSubmitScore);

            // Restart 버튼
            var rstGo = new GameObject("RestartButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            rstGo.transform.SetParent(_gameOverPanel.transform, false);
            var rstRect = rstGo.GetComponent<RectTransform>();
            rstRect.anchorMin = new Vector2(0.5f, 0.5f);
            rstRect.anchorMax = new Vector2(0.5f, 0.5f);
            rstRect.anchoredPosition = new Vector2(80, -5);
            rstRect.sizeDelta = new Vector2(140, 44);
            var rstImg = rstGo.GetComponent<Image>();
            rstImg.color = new Color(0.2f, 0.6f, 0.2f);
            var rstText = CreateChildText(rstGo, "🔄 Restart", 24, Color.white);
            _restartButton = rstGo.GetComponent<Button>();
            _restartButton.onClick.AddListener(RestartGame);

            // 랭킹 텍스트 영역
            var rankGo = new GameObject("LeaderboardText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            rankGo.transform.SetParent(_gameOverPanel.transform, false);
            var rankRect = rankGo.GetComponent<RectTransform>();
            rankRect.anchorMin = new Vector2(0.5f, 0.5f);
            rankRect.anchorMax = new Vector2(0.5f, 0.5f);
            rankRect.anchoredPosition = new Vector2(0, -90);
            rankRect.sizeDelta = new Vector2(400, 180);

            _leaderboardText = rankGo.GetComponent<Text>();
            var lbFont = FontHelper.GetDefaultFont();
            if (lbFont != null) _leaderboardText.font = lbFont;
            _leaderboardText.fontSize = 18;
            _leaderboardText.alignment = TextAnchor.UpperCenter;
            _leaderboardText.color = new Color(0.9f, 0.9f, 0.9f);
            _leaderboardText.text = "";
        }

        private Text CreateChildText(GameObject parent, string text, int fontSize, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var txt = go.GetComponent<Text>();
            txt.text = text;
            var font = FontHelper.GetDefaultFont();
            if (font != null) txt.font = font;
            txt.fontSize = fontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            return txt;
        }

        // ── 리더보드 API ──

        private void ShowLeaderboard()
        {
            if (_leaderboardText == null) return;
            _leaderboardText.text = "🏆 TOP 10 로딩 중...";
            StartCoroutine(LoadLeaderboardRoutine());
        }

        private IEnumerator LoadLeaderboardRoutine()
        {
            if (_leaderboard == null)
            {
                _leaderboardText.text = "⚠️ LeaderboardClient 없음";
                yield break;
            }

            bool done = false;
            _leaderboard.GetLeaderboard(10, (response) =>
            {
                if (response != null && response.items != null && response.items.Count > 0)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("<b>━━━ TOP 10 ━━━</b>");
                    for (int i = 0; i < response.items.Count; i++)
                    {
                        var e = response.items[i];
                        string medal = i == 0 ? "🥇" : i == 1 ? "🥈" : i == 2 ? "🥉" : $"  {i + 1}.";
                        sb.AppendLine($"{medal} {e.player_name,-12} {e.score,8:N0}");
                    }
                    _leaderboardText.text = sb.ToString();
                }
                else
                {
                    _leaderboardText.text = "🏆 아직 등록된 점수가 없습니다!\n첫 기록의 주인공이 되어보세요!";
                }
                done = true;
            }, (error) =>
            {
                _leaderboardText.text = $"⚠️ 로드 실패: {error}";
                done = true;
            });

            yield return new WaitUntil(() => done);
        }

        private void OnSubmitScore()
        {
            if (_leaderboard == null || _gameController == null) return;
            if (_submitButton != null) _submitButton.interactable = false;

            string name = _nameInput != null ? _nameInput.text.Trim() : "";
            if (string.IsNullOrEmpty(name))
                name = "Player";

            int score = _gameController.Score.Score;
            int combo = _gameController.Score.ComboMultiplier;
            int moves = _gameController.Score.TotalMoves;

            if (_statusText != null)
                _statusText.text = "📤 점수 제출 중...";
            if (_leaderboardText != null)
                _leaderboardText.text = "📤 점수 제출 중...";

            StartCoroutine(SubmitScoreRoutine(name, score, combo, moves));
        }

        private IEnumerator SubmitScoreRoutine(string name, int score, int combo, int moves)
        {
            bool done = false;
            _leaderboard.SubmitScore(name, score, combo, moves,
                (entry) =>
                {
                    if (_statusText != null)
                        _statusText.text = $"✅ 등록 완료! 🏆 Rank #{entry.rank}";
                    if (_submitButton != null) _submitButton.interactable = false;

                    StartCoroutine(LoadLeaderboardRoutine());
                    done = true;
                },
                (error) =>
                {
                    if (_statusText != null)
                        _statusText.text = $"❌ 제출 실패";
                    if (_submitButton != null) _submitButton.interactable = true;
                    if (_leaderboardText != null)
                        _leaderboardText.text = $"⚠️ 오류: {error}";
                    done = true;
                });

            yield return new WaitUntil(() => done);
        }
    }
}
