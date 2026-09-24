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
    /// Owns the list of skill rows shown above the health panel. Lives on the plugin object for the whole session;
    /// the UI itself is parented to the game's HUD root so it appears, hides and is destroyed along with it.
    /// </summary>
    internal sealed class TrackerHud : MonoBehaviour
    {
        private static readonly Vector3[] Corners = new Vector3[4];

        private readonly Dictionary<Skills.SkillType, SkillRow> _rows = new Dictionary<Skills.SkillType, SkillRow>();
        private readonly List<Skills.SkillType> _expired = new List<Skills.SkillType>();
        private readonly List<RectTransform> _anchorRects = new List<RectTransform>();

        private RectTransform _root;
        private RectTransform _anchor;
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
                _anchor = null;
                return;
            }

            if (_rebuildRequested)
            {
                _rebuildRequested = false;
                DestroyRoot();
                return;
            }

            ExpireRows();
            if (_rows.Count > 0)
            {
                PositionAboveHealthPanel();
            }
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
        /// Pins our bottom-left corner to the top-left of the health panel (health bar, food and guardian power in the
        /// bottom-left of the screen). The panel grows with food/health, so the bounds are taken over it and all of its
        /// active children every frame.
        /// </summary>
        private void PositionAboveHealthPanel()
        {
            if (_anchor == null)
            {
                _anchor = Hud.instance.m_healthPanel;
            }

            if (_anchor == null)
            {
                // No health panel found (another mod replaced it?). Fall back to a fixed bottom-left position.
                _root.anchoredPosition = new Vector2(S.OffsetX.Value, S.OffsetY.Value + 220f);
                return;
            }

            float minX = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;
            _anchor.GetComponentsInChildren(false, _anchorRects);
            foreach (var rect in _anchorRects)
            {
                AccumulateBounds(rect, ref minX, ref maxY);
            }
            _anchorRects.Clear();

            _root.position = new Vector3(minX, maxY, _anchor.position.z);
            _root.anchoredPosition += new Vector2(S.OffsetX.Value, S.OffsetY.Value);
        }

        private static void AccumulateBounds(RectTransform rect, ref float minX, ref float maxY)
        {
            rect.GetWorldCorners(Corners);
            for (int i = 0; i < 4; i++)
            {
                if (Corners[i].x < minX) minX = Corners[i].x;
                if (Corners[i].y > maxY) maxY = Corners[i].y;
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

            // Bottom-left pivot: the list grows upward from the top of the health panel.
            _root = (RectTransform)go.transform;
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.zero;
            _root.pivot = Vector2.zero;

            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.spacing = S.RowSpacing.Value;
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _anchor = null;
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
            _anchor = null;
        }
    }
}
