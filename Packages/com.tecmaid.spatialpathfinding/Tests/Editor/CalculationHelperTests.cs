using NUnit.Framework;
using UnityEngine;
using Unity.Mathematics;
using Pathfinding.Helpers;

namespace Tests
{
    public class CalculationHelperTests
    {
        [Test]
        public void CheckFloatToVectorConversion()
        {
            Assert.AreEqual(new Vector3(15f, 15f, 15f), CalculationHelper.Float3ToVector3(new float3(15f, 15f, 15f)));
        }

        [Test]
        public void CheckInt3ToFloat3Conversion()
        {
            Assert.AreEqual(CalculationHelper.Int3ToFloat3(new int3(15, 15, 15)), new float3(15f, 15f, 15f));
        }
    }
}

