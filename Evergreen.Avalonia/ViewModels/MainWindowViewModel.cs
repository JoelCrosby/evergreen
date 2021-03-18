using System.Collections.Generic;
using System.Linq;

using Evergreen.Avalonia.ViewModels.Common;
using Evergreen.Lib.Configuration;
using Evergreen.Lib.Git;
using Evergreen.Lib.Session;

namespace Evergreen.Avalonia.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public RepositorySession ActiveSession { get; }
        public GitService Git { get; }

        public IEnumerable<CommitListItemViewModel> CommitList { get; private set; }

        public MainWindowViewModel()
        {
            ActiveSession = RestoreSession.LoadSession();
            Git = new GitService(ActiveSession);

            CommitList = BuildCommitList();
        }

        private IEnumerable<CommitListItemViewModel> BuildCommitList()
        {
            var commits = Git.GetCommits();

            return commits.Select(commit => new CommitListItemViewModel
            {
                CommitDate = $"{commit.Author.When:dd MMM yyyy HH:mm}",
                Author = commit.Author.Name,
                Message = commit.MessageShort,
                Sha = commit.Sha.Substring(0, 7),
            });
        }
    }
}
