using UnityEngine;
using Match3.Core;

namespace Match3.Unity
{
    /// <summary>각 Gem GameObject에 붙는 컴포넌트</summary>
    public class Gem : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        public GemType Type { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }

        public SpriteRenderer SpriteRenderer
        {
            get
            {
                if (_spriteRenderer == null)
                    _spriteRenderer = GetComponent<SpriteRenderer>();
                return _spriteRenderer;
            }
        }

        private void Awake()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        public void SetSprite(Sprite sprite)
        {
            if (SpriteRenderer != null && sprite != null)
                SpriteRenderer.sprite = sprite;
        }

        public void SetColor(Color color)
        {
            if (SpriteRenderer != null)
                SpriteRenderer.color = color;
        }
    }
}
