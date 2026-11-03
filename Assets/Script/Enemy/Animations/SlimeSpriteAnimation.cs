public sealed class SlimeSpriteAnimation : EnemySpriteAnimation
{
    protected override void Dispatch(string function)
    {
        if (function == "Attack") GetComponent<MeleeEnemyAttack>()?.Attack();
        else if (function == "PlayJumpSound") GetComponent<SlimeJumpSound>()?.PlayJumpSound();
    }
}
