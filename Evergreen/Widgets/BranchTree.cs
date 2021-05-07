using System;

using Evergreen.Lib.Git;
using Evergreen.Lib.Git.Models;
using Evergreen.Lib.Helpers;
using Evergreen.Utils;
using Evergreen.Widgets.Common;

using Gtk;

namespace Evergreen.Widgets
{
    public class BranchTree : TreeWidget, IDisposable
    {
        private TreeStore store;

        private const string ChangesItemId = "evergreen:changes";

        public event EventHandler<BranchSelectedEventArgs> CheckoutClicked;
        public event EventHandler<BranchSelectedEventArgs> DeleteClicked;
        public event EventHandler<BranchSelectedEventArgs> FastForwardClicked;
        public event EventHandler<BranchSelectedEventArgs> MergeClicked;
        public event EventHandler<EventArgs> ChangesSelected;
        public event EventHandler<BranchSelectedEventArgs> BranchSelected;

        public BranchTree(TreeView view, GitService git) : base(view, git)
        {
            View.HeadersVisible = false;
            View.ButtonPressEvent += BranchTreeOnButtonPress;
            View.CursorChanged += BranchTreeCursorChanged;

            var labelColumn = new TreeViewColumn();
            var cellName = new CellRendererText();

            labelColumn.PackStart(cellName, true);
            labelColumn.AddAttribute(cellName, "text", 0);
            labelColumn.AddAttribute(cellName, "weight", 2);

            View.AppendColumn(labelColumn);

            var nameColumn = new TreeViewColumn
            {
                Title = "CanonicalName",
                Visible = false,
            };

            View.AppendColumn(nameColumn);

            View.EnableSearch = true;
        }

        public void Refresh()
        {
            var tree = Git.GetBranchTree();

            store = new TreeStore(
                typeof(string),
                typeof(string),
                typeof(int),
                typeof(BranchTreeItemType)
            );

            View.Model = store;

            var activeBranch = Git.GetHeadCanonicalName();

            void AddTreeItems(TreeIter parentIter, TreeItem<BranchTreeItem> item, BranchTreeItemType type)
            {
                var isHead = item.Item.Name == activeBranch;
                var weight = isHead ? Pango.Weight.Bold : Pango.Weight.Normal;
                var itemType = isHead ? BranchTreeItemType.Head : type;

                var treeIter = store.AppendValues(
                    parentIter,
                    item.Item.Label,
                    item.Item.Name,
                    weight,
                    itemType
                );

                foreach (var child in item.Children)
                {
                    AddTreeItems(treeIter, child, type);
                }
            }

            var headIter = store.AppendValues(
                Git.GetRepositoryFriendlyName(),
                "head",
                Pango.Weight.Bold,
                BranchTreeItemType.Noop
            );

            var changeCount = Git.GetHeadDiffCount();

            store.AppendValues(
                headIter,
                $"Changes ({changeCount})",
                ChangesItemId,
                Pango.Weight.Normal,
                BranchTreeItemType.Noop
            );

            var branchesIter = store.AppendValues(
                "Branches",
                "branches",
                Pango.Weight.Bold,
                BranchTreeItemType.Noop
            );

            foreach (var b in tree.Local)
            {
                AddTreeItems(branchesIter, b, BranchTreeItemType.Local);
            }

            var remoteIter = store.AppendValues(
                "Remotes",
                "remotes",
                Pango.Weight.Bold,
                BranchTreeItemType.Noop
            );

            foreach (var b in tree.Remote)
            {
                AddTreeItems(remoteIter, b, BranchTreeItemType.Remote);
            }

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

            OnBranchSelectedChanged(new BranchSelectedEventArgs
            {
                Branch = selected,
            });
        }

        [GLib.ConnectBefore]
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
                    ("Checkout", CheckoutActivated),
                    ("Fast-forward", FastForwardActivated),
                    ($"Merge {selected} into {current}", MergeActivated),
                    ("Delete", DeleteActivated)
                },
                BranchTreeItemType.Remote => new (string, EventHandler)[]
                {
                    ("Checkout", CheckoutActivated),
                    ($"Merge {selected} into {current}", MergeActivated),
                    ("Delete", DeleteActivated)
                },
                BranchTreeItemType.Head => new (string, EventHandler)[]
                {
                    ("Fast-forward", FastForwardActivated),
                },
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

            OnCheckoutClicked(new BranchSelectedEventArgs
            {
                Branch = branch,
            });
        }

        private void FastForwardActivated(object sender, EventArgs args)
        {
            var branch = View.GetSelected<string>(1);

            if (string.IsNullOrEmpty(branch))
            {
                return;
            }

            OnFastForwardClicked(new BranchSelectedEventArgs
            {
                Branch = branch,
            });
        }

        private void MergeActivated(object sender, EventArgs args)
        {
            var branch = View.GetSelected<string>(1);

            if (string.IsNullOrEmpty(branch))
            {
                return;
            }

            OnMergeClicked(new BranchSelectedEventArgs
            {
                Branch = branch,
            });
        }

        private void DeleteActivated(object sender, EventArgs args)
        {
            var branch = View.GetSelected<string>(1);

            if (string.IsNullOrEmpty(branch))
            {
                return;
            }

            OnDeleteClicked(new BranchSelectedEventArgs
            {
                Branch = branch,
            });
        }

        protected virtual void OnCheckoutClicked(BranchSelectedEventArgs e)
        {
            CheckoutClicked?.Invoke(this, e);
        }

        protected virtual void OnFastForwardClicked(BranchSelectedEventArgs e)
        {
            FastForwardClicked?.Invoke(this, e);
        }

        protected virtual void OnMergeClicked(BranchSelectedEventArgs e)
        {
            MergeClicked?.Invoke(this, e);
        }

        protected virtual void OnDeleteClicked(BranchSelectedEventArgs e)
        {
            DeleteClicked?.Invoke(this, e);
        }

        protected virtual void OnBranchSelectedChanged(BranchSelectedEventArgs e)
        {
            BranchSelected?.Invoke(this, e);
        }

        protected virtual void OnChangesSelected()
        {
            ChangesSelected?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            View.ButtonPressEvent -= BranchTreeOnButtonPress;
            View.CursorChanged -= BranchTreeCursorChanged;
        }
    }

    public class BranchSelectedEventArgs : EventArgs
    {
        public string Branch { get; init; }
    }
}
