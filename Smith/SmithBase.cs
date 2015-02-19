using System.Collections;

namespace Smith
{
    public abstract class SmithBase<T> : ISmith<T> where T : class, new()
    {
        private Hashtable _context;

        public void SetContext(Hashtable context)
        {
            _context = context;
        }

        protected SmithBase()
        {
            _context = new Hashtable();
        }

        protected virtual TProp CloneProp<TProp>(TProp original) where TProp : class, new()
        {
            if (_context.Contains(original))
            {
                return (TProp)_context[original];
            }

            return Smith.GetSmith<TProp>(_context).Clone(original);
        }

        public virtual T Clone(T original)
        {
            var clone = new T();
            _context.Add(original, clone);

            DeepClone(original, clone);

            return clone;
        }

        protected abstract void DeepClone(T original, T clone);
    }
}
