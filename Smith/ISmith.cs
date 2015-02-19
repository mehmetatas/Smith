using System.Collections;

namespace Smith
{
    public interface ISmith<T> where T : class, new()
    {
        void SetContext(Hashtable context);

        T Clone(T original);
    }
}
