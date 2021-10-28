using System;
using System.Collections.Generic;
using System.Linq;

using Evergreen.Core.Configuration;
using Evergreen.Core.Git;
using Evergreen.Core.Session;
using Evergreen.Utils;
using Evergreen.Widgets;

using Gtk;

using AboutDialog = Evergreen.Dialogs.AboutDialog;
using Action = System.Action;
using UI = Gtk.Builder.ObjectAttribute;

namespace Evergreen.Windows
{
    public class MainWindow : Window
    {
#pragma warning disable 0649

        [UI("openRepo")]
        private readonly Button _openRepo;

        [UI("fetch")]
        private readonly Button _fetch;

        [UI("pull")]
        private readonly Button _pull;

        [UI("push")]
        private readonly Button _push;

        [UI("btnCreateBranch")]
        private readonly Button _btnCreateBranch;

        [UI("search")]
        private readonly Button _search;

        [UI("about")]
        private readonly Button _about;

        [UI("closeRepo")]
        private readonly Button _closeRepo;

        [UI("headerBar")]
        private readonly HeaderBar _headerBar;

        [UI("searchBar")]
        private readonly SearchBar _searchBar;

        [UI("spinner")]
        private readonly Spinner _spinner;

        [UI("repoNotebook")]
        private readonly Notebook _repoNotebook;

#pragma warning restore 064

        private int _selectedRepoIndex;
        private readonly List<Repository> _repositories = new();
        private Repository Repository => _repositories.ElementAtOrDefault(_selectedRepoIndex);
        private readonly RepositorySession _session;
        private readonly AboutDialog _aboutDialog = new();

        public MainWindow() : this(new Builder("main.ui")) { }

        private MainWindow(Builder builder) : base(builder.GetObject("main").Handle)
        {
            builder.Autoconnect(this);

            // Gtk widget events
            DeleteEvent += WindowDeleteEvent;
            FocusInEvent += WindowFocusGrabbed;
            _openRepo.Clicked += OpenRepoClicked;
            _closeRepo.Clicked += CloseRepoClicked;
            _fetch.Clicked += FetchClicked;
            _pull.Clicked += PullClicked;
            _push.Clicked += PushClicked;
            _search.Clicked += SearchClicked;
            _about.Clicked += AboutClicked;
            _btnCreateBranch.Clicked += CreateBranchClicked;
            _repoNotebook.SwitchPage += RepoTabChanged;

            // Set the client side header bar
            Titlebar = _headerBar;

            _session = Sessions.LoadSession();

            OpenRepository();
        }

        private void WindowDeleteEvent(object sender, DeleteEventArgs a)
        {
            Sessions.SaveSession(_session);
            Application.Quit();
        }

        private void WindowFocusGrabbed(object sender, FocusInEventArgs a) => Repository?.OnFocus();

        private void OpenRepository()
        {
            if (_session.Paths.Count == 0)
            {
                ToggleRepositoryButtons(false);
                return;
            }

            ToggleRepositoryButtons(true);

            var paths = _repositories.Select(r => r.WorkingDir).ToHashSet();

            foreach (var path in _session.Paths)
            {
                if (paths.Contains(path))
                {
                    continue;
                }

                if (!GitService.IsRepository(path))
                {
                    continue;
                }

                var repo = new Repository(path);
                var tabLabel = repo.Git.GetRepositoryFriendlyName();

                _repositories.Add(repo);

                _repoNotebook.AppendPage(
                    repo, new Label
                    {
                        Text = tabLabel,
                        WidthRequest = 160,
                    }
                );

                SetPanedPositions(repo);
            }

            _selectedRepoIndex = _repositories.Count - 1;
            _repoNotebook.Page = _selectedRepoIndex;

            UpdateHeaderBar();

            Sessions.SaveSession(_session);
        }

        private void ToggleRepositoryButtons(bool isEnabled)
        {
            _fetch.Sensitive = isEnabled;
            _pull.Sensitive = isEnabled;
            _push.Sensitive = isEnabled;
            _btnCreateBranch.Sensitive = isEnabled;
            _search.Sensitive = isEnabled;
            _about.Sensitive = isEnabled;
        }

        private void SetPanedPositions(Repository repo)
        {
            GetSize(out var _, out var height);

            repo.SetPanedPosition(height);
        }

        private void UpdateHeaderBar()
        {
            var repoName = Repository.Git.GetRepositoryFriendlyName();

            Title = repoName;
            _headerBar.Title = repoName;
            _headerBar.Subtitle = Repository.Git.GetFriendlyPath();
        }

        private void RepoTabChanged(object o, SwitchPageArgs args)
        {
            _selectedRepoIndex = (int)args.PageNum;

            UpdateHeaderBar();

            Repository?.OnFocus();
        }

        private void OpenRepoClicked(object sender, EventArgs _)
        {
            var (response, dialog) = FileChooser.Open(this, "Open Repository", FileChooserAction.SelectFolder);

            if (response == ResponseType.Accept)
            {
                var path = dialog.Filename;

                if (!GitService.IsRepository(path))
                {
                    return;
                }

                _session.Paths.Add(path);

                OpenRepository();
            }

            dialog.Dispose();
        }

        private void CloseRepoClicked(object sender, EventArgs _)
        {
            if (_repositories.Count == 0)
            {
                return;
            }

            Repository.Dispose();
            _repositories.RemoveAt(_selectedRepoIndex);

            _repoNotebook.RemovePage(_selectedRepoIndex);

            _session.Paths.RemoveAt(_selectedRepoIndex);

            Sessions.SaveSession(_session);
        }

        private void FetchClicked(object sender, EventArgs e) =>
            ShowProgress(() => Repository?.FetchClicked(sender, e));

        private void PullClicked(object sender, EventArgs e) => ShowProgress(() => Repository?.PullClicked(sender, e));

        private void SearchClicked(object sender, EventArgs _) =>
            _searchBar.SearchModeEnabled = !_searchBar.SearchModeEnabled;

        private void PushClicked(object sender, EventArgs e) => ShowProgress(() => Repository?.PushClicked(sender, e));

        private void AboutClicked(object sender, EventArgs _) => _aboutDialog.Show();

        private void CreateBranchClicked(object sender, EventArgs e) => Repository?.CreateBranchClicked(sender, e);

        private void ShowProgress(Action action)
        {
            _spinner.Active = true;
            _spinner.Show();

            action();

            _spinner.Hide();
        }
    }
}
