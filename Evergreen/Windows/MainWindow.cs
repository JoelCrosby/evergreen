using System;

using Evergreen.Utils;
using Evergreen.Widgets;
using Evergreen.Lib.Configuration;
using Evergreen.Lib.Git;
using Evergreen.Lib.Session;

using Gtk;
using GtkSource;

using LibGit2Sharp;

using UI = Gtk.Builder.ObjectAttribute;
using Window = Gtk.Window;

namespace Evergreen.Windows
{
    public class MainWindow : Window
    {
        [UI] private readonly TreeView branchTree = null;
        [UI] private readonly TreeView commitList = null;
        [UI] private readonly Button openRepo = null;
        [UI] private readonly TreeView commitFiles = null;
        [UI] private readonly Label branchLabel = null;
        [UI] private readonly Label commitShaLabel = null;
        [UI] private readonly Label commitFileLabel = null;
        [UI] private readonly Label commitAuthorLabel = null;
        [UI] private readonly HeaderBar headerBar = null;

        private RepositorySession ActiveSession { get; set; }
        private GitService Git { get; set; }

        private BranchTree BranchTreeWidget;
        private CommitList CommitListWidget;
        private CommitFiles CommitFilesWidget;

        private SourceView CommitFileSourceView;

        public MainWindow() : this(new Builder("MainWindow.glade")) { }

        private MainWindow(Builder builder) : base(builder.GetObject("MainWindow").Handle)
        {
            BuildDiffView(builder);

            builder.Autoconnect(this);

            DeleteEvent += Window_DeleteEvent;
            openRepo.Clicked += OpenRepo_Clicked;

            RenderSession(RestoreSession.LoadSession());
        }

        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            RestoreSession.SaveSession(ActiveSession);
            Application.Quit();
        }

        private void OpenRepo_Clicked(object sender, EventArgs _)
        {
            var (response, dialog) = Dialogs.Open(this, "Open Reposiory", FileChooserAction.SelectFolder);

            if (response == ResponseType.Accept)
            {
                var session = new RepositorySession
                {
                    Path = dialog.CurrentFolder,
                };

                RenderSession(session);
            }

            dialog.Dispose();
        }

        private void RenderSession(RepositorySession session)
        {
            ActiveSession = session;

            Git = new GitService(session);

            Title = $"{session.RepositoryFriendlyName} - Evergreen";
            headerBar.Title = $"{session.RepositoryFriendlyName} - Evergreen";

            commitShaLabel.Text = string.Empty;
            commitFileLabel.Text = string.Empty;

            BranchTreeWidget = new BranchTree(branchTree, Git).Build();
            CommitListWidget = new CommitList(commitList, Git).Build();
            CommitFilesWidget = new CommitFiles(commitFiles, Git).Build();

            CommitListWidget.CommitSelected += CommitSelected;
            BranchTreeWidget.CheckoutClicked += CheckoutClicked;
            CommitFilesWidget.CommitFileSelected += CommitFileSelected;

            RefreshStatusBar();
        }

        private void  RefreshStatusBar()
        {
            branchLabel.Text = Git.GetHeadFriendlyName();
        }

        private void BuildDiffView(Builder builder)
        {
            var paned = builder.GetObject("commitFilesDiffPanned") as Paned;

            var scroller = new ScrolledWindow();
            scroller.Visible = true;

            var sourceView = new SourceView();

            sourceView.ShowLineNumbers = true;
            sourceView.ShowLineMarks = true;
            sourceView.TabWidth = 4;
            sourceView.Editable = false;
            sourceView.Visible = true;
            sourceView.Monospace = true;
            sourceView.SetSizeRequest(400, 4000);

            CommitFileSourceView = sourceView;

            scroller.Add(sourceView);
            paned.Pack2(scroller, true, true);
        }

        private void CheckoutClicked(object sender, CheckoutClickedEventArgs e)
        {
            Git.Checkout(e.Branch);
            BranchTreeWidget.Refresh();
            RefreshStatusBar();
        }

        private void CommitSelected(object sender, CommitSelectedEventArgs e)
        {
            CommitFilesWidget.Update(e.CommitId);

            commitShaLabel.Text = $"CommitId: {e.CommitId}";
            commitFileLabel.Text = string.Empty;
            commitAuthorLabel.Text = Git.GetCommitAuthor(e.CommitId);
        }

        private void CommitFileSelected(object sender, CommitFileSelectedEventArgs e)
        {
            CommitFileSourceView.Buffer.HighlightSyntax = true;
            CommitFileSourceView.Buffer.Language = GetLanguage(e.Path);
            CommitFileSourceView.Buffer.Text = Git.GetFileContent(e.Path, e.CommitId);

            commitShaLabel.Text = $"CommitId: {e.CommitId}";
            commitFileLabel.Text = $"File: {System.IO.Path.GetFileName(e.Path)}";
            commitAuthorLabel.Text = Git.GetCommitAuthor(e.CommitId);
        }

        private Language GetLanguage(string path)
        {
            var ext = System.IO.Path.GetExtension(path);
            var mgr = new LanguageManager();

            var ls = mgr.LanguageIds;
            var lss = string.Join('\n', ls);

            switch (ext)
            {
                case ".cs":
                    return mgr.GetLanguage("c-sharp");
                case ".html":
                    return mgr.GetLanguage("html");
                case ".css":
                case ".scss":
                    return mgr.GetLanguage("css");
                case ".sql":
                    return mgr.GetLanguage("sql");
                case ".ts":
                    return mgr.GetLanguage("typescript");
                case ".js":
                    return mgr.GetLanguage("javascript");
                case ".json":
                    return mgr.GetLanguage("json");
                case ".rs":
                    return mgr.GetLanguage("rust");
                default:
                    return mgr.GetLanguage("c-sharp");
            }
        }
    }
}