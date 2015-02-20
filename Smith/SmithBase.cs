using System.Collections;

namespace Smith
{
    public abstract class SmithBase : ISmith
    {
        private Hashtable _context;

        public void SetContext(Hashtable context)
        {
            _context = context;
        }

        protected void AddToContext(object original, object clone)
        {
            _context.Add(original, clone);
        }

        protected virtual object CloneProp(object original)
        {
            return Smith.Clone(original, _context);
        }

        protected virtual void CloneList(IList original, IList clone)
        {
            foreach (var item in original)
            {
                clone.Add(Smith.Clone(item, _context));
            }
        }

        protected virtual void CloneArray(IList original, IList clone)
        {
            for (var i = 0; i < original.Count; i++)
            {
                clone[i] = Smith.Clone(original[i], _context);
            }
        }
        
        protected virtual void CloneDictionary(IDictionary original, IDictionary clone)
        {
            foreach (var key in original.Keys)
            {
                var value = original[key];
                clone.Add(Smith.Clone(key, _context), Smith.Clone(value, _context));
            }
        }

        public abstract object Clone(object original);
    }
}
