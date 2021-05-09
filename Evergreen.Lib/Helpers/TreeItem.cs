using System.Collections.Generic;

namespace Evergreen.Lib.Helpers
{
    public record TreeItem<T>
    {
        public T Item { get; init; }

        public IEnumerable<TreeItem<T>> Children { get; init; }
    }
}
