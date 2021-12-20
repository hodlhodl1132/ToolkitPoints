using System.Globalization;
using UnityEngine;
using Verse;

namespace ToolkitPoints
{
    [StaticConstructorOnStartup]
    public static class SettingsHelper
    {
        public static readonly Texture2D SortingAscend;
        public static readonly Texture2D SortingDescend;

        static SettingsHelper()
        {
            SortingAscend = ContentFinder<Texture2D>.Get("UI/Icons/Sorting");
            SortingDescend = ContentFinder<Texture2D>.Get("UI/Icons/SortingDescending");
        }

        public static bool WasLeftClicked(this Rect region)
        {
            if (!Mouse.IsOver(region))
            {
                return false;
            }

            Event current = Event.current;
            bool was = current.button == 0;

            switch (current.type)
            {
                case EventType.Used when was:
                case EventType.MouseDown when was:
                    current.Use();
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsRegionVisible(this Rect region, Rect scrollRect, Vector2 scrollPos)
        {
            return (region.y >= scrollPos.y || region.y + region.height - 1f >= scrollPos.y) && region.y <= scrollPos.y + scrollRect.height;
        }

        public static void DrawLabel(Rect region, string text, TextAnchor anchor = TextAnchor.MiddleLeft, GameFont fontScale = GameFont.Small, bool vertical = false)
        {
            Text.Anchor = anchor;
            Text.Font = fontScale;

            if (vertical)
            {
                region.y += region.width;
                GUIUtility.RotateAroundPivot(-90f, region.position);
            }

            Widgets.Label(region, text);

            if (vertical)
            {
                GUI.matrix = Matrix4x4.identity;
            }

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        public static void DrawColoredLabel(
            Rect region,
            string text,
            Color color,
            TextAnchor anchor = TextAnchor.MiddleLeft,
            GameFont fontScale = GameFont.Small,
            bool vertical = false
        )
        {
            GUI.color = color;
            DrawLabel(region, text, anchor, fontScale, vertical);
            GUI.color = Color.white;
        }

        public static bool DrawTableHeader(Rect backgroundRect, bool vertical = false)
        {
            if (vertical)
            {
                backgroundRect.y += backgroundRect.width;
                GUIUtility.RotateAroundPivot(-90f, backgroundRect.position);
            }

            GUI.color = new Color(0.62f, 0.65f, 0.66f);
            Widgets.DrawHighlight(backgroundRect);
            GUI.color = Color.white;

            if (Mouse.IsOver(backgroundRect))
            {
                GUI.color = Color.grey;
                Widgets.DrawLightHighlight(backgroundRect);
                GUI.color = Color.white;
            }

            bool pressed = Widgets.ButtonInvisible(backgroundRect);

            if (vertical)
            {
                GUI.matrix = Matrix4x4.identity;
            }

            return pressed;
        }

        public static void DrawSortIndicator(Rect canvas, SortOrder order)
        {
            var region = new Rect(canvas.x + canvas.width - canvas.height + 3f, canvas.y + 8f, canvas.height - 9f, canvas.height - 16f);

            switch (order)
            {
                case SortOrder.Ascending:
                    GUI.DrawTexture(region, SortingAscend);
                    return;
                case SortOrder.Descending:
                    GUI.DrawTexture(region, SortingDescend);
                    return;
                default:
                    return;
            }
        }

        public static Rect RectForIcon(Rect region)
        {
            float shortest = Mathf.Min(region.width, region.height);
            float half = Mathf.FloorToInt(shortest / 2f);
            Vector2 center = region.center;

            return new Rect(
                Mathf.Clamp(center.x - half, region.x, region.x + region.width),
                Mathf.Clamp(center.y - half, region.y, region.y + region.height),
                shortest,
                shortest
            );
        }

        public static bool DrawTextField(Rect region, string content, out string newContent)
        {
            string text = Widgets.TextField(region, content);

            newContent = !text.Equals(content) ? text : null;
            return newContent != null;
        }

        public static bool DrawFieldIcon(Rect canvas, string label, string tooltip = null)
        {
            var region = new Rect(canvas.x + canvas.width - 16f, canvas.y, 16f, canvas.height);
            GameFont cache = Text.Font;

            Text.Font = GameFont.Medium;
            Widgets.ButtonText(region, label, false);
            Text.Font = cache;

            if (!tooltip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(region, tooltip);
            }

            bool clicked = Mouse.IsOver(region) && Event.current.type == EventType.Used && Input.GetMouseButtonDown(0);

            if (!clicked)
            {
                return false;
            }

            GUIUtility.keyboardControl = 0;
            return true;
        }

        public static bool DrawFieldIcon(Rect canvas, Texture2D icon, string tooltip = null)
        {
            var region = new Rect(canvas.x + canvas.width - canvas.height + 6f, canvas.y + 6f, canvas.height - 12f, canvas.height - 12f);
            Widgets.ButtonImage(region, icon);

            if (!tooltip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(region, tooltip);
            }

            bool clicked = Mouse.IsOver(region) && Event.current.type == EventType.Used && Input.GetMouseButtonDown(0);

            if (!clicked)
            {
                return false;
            }

            GUIUtility.keyboardControl = 0;
            return true;
        }

        public static bool DrawNumberField(Rect region, ref string buffer, out int value, out bool invalid)
        {
            if (!DrawTextField(region, buffer, out string content))
            {
                value = 0;
                invalid = false;
                return false;
            }

            bool wasRemoval = content.Length < buffer.Length;
            string diff = wasRemoval ? buffer.Substring(content.Length) : content.Substring(buffer.Length);
            buffer = content;

            if (!diff.NullOrEmpty() && char.IsNumber(diff, diff.Length - 1) || wasRemoval)
            {
                if (int.TryParse(
                    buffer,
                    NumberStyles.AllowExponent | NumberStyles.AllowThousands | NumberStyles.Integer | NumberStyles.Currency,
                    CultureInfo.CurrentCulture,
                    out value
                ))
                {
                    invalid = false;
                    return true;
                }

                invalid = true;
                return false;
            }

            value = 0;
            invalid = true;
            return false;
        }
    }
}
