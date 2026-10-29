using UnityEngine;

public sealed class PlaySoundOnEnable : MonoBehaviour
{
    [SerializeField] private int soundId = SoundIds.SwordHit;

    private void OnEnable()
    {
        SoundManager.Instance?.Play(soundId);
    }
}
