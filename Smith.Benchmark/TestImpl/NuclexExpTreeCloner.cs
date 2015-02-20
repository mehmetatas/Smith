using Nuclex.Support.Cloning;

namespace Smith.Benchmark.TestImpl
{
    class NuclexExpTree : ISmithTest<Agent>
    {
        private readonly ICloneFactory _cloneFactory = new ExpressionTreeCloner();
        public Agent Clone(Agent obj)
        {
            return _cloneFactory.DeepPropertyClone(obj);
        }
    }

    class NuclexReflection : ISmithTest<Agent>
    {
        private readonly ICloneFactory _cloneFactory = new ReflectionCloner();
        public Agent Clone(Agent obj)
        {
            return _cloneFactory.DeepPropertyClone(obj);
        }
    }

    class NuclexSerialization : ISmithTest<Agent>
    {
        private readonly ICloneFactory _cloneFactory = new SerializationCloner();
        public Agent Clone(Agent obj)
        {
            return _cloneFactory.DeepPropertyClone(obj);
        }
    }
}
