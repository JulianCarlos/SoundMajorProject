using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElvenRotator : MonoBehaviour
{
    [SerializeField] private Vector3 _rotationAxis;
    [SerializeField] private float _speedMultiplier;


    void Update()
    {
        transform.Rotate(_rotationAxis * _speedMultiplier * Time.deltaTime);
    }
}
