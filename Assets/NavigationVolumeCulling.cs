using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class NavigationVolumeCulling : MonoBehaviour
{
    [SerializeField] private Color volumeColor = new Color(0f, 1f, 0.85f, 0.72f);

    private void OnValidate()
    {
        GetComponent<BoxCollider>().size = Vector3.one;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        Gizmos.color = volumeColor;
        Gizmos.DrawCube(Vector3.zero, new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z));
    }
}
