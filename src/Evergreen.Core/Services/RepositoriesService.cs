using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Evergreen.Core.Configuration;
using Evergreen.Core.Git;
using Evergreen.Core.Git.Models;
using Evergreen.Core.Models;
using Evergreen.Core.Models.Common;
using Evergreen.Core.Session;

namespace Evergreen.Core.Services
{
    public class RepositoriesService
    {
        private int _selectedRepoIndex;

        private readonly RepositorySession _session;
        private readonly List<GitService> _repositories = new();

        private GitService Repository => _repositories.ElementAt(_selectedRepoIndex);

        public RepositoriesService()
        {
            _session = Sessions.LoadSession();
        }

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

        public IEnumerable<CommitListItem> GetCommits()
        {
            return Repository.GetCommitListItems();
        }

        public BranchTree GetBranchTree()
        {
            return Repository.GetBranchTree();
        }

        public Task<Result<ExecResult>> Fetch()
        {
            return Repository.Fetch();
        }
    }
}
