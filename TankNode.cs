using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankNode : MonoBehaviour
{

    [SerializeField]
    private Transform NodeDestination;
    [SerializeField]
    private Transform PreAimLocation;
    [SerializeField]
    private int PriorityLevel;


    private void Awake()
    {
        NodeDestination = transform;
        PreAimLocation =transform.GetChild(0);
        PriorityLevel = 0;
    }

    public Transform Location()
    {
        return NodeDestination;
    }

    public Transform PreAim()
    {
        return PreAimLocation;
    }

    public int Priority()
    {
        return PriorityLevel;
    }

}
