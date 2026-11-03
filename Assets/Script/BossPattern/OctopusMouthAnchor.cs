using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public sealed class OctopusMouthAnchor : MonoBehaviour
{
    [Serializable]
    public class Frame
    {
        public Sprite sprite;
        public Vector2 position;
        public float angle;
    }
    [SerializeField] private Transform mouth;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private List<Frame> frames = new List<Frame>();
    private readonly Dictionary<Sprite, Frame> lookup = new Dictionary<Sprite, Frame>();
    private void Awake()
    {
        if (body == null) body = GetComponent<SpriteRenderer>();
        foreach (var frame in frames) if (frame.sprite != null) lookup[frame.sprite] = frame;
    }
    private void LateUpdate()
    {
        if (mouth == null || body == null || body.sprite == null || !lookup.TryGetValue(body.sprite, out var frame)) return;
        Vector2 position = frame.position;
        if (body.flipX) position.x = -position.x;
        mouth.localPosition = position;
        mouth.localRotation = Quaternion.Euler(0, 0, body.flipX ? 180f - frame.angle : frame.angle);
    }
}
