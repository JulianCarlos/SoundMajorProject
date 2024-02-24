using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Pathfinding;
using UnityEngine;
using UnityEngine.TestTools;

public class PathingManagerTests
{
    [UnityTest]
    public IEnumerator InstanceCheck()
    {
        GameObject pathingManagerObj = new GameObject();
        var pathingManager = pathingManagerObj.AddComponent<PathingManager>();

        pathingManager.CreateInstance();

        yield return null;

        Assert.AreNotEqual(null, PathingManager.Instance);
    }

    [UnityTest]
    public IEnumerator CheckGridCellAmount()
    {
        GameObject pathingManagerObj = new GameObject();
        var pathingManager = pathingManagerObj.AddComponent<PathingManager>();

        pathingManager.CreateInstance();

        GameObject navigationVolumeObj = new GameObject();
        var navigationVolume = navigationVolumeObj.AddComponent<NavigationVolume>();

        navigationVolume.InitializeGrid();

        yield return null;

        Assert.AreEqual(3375, navigationVolume.TotalCells);
    }
}
