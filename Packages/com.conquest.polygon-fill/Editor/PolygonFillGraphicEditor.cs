using Conquest.PolygonFill;
using UnityEditor;
using UnityEngine;

namespace Conquest.PolygonFill.Editor
{
    [CustomEditor(typeof(PolygonFillGraphic))]
    public sealed class PolygonFillGraphicEditor : UnityEditor.Editor
    {
        private bool editCorners;
        private int insertionAnchor = -1;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Polygon Editing", EditorStyles.boldLabel);

            PolygonFillGraphic graphic = (PolygonFillGraphic)target;
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(graphic.Corners.Count == 0))
                {
                    if (GUILayout.Button("Clear Corners"))
                    {
                        Undo.RecordObject(graphic, "Clear Polygon Corners");
                        graphic.SetCorners(null);
                        insertionAnchor = -1;
                        EditorUtility.SetDirty(graphic);
                    }
                }

                if (GUILayout.Button("Add Corner"))
                {
                    Undo.RecordObject(graphic, "Add Polygon Corner");
                    Vector2[] corners = CopyCorners(graphic);
                    Vector2 next = corners.Length == 0
                        ? new Vector2(0.5f, 0.5f)
                        : (corners[corners.Length - 1] + corners[0]) * 0.5f;
                    graphic.AddCorner(next);
                    insertionAnchor = graphic.Corners.Count - 1;
                    EditorUtility.SetDirty(graphic);
                }

