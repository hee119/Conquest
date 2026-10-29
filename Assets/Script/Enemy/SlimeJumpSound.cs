using UnityEngine;

[DisallowMultipleComponent]
public sealed class SlimeJumpSound : MonoBehaviour
{
    private EnemyCombat combat;

    private void Awake()
    {
        combat = GetComponent<EnemyCombat>();
    }

    // Called by the takeoff frame in SlimeJump, not by a movement timer.
    public void PlayJumpSound()
    {
        if (Time.timeScale <= 0f || (combat != null && combat.isDead))
            return;
        SoundManager.Instance?.Play(SoundIds.SlimeJump);
    }
}
