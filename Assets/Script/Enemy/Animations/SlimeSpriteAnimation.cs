public sealed class SlimeSpriteAnimation : EnemySpriteAnimation
{
    protected override void Dispatch(string function)
    {
        if (function == "PlayJumpSound") GetComponent<SlimeJumpSound>()?.PlayJumpSound();
    }
}
