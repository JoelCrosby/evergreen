using System;

using Evergreen.Lib.Git;
using Evergreen.Lib.Git.Models;
using Evergreen.Lib.Helpers;

using Gtk;

namespace Evergreen.Widgets
{
    public class BranchTree : IDisposable
    {
        private GitService Git { get; }
        private TreeView View { get; }
        private TreeStore store;

        private const string ChangesItemId = "evergreen:changes";

        public event EventHandler<BranchSelectedEventArgs> CheckoutClicked;
        public event EventHandler<BranchSelectedEventArgs> DeleteClicked;
        public event EventHandler<BranchSelectedEventArgs> FastForwardClicked;
        public event EventHandler<EventArgs> ChangesSelected;
        public event EventHandler<BranchSelectedEventArgs> BranchSelected;

        public BranchTree(TreeView view, GitService git)
        {
            View = view;
            Git = git;
        }

        public BranchTree Build()
        {
            View.HeadersVisible = false;
            View.ButtonPressEvent += BranchTreeOnButtonPress;
            View.CursorChanged += BranchTreeCursorChanged;

            // Init cells

            if (View.Columns.Length == 0)
            {
                // Init columns
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
            }

            View.EnableSearch = true;

            return this;
        }

        public void Refresh()
        {
            var tree = Git.GetBranchTree();

            store = new TreeStore(typeof(string), typeof(string), typeof(int));
            View.Model = store;

            var activeBranch = Git.GetHeadCanonicalName();

            void AddTreeItems(TreeIter parentIter, TreeItem<BranchTreeItem> item)
            {
                var weight = item.Item.Name == activeBranch ? Pango.Weight.Bold : Pango.Weight.Normal;

                var treeIter = store.AppendValues(
                    parentIter,
                    item.Item.Label,
                    item.Item.Name,
                    weight
                );

                foreach (var child in item.Children)
                {
                    AddTreeItems(treeIter, child);
                }
            }

            var headIter = store.AppendValues(Git.Session.RepositoryFriendlyName, "head", Pango.Weight.Bold);

            var changeCount = Git.GetHeadDiffCount();

            var treeIter = store.AppendValues(
                headIter,
                $"Changes ({changeCount})",
                ChangesItemId,
                Pango.Weight.Normal
            );

            var branchesIter = store.AppendValues("Branches", "branches", Pango.Weight.Bold);

            foreach (var b in tree.Local)
            {
                AddTreeItems(branchesIter, b);
            }

            var remoteIter = store.AppendValues("Remotes", "remotes", Pango.Weight.Bold);

            foreach (var b in tree.Remote)
            {
                AddTreeItems(remoteIter, b);
            }

            View.ExpandAll();
            View.EnableSearch = true;

            View.Columns[0].Title = Git.Session.RepositoryFriendlyName;
        }

        private void BranchTreeCursorChanged(object sender, EventArgs args)
        {
            var selected = GetSelected<string>(1);

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

            var menu = new Menu();

            var checkoutMenuItem = new MenuItem("Checkout");
            checkoutMenuItem.Activated += CheckoutActivated;
            menu.Add(checkoutMenuItem);

            var fastforwardMenuItem = new MenuItem("Fast-forward");
            fastforwardMenuItem.Activated += FastForwardActivated;
            menu.Add(fastforwardMenuItem);

            var deleteMenuItem = new MenuItem("Delete");
            deleteMenuItem.Activated += DeleteActivated;
            menu.Add(deleteMenuItem);

            var renameMenuItem = new MenuItem("Rename");
            menu.Add(renameMenuItem);

            menu.ShowAll();
            menu.Popup();
        }

        private void CheckoutActivated(object sender, EventArgs args)
        {
            var branch = GetSelected<string>(1);

            if (string.IsNullOrEmpty(branch))
            {
                return;
            }

            OnCheckoutClicked(new BranchSelectedEventArgs
            {
                Branch = branch,
            });
        }

        protected virtual void OnCheckoutClicked(BranchSelectedEventArgs e)
        {
            var handler = CheckoutClicked;

            if (handler is null)
            {
                return;
            }

            handler(this, e);
        }

        private void FastForwardActivated(object sender, EventArgs args)
        {
            var branch = GetSelected<string>(1);

            if (string.IsNullOrEmpty(branch))
            {
                return;
            }

            OnFastForwardClicked(new BranchSelectedEventArgs
            {
                Branch = branch,
            });
        }

        protected virtual void OnFastForwardClicked(BranchSelectedEventArgs e)
        {
            var handler = FastForwardClicked;

            if (handler is null)
            {
                return;
            }

            handler(this, e);
        }

        private void DeleteActivated(object sender, EventArgs args)
        {
            var branch = GetSelected<string>(1);

            if (string.IsNullOrEmpty(branch))
            {
                return;
            }

            OnDeleteClicked(new BranchSelectedEventArgs
            {
                Branch = branch,
            });
        }

        protected virtual void OnDeleteClicked(BranchSelectedEventArgs e)
        {
            var handler = DeleteClicked;

            if (handler is null)
            {
                return;
            }

            handler(this, e);
        }

        protected virtual void OnBranchSelectedChanged(BranchSelectedEventArgs e)
        {
            var handler = BranchSelected;

            if (handler is null)
            {
                return;
            }

            handler(this, e);
        }

        protected virtual void OnChangesSelected()
        {
            var handler = ChangesSelected;

            if (handler is null)
            {
                return;
            }

            handler(this, EventArgs.Empty);
        }

        private T GetSelected<T>(int index)
        {
            View.Selection.GetSelected(out var model, out var iter);

            return (T)model.GetValue(iter, index);
        }

        public void Dispose()
        {
            View.ButtonPressEvent -= BranchTreeOnButtonPress;
            View.CursorChanged -= BranchTreeCursorChanged;
        }
    }

    public class BranchSelectedEventArgs : EventArgs
    {
        public string Branch { get; set; }
    }
}
