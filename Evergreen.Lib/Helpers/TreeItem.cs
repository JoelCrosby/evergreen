using System.Collections.Generic;

namespace Evergreen.Lib.Helpers
{
    public record TreeItem<T>(T Item, IEnumerable<TreeItem<T>> Children);
}
