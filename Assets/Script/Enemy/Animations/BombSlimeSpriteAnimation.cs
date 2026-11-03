public sealed class BombSlimeSpriteAnimation : EnemySpriteAnimation
{
    protected override void Dispatch(string function)
    {
        if (function == "Boom") GetComponent<BoomSlime>()?.Boom();
    }
}
