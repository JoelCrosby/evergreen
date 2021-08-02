using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Evergreen.Lib.Configuration;
using Evergreen.Lib.Git;
using Evergreen.Lib.Git.Models;
using Evergreen.Lib.Models;
using Evergreen.Lib.Models.Common;
using Evergreen.Lib.Session;

namespace Evergreen.Lib.Services
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
