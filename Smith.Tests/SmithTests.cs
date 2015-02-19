using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Smith.Tests
{
    [TestClass]
    public class SmithTests
    {
        [TestMethod]
        public void ShouldCreateNewInstance()
        {
            var smith = new Agent
            {
                Name = "Smith",
                Age = 42,
                IsAlive = true
            };

            smith.Gun2 = new Gun
            {
                Agent = smith,
                Type = GunType.SmithWessonChampion
            };

            var clone = Smith.Clone(smith);

            Assert.IsNotNull(clone);

            Assert.AreNotEqual(smith, clone);

            Assert.AreEqual(smith.Name, clone.Name);
            Assert.AreEqual(smith.Age, clone.Age);

            Assert.IsNull(clone.Gun1);

            Assert.AreNotEqual(smith.Gun2, clone.Gun2);
            Assert.AreEqual(smith.Gun2.Type, clone.Gun2.Type);
            Assert.AreEqual(clone, clone.Gun2.Agent);

            Assert.AreEqual(smith.IsAlive, clone.IsAlive);
        }
    }

    public class Agent
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public bool? IsAlive { get; set; }
        public Gun Gun1 { get; set; }
        public Gun Gun2 { get; set; }
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
