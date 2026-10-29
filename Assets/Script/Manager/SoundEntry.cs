using System;
using UnityEngine;
using Sirenix.OdinInspector;

[Serializable]
public sealed class SoundEntry
{
    [LabelText("이름")]
    public string name;
    [LabelText("ID"), MinValue(2000)]
    public int id;
    [LabelText("사운드 파일"), Required]
    public AudioClip clip;
    [LabelText("보정 음량"), Range(0f, 1f)]
    [Tooltip("효과음의 원래 음량 차이를 보정합니다. 전체 효과음 볼륨과 함께 적용됩니다.")]
    public float volume = 1f;
    [LabelText("최대 동시 재생 수"), MinValue(1)]
    public int maxConcurrentVoices = 10;

    [LabelText("적 사망 시 페이드아웃")]
    [Tooltip("적에게 연결된 피격/상태 효과음에 적용됩니다. 발사음과 배경음에는 적용되지 않습니다.")]
    public bool fadeOnEnemyDeath;

    [LabelText("페이드 시간 (초)"), ShowIf(nameof(fadeOnEnemyDeath)), MinValue(0.01f)]
    public float enemyDeathFadeSeconds = 0.15f;
}
