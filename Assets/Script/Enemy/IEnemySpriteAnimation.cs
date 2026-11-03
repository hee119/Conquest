using UnityEngine;

public interface IEnemySpriteAnimation
{
    Sprite[] AttackFrames { get; }
    Sprite[] MoveFrames { get; }
    Sprite[] HitFrames { get; }
    bool Attack();
    bool Move();
    bool Hit();
}
