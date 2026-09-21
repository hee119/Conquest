using System;
using UnityEngine;

public class PredictionBulletMove : BulletMove
{
    [SerializeField]
    private float predictTime;
    public override void Move()
    {
        Vector2 targetPosition = target.transform.position;

        if (target.CompareTag("Player"))
        {
            PlayerMove playerMove = target.GetComponent<PlayerMove>();
            
            Vector2 playerMoveDir = playerMove.moveDir;

            float playerSpeed = playerMove.speed;

            targetPosition += playerMoveDir * playerSpeed * predictTime;
        }

        Vector2 direction = targetPosition - (Vector2)transform.position;
        rb.linearVelocity = direction.normalized * bulletInfo.speed;
    }
}
