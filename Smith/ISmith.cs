using System.Collections;

namespace Smith
{
    public interface ISmith
    {
        void SetContext(Hashtable context);

        object Clone(object original);
    }

    public class DefaultSmith : ISmith
    {
        public static readonly ISmith Instance = new DefaultSmith();

        private DefaultSmith()
        {
            
        }
        
        public void SetContext(Hashtable context)
        {
            
        }

        public object Clone(object original)
        {
            return original;
        }
    }
}
