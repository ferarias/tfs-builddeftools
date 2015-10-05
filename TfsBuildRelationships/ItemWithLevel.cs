using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfsBuildRelationships
{
    public class ItemWithLevel<T>
    {
        public T Item;
        public int Level;
        public ItemWithLevel(T item, int level)
        {
            Item = item;
            Level = level;
        }
    }
}
