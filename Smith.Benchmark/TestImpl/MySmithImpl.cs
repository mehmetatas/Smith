using System.Collections;

namespace Smith.Benchmark.TestImpl
{
    class MySmithImpl : ISmithTest<Agent>
    {
        public Agent Clone(Agent obj)
        {
            return (Agent)Smith.Clone(obj, new Hashtable());
        }
    }
}
