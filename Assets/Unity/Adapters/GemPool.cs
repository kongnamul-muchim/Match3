using System.Collections.Generic;
using UnityEngine;

namespace Match3.Unity
{
    /// <summary>Gem 오브젝트 풀 — Instantiate 최소화</summary>
    public class GemPool
    {
        private readonly Gem _template;
        private readonly Transform _parent;
        private readonly Queue<Gem> _available = new Queue<Gem>();
        private readonly List<Gem> _active = new List<Gem>();

        public GemPool(Gem template, Transform parent, int prewarm = 64)
        {
            _template = template;
            _parent = parent;

            for (int i = 0; i < prewarm; i++)
            {
                var gem = CreateNew();
                gem.gameObject.SetActive(false);
                _available.Enqueue(gem);
            }
        }

        private Gem CreateNew()
        {
            var go = Object.Instantiate(_template.gameObject, _parent);
            go.name = $"Gem_{_active.Count + _available.Count + 1}";
            return go.GetComponent<Gem>();
        }

        public Gem Get()
        {
            Gem gem;
            if (_available.Count > 0)
            {
                gem = _available.Dequeue();
            }
            else
            {
                gem = CreateNew();
            }

            gem.gameObject.SetActive(true);
            gem.transform.localScale = Vector3.one;
            _active.Add(gem);
            return gem;
        }

        public void Release(Gem gem)
        {
            if (gem == null || !_active.Contains(gem)) return;

            _active.Remove(gem);
            gem.gameObject.SetActive(false);
            gem.transform.localScale = Vector3.one;
            _available.Enqueue(gem);
        }

        public void ReleaseAll()
        {
            foreach (var gem in _active.ToArray())
                Release(gem);
        }

        public int ActiveCount => _active.Count;
        public int AvailableCount => _available.Count;
    }
}
