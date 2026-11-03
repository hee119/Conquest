public sealed class CrabSpriteAnimation : EnemySpriteAnimation
{
    protected override void Dispatch(string function)
    {
        if (function == "Attack") GetComponent<MeleeEnemyAttack>()?.Attack();
    }
}
