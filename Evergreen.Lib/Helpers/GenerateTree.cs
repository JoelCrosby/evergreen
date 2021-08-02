using System;
using System.Collections.Generic;
using System.Linq;

namespace Evergreen.Lib.Helpers
{
    public static class TreeHelpers
    {
        public static IEnumerable<TreeItem<T>> GenerateTree<T, TK>(
            this IEnumerable<T> collection,
            Func<T, TK> idSelector,
            Func<T, TK> parentIdSelector,
            TK rootId = default!)
        {
            var list = collection.ToList();

            return list
                .Where(c => parentIdSelector(c)?.Equals(rootId) ?? false)
                .Select(c => new TreeItem<T>(c, list.GenerateTree(idSelector, parentIdSelector, idSelector(c))))
                .OrderBy(l => l.Children.Any());
        }
    }
}
