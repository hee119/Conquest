using UnityEngine;
using System;
using System.Collections;

public class SporeWarhead : Mold
{
    public override IEnumerator TargetMoldCor()
    {
        animator.enabled = false;
        int i = 1;
        transform.localRotation = Quaternion.Euler(0, 0, 0);

        Debug.Log("Mushroom 시작 / Sprite 개수 : " + sprites.Count);

        while (i < sprites.Count)
        {
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