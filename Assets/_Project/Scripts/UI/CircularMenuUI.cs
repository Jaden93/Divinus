using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DivinePrototype
{
    [System.Serializable]
    public class IntUnityEvent : UnityEvent<int> { }

    /// <summary>
    /// Overlay centrato a schermo.
    /// Sfondo scuro cliccabile per chiudere + icone in cerchio al centro.
    /// Tap su icona → onItemSelected(index) → menu si chiude.
    /// </summary>
    public class CircularMenuUI : MonoBehaviour
    {
        [Header("Layout")]
        public float radius      = 160f;
        public float animDuration = 0.2f;

        [Header("Overlay")]
        public Color overlayColor = new Color(0f, 0f, 0f, 0.55f);

        [Header("Evento")]
        public IntUnityEvent onItemSelected;

        // Runtime
        private bool          _isOpen;
        private RectTransform[] _items;
        private Vector2[]     _openPositions;
        private Coroutine     _anim;
        private Image         _overlayImg;
        private RectTransform _itemsRoot;

        private void Awake()
        {
            // Stretch a tutto schermo
            var rt = GetComponent<RectTransform>();
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.sizeDelta        = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            // ── Overlay scuro (fondo cliccabile per chiudere) ─────────────
            var overlayGO = new GameObject("OverlayBg");
            overlayGO.transform.SetParent(transform, false);
            overlayGO.transform.SetAsFirstSibling();

            _overlayImg       = overlayGO.AddComponent<Image>();
            _overlayImg.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0f);

            var oRT = overlayGO.GetComponent<RectTransform>();
            oRT.anchorMin = Vector2.zero; oRT.anchorMax = Vector2.one;
            oRT.sizeDelta = Vector2.zero; oRT.anchoredPosition = Vector2.zero;

            var closeBtn = overlayGO.AddComponent<Button>();
            closeBtn.targetGraphic = _overlayImg;
            var cc = ColorBlock.defaultColorBlock;
            cc.normalColor = cc.highlightedColor = cc.pressedColor = cc.selectedColor = Color.white;
            closeBtn.colors = cc;
            closeBtn.onClick.AddListener(CloseMenu);

            // ── Contenitore item centrato ─────────────────────────────────
            var itemsGO = new GameObject("ItemsRoot");
            itemsGO.transform.SetParent(transform, false);

            _itemsRoot = itemsGO.AddComponent<RectTransform>();
            _itemsRoot.anchorMin        = new Vector2(0.5f, 0.5f);
            _itemsRoot.anchorMax        = new Vector2(0.5f, 0.5f);
            _itemsRoot.pivot            = new Vector2(0.5f, 0.5f);
            _itemsRoot.anchoredPosition = Vector2.zero;
            _itemsRoot.sizeDelta        = Vector2.zero;

            // Sposta i figli esistenti (AxeIcon, HomeIcon, BenchIcon) sotto ItemsRoot
            var toMove = new List<Transform>();
            foreach (Transform child in transform)
                if (child.gameObject != overlayGO && child.gameObject != itemsGO)
                    toMove.Add(child);
            foreach (var child in toMove)
                child.SetParent(_itemsRoot, false);

            // Raccoglie array item
            var list = new List<RectTransform>();
            foreach (Transform child in _itemsRoot)
                list.Add(child as RectTransform);
            _items = list.ToArray();

            // Aggiunge Button su ogni item per intercettare tap
            for (int i = 0; i < _items.Length; i++)
            {
                int idx = i;
                var btn = _items[i].GetComponent<Button>();
                if (btn == null)
                {
                    // Serve un'Image per il Button; se non esiste, usa quella esistente o crea
                    var img = _items[i].GetComponent<Image>();
                    btn = _items[i].gameObject.AddComponent<Button>();
                    if (img != null) btn.targetGraphic = img;
                }
                btn.onClick.AddListener(() => OnItemTapped(idx));
            }

            BuildPositions();

            // Parte nascosto
            gameObject.SetActive(false);
        }

        // ── Posizioni in cerchio ──────────────────────────────────────────

        private void BuildPositions()
        {
            _openPositions = new Vector2[_items.Length];
            if (_items.Length == 0) return;

            if (_items.Length == 1)
            {
                _openPositions[0] = new Vector2(0f, radius);
                return;
            }

            float arcTotal = _items.Length <= 3 ? 180f : 270f;
            float arcStart = 90f + arcTotal * 0.5f;
            float step     = arcTotal / (_items.Length - 1);

            for (int i = 0; i < _items.Length; i++)
            {
                float deg = arcStart - step * i;
                float rad = deg * Mathf.Deg2Rad;
                _openPositions[i] = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
            }
        }

        // ── API pubblica ──────────────────────────────────────────────────

        public bool IsOpen => _isOpen;

        public void OpenMenu()
        {
            if (_isOpen) return;
            _isOpen = true;
            gameObject.SetActive(true);
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(DoOpen());
        }

        public void CloseMenu()
        {
            if (!_isOpen) return;
            _isOpen = false;
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(DoClose());
        }

        /// <summary>
        /// Chiamato quando un item inizia un drag dall'interno del menu.
        /// Nasconde overlay e tutti gli altri item senza disattivare il GO corrente
        /// (che disattivarsi distruggerebbe il drag in corso).
        /// </summary>
        public void OnItemBeginDrag(RectTransform draggingItem)
        {
            if (_anim != null) { StopCoroutine(_anim); _anim = null; }
            _isOpen = false;

            // Nascondi overlay (non blocca più input)
            if (_overlayImg != null) _overlayImg.gameObject.SetActive(false);

            // Nascondi tutti gli item tranne quello trascinato
            foreach (var item in _items)
                if (item != draggingItem)
                    item.gameObject.SetActive(false);
        }

        /// <summary>
        /// Chiamato quando il drag termina. Chiude completamente il menu.
        /// </summary>
        public void OnItemEndDrag()
        {
            _isOpen = false;
            if (_anim != null) { StopCoroutine(_anim); _anim = null; }

            if (_overlayImg != null) _overlayImg.gameObject.SetActive(false);
            foreach (var item in _items)
            {
                item.anchoredPosition = Vector2.zero;
                item.localScale       = Vector3.one;
                item.gameObject.SetActive(false);
            }
            gameObject.SetActive(false);
        }

        // ── Event handler ─────────────────────────────────────────────────

        private void OnItemTapped(int index)
        {
            CloseMenu();
            if (onItemSelected != null) onItemSelected.Invoke(index);
        }

        // ── Animazioni ────────────────────────────────────────────────────

        private IEnumerator DoOpen()
        {
            foreach (var item in _items)
            {
                item.anchoredPosition = Vector2.zero;
                item.localScale       = Vector3.one * 0.3f;
                item.gameObject.SetActive(true);
            }
            _overlayImg.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0f);

            float elapsed = 0f;
            while (elapsed < animDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / animDuration));
                _overlayImg.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, overlayColor.a * t);
                for (int i = 0; i < _items.Length; i++)
                {
                    _items[i].anchoredPosition = Vector2.Lerp(Vector2.zero, _openPositions[i], t);
                    _items[i].localScale       = Vector3.Lerp(Vector3.one * 0.3f, Vector3.one, t);
                }
                yield return null;
            }
            _overlayImg.color = overlayColor;
            for (int i = 0; i < _items.Length; i++)
            {
                _items[i].anchoredPosition = _openPositions[i];
                _items[i].localScale       = Vector3.one;
            }
        }

        private IEnumerator DoClose()
        {
            var startPos = new Vector2[_items.Length];
            for (int i = 0; i < _items.Length; i++)
                startPos[i] = _items[i].anchoredPosition;

            float elapsed = 0f;
            while (elapsed < animDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / animDuration));
                _overlayImg.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, overlayColor.a * (1f - t));
                for (int i = 0; i < _items.Length; i++)
                {
                    _items[i].anchoredPosition = Vector2.Lerp(startPos[i], Vector2.zero, t);
                    _items[i].localScale       = Vector3.Lerp(Vector3.one, Vector3.one * 0.3f, t);
                }
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}
