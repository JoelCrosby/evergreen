using System;

using Evergreen.Lib.Git;
using Evergreen.Lib.Git.Models;
using Evergreen.Lib.Helpers;
using Evergreen.Widgets.Common;

using Gtk;

namespace Evergreen.Widgets
{
    public class BranchTree : TreeWidget, IDisposable
    {
        private TreeStore store;

        private const string _changesItemId = "evergreen:changes";

        public event EventHandler<BranchSelectedEventArgs> CheckoutClicked;
        public event EventHandler<BranchSelectedEventArgs> DeleteClicked;
        public event EventHandler<BranchSelectedEventArgs> FastForwardClicked;
        public event EventHandler<EventArgs> ChangesSelected;
        public event EventHandler<BranchSelectedEventArgs> BranchSelected;

        public BranchTree(TreeView view, GitService git) : base(view, git)
        {
            _view.HeadersVisible = false;
            _view.ButtonPressEvent += BranchTreeOnButtonPress;
            _view.CursorChanged += BranchTreeCursorChanged;

            var labelColumn = new TreeViewColumn();
            var cellName = new CellRendererText();

            labelColumn.PackStart(cellName, true);
            labelColumn.AddAttribute(cellName, "text", 0);
            labelColumn.AddAttribute(cellName, "weight", 2);

            _view.AppendColumn(labelColumn);

            var nameColumn = new TreeViewColumn
            {
                Title = "CanonicalName",
                Visible = false,
            };

            _view.AppendColumn(nameColumn);

            _view.EnableSearch = true;
        }

        public void Refresh()
        {
            var tree = _git.GetBranchTree();

            store = new TreeStore(typeof(string), typeof(string), typeof(int));
            _view.Model = store;

            var activeBranch = _git.GetHeadCanonicalName();

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

            var headIter = store.AppendValues(_git.Session.RepositoryFriendlyName, "head", Pango.Weight.Bold);

            var changeCount = _git.GetHeadDiffCount();

            var treeIter = store.AppendValues(
                headIter,
                $"Changes ({changeCount})",
                _changesItemId,
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

            _view.ExpandAll();
            _view.EnableSearch = true;

            _view.Columns[0].Title = _git.Session.RepositoryFriendlyName;
        }

        private void BranchTreeCursorChanged(object sender, EventArgs args)
        {
            var selected = GetSelected<string>(1);

            if (string.IsNullOrEmpty(selected))
            {
                return;
            }

            if (selected == _changesItemId)
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

        protected virtual void OnCheckoutClicked(BranchSelectedEventArgs e)
        {
            CheckoutClicked?.Invoke(this, e);
        }


        protected virtual void OnFastForwardClicked(BranchSelectedEventArgs e)
        {
            FastForwardClicked?.Invoke(this, e);
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
            _view.ButtonPressEvent -= BranchTreeOnButtonPress;
            _view.CursorChanged -= BranchTreeCursorChanged;
        }
    }

    public class BranchSelectedEventArgs : EventArgs
    {
        public string Branch { get; set; }
    }
}
