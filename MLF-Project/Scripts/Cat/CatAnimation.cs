using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CatAnimation : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;
    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (agent.velocity.magnitude > 0.1f)
            animator.SetBool("isWalking", true);
        else
            animator.SetBool("isWalking", false);
    }

}
