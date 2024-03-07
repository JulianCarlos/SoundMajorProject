using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Pathfinding
{
    [DefaultExecutionOrder(120)]
    public class NavigationLink : MonoBehaviour
    {
        [SerializeField] private NavigationSubLink link1;
        [SerializeField] private NavigationSubLink link2;

        private void Start()
        {
            GenerateLinks();
            LinkVolumes();
        }

        private void GenerateLinks()
        {
            Collider[] collisions;
            int mask = LayerMask.GetMask("NavigationVolume");

            collisions = Physics.OverlapSphere(link1.transform.position, 1f, mask);
            link2.LinkedVolume = collisions[0].GetComponent<NavigationVolume>();
            link2.NeighborLink = link1;

            collisions = Physics.OverlapSphere(link2.transform.position, 1f, mask);
            link1.LinkedVolume = collisions[0].GetComponent<NavigationVolume>();
            link1.NeighborLink = link2;
        }

        private void LinkVolumes()
        {
            link1.LinkedVolume.Links.Add(link2);
            link2.LinkedVolume.Links.Add(link1);
        }

        private void OnDrawGizmos()
        {
            if (link1 == null || link2 == null)
                return;

            Gizmos.color = Color.white;
            Gizmos.DrawLine(link1.transform.position, link2.transform.position);
        }
    }
}

