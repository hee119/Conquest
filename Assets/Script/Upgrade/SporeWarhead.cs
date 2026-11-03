using UnityEngine;
using System;
using System.Collections;

/// <summary>SporeWarhead 증강 관련 동작과 상태를 관리한다.</summary>
public class SporeWarhead : Mold
{
    /// <summary>TargetMoldCor 동작을 처리한다.</summary>
    public override IEnumerator TargetMoldCor()
    {
        animator.enabled = false;
        int i = 1;
        transform.localRotation = Quaternion.Euler(0, 0, 0);

        Debug.Log("Mushroom 시작 / Sprite 개수 : " + sprites.Count);

        while (i < sprites.Count)
        {
            /// <summary>WaitForSeconds 동작을 처리한다.</summary>
            yield return new WaitForSeconds(0.3f);

            Debug.Log(
                $"변경 전 : {spriteRenderer.sprite?.name} → 변경 : {sprites[i]?.name}"
            );

            spriteRenderer.sprite = sprites[i];

            Debug.Log(
                $"변경 후 : {spriteRenderer.sprite?.name}"
            );

            i++;
        }

        Debug.Log("Boom!");
        animator.enabled = true;
        animator.SetTrigger("Boom");
    }
}