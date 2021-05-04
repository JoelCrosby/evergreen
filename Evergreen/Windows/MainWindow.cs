using System.Linq;
using System.Collections.Generic;
using System;

using Evergreen.Lib.Configuration;
using Evergreen.Lib.Session;
using Evergreen.Utils;
using Evergreen.Widgets;

using Gtk;

using UI = Gtk.Builder.ObjectAttribute;
using Window = Gtk.Window;
using Evergreen.Lib.Git;

namespace Evergreen.Windows
{
    public class MainWindow : Window
    {
#pragma warning disable 0649

        [UI] private readonly Button openRepo;
        [UI] private readonly Button fetch;
        [UI] private readonly Button pull;
        [UI] private readonly Button push;
        [UI] private readonly Button btnCreateBranch;
        [UI] private readonly Button search;
        [UI] private readonly Button about;
        [UI] private readonly HeaderBar headerBar;
        [UI] private readonly SearchBar searchBar;
        [UI] private readonly Spinner spinner;
        [UI] private readonly Notebook repoNotebook;

#pragma warning restore 064

        private int _selectedRepoIndex;
        private readonly List<Repository> _repositories = new ();
        private Repository _repository => _repositories.ElementAtOrDefault(_selectedRepoIndex);
        private readonly RepositorySession Session;
        private readonly Dialogs.AboutDialog aboutDialog = new ();

        public MainWindow() : this(new Builder("main.ui")) { }

        private MainWindow(Builder builder) : base(builder.GetObject("main").Handle)
        {
            builder.Autoconnect(this);

            // Gtk widget events
            DeleteEvent += WindowDeleteEvent;
            FocusInEvent += WindowFocusGrabbed;
            openRepo.Clicked += OpenRepoClicked;
            fetch.Clicked += FetchClicked;
            pull.Clicked += PullClicked;
            push.Clicked += PushClicked;
            search.Clicked += SearchClicked;
            about.Clicked += AboutClicked;
            btnCreateBranch.Clicked += CreateBranchClicked;
            repoNotebook.SwitchPage += RepoTabChanged;

            // Set the clientside headerbar
            Titlebar = headerBar;

            Session = Sessions.LoadSession();

            OpenRepository();
        }

        private void WindowDeleteEvent(object sender, DeleteEventArgs a)
        {
            Sessions.SaveSession(Session);
            Application.Quit();
        }

        private void WindowFocusGrabbed(object sender, FocusInEventArgs a)
        {
            _repository?.OnFocus();
        }

        private void OpenRepository()
        {
            if (Session.Paths.Count == 0)
            {
                ToggleRepositoryButtons(false);
                return;
            }

            ToggleRepositoryButtons(true);

            foreach (var path in Session.Paths)
            {
                var repo = new Repository(path);
                var tabLabel = repo.Git.GetRepositoryFriendlyName();

                _repositories.Add(repo);

                repoNotebook.AppendPage(repo, new Label(tabLabel) { WidthRequest = 160 });

                SetPanedPositions(repo);
            }

            _selectedRepoIndex = _repositories.Count - 1;
            repoNotebook.Page = _selectedRepoIndex;

            UpdateHeaderBar();

            Sessions.SaveSession(Session);
        }

        private void ToggleRepositoryButtons(bool isEnabled)
        {
            fetch.Sensitive = isEnabled;
            pull.Sensitive = isEnabled;
            push.Sensitive = isEnabled;
            btnCreateBranch.Sensitive = isEnabled;
            search.Sensitive = isEnabled;
            about.Sensitive = isEnabled;
        }

        private void SetPanedPositions(Repository repo)
        {
            GetSize(out var _, out var height);

            repo.SetPanedPosition(height);
        }

        private void UpdateHeaderBar()
        {
            var repoName = _repository.Git.GetRepositoryFriendlyName();

            Title = $"{repoName} - Evergreen";
            headerBar.Title = $"{repoName} - Evergreen";
            headerBar.Subtitle = _repository.Git.GetFreindlyPath();
        }

        private void RepoTabChanged(object o, SwitchPageArgs args)
        {
            _selectedRepoIndex = (int)args.PageNum;

            UpdateHeaderBar();

            _repository?.OnFocus();
        }

        private void OpenRepoClicked(object sender, EventArgs _)
        {
            var (response, dialog) = FileChooser.Open(this, "Open Reposiory", FileChooserAction.SelectFolder);

            if (response == ResponseType.Accept)
            {
                var path = dialog.Filename;

                if (!GitService.IsRepository(path))
                {
                    return;
                }

                Session.Paths.Add(path);

                OpenRepository();
            }

            dialog.Dispose();
        }

        private void FetchClicked(object sender, EventArgs e)
        {
            ShowProgress(() => _repository?.FetchClicked(sender, e));
        }

        private void PullClicked(object sender, EventArgs e)
        {
            ShowProgress(() => _repository?.PullClicked(sender, e));
        }

        private void SearchClicked(object sender, EventArgs _)
        {
            searchBar.SearchModeEnabled = !searchBar.SearchModeEnabled;
        }

        private void PushClicked(object sender, EventArgs e)
        {
            ShowProgress(() => _repository?.PushClicked(sender, e));
        }

        private void AboutClicked(object sender, EventArgs _)
        {
            aboutDialog.Show();
        }

        private void CreateBranchClicked(object sender, EventArgs e)
        {
            _repository?.CreateBranchClicked(sender, e);
        }

        private void ShowProgress(System.Action action)
        {
            spinner.Active = true;
            spinner.Show();

            action();

            spinner.Hide();
        }
    }
}
