/// <summary>Runtime values applied to a character's basic attack.</summary>
public struct BasicAttackModifiers
{
    public float rangeMultiplier;
    public int attackCount;
    public int pierceCount;
    public int chainCount;
    public float chainRange;
    public float damageMultiplier;

    public static BasicAttackModifiers Default => new BasicAttackModifiers
    {
        rangeMultiplier = 1f,
        attackCount = 1,
        pierceCount = 0,
        chainCount = 0,
        chainRange = 0f,
        damageMultiplier = 1f
    };
}