                using (new EditorGUI.DisabledScope(graphic.Corners.Count == 0))
                {
                    if (GUILayout.Button("Remove Last"))
                    {
                        Undo.RecordObject(graphic, "Remove Polygon Corner");
                        graphic.RemoveLastCorner();
                        EditorUtility.SetDirty(graphic);
                    }
                }
            }

            bool nextEditCorners = GUILayout.Toggle(
                editCorners,
                editCorners ? "Finish Editing Corners" : "Edit Corners in Scene",
                "Button"
            );
            if (nextEditCorners != editCorners)
            {
                editCorners = nextEditCorners;
                SceneView.RepaintAll();
            }

            EditorGUILayout.HelpBox(
                editCorners
                    ? "Left-click an existing point to choose where to continue the outline. Shift-click inside the RectTransform to insert the next point after it. Drag handles to move points."
                    : "Enable Scene editing to place and move polygon corners. The point list uses normalized coordinates from 0 to 1.",
                MessageType.Info
            );

            if (serializedObject.ApplyModifiedProperties())
                graphic.SetVerticesDirty();
        }

        private void OnSceneGUI()
        {
            if (!editCorners)
                return;

            PolygonFillGraphic graphic = (PolygonFillGraphic)target;
            RectTransform rectTransform = graphic.rectTransform;
            Event current = Event.current;

            // Keep clicks in the Scene view from selecting another object while drawing.
            if (current.type == EventType.Layout)
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            int count = graphic.Corners.Count;
            Vector3[] worldPoints = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                Vector2 normalized = graphic.Corners[i];
                Vector3 local = new Vector3(
                    Mathf.LerpUnclamped(rectTransform.rect.xMin, rectTransform.rect.xMax, normalized.x),
                    Mathf.LerpUnclamped(rectTransform.rect.yMin, rectTransform.rect.yMax, normalized.y),
                    0f
                );
                worldPoints[i] = rectTransform.TransformPoint(local);
            }

            HandleSelectInsertionAnchor(worldPoints);

            Handles.color = new Color(0.2f, 0.9f, 1f, 1f);
            if (worldPoints.Length >= 2)
            {
                Vector3[] lines = worldPoints;
                if (worldPoints.Length >= 3)
                {
                    lines = new Vector3[worldPoints.Length + 1];
                    worldPoints.CopyTo(lines, 0);
                    lines[lines.Length - 1] = worldPoints[0];
                }

                Handles.DrawAAPolyLine(3f, lines);
            }

            for (int i = 0; i < worldPoints.Length; i++)
            {
                float handleSize = HandleUtility.GetHandleSize(worldPoints[i]) * 0.075f;
                Handles.color = i == insertionAnchor
                    ? new Color(1f, 0.72f, 0.15f, 1f)
                    : new Color(0.2f, 0.9f, 1f, 1f);
                EditorGUI.BeginChangeCheck();
                var fmh_108_21_639268925882861540 = Quaternion.identity; Vector3 moved = Handles.FreeMoveHandle(
                    worldPoints[i],
                    handleSize,
                    Vector3.zero,
                    Handles.DotHandleCap
                );

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(graphic, "Move Polygon Corner");
                    Vector3 local = rectTransform.InverseTransformPoint(moved);
                    Rect rect = rectTransform.rect;
                    Vector2 normalized = new Vector2(
                        Mathf.InverseLerp(rect.xMin, rect.xMax, local.x),
                        Mathf.InverseLerp(rect.yMin, rect.yMax, local.y)
                    );
                    graphic.SetCorner(i, normalized);
                    EditorUtility.SetDirty(graphic);
                }

                Handles.Label(worldPoints[i], $"{i}");
            }

            DrawNextCornerPreview(graphic, rectTransform, worldPoints);
            HandleAddCornerClick(graphic, rectTransform);
        }

        private void HandleSelectInsertionAnchor(Vector3[] worldPoints)
        {
            Event current = Event.current;
            if (current.type != EventType.MouseDown ||
                current.button != 0 ||
                current.shift ||
                current.alt ||
                current.control ||
                current.command)
                return;

            const float selectionRadius = 14f;
            float closestDistance = selectionRadius;
            int closestPoint = -1;
            for (int i = 0; i < worldPoints.Length; i++)
            {
                float distance = Vector2.Distance(current.mousePosition, HandleUtility.WorldToGUIPoint(worldPoints[i]));
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = i;
                }
            }

            if (closestPoint < 0)
                return;

            insertionAnchor = closestPoint;
            SceneView.RepaintAll();
        }

        private void HandleAddCornerClick(PolygonFillGraphic graphic, RectTransform rectTransform)
        {
            Event current = Event.current;
            if (current.type != EventType.MouseDown ||
                current.button != 0 ||
                !current.shift ||
                current.alt ||
                current.control ||
                current.command)
                return;

            if (!TryGetCornerAtMouse(rectTransform, current.mousePosition, out Vector2 normalized))
                return;

            Undo.RecordObject(graphic, "Add Polygon Corner");
            if (insertionAnchor >= 0 && insertionAnchor < graphic.Corners.Count)
            {
                graphic.InsertCornerAfter(insertionAnchor, normalized);
                insertionAnchor++;
            }
            else
            {
                graphic.AddCorner(normalized);
                insertionAnchor = graphic.Corners.Count - 1;
            }

            EditorUtility.SetDirty(graphic);
            current.Use();
            SceneView.RepaintAll();
        }

        private void DrawNextCornerPreview(PolygonFillGraphic graphic, RectTransform rectTransform, Vector3[] worldPoints)
        {
            Event current = Event.current;
            if (current.type == EventType.MouseMove)
                SceneView.RepaintAll();

            if (worldPoints.Length == 0 ||
                !TryGetCornerAtMouse(rectTransform, current.mousePosition, out Vector2 normalized))
                return;

            Rect rect = rectTransform.rect;
            Vector3 local = new Vector3(
                Mathf.LerpUnclamped(rect.xMin, rect.xMax, normalized.x),
                Mathf.LerpUnclamped(rect.yMin, rect.yMax, normalized.y),
                0f
            );
            Vector3 preview = rectTransform.TransformPoint(local);
            Vector3 previewStart = insertionAnchor >= 0 && insertionAnchor < worldPoints.Length
                ? worldPoints[insertionAnchor]
                : worldPoints[worldPoints.Length - 1];
            Handles.color = new Color(0.2f, 0.9f, 1f, 0.65f);
            Handles.DrawAAPolyLine(2f, previewStart, preview);
            Handles.DrawSolidDisc(preview, rectTransform.forward, HandleUtility.GetHandleSize(preview) * 0.045f);
        }

        private static bool TryGetCornerAtMouse(RectTransform rectTransform, Vector2 mousePosition, out Vector2 normalized)
        {
            normalized = default;
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            Plane plane = new Plane(rectTransform.forward, rectTransform.position);
            if (!plane.Raycast(ray, out float distance))
                return false;

            Vector3 local = rectTransform.InverseTransformPoint(ray.GetPoint(distance));
            Rect rect = rectTransform.rect;
            Vector2 localPoint = new Vector2(local.x, local.y);
            if (!rect.Contains(localPoint))
                return false;

            normalized = new Vector2(
                Mathf.InverseLerp(rect.xMin, rect.xMax, local.x),
                Mathf.InverseLerp(rect.yMin, rect.yMax, local.y)
            );
            return true;
        }

        private static Vector2[] CopyCorners(PolygonFillGraphic graphic)
        {
            Vector2[] result = new Vector2[graphic.Corners.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = graphic.Corners[i];
            return result;
        }
    }
}
