using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAnimationData", menuName = "Game/Enemy Animation Data")]
public sealed class EnemyAnimationData : ScriptableObject
{
    public Sprite[] attack;
    public Sprite[] move;
    public Sprite[] hit;
    [Min(0.01f)] public float framesPerSecond = 12f;

    // Timing and event information imported from the original animation clips.
    [HideInInspector] public EnemySpriteAnimation.Sequence[] sequences;
    [HideInInspector] public string defaultState;
}
