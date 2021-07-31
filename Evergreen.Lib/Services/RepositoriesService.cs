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
        private int selectedRepoIndex;
        private readonly List<GitService> repositories = new();
        private GitService Repository => repositories.ElementAt(selectedRepoIndex);
        private readonly RepositorySession session;

        public void OpenRepository(string path)
        {
            var notValid = !GitService.IsRepository(path);

            if (notValid)
            {
                return;
            }

            repositories.Add(new GitService(path));
            selectedRepoIndex = repositories.Count - 1;

            Sessions.SaveSession(session);
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
