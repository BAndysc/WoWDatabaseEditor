using System.Collections.Generic;
using System.Diagnostics;

namespace WDE.Common.Utils.DragDrop
{
    public class DropUtils<T>
    {
        private readonly IReadOnlyList<T> originalList;
        private readonly IList<T> itemsToDrop;
        private readonly int dropIndex;
        private readonly int realDropIndex;
        private readonly List<(T, int)> itemAndIndices = new();

        public DropUtils(IReadOnlyList<T> originalList, IList<T> itemsToDrop, int dropIndex)
        {
            this.originalList = originalList;
            this.itemsToDrop = itemsToDrop;
            this.dropIndex = dropIndex;

            realDropIndex = dropIndex;
            foreach (var item in itemsToDrop)
            {
                var index = originalList.IndexOf(item);
                if (index < dropIndex)
                    realDropIndex--;
                itemAndIndices.Add((item, index));
            }
            
            itemAndIndices.Sort((a, b) => a.Item2.CompareTo(b.Item2));
        }

        public void DoDrop(IList<T> list)
        {
            foreach (var item in itemsToDrop)
            {
                list.Remove(item);
            }

            for (int i = 0; i < itemsToDrop.Count; i++)
            {
                list.Insert(realDropIndex + i, itemsToDrop[i]);
            }
        }

        public void Undo(IList<T> list)
        {
            for (int i = 0; i < itemsToDrop.Count; ++i)
            {
                Debug.Assert(EqualityComparer<T>.Default.Equals(itemsToDrop[i], list[realDropIndex]));
                list.RemoveAt(realDropIndex);
            }
            
            foreach (var pair in itemAndIndices)
            {
                list.Insert(pair.Item2, pair.Item1);
            }
        }
    }
}