using System;
using System.Collections;
using System.Collections.Generic;

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
                clone.Gun1 = SmithGeneric.Clone(tmp);
            }

            return clone;
        }

        private void DeclareArray()
        {
            int[] arr;
            arr = new int[50];
        }

        //private static void LoopSample(Agent obj, Agent clone)
        //{
        //    clone.Guns = new Gun[obj.Guns.Length];
        //    IList list = obj.Guns;

        //    var index = 0;

        //    loop_start:

        //    var item = list[index];

        //    clone.Guns[index] = (Gun) item;

        //    index += 1;
        //    if (index < list.Count)
        //        goto loop_start;
        //}

        public void SetContext(Hashtable context)
        {
            
        }
    }
}
