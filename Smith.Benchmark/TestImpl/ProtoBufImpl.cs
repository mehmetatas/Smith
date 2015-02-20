namespace Smith.Benchmark.TestImpl
{
    class ProtoBufImpl : ISmithTest<Agent>
    {
        public Agent Clone(Agent obj)
        {
            return ProtoBuf.Serializer.DeepClone(obj);
        }
    }
}
