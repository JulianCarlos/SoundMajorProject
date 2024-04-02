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
        [SerializeField] private NavigationSubLink startLink;
        [SerializeField] private NavigationSubLink endLink;

        [SerializeField] private bool biDirectional = true;

        private void Awake()
        {
            startLink.RootLink = this;
            endLink.RootLink = this;
        }

        private void Start()
        {
            GenerateLinks();
        }

        public bool CheckTraverseAccess(NavigationSubLink link)
        {
            return link == startLink || link == endLink && biDirectional;
        }

        private void GenerateLinks()
        {
            Collider[] collisions;
            int mask = LayerMask.GetMask("NavigationVolume");
            
            try
            {
                collisions = Physics.OverlapSphere(startLink.transform.position, 1f, mask);
                endLink.LinkedVolume = collisions[0].GetComponent<NavigationVolume>();
                endLink.NeighborLink = startLink;

                collisions = Physics.OverlapSphere(endLink.transform.position, 1f, mask);
                startLink.LinkedVolume = collisions[0].GetComponent<NavigationVolume>();
                startLink.NeighborLink = endLink;

                LinkVolumes();
            }
            catch
            {
                Debug.LogWarning($"A link is not inside a Navigation Volume, links only work if both links are inside a valid Volume");
            }

            if (startLink.LinkedVolume == endLink.LinkedVolume)
            {
                Debug.LogWarning($"{startLink} and {endLink} cant be on the same Volume");
            }
        }

        private void LinkVolumes()
        {
            startLink.LinkedVolume.Links.Add(endLink);
            endLink.LinkedVolume.Links.Add(startLink);
        }

        private void OnDrawGizmos()
        {
            if (startLink == null || endLink == null)
                return;

            Gizmos.color = Color.white;
            Gizmos.DrawLine(startLink.transform.position, endLink.transform.position);
        }
    }
}

