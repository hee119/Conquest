public class RangedEnemySpriteAnimation : EnemySpriteAnimation
{
    protected override void Dispatch(string function)
    {
        if (function == "FireProjectile") GetComponent<RamgedEnemyAttack>()?.FireProjectile();
    }
}
