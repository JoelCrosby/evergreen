using System.Collections.Generic;

namespace Evergreen.Core.Helpers
{
    public record TreeItem<T>(T Item, IEnumerable<TreeItem<T>> Children);
}
