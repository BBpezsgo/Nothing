using UnityEngine;

namespace Utilities.Drawers
{
    internal readonly struct CrossDrawer
    {
        internal static void Draw(Vector2 center, float innerSize, float outerSize, float thickness, Color color, Color shadowColor)
        {
            CrossDrawer.Draw(center + Vector2.one, innerSize, outerSize, thickness, shadowColor);
            CrossDrawer.Draw(center, innerSize, outerSize, thickness, color);
        }
        internal static void Draw(Vector2 center, float innerSize, float outerSize, float thickness, Color color)
        {
            Vector2 innerPointV = Vector2.up * innerSize;
            Vector2 outerPointV = Vector2.up * outerSize;
            Vector2 innerPointH = Vector2.left * innerSize;
            Vector2 outerPointH = Vector2.left * outerSize;

            GLUtils.DrawLine(center + innerPointV, center + outerPointV, thickness, color);
            GLUtils.DrawLine(center - innerPointV, center - outerPointV, thickness, color);
            GLUtils.DrawLine(center + innerPointH, center + outerPointH, thickness, color);
            GLUtils.DrawLine(center - innerPointH, center - outerPointH, thickness, color);
        }

        internal static void DrawCrossOrRect(Vector2 cross, float innerSize, float outerSize, Rect? rect, float animation, Color color, Color shadowColor)
        {
            if (!rect.HasValue || animation == 0f)
            {
                CrossDrawer.Draw(cross, innerSize, outerSize, 1f, color, shadowColor);
                return;
            }

            Rect _rect = rect.Value;
            if (animation != 1f)
            {
                CrossDrawer.DrawCornerBoxFromCross(_rect.center + Vector2.one, _rect.size, 8f, cross + Vector2.one, innerSize, outerSize, animation, shadowColor);
                CrossDrawer.DrawCornerBoxFromCross(_rect.center, _rect.size, 8f, cross, innerSize, outerSize, animation, color);
            }
            else
            {
                CornerBoxDrawer.Draw(_rect.center + Vector2.one, _rect.size, 8f, shadowColor);
                CornerBoxDrawer.Draw(_rect.center, _rect.size, 8f, color);
            }
        }

        internal static void DrawCornerBoxFromCross(Vector2 boxCenter, Vector2 boxSize, float boxCornerSize, Vector2 crossCenter, float crossInnerSize, float crossOuterSize, float t, Color color)
        {
            Vector2 halfSize = boxSize / 2;

            float boxCornerSizeWidth = Mathf.Min(halfSize.x, boxCornerSize);
            float boxCornerSizeHeight = Mathf.Min(halfSize.y, boxCornerSize);

            Vector2 innerPointV = Vector2.up * crossInnerSize;
            Vector2 outerPointV = Vector2.up * crossOuterSize;
            Vector2 innerPointH = Vector2.left * crossInnerSize;
            Vector2 outerPointH = Vector2.left * crossOuterSize;

            (Vector2 Inner, Vector2 Outer) crossUp = (innerPointV, outerPointV);
            (Vector2 Inner, Vector2 Outer) crossDown = (-innerPointV, -outerPointV);
            (Vector2 Inner, Vector2 Outer) crossRight = (innerPointH, outerPointH);
            (Vector2 Inner, Vector2 Outer) crossLeft = (-innerPointH, -outerPointH);

            Vector2 center = Vector2.Lerp(crossCenter, boxCenter, t);

            {
                GL.Begin(GL.LINES);
                GL.Color(color);

                Vector2 crossLeftOuter = new(Mathf.Lerp(crossLeft.Outer.x, halfSize.x, t), crossLeft.Outer.y);
                Vector2 crossRightOuter = new(Mathf.Lerp(crossRight.Outer.x, -halfSize.x, t), crossRight.Outer.y);

                GL.Vertex(center + Vector2.Lerp(crossLeft.Inner, crossLeftOuter, t));
                GL.Vertex(center + crossLeftOuter);

                GL.Vertex(center + Vector2.Lerp(crossRight.Inner, crossRightOuter, t));
                GL.Vertex(center + crossRightOuter);

                GL.End();
            }

            Vector2 topleft = new(-halfSize.x, -halfSize.y);
            Vector2 topright = new(halfSize.x, -halfSize.y);
            Vector2 bottomleft = new(-halfSize.x, halfSize.y);
            Vector2 bottomright = new(halfSize.x, halfSize.y);

            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(center + Vector2.Lerp(crossDown.Outer, new Vector2(topleft.x + boxCornerSizeWidth, topleft.y), t));
                GL.Vertex(center + Vector2.Lerp(crossDown.Outer, topleft, t));
                GL.Vertex(center + Vector2.Lerp(crossDown.Inner, new Vector2(topleft.x, topleft.y + boxCornerSizeHeight), t));
                GL.End();
            }

            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(center + Vector2.Lerp(crossDown.Outer, new Vector2(topright.x - boxCornerSizeWidth, topright.y), t));
                GL.Vertex(center + Vector2.Lerp(crossDown.Outer, topright, t));
                GL.Vertex(center + Vector2.Lerp(crossDown.Inner, new Vector2(topright.x, topright.y + boxCornerSizeHeight), t));
                GL.End();
            }

            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(center + Vector2.Lerp(crossUp.Outer, new Vector2(bottomleft.x + boxCornerSizeWidth, bottomleft.y), t));
                GL.Vertex(center + Vector2.Lerp(crossUp.Outer, bottomleft, t));
                GL.Vertex(center + Vector2.Lerp(crossUp.Inner, new Vector2(bottomleft.x, bottomleft.y - boxCornerSizeHeight), t));
                GL.End();
            }

            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(center + Vector2.Lerp(crossUp.Outer, new Vector2(bottomright.x - boxCornerSizeWidth, bottomright.y), t));
                GL.Vertex(center + Vector2.Lerp(crossUp.Outer, bottomright, t));
                GL.Vertex(center + Vector2.Lerp(crossUp.Inner, new Vector2(bottomright.x, bottomright.y - boxCornerSizeHeight), t));
                GL.End();
            }
        }
    }

    internal readonly struct DiagonalCrossDrawer
    {
        internal static void Draw(Vector2 center, float innerSize, float outerSize, float thickness, Color color, Color shadowColor)
        {
            DiagonalCrossDrawer.Draw(center + Vector2.one, innerSize, outerSize, thickness, shadowColor);
            DiagonalCrossDrawer.Draw(center, innerSize, outerSize, thickness, color);
        }
        internal static void Draw(Vector2 center, float innerSize, float outerSize, float thickness, Color color)
        {
            Vector2 innerPointV = DiagonalVectors.TopRight * innerSize;
            Vector2 outerPointV = DiagonalVectors.TopRight * outerSize;
            Vector2 innerPointH = DiagonalVectors.TopLeft * innerSize;
            Vector2 outerPointH = DiagonalVectors.TopLeft * outerSize;

            GLUtils.DrawLine(center + innerPointV, center + outerPointV, thickness, color);
            GLUtils.DrawLine(center - innerPointV, center - outerPointV, thickness, color);
            GLUtils.DrawLine(center + innerPointH, center + outerPointH, thickness, color);
            GLUtils.DrawLine(center - innerPointH, center - outerPointH, thickness, color);
        }

        internal static void DrawCrossOrRect(Vector2 cross, float innerSize, float outerSize, Rect? rect, float animation, Color color, Color shadowColor)
        {
            if (!rect.HasValue || animation == 0f)
            {
                DiagonalCrossDrawer.Draw(cross, innerSize, outerSize, 1f, color, shadowColor);
                return;
            }

            Rect _rect = rect.Value;
            if (animation != 1f)
            {
                DiagonalCrossDrawer.DrawCornerBoxFromCross(_rect.center + Vector2.one, _rect.size, 8f, cross + Vector2.one, innerSize, outerSize, animation, shadowColor);
                DiagonalCrossDrawer.DrawCornerBoxFromCross(_rect.center, _rect.size, 8f, cross, innerSize, outerSize, animation, color);
            }
            else
            {
                CornerBoxDrawer.Draw(_rect.center + Vector2.one, _rect.size, 8f, shadowColor);
                CornerBoxDrawer.Draw(_rect.center, _rect.size, 8f, color);
            }
        }

        internal static void DrawCornerBoxFromCross(Vector2 boxCenter, Vector2 boxSize, float boxCornerSize, Vector2 crossCenter, float crossInnerSize, float crossOuterSize, float t, Color color)
        {
            Vector2 halfSize = boxSize / 2;

            float boxCornerSizeWidth = Mathf.Min(halfSize.x, boxCornerSize);
            float boxCornerSizeHeight = Mathf.Min(halfSize.y, boxCornerSize);

            Vector2 innerPointV = DiagonalVectors.TopRight * crossInnerSize;
            Vector2 outerPointV = DiagonalVectors.TopRight * crossOuterSize;
            Vector2 innerPointH = DiagonalVectors.TopLeft * crossInnerSize;
            Vector2 outerPointH = DiagonalVectors.TopLeft * crossOuterSize;

            (Vector2 Inner, Vector2 Outer) crossUp = (innerPointV, outerPointV);
            (Vector2 Inner, Vector2 Outer) crossDown = (-innerPointV, -outerPointV);
            (Vector2 Inner, Vector2 Outer) crossRight = (innerPointH, outerPointH);
            (Vector2 Inner, Vector2 Outer) crossLeft = (-innerPointH, -outerPointH);

            Vector2 center = Vector2.Lerp(crossCenter, boxCenter, t);

            {
                GL.Begin(GL.LINES);
                GL.Color(color);

                Vector2 crossLeftOuter = new(Mathf.Lerp(crossLeft.Outer.x, halfSize.x, t), crossLeft.Outer.y);
                Vector2 crossRightOuter = new(Mathf.Lerp(crossRight.Outer.x, -halfSize.x, t), crossRight.Outer.y);

                GL.Vertex(center + Vector2.Lerp(crossLeft.Inner, crossLeftOuter, t));
                GL.Vertex(center + crossLeftOuter);

                GL.Vertex(center + Vector2.Lerp(crossRight.Inner, crossRightOuter, t));
                GL.Vertex(center + crossRightOuter);

                GL.End();
            }

            Vector2 topleft = new(-halfSize.x, -halfSize.y);
            Vector2 topright = new(halfSize.x, -halfSize.y);
            Vector2 bottomleft = new(-halfSize.x, halfSize.y);
            Vector2 bottomright = new(halfSize.x, halfSize.y);

            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(center + Vector2.Lerp(crossDown.Outer, new Vector2(topleft.x + boxCornerSizeWidth, topleft.y), t));
                GL.Vertex(center + Vector2.Lerp(crossDown.Outer, topleft, t));
                GL.Vertex(center + Vector2.Lerp(crossDown.Inner, new Vector2(topleft.x, topleft.y + boxCornerSizeHeight), t));
                GL.End();
            }

            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(center + Vector2.Lerp(crossDown.Outer, new Vector2(topright.x - boxCornerSizeWidth, topright.y), t));
                GL.Vertex(center + Vector2.Lerp(crossDown.Outer, topright, t));
                GL.Vertex(center + Vector2.Lerp(crossDown.Inner, new Vector2(topright.x, topright.y + boxCornerSizeHeight), t));
                GL.End();
            }

            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(center + Vector2.Lerp(crossUp.Outer, new Vector2(bottomleft.x + boxCornerSizeWidth, bottomleft.y), t));
                GL.Vertex(center + Vector2.Lerp(crossUp.Outer, bottomleft, t));
                GL.Vertex(center + Vector2.Lerp(crossUp.Inner, new Vector2(bottomleft.x, bottomleft.y - boxCornerSizeHeight), t));
                GL.End();
            }

            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(center + Vector2.Lerp(crossUp.Outer, new Vector2(bottomright.x - boxCornerSizeWidth, bottomright.y), t));
                GL.Vertex(center + Vector2.Lerp(crossUp.Outer, bottomright, t));
                GL.Vertex(center + Vector2.Lerp(crossUp.Inner, new Vector2(bottomright.x, bottomright.y - boxCornerSizeHeight), t));
                GL.End();
            }
        }
    }

    internal readonly struct Cross3Drawer
    {
        static readonly Vector2 Direction1 = new(Mathf.Cos((float)(Mathf.Deg2Rad * (120 * 0 + 30))), Mathf.Sin((float)(Mathf.Deg2Rad * (120 * 0 + 30))));
        static readonly Vector2 Direction2 = new(Mathf.Cos((float)(Mathf.Deg2Rad * (120 * 1 + 30))), Mathf.Sin((float)(Mathf.Deg2Rad * (120 * 1 + 30))));
        static readonly Vector2 Direction3 = new(Mathf.Cos((float)(Mathf.Deg2Rad * (120 * 2 + 30))), Mathf.Sin((float)(Mathf.Deg2Rad * (120 * 2 + 30))));

        static (Line Line1, Line Line2, Line Line3) GetLines(Vector2 center, float innerSize, float outerSize)
        {
            Vector2 innerPoint1 = Direction1 * innerSize;
            Vector2 outerPoint1 = Direction1 * outerSize;

            Vector2 innerPoint2 = Direction2 * innerSize;
            Vector2 outerPoint2 = Direction2 * outerSize;

            Vector2 innerPoint3 = Direction3 * innerSize;
            Vector2 outerPoint3 = Direction3 * outerSize;

            Line line1 = new(center + innerPoint1, center + outerPoint1);
            Line line2 = new(center + innerPoint2, center + outerPoint2);
            Line line3 = new(center + innerPoint3, center + outerPoint3);

            return (line1, line2, line3);
        }

        static (Line Line1, Line Line2, Line Line3) GetLines(float innerSize, float outerSize)
        {
            Vector2 innerPoint1 = Direction1 * innerSize;
            Vector2 outerPoint1 = Direction1 * outerSize;

            Vector2 innerPoint2 = Direction2 * innerSize;
            Vector2 outerPoint2 = Direction2 * outerSize;

            Vector2 innerPoint3 = Direction3 * innerSize;
            Vector2 outerPoint3 = Direction3 * outerSize;

            Line line1 = new(innerPoint1, outerPoint1);
            Line line2 = new(innerPoint2, outerPoint2);
            Line line3 = new(innerPoint3, outerPoint3);

            return (line1, line2, line3);
        }

        internal static void Draw(Vector2 center, float innerSize, float outerSize, float thickness, Color color, Color shadowColor)
        {
            Cross3Drawer.Draw(center + Vector2.one, innerSize, outerSize, thickness, shadowColor);
            Cross3Drawer.Draw(center, innerSize, outerSize, thickness, color);
        }
        internal static void Draw(Vector2 center, float innerSize, float outerSize, float thickness, Color color)
        {
            Vector2 innerPoint1 = Direction1 * innerSize;
            Vector2 outerPoint1 = Direction1 * outerSize;

            Vector2 innerPoint2 = Direction2 * innerSize;
            Vector2 outerPoint2 = Direction2 * outerSize;

            Vector2 innerPoint3 = Direction3 * innerSize;
            Vector2 outerPoint3 = Direction3 * outerSize;

            GLUtils.DrawLine(center + innerPoint1, center + outerPoint1, thickness, color);
            GLUtils.DrawLine(center + innerPoint2, center + outerPoint2, thickness, color);
            GLUtils.DrawLine(center + innerPoint3, center + outerPoint3, thickness, color);
        }

        internal static void DrawCrossOrRect(Vector2 cross, float innerSize, float outerSize, Rect? rect, float animation, Color color, Color shadowColor)
        {
            if (!rect.HasValue || animation == 0f)
            {
                Cross3Drawer.Draw(cross, innerSize, outerSize, 1f, color, shadowColor);
                return;
            }

            Rect _rect = rect.Value;
            if (animation != 1f)
            {
                Cross3Drawer.DrawCornerBoxFromCross(_rect.center + Vector2.one, _rect.size, 8f, cross + Vector2.one, innerSize, outerSize, animation, shadowColor);
                Cross3Drawer.DrawCornerBoxFromCross(_rect.center, _rect.size, 8f, cross, innerSize, outerSize, animation, color);
            }
            else
            {
                CornerBoxDrawer.Draw(_rect.center + Vector2.one, _rect.size, 8f, shadowColor);
                CornerBoxDrawer.Draw(_rect.center, _rect.size, 8f, color);
            }
        }

        internal static void DrawCornerBoxFromCross(Vector2 boxCenter, Vector2 boxSize, float boxCornerSize, Vector2 crossCenter, float crossInnerSize, float crossOuterSize, float t, Color color)
        {
            CornerBoxDrawer.Draw(boxCenter, boxSize, boxCornerSize, color);
        }
    }

    internal readonly struct CornerBoxDrawer
    {
        internal static void Draw(Vector2 center, Vector2 size, float cornerSize, Color color)
        {
            if (size.x <= .1f || size.y <= .1f)
            { return; }

            Vector2 halfSize = size / 2;

            float cornerSizeWidth = Mathf.Min(halfSize.x, cornerSize);
            float cornerSizeHeight = Mathf.Min(halfSize.y, cornerSize);

            Vector2 topleft = new(-halfSize.x, -halfSize.y);
            Vector2 topright = new(halfSize.x, -halfSize.y);
            Vector2 bottomleft = new(-halfSize.x, halfSize.y);
            Vector2 bottomright = new(halfSize.x, halfSize.y);

            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(center + new Vector2(topleft.x + cornerSizeWidth, topleft.y));
                GL.Vertex(center + topleft);
                GL.Vertex(center + new Vector2(topleft.x, topleft.y + cornerSizeHeight));
                GL.End();
            }

            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(center + new Vector2(topright.x - cornerSizeWidth, topright.y));
                GL.Vertex(center + topright);
                GL.Vertex(center + new Vector2(topright.x, topright.y + cornerSizeHeight));
                GL.End();
            }

            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(center + new Vector2(bottomleft.x + cornerSizeWidth, bottomleft.y));
                GL.Vertex(center + bottomleft);
                GL.Vertex(center + new Vector2(bottomleft.x, bottomleft.y - cornerSizeHeight));
                GL.End();
            }

            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(color);
                GL.Vertex(center + new Vector2(bottomright.x - cornerSizeWidth, bottomright.y));
                GL.Vertex(center + bottomright);
                GL.Vertex(center + new Vector2(bottomright.x, bottomright.y - cornerSizeHeight));
                GL.End();
            }
        }
    }

    internal readonly struct DiagonalVectors
    {
        internal static readonly float Sqrt2 = Mathf.Sqrt(2);

        internal static Vector2 TopLeft => new(-Sqrt2, Sqrt2);
        internal static Vector2 TopRight => new(Sqrt2, Sqrt2);
        internal static Vector2 BottomLeft => new(-Sqrt2, -Sqrt2);
        internal static Vector2 BottomRight => new(Sqrt2, -Sqrt2);
    }
}