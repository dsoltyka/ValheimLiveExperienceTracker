using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LiveExperienceTracker
{
    /// <summary>
    /// Owns the list of skill rows under the hotbar. Lives on the plugin object for the whole session;
    /// the UI itself is parented to the game's HUD root so it appears, hides and is destroyed along with it.
    /// </summary>
    internal sealed class TrackerHud : MonoBehaviour
    {
        private static readonly Vector3[] Corners = new Vector3[4];

        private readonly Dictionary<Skills.SkillType, SkillRow> _rows = new Dictionary<Skills.SkillType, SkillRow>();
        private readonly List<Skills.SkillType> _expired = new List<Skills.SkillType>();

        private RectTransform _root;
        private RectTransform _hotbar;
        private TMP_FontAsset _font;
        private Material _fontMaterial;
        private bool _rebuildRequested;

        private static Settings S => Plugin.Settings;

        private void OnEnable()
        {
            SkillGainPatches.SkillGained += OnSkillGained;
            S.Changed += RequestRebuild;
        }

        private void OnDisable()
        {
            SkillGainPatches.SkillGained -= OnSkillGained;
            S.Changed -= RequestRebuild;
            DestroyRoot();
        }

        private void OnSkillGained(SkillGain gain)
        {
            if (!EnsureRoot())
            {
                return;
            }

            if (!_rows.TryGetValue(gain.Type, out var row))
            {
                row = SkillRow.Create(_root, _font, _fontMaterial, S);
                _rows.Add(gain.Type, row);
                row.Apply(gain, Time.time + S.DisplaySeconds.Value, S.DecimalPlaces.Value);
                SortRows();
            }
            else
            {
                row.Apply(gain, Time.time + S.DisplaySeconds.Value, S.DecimalPlaces.Value);
            }
        }

        private void LateUpdate()
        {
            // Leaving the world destroys the HUD and, with it, our root and rows. Forget the dead references.
            if (Hud.instance == null || _root == null)
            {
                if (_rows.Count > 0)
                {
                    _rows.Clear();
                }
                _root = null;
                _hotbar = null;
                return;
            }

            if (_rebuildRequested)
            {
                _rebuildRequested = false;
                DestroyRoot();
                return;
            }

            ExpireRows();
            PositionUnderHotbar();
        }

        private void ExpireRows()
        {
            if (_rows.Count == 0)
            {
                return;
            }

            float now = Time.time;
            float fade = S.FadeSeconds.Value;
            foreach (var pair in _rows)
            {
                if (!pair.Value.Tick(now, fade))
                {
                    _expired.Add(pair.Key);
                }
            }

            foreach (var type in _expired)
            {
                _rows[type].Destroy();
                _rows.Remove(type);
            }
            _expired.Clear();
        }

        /// <summary>Rows are kept alphabetical by (localized) skill name.</summary>
        private void SortRows()
        {
            int index = 0;
            foreach (var row in _rows.Values.OrderBy(r => r.SortKey, StringComparer.CurrentCultureIgnoreCase))
            {
                row.Rect.SetSiblingIndex(index++);
            }
        }

        /// <summary>
        /// Pins our top-left corner to the bottom-left of the hotbar. The HotkeyBar rect itself is just a pivot with
        /// the slot icons hanging off it, so the bounds are taken over the bar and its (active) children.
        /// </summary>
        private void PositionUnderHotbar()
        {
            if (_hotbar == null)
            {
                var bar = Hud.instance.GetComponentInChildren<HotkeyBar>(true);
                _hotbar = bar != null ? bar.transform as RectTransform : null;
            }

            if (_hotbar == null)
            {
                // No hotbar found (another mod replaced it?). Fall back to a fixed top-left position.
                _root.anchoredPosition = new Vector2(S.OffsetX.Value, -(S.OffsetY.Value + 90f));
                return;
            }

            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            AccumulateBounds(_hotbar, ref minX, ref minY);
            for (int i = 0; i < _hotbar.childCount; i++)
            {
                var child = _hotbar.GetChild(i) as RectTransform;
                if (child != null && child.gameObject.activeSelf)
                {
                    AccumulateBounds(child, ref minX, ref minY);
                }
            }

            _root.position = new Vector3(minX, minY, _hotbar.position.z);
            _root.anchoredPosition += new Vector2(S.OffsetX.Value, -S.OffsetY.Value);
        }

        private static void AccumulateBounds(RectTransform rect, ref float minX, ref float minY)
        {
            rect.GetWorldCorners(Corners);
            for (int i = 0; i < 4; i++)
            {
                if (Corners[i].x < minX) minX = Corners[i].x;
                if (Corners[i].y < minY) minY = Corners[i].y;
            }
        }

        private bool EnsureRoot()
        {
            var hud = Hud.instance;
            if (hud == null || hud.m_rootObject == null)
            {
                return false;
            }

            if (_root != null)
            {
                return true;
            }

            ResolveFont(hud);

            var go = new GameObject("LiveExperienceTracker", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(hud.m_rootObject.transform, false);

            _root = (RectTransform)go.transform;
            _root.anchorMin = new Vector2(0f, 1f);
            _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(0f, 1f);

            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.spacing = S.RowSpacing.Value;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _hotbar = null;
            return true;
        }

        /// <summary>Match the HUD's own text (font + outline material) so the rows look native; Jotunn's Norse font is the fallback.</summary>
        private void ResolveFont(Hud hud)
        {
            var reference = hud.m_healthText;
            if (reference != null && reference.font != null)
            {
                _font = reference.font;
                _fontMaterial = reference.fontSharedMaterial;
                return;
            }

            _font = GUIManager.Instance != null ? GUIManager.Instance.TMP_Norse : null;
            _fontMaterial = null;
        }

        private void RequestRebuild()
        {
            _rebuildRequested = true;
        }

        private void DestroyRoot()
        {
            _rows.Clear();
            if (_root != null)
            {
                Destroy(_root.gameObject);
            }
            _root = null;
            _hotbar = null;
        }
    }
}
