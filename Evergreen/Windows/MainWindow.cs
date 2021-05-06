using System;
using System.Collections.Generic;
using System.Linq;

using Evergreen.Lib.Configuration;
using Evergreen.Lib.Git;
using Evergreen.Lib.Session;
using Evergreen.Utils;
using Evergreen.Widgets;

using Gtk;

using UI = Gtk.Builder.ObjectAttribute;
using Window = Gtk.Window;

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
        [UI] private readonly Button closeRepo;
        [UI] private readonly HeaderBar headerBar;
        [UI] private readonly SearchBar searchBar;
        [UI] private readonly Spinner spinner;
        [UI] private readonly Notebook repoNotebook;

#pragma warning restore 064

        private int selectedRepoIndex;
        private readonly List<Repository> repositories = new();
        private Repository Repository => repositories.ElementAtOrDefault(selectedRepoIndex);
        private readonly RepositorySession session;
        private readonly Dialogs.AboutDialog aboutDialog = new();

        public MainWindow() : this(new Builder("main.ui")) { }

        private MainWindow(Builder builder) : base(builder.GetObject("main").Handle)
        {
            builder.Autoconnect(this);

            // Gtk widget events
            DeleteEvent += WindowDeleteEvent;
            FocusInEvent += WindowFocusGrabbed;
            openRepo.Clicked += OpenRepoClicked;
            closeRepo.Clicked += CloseRepoClicked;
            fetch.Clicked += FetchClicked;
            pull.Clicked += PullClicked;
            push.Clicked += PushClicked;
            search.Clicked += SearchClicked;
            about.Clicked += AboutClicked;
            btnCreateBranch.Clicked += CreateBranchClicked;
            repoNotebook.SwitchPage += RepoTabChanged;

            // Set the clientside header bar
            Titlebar = headerBar;

            session = Sessions.LoadSession();

            OpenRepository();
        }

        private void WindowDeleteEvent(object sender, DeleteEventArgs a)
        {
            Sessions.SaveSession(session);
            Application.Quit();
        }

        private void WindowFocusGrabbed(object sender, FocusInEventArgs a)
        {
            Repository?.OnFocus();
        }

        private void OpenRepository()
        {
            if (session.Paths.Count == 0)
            {
                ToggleRepositoryButtons(false);
                return;
            }

            ToggleRepositoryButtons(true);

            var paths = repositories.Select(r => r.Path).ToHashSet();

            foreach (var path in session.Paths)
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

                repositories.Add(repo);

                repoNotebook.AppendPage(repo, new Label
                {
                    Text = tabLabel,
                    WidthRequest = 160,
                });

                SetPanedPositions(repo);
            }

            selectedRepoIndex = repositories.Count - 1;
            repoNotebook.Page = selectedRepoIndex;

            UpdateHeaderBar();

            Sessions.SaveSession(session);
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
            var repoName = Repository.Git.GetRepositoryFriendlyName();

            Title = repoName;
            headerBar.Title = repoName;
            headerBar.Subtitle = Repository.Git.GetFriendlyPath();
        }

        private void RepoTabChanged(object o, SwitchPageArgs args)
        {
            selectedRepoIndex = (int)args.PageNum;

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

                session.Paths.Add(path);

                OpenRepository();
            }

            dialog.Dispose();
        }

        private void CloseRepoClicked(object sender, EventArgs _)
        {
            if (repositories.Count == 0)
            {
                return;
            }

            Repository.Dispose();
            repositories.RemoveAt(selectedRepoIndex);

            repoNotebook.RemovePage(selectedRepoIndex);

            session.Paths.RemoveAt(selectedRepoIndex);

            Sessions.SaveSession(session);
        }

        private void FetchClicked(object sender, EventArgs e)
        {
            ShowProgress(() => Repository?.FetchClicked(sender, e));
        }

        private void PullClicked(object sender, EventArgs e)
        {
            ShowProgress(() => Repository?.PullClicked(sender, e));
        }

        private void SearchClicked(object sender, EventArgs _)
        {
            searchBar.SearchModeEnabled = !searchBar.SearchModeEnabled;
        }

        private void PushClicked(object sender, EventArgs e)
        {
            ShowProgress(() => Repository?.PushClicked(sender, e));
        }

        private void AboutClicked(object sender, EventArgs _)
        {
            aboutDialog.Show();
        }

        private void CreateBranchClicked(object sender, EventArgs e)
        {
            Repository?.CreateBranchClicked(sender, e);
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
