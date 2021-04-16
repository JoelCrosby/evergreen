using System;

using Evergreen.Lib.Git;
using Evergreen.Lib.Git.Models;
using Evergreen.Lib.Helpers;
using Evergreen.Lib.Session;

using Gtk;

namespace Evergreen.Widgets
{
    public class BranchTree
    {
        private RepositorySession ActiveSession { get; set; }
        private GitService Git { get; set; }
        private TreeView View { get; set; }
        private TreeStore store;

        public event EventHandler<BranchClickedEventArgs> CheckoutClicked;
        public event EventHandler<BranchClickedEventArgs> DeleteClicked;
        public event EventHandler<BranchClickedEventArgs> FastForwardClicked;

        public BranchTree(TreeView view, GitService git)
        {
            View = view;
            Git = git;
            ActiveSession = Git.Session;
        }

        public BranchTree Build()
        {
            View.ButtonPressEvent += BranchTreeOnButtonPress;

            // Init cells
            var cellName = new CellRendererText();

            if (View.Columns.Length == 0)
            {
                // Init columns
                var labelColumn = new TreeViewColumn
                {
                    Title = Git.Session.RepositoryFriendlyName,
                };

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

            Refresh();

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

            OnCheckoutClicked(new BranchClickedEventArgs
            {
                Branch = branch,
            });
        }

        protected virtual void OnCheckoutClicked(BranchClickedEventArgs e)
        {
            EventHandler<BranchClickedEventArgs> handler = CheckoutClicked;

            if (handler is null) return;

            handler(this, e);
        }

        private void FastForwardActivated(object sender, EventArgs args)
        {
            var branch = GetSelected<string>(1);

            if (string.IsNullOrEmpty(branch))
            {
                return;
            }

            OnFastForwardClicked(new BranchClickedEventArgs
            {
                Branch = branch,
            });
        }

        protected virtual void OnFastForwardClicked(BranchClickedEventArgs e)
        {
            EventHandler<BranchClickedEventArgs> handler = FastForwardClicked;

            if (handler is null) return;

            handler(this, e);
        }

        private void DeleteActivated(object sender, EventArgs args)
        {
            var branch = GetSelected<string>(1);

            if (string.IsNullOrEmpty(branch))
            {
                return;
            }

            OnDeleteClicked(new BranchClickedEventArgs
            {
                Branch = branch,
            });
        }

        protected virtual void OnDeleteClicked(BranchClickedEventArgs e)
        {
            EventHandler<BranchClickedEventArgs> handler = DeleteClicked;

            if (handler is null) return;

            handler(this, e);
        }

        private T GetSelected<T>(int index)
        {
            View.Selection.GetSelected(out var model, out var iter);

            return (T)model.GetValue(iter, index);
        }
    }


    public class BranchClickedEventArgs : EventArgs
    {
        public string Branch { get; set; }
    }
}
