using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LiveExperienceTracker
{
    /// <summary>
    /// One line of the tracker:  Name  [=====    ]  (current / needed)  +gain
    /// Built from plain uGUI + TextMeshPro so it needs no bundled assets.
    /// </summary>
    internal sealed class SkillRow
    {
        public string SortKey { get; private set; }
        public RectTransform Rect { get; }

        private readonly CanvasGroup _group;
        private readonly TextMeshProUGUI _name;
        private readonly TextMeshProUGUI _value;
        private readonly TextMeshProUGUI _gain;
        private readonly RectTransform _fill;
        private float _expiresAt;

        private SkillRow(RectTransform rect, CanvasGroup group, TextMeshProUGUI name, TextMeshProUGUI value, TextMeshProUGUI gain, RectTransform fill)
        {
            Rect = rect;
            _group = group;
            _name = name;
            _value = value;
            _gain = gain;
            _fill = fill;
        }

        public static SkillRow Create(Transform parent, TMP_FontAsset font, Material fontMaterial, Settings s)
        {
            var go = new GameObject("SkillRow", typeof(RectTransform), typeof(CanvasGroup), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);

            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = s.ElementSpacing.Value;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var name = CreateText(go.transform, "Name", font, fontMaterial, s.FontSize.Value, s.SkillNameColor.Value);
            var fill = CreateBar(go.transform, s);
            var value = CreateText(go.transform, "Value", font, fontMaterial, s.FontSize.Value, s.ValueColor.Value);
            var gain = CreateText(go.transform, "Gain", font, fontMaterial, s.FontSize.Value, s.GainColor.Value);

            return new SkillRow((RectTransform)go.transform, go.GetComponent<CanvasGroup>(), name, value, gain, fill);
        }

        public void Apply(in SkillGain gain, float expiresAt, int decimals)
        {
            string format = "F" + Mathf.Clamp(decimals, 0, 6);
            var culture = CultureInfo.InvariantCulture;

            SortKey = gain.Name;
            _name.text = gain.Name;
            _value.text = "(" + gain.Accumulator.ToString(format, culture) + " / " + gain.Required.ToString(format, culture) + ")";
            _gain.text = "+" + gain.Gained.ToString(format, culture);

            float pct = gain.Required > 0f ? Mathf.Clamp01(gain.Accumulator / gain.Required) : 0f;
            _fill.anchorMax = new Vector2(pct, 1f);

            _expiresAt = expiresAt;
            _group.alpha = 1f;
        }

        /// <summary>Advances the row's lifetime. Returns false once it should be removed.</summary>
        public bool Tick(float now, float fadeSeconds)
        {
            float remaining = _expiresAt - now;
            if (remaining <= 0f)
            {
                return false;
            }

            _group.alpha = fadeSeconds > 0f ? Mathf.Clamp01(remaining / fadeSeconds) : 1f;
            return true;
        }

        public void Destroy()
        {
            if (Rect != null)
            {
                Object.Destroy(Rect.gameObject);
            }
        }

        private static TextMeshProUGUI CreateText(Transform parent, string objectName, TMP_FontAsset font, Material fontMaterial, int fontSize, Color color)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }
            if (fontMaterial != null)
            {
                text.fontSharedMaterial = fontMaterial;
            }
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateBar(Transform parent, Settings s)
        {
            var bar = new GameObject("Bar", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            bar.transform.SetParent(parent, false);

            var size = bar.GetComponent<LayoutElement>();
            size.preferredWidth = s.BarWidth.Value;
            size.preferredHeight = s.BarHeight.Value;
            size.minWidth = s.BarWidth.Value;
            size.minHeight = s.BarHeight.Value;

            var background = bar.GetComponent<Image>();
            background.color = s.BarBackgroundColor.Value;
            background.raycastTarget = false;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(bar.transform, false);
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = new Vector2(1f, 1f);
            fillRect.offsetMax = new Vector2(-1f, -1f);

            var fillImage = fill.GetComponent<Image>();
            fillImage.color = s.BarFillColor.Value;
            fillImage.raycastTarget = false;

            return fillRect;
        }
    }
}
