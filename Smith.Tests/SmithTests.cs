using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Smith.Tests
{
    [TestClass]
    public class SmithTests
    {
        [TestMethod]
        public void ShouldDeepClone()
        {
            var smith = new Agent
            {
                Name = "Smith",
                Age = 42,
                IsAlive = true,
                Guns1 = new[]
                {
                    new Gun { Type = GunType.SmithWessonChampion },
                    new Gun { Type = GunType.SmithWessonMP15 }
                },
                Guns3 = new List<Gun>
                {
                    new Gun { Type = GunType.SmithWessonChampion },
                    new Gun { Type = GunType.SmithWessonMP15 }
                },
                IntArray = new[] { 1, 2, 3 },
                IntList = new List<int> { 1, 2, 3 },
                GunDictionary = new Dictionary<string, Gun>
                {
                    { "Gun1", new Gun { Type = GunType.SmithWessonChampion } },
                    { "Gun2", new Gun { Type = GunType.SmithWessonMP15 } }
                }
            };

            smith.Gun2 = new Gun
            {
                Agent = smith,
                Type = GunType.SmithWessonChampion
            };

            var clone = (Agent)Smith.Clone(smith, new Hashtable());

            Assert.IsNotNull(clone);

            Assert.AreNotEqual(smith, clone);

            Assert.AreEqual(smith.Name, clone.Name);
            Assert.AreEqual(smith.Age, clone.Age);
            Assert.AreEqual(smith.IsAlive, clone.IsAlive);

            Assert.IsNull(clone.Gun1);

            Assert.AreNotEqual(smith.Gun2, clone.Gun2);
            Assert.AreEqual(smith.Gun2.Type, clone.Gun2.Type);
            Assert.AreEqual(clone, clone.Gun2.Agent);

            Assert.AreEqual(smith.Guns1.Length, clone.Guns1.Length);
            for (var i = 0; i < smith.Guns1.Length; i++)
            {
                Assert.AreNotEqual(smith.Guns1[i], clone.Guns1[i]);
                Assert.AreEqual(smith.Guns1[i].Type, clone.Guns1[i].Type);
            }

            Assert.IsNull(clone.Guns2);

            Assert.AreEqual(smith.Guns3.Count, clone.Guns3.Count);
            for (var i = 0; i < smith.Guns3.Count; i++)
            {
                Assert.AreNotEqual(smith.Guns3[i], clone.Guns3[i]);
                Assert.AreEqual(smith.Guns3[i].Type, clone.Guns3[i].Type);
            }

            Assert.AreEqual(smith.IntArray.Length, clone.IntArray.Length);
            for (var i = 0; i < smith.IntArray.Length; i++)
            {
                Assert.AreEqual(smith.IntArray[i], clone.IntArray[i]);
            }

            Assert.AreEqual(smith.IntList.Count, clone.IntList.Count);
            for (var i = 0; i < smith.IntList.Count; i++)
            {
                Assert.AreEqual(smith.IntList[i], clone.IntList[i]);
            }

            Assert.AreEqual(smith.GunDictionary.Count, clone.GunDictionary.Count);
            foreach (var key in smith.GunDictionary.Keys)
            {
                Assert.IsTrue(clone.GunDictionary.ContainsKey(key));

                var originalValue = smith.GunDictionary[key];
                var cloneValue = clone.GunDictionary[key];

                Assert.AreNotEqual(originalValue, cloneValue);
                Assert.AreEqual(originalValue.Type, cloneValue.Type);
            }
        }

        [TestMethod]
        public void ShouldDeepCloneGeneric()
        {
            var smith = new Agent
            {
                Name = "Smith",
                Age = 42,
                IsAlive = true,
                Guns1 = new[]
                {
                    new Gun { Type = GunType.SmithWessonChampion },
                    new Gun { Type = GunType.SmithWessonMP15 }
                },
                Guns3 = new List<Gun>
                {
                    new Gun { Type = GunType.SmithWessonChampion },
                    new Gun { Type = GunType.SmithWessonMP15 }
                },
                IntArray = new[] { 1, 2, 3 },
                IntList = new List<int> { 1, 2, 3 }
            };

            smith.Gun1 = new Gun
            {
                Agent = smith,
                Type = GunType.SmithWessonChampion
            };

            var clone = SmithGeneric.Clone(smith);

            Assert.IsNotNull(clone);

            Assert.AreNotEqual(smith, clone);

            Assert.AreEqual(smith.Name, clone.Name);
            Assert.AreEqual(smith.Age, clone.Age);
            Assert.AreEqual(smith.IsAlive, clone.IsAlive);

            Assert.AreNotEqual(smith.Gun1, clone.Gun1);
            Assert.AreEqual(smith.Gun1.Type, clone.Gun1.Type);
            Assert.AreEqual(clone, clone.Gun1.Agent);

            Assert.IsNull(clone.Gun2);

            Assert.AreEqual(smith.Guns1.Length, clone.Guns1.Length);
            for (var i = 0; i < smith.Guns1.Length; i++)
            {
                Assert.AreNotEqual(smith.Guns1[i], clone.Guns1[i]);
                Assert.AreEqual(smith.Guns1[i].Type, clone.Guns1[i].Type);
            }

            Assert.IsNull(clone.Guns2);

            Assert.AreEqual(smith.Guns3.Count, clone.Guns3.Count);
            for (var i = 0; i < smith.Guns3.Count; i++)
            {
                Assert.AreNotEqual(smith.Guns3[i], clone.Guns3[i]);
                Assert.AreEqual(smith.Guns3[i].Type, clone.Guns3[i].Type);
            }

            Assert.AreEqual(smith.IntArray.Length, clone.IntArray.Length);
            for (var i = 0; i < smith.IntArray.Length; i++)
            {
                Assert.AreEqual(smith.IntArray[i], clone.IntArray[i]);
            }

            Assert.AreEqual(smith.IntList.Count, clone.IntList.Count);
            for (var i = 0; i < smith.IntList.Count; i++)
            {
                Assert.AreEqual(smith.IntList[i], clone.IntList[i]);
            }
        }
    }

    public class Agent
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public bool? IsAlive { get; set; }
        public Gun Gun1 { get; set; }
        public Gun Gun2 { get; set; }
        public Gun[] Guns1 { get; set; }
        public Gun[] Guns2 { get; set; }
        public List<Gun> Guns3 { get; set; }
        public int[] IntArray { get; set; }
        public List<int> IntList { get; set; }
        public Dictionary<string, Gun> GunDictionary { get; set; }
    }

    public class Gun
    {
        public GunType Type { get; set; }
        public Agent Agent { get; set; }
    }

    public enum GunType
    {
        SmithWessonMP15,
        SmithWessonChampion
    }
}
