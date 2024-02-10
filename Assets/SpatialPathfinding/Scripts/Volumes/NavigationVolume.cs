using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class NavigationVolume : MonoBehaviour
{
    [SerializeField] private Vector3Int size = new Vector3Int(80, 35, 50);

    [SerializeField] private Color volumeColor = new Color(0f, 1f, 0.85f, 0.72f);

    private void OnDrawGizmos()
    {
        Gizmos.color = volumeColor;
        Gizmos.DrawCube(transform.position, size);
    }
}
