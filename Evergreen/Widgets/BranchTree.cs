using System;
using System.Linq;

using Evergreen.Core.Git;
using Evergreen.Core.Git.Models;
using Evergreen.Utils;
using Evergreen.Widgets.Common;

using GLib;

using Gtk;

using Pango;

namespace Evergreen.Widgets
{
    public class BranchTree : TreeWidget, IDisposable
    {
        private const string ChangesItemId = "[evergreen:changes]";
        private TreeStore _store;

        public BranchTree(TreeView view, GitService git) : base(view, git)
        {
            View.HeadersVisible = false;
            View.FixedHeightMode = true;
            View.ButtonPressEvent += BranchTreeOnButtonPress;
            View.CursorChanged += BranchTreeCursorChanged;

            var labelColumn = new TreeViewColumn();
            var cellName = new CellRendererText();

            labelColumn.PackStart(cellName, true);
            labelColumn.AddAttribute(cellName, "text", 0);
            labelColumn.AddAttribute(cellName, "weight", 2);
            labelColumn.Sizing = TreeViewColumnSizing.Fixed;

            View.AppendColumn(labelColumn);

            var nameColumn = new TreeViewColumn
            {
                Visible = false,
            };

            View.AppendColumn(nameColumn);

            View.EnableSearch = true;
        }

        public void Dispose()
        {
            View.ButtonPressEvent -= BranchTreeOnButtonPress;
            View.CursorChanged -= BranchTreeCursorChanged;
        }

        public event EventHandler<BranchSelectedEventArgs> CheckoutClicked;
        public event EventHandler<BranchSelectedEventArgs> DeleteClicked;
        public event EventHandler<BranchSelectedEventArgs> FastForwardClicked;
        public event EventHandler<BranchSelectedEventArgs> MergeClicked;
        public event EventHandler<EventArgs> ChangesSelected;
        public event EventHandler<BranchSelectedEventArgs> BranchSelected;

        public void Refresh()
        {
            var tree = Git.GetBranchTree();

            _store = new TreeStore(
                typeof(string),
                typeof(string),
                typeof(int),
                typeof(BranchTreeItemType)
            );

            var activeBranch = Git.GetHeadFriendlyName();

            void AddTreeItems(TreeIter parentIter, BranchTreeItem item, BranchTreeItemType type)
            {
                var isHead = item.Name == activeBranch;
                var weight = isHead ? Weight.Bold : Weight.Normal;

                BranchTreeItemType GetItemType()
                {
                    if (isHead)
                    {
                        return BranchTreeItemType.Head;
                    }

                    return item.Children.Any() ? BranchTreeItemType.Noop : type;
                }

                var treeIter = _store.AppendValues(
                    parentIter,
                    item.Label,
                    item.Name,
                    weight,
                    GetItemType()
                );

                foreach (var child in item.Children)
                {
                    AddTreeItems(treeIter, child, type);
                }
            }

            var headIter = _store.AppendValues(
                Git.GetRepositoryFriendlyName(),
                "head",
                Weight.Bold,
                BranchTreeItemType.Noop
            );

            var changeCount = Git.GetHeadDiffCount();

            _store.AppendValues(
                headIter,
                $"Changes ({changeCount})",
                ChangesItemId,
                Weight.Normal,
                BranchTreeItemType.Noop
            );

            var branchesIter = _store.AppendValues(
                "Branches",
                "branches",
                Weight.Bold,
                BranchTreeItemType.Noop
            );

            foreach (var b in tree.Local)
            {
                AddTreeItems(branchesIter, b, BranchTreeItemType.Local);
            }

            var remoteIter = _store.AppendValues(
                "Remotes",
                "remotes",
                Weight.Bold,
                BranchTreeItemType.Noop
            );

            foreach (var b in tree.Remote)
            {
                AddTreeItems(remoteIter, b, BranchTreeItemType.Remote);
            }

            View.Model = _store;

            View.ExpandAll();
            View.EnableSearch = true;

            View.Columns[0].Title = Git.GetRepositoryFriendlyName();
        }

        private void BranchTreeCursorChanged(object sender, EventArgs args)
        {
            var selected = View.GetSelected<string>(1);

            if (string.IsNullOrEmpty(selected))
            {
                return;
            }

            if (selected == ChangesItemId)
            {
                OnChangesSelected();
                return;
            }

            OnBranchSelectedChanged(
                new BranchSelectedEventArgs
                {
                    Branch = selected,
                }
            );
        }

        [ConnectBefore]
        private void BranchTreeOnButtonPress(object sender, ButtonPressEventArgs args)
        {
            // right click
            if (args.Event.Button != 3)
            {
                return;
            }

            var selected = View.GetSelectedAtPos<string>(args.Event.X, args.Event.Y);
            var itemType = View.GetSelectedAtPos<BranchTreeItemType>(args.Event.X, args.Event.Y, 3);
            var current = Git.GetHeadFriendlyName();

            if (itemType == BranchTreeItemType.Noop)
            {
                return;
            }

            var menuItems = itemType switch
            {
                BranchTreeItemType.Local => new (string, EventHandler)[]
                {
                    ("Checkout", CheckoutActivated), ("Fast-forward", FastForwardActivated),
                    ($"Merge {selected} into {current}", MergeActivated), ("Delete", DeleteActivated),
                },
                BranchTreeItemType.Remote => new (string, EventHandler)[]
                {
                    ("Checkout", CheckoutActivated), ($"Merge {selected} into {current}", MergeActivated),
                    ("Delete", DeleteActivated),
                },
                BranchTreeItemType.Head => new (string, EventHandler)[]
                {
                    ("Fast-forward", FastForwardActivated),
                },
                _ => null,
            };

            Menus.Open(menuItems);
        }

        private void CheckoutActivated(object sender, EventArgs args)
        {
            var branch = View.GetSelected<string>(1);

            if (string.IsNullOrEmpty(branch))
            {
                return;
            }

            OnCheckoutClicked(
                new BranchSelectedEventArgs
                {
                    Branch = branch,
                }
            );
        }

        private void FastForwardActivated(object sender, EventArgs args)
        {
            var branch = View.GetSelected<string>(1);

            if (string.IsNullOrEmpty(branch))
            {
                return;
            }

            OnFastForwardClicked(
                new BranchSelectedEventArgs
                {
                    Branch = branch,
                }
            );
        }

        private void MergeActivated(object sender, EventArgs args)
        {
            var branch = View.GetSelected<string>(1);

            if (string.IsNullOrEmpty(branch))
            {
                return;
            }

            OnMergeClicked(
                new BranchSelectedEventArgs
                {
                    Branch = branch,
                }
            );
        }

        private void DeleteActivated(object sender, EventArgs args)
        {
            var branch = View.GetSelected<string>(1);

            if (string.IsNullOrEmpty(branch))
            {
                return;
            }

            OnDeleteClicked(
                new BranchSelectedEventArgs
                {
                    Branch = branch,
                }
            );
        }

        private void OnCheckoutClicked(BranchSelectedEventArgs e) => CheckoutClicked?.Invoke(this, e);

        private void OnFastForwardClicked(BranchSelectedEventArgs e) => FastForwardClicked?.Invoke(this, e);

        private void OnMergeClicked(BranchSelectedEventArgs e) => MergeClicked?.Invoke(this, e);

        private void OnDeleteClicked(BranchSelectedEventArgs e) => DeleteClicked?.Invoke(this, e);

        private void OnBranchSelectedChanged(BranchSelectedEventArgs e) => BranchSelected?.Invoke(this, e);

        private void OnChangesSelected() => ChangesSelected?.Invoke(this, EventArgs.Empty);
    }

    public class BranchSelectedEventArgs : EventArgs
    {
        public string Branch { get; init; }
    }
}
