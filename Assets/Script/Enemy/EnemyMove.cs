using System;
using System.Collections;
using System.Collections.Generic;
using NavMeshPlus.Components;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMove : MonoBehaviour
{
    public GameObject target;
    public NavMeshAgent agent;
    public float moveSpeed;
    public float stoppingDistance;
    [SerializeField] private float pathUpdateInterval = 0.2f;    
    public Coroutine moveCoroutine;
    void Awake()
    {
        target = GameObject.FindGameObjectWithTag("Player");
        
        agent = GetComponent<NavMeshAgent>();
        agent.updateUpAxis = false;
        agent.updateRotation = false;
    }

    void OnEnable()
    {
        moveCoroutine = StartCoroutine(Move());
    }

    public IEnumerator Move()
    {
        while (true)
        {
            agent.SetDestination(target.transform.position);
            yield return new WaitForSeconds(pathUpdateInterval);
        }
    }

    public void OnDisable()
    {
        StopCoroutine(moveCoroutine);
    }
}
