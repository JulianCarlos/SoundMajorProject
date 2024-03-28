using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Pathfinding
{
    public class NavigationSubLink : MonoBehaviour
    {
        public NavigationLink RootLink;

        public NavigationVolume LinkedVolume;

        public NavigationSubLink NeighborLink;
    }
}

