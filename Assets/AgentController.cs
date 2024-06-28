using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentController : MonoBehaviour
{
    [SerializeField] private Transform target;

    private FlyingAgent controller;

    void Start()
    {
        controller = GetComponent<FlyingAgent>();
    }

    void FixedUpdate()
    {
        controller.MoveTo(target.position);
    }
}
