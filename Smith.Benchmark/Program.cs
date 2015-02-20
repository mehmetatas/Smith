using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using Smith.Benchmark.TestImpl;

namespace Smith.Benchmark
{
    class Program
    {
        static void Main(string[] args)
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

            var times = 100000;
            Test(new MySmithImpl(), smith, times);
            Test(new ProtoBufImpl(), smith, times);
            Test(new ExtensionImpl(), smith, times);
            Test(new SerializeImpl(), smith, times);
            //Test(new NuclexSerialization(), smith, times);
            //Test(new NuclexReflection(), smith, times);
            //Test(new NuclexExpTree(), smith, times);

            Console.ReadLine();
        }

        private static void Test(ISmithTest<Agent> test, Agent agent, int times)
        {
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < times; i++)
            {
                test.Clone(agent);   
            }
            sw.Stop();

            Console.WriteLine(times + " times with " + test.ToString().Substring("Smith.Benchmark.TestImpl.".Length) + " " + sw.ElapsedMilliseconds + " ms.");
        }
    }

    [Serializable]
    [DataContract]
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

    [Serializable]
    [DataContract]
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
