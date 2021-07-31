using System.Collections.Generic;
using System.Linq;

using Evergreen.Lib.Configuration;
using Evergreen.Lib.Git;
using Evergreen.Lib.Git.Models;
using Evergreen.Lib.Models;
using Evergreen.Lib.Session;

namespace Evergreen.Lib.Services
{
    public class RepositoriesService
    {
        private int _selectedRepoIndex;
        private readonly List<GitService> _repositories = new();
        private GitService Repository => _repositories.ElementAtOrDefault(_selectedRepoIndex);
        private readonly RepositorySession _session;

        public void OpenRepository(string path)
        {
            var notValid = !GitService.IsRepository(path);

            if (notValid)
            {
                return;
            }

            _repositories.Add(new GitService(path));
            _selectedRepoIndex = _repositories.Count - 1;

            Sessions.SaveSession(_session);
        }

        public IEnumerable<CommitListItem> GetCommits() => Repository.GetCommitListItems();

        public IEnumerable<BranchTreeItem> GetBranchTree() => Repository.GetBranchTree();
    }
}
