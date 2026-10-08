using System;
using UnityEngine;

public class PredictionBulletMove : BulletMove
{
    [SerializeField]
    private float predictTime;
    public override void Move()
    {
        if (target == null || !target.activeInHierarchy || rb == null || bulletInfo == null)
        {
            if (pooledObject != null)
                pooledObject.DestroyObj();
            return;
        }
        Vector2 targetPosition = target.transform.position;

        if (target.CompareTag("Player"))
        {
            PlayerMove playerMove = target.GetComponent<PlayerMove>();
            if (playerMove == null)
            {
                if (pooledObject != null)
                    pooledObject.DestroyObj();
                return;
            }
            
            Vector2 playerMoveDir = playerMove.moveDir;

            float playerSpeed = playerMove.speed;

            targetPosition += playerMoveDir * playerSpeed * predictTime;
        }

        Vector2 direction = targetPosition - (Vector2)transform.position;
        rb.linearVelocity = direction.normalized * bulletInfo.speed;
    }
}
