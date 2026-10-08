# Editable Polygon Fill

Editable Polygon Fill is a uGUI `MaskableGraphic` for creating filled custom shapes from hand-placed corner points. It is edited like a 2D edge path: add corners, drag them, and the last corner closes the path so the inside can be filled.

## Use in the current project

1. Create a UI GameObject under a Canvas and add **UI > Editable Polygon Fill**.
2. Set its RectTransform to the area the polygon should occupy.
3. Select **Edit Corners in Scene** in the Inspector. Use **Clear Corners** if you want to draw a new shape from scratch.
4. Left-click an existing point in the Scene view to choose where to continue the outline. Shift-click inside the RectTransform to insert the next point after it; a preview line shows the connection. Drag the numbered handles to adjust points. The outline connects the points and closes from the last point back to the first.
5. Set **Fill Amount** and **Fill Direction**, then choose the Graphic color or optional texture.

The corner list is normalized to the RectTransform: `(0,0)` is bottom-left and `(1,1)` is top-right. Concave simple polygons are supported. Self-intersecting paths and duplicate/collinear corners may not triangulate; move or remove those points.

## Animate the fill

```csharp
using Conquest.PolygonFill;
using UnityEngine;

public class ExampleFillDriver : MonoBehaviour
{
    [SerializeField] private PolygonFillGraphic fill;

    public void SetProgress(float normalizedValue)
    {
        fill.FillAmount = normalizedValue;
    }
}
```

For a custom polygon that reveals across a fixed area, keep its RectTransform stretched over the bar and connect a Slider's **On Value Changed** event to `PolygonFillGraphic.SetFillAmount(float)`. This clips the polygon face from the chosen direction without changing its authored points. If you instead assign its RectTransform to Unity's **Fill Rect**, leave this component's **Fill Amount** at `1`; the Slider will resize that area as usual.

## Distribution

This is an embedded UPM package. Copy `Packages/com.conquest.polygon-fill` to another Unity project's `Packages` folder, or create a `.tgz` archive of that folder and add it with **Window > Package Manager > + > Install package from tarball**. It depends on `com.unity.ugui`.

The package does not execute edit-mode callbacks that rewrite the point list or RectTransform. Corner changes are explicit and support Unity Undo.
