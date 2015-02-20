namespace Smith.Benchmark.TestImpl
{
    interface ISmithTest<T>
    {
        T Clone(T obj);
    }
}
