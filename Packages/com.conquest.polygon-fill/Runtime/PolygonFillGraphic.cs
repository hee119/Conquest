using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Conquest.PolygonFill
{
    public enum PolygonFillDirection
    {
        LeftToRight,
        RightToLeft,
        BottomToTop,
        TopToBottom
    }

    [AddComponentMenu("UI/Editable Polygon Fill")]
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
    public sealed class PolygonFillGraphic : MaskableGraphic
    {
        [SerializeField]
        private List<Vector2> corners = new List<Vector2>
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };

        [SerializeField, Range(0f, 1f)]
        private float fillAmount = 1f;

        [SerializeField]
        private PolygonFillDirection fillDirection = PolygonFillDirection.LeftToRight;

        [SerializeField]
        private Texture fillTexture;

        [SerializeField, Min(0f)]
        private float outlineWidth;

        [SerializeField]
        private Color outlineColor = Color.white;

        private readonly List<int> triangles = new List<int>();
        private readonly List<Vector2> clipA = new List<Vector2>(6);
        private readonly List<Vector2> clipB = new List<Vector2>(6);

        public IReadOnlyList<Vector2> Corners => corners;

        public float FillAmount
        {
            get => fillAmount;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(fillAmount, clamped))
                    return;

                fillAmount = clamped;
                SetVerticesDirty();
            }
        }

        public PolygonFillDirection FillDirection
        {
            get => fillDirection;
            set
            {
                if (fillDirection == value)
                    return;

                fillDirection = value;
                SetVerticesDirty();
            }
        }

        public override Texture mainTexture =>
            fillTexture != null ? fillTexture : Texture2D.whiteTexture;

        public void AddCorner(Vector2 normalizedPosition)
        {
            EnsureCornerList();
            corners.Add(ClampPoint(normalizedPosition));
            SetVerticesDirty();
        }

        public void InsertCornerAfter(int index, Vector2 normalizedPosition)
        {
            EnsureCornerList();
            if (index < 0 || index >= corners.Count)
            {
                AddCorner(normalizedPosition);
                return;
            }

            corners.Insert(index + 1, ClampPoint(normalizedPosition));
            SetVerticesDirty();
        }

        public void SetFillAmount(float normalizedAmount)
        {
            FillAmount = normalizedAmount;
        }

        public void SetCorner(int index, Vector2 normalizedPosition)
        {
            EnsureCornerList();
            if (index < 0 || index >= corners.Count)
                return;

            corners[index] = ClampPoint(normalizedPosition);
            SetVerticesDirty();
        }

        public void RemoveLastCorner()
        {
            EnsureCornerList();
            if (corners.Count == 0)
                return;

            corners.RemoveAt(corners.Count - 1);
            SetVerticesDirty();
        }

        public void SetCorners(IEnumerable<Vector2> normalizedPositions)
        {
            EnsureCornerList();
            corners.Clear();

            if (normalizedPositions != null)
            {
                foreach (Vector2 point in normalizedPositions)
                    corners.Add(ClampPoint(point));
            }

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            EnsureCornerList();

            if (corners.Count < 3 || fillAmount <= 0f)
            {
                AddOutline(vertexHelper);
                return;
            }

            triangles.Clear();
            if (!TryTriangulate(corners, triangles))
            {
                AddOutline(vertexHelper);
                return;
            }

            Rect rect = GetPixelAdjustedRect();
            GetClipBoundary(out int axis, out float boundary, out bool keepLess);

            for (int triangleIndex = 0; triangleIndex < triangles.Count; triangleIndex += 3)
            {
                clipA.Clear();
                clipB.Clear();
                clipA.Add(corners[triangles[triangleIndex]]);
                clipA.Add(corners[triangles[triangleIndex + 1]]);
                clipA.Add(corners[triangles[triangleIndex + 2]]);

                ClipToFillBoundary(clipA, clipB, axis, boundary, keepLess);
                if (clipB.Count < 3)
                    continue;

                int firstVertex = vertexHelper.currentVertCount;
                for (int i = 0; i < clipB.Count; i++)
                {
                    Vector2 normalized = clipB[i];
                    Vector2 position = new Vector2(
                        Mathf.LerpUnclamped(rect.xMin, rect.xMax, normalized.x),
                        Mathf.LerpUnclamped(rect.yMin, rect.yMax, normalized.y)
                    );

                    UIVertex vertex = UIVertex.simpleVert;
                    vertex.position = position;
                    vertex.color = color;
                    vertex.uv0 = normalized;
                    vertexHelper.AddVert(vertex);
                }

                for (int i = 1; i < clipB.Count - 1; i++)
                    vertexHelper.AddTriangle(firstVertex, firstVertex + i, firstVertex + i + 1);
            }

            AddOutline(vertexHelper);
        }

        private void AddOutline(VertexHelper vertexHelper)
        {
            if (outlineWidth <= 0f || outlineColor.a <= 0f || corners.Count < 2)
                return;

            Rect rect = GetPixelAdjustedRect();
            float halfWidth = outlineWidth * 0.5f;

            for (int i = 0; i < corners.Count; i++)
            {
                Vector2 start = NormalizedToLocal(corners[i], rect);
                Vector2 end = NormalizedToLocal(corners[(i + 1) % corners.Count], rect);
                Vector2 direction = end - start;
                if (direction.sqrMagnitude <= 0.000001f)
                    continue;

                Vector2 normal = new Vector2(-direction.y, direction.x).normalized * halfWidth;
                int firstVertex = vertexHelper.currentVertCount;
                AddOutlineVertex(vertexHelper, start - normal, corners[i]);
                AddOutlineVertex(vertexHelper, start + normal, corners[i]);
                AddOutlineVertex(vertexHelper, end + normal, corners[(i + 1) % corners.Count]);
                AddOutlineVertex(vertexHelper, end - normal, corners[(i + 1) % corners.Count]);
                vertexHelper.AddTriangle(firstVertex, firstVertex + 1, firstVertex + 2);
                vertexHelper.AddTriangle(firstVertex, firstVertex + 2, firstVertex + 3);
            }
        }

        private void AddOutlineVertex(VertexHelper vertexHelper, Vector2 position, Vector2 uv)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = outlineColor * color;
            vertex.uv0 = uv;
            vertexHelper.AddVert(vertex);
        }

        private static Vector2 NormalizedToLocal(Vector2 normalized, Rect rect)
        {
            return new Vector2(
                Mathf.LerpUnclamped(rect.xMin, rect.xMax, normalized.x),
                Mathf.LerpUnclamped(rect.yMin, rect.yMax, normalized.y)
            );
        }

        private void GetClipBoundary(out int axis, out float boundary, out bool keepLess)
        {
            switch (fillDirection)
            {
                case PolygonFillDirection.RightToLeft:
                    axis = 0;
                    boundary = 1f - fillAmount;
                    keepLess = false;
                    break;
                case PolygonFillDirection.BottomToTop:
                    axis = 1;
                    boundary = fillAmount;
                    keepLess = true;
                    break;
                case PolygonFillDirection.TopToBottom:
                    axis = 1;
                    boundary = 1f - fillAmount;
                    keepLess = false;
                    break;
                default:
                    axis = 0;
                    boundary = fillAmount;
                    keepLess = true;
                    break;
            }
        }

        private static void ClipToFillBoundary(
            List<Vector2> source,
            List<Vector2> destination,
            int axis,
            float boundary,
            bool keepLess)
        {
            if (source.Count == 0)
                return;

            Vector2 previous = source[source.Count - 1];
            bool previousInside = IsInside(previous, axis, boundary, keepLess);

            for (int i = 0; i < source.Count; i++)
            {
                Vector2 current = source[i];
                bool currentInside = IsInside(current, axis, boundary, keepLess);

                if (currentInside)
                {
                    if (!previousInside)
                        destination.Add(Intersect(previous, current, axis, boundary));

                    destination.Add(current);
                }
                else if (previousInside)
                {
                    destination.Add(Intersect(previous, current, axis, boundary));
                }

                previous = current;
                previousInside = currentInside;
            }
        }

        private static bool IsInside(Vector2 point, int axis, float boundary, bool keepLess)
        {
            float value = axis == 0 ? point.x : point.y;
            return keepLess ? value <= boundary + 0.000001f : value >= boundary - 0.000001f;
        }

        private static Vector2 Intersect(Vector2 start, Vector2 end, int axis, float boundary)
        {
            float startValue = axis == 0 ? start.x : start.y;
            float endValue = axis == 0 ? end.x : end.y;
            float denominator = endValue - startValue;
            float t = Mathf.Abs(denominator) <= 0.000001f
                ? 0f
                : Mathf.Clamp01((boundary - startValue) / denominator);
            return Vector2.LerpUnclamped(start, end, t);
        }

        private static bool TryTriangulate(List<Vector2> polygon, List<int> result)
        {
            result.Clear();
            if (polygon == null || polygon.Count < 3)
                return false;

            float signedArea = 0f;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 current = polygon[i];
                Vector2 next = polygon[(i + 1) % polygon.Count];
                signedArea += current.x * next.y - next.x * current.y;
            }

            if (Mathf.Abs(signedArea) <= 0.000001f)
                return false;

            List<int> remaining = new List<int>(polygon.Count);
            if (signedArea > 0f)
            {
                for (int i = 0; i < polygon.Count; i++)
                    remaining.Add(i);
            }
            else
            {
                for (int i = polygon.Count - 1; i >= 0; i--)
                    remaining.Add(i);
            }

            int safety = polygon.Count * polygon.Count;
            while (remaining.Count > 3 && safety-- > 0)
            {
                bool earFound = false;

                for (int i = 0; i < remaining.Count; i++)
                {
                    int previousIndex = remaining[(i - 1 + remaining.Count) % remaining.Count];
                    int currentIndex = remaining[i];
                    int nextIndex = remaining[(i + 1) % remaining.Count];

                    Vector2 a = polygon[previousIndex];
                    Vector2 b = polygon[currentIndex];
                    Vector2 c = polygon[nextIndex];
                    if (Cross(b - a, c - b) <= 0.000001f)
                        continue;

                    bool containsOtherCorner = false;
                    for (int j = 0; j < remaining.Count; j++)
                    {
                        int candidateIndex = remaining[j];
                        if (candidateIndex == previousIndex ||
                            candidateIndex == currentIndex ||
                            candidateIndex == nextIndex)
                            continue;

                        if (IsInsideTriangle(polygon[candidateIndex], a, b, c))
                        {
                            containsOtherCorner = true;
                            break;
                        }
                    }

                    if (containsOtherCorner)
                        continue;

                    result.Add(previousIndex);
                    result.Add(currentIndex);
                    result.Add(nextIndex);
                    remaining.RemoveAt(i);
                    earFound = true;
                    break;
                }

                if (!earFound)
                {
                    result.Clear();
                    return false;
                }
            }

            if (remaining.Count != 3)
                return false;

            result.Add(remaining[0]);
            result.Add(remaining[1]);
            result.Add(remaining[2]);
            return true;
        }

        private static bool IsInsideTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            const float epsilon = -0.000001f;
            return Cross(b - a, point - a) >= epsilon &&
                   Cross(c - b, point - b) >= epsilon &&
                   Cross(a - c, point - c) >= epsilon;
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        private void EnsureCornerList()
        {
            if (corners == null)
                corners = new List<Vector2>();
        }

        private static Vector2 ClampPoint(Vector2 point)
        {
            return new Vector2(Mathf.Clamp01(point.x), Mathf.Clamp01(point.y));
        }
    }
}
