using System.Collections;

namespace Smith.Tests
{
    class SmithMock : ISmith<Agent>
    {
        public Agent Clone(Agent obj)
        {
            var clone = new Agent();

            clone.Name = obj.Name;

            var tmp = obj.Gun1;
            if (tmp == null)
            {

            }
            else
            {
                clone.Gun1 = Smith.Clone(tmp);
            }

            return clone;
        }

        public void SetContext(Hashtable context)
        {
            
        }
    }
}
