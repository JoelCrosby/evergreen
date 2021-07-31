using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Evergreen.Lib.Git.Models;
using Evergreen.Lib.Queries;
using Evergreen.Lib.Services;

using MediatR;

namespace Evergreen.Lib.Handlers
{
    public class GetBranchTreeHandler : IRequestHandler<GetBranchTreeQuery, IEnumerable<BranchTreeItem>>
    {
        private readonly RepositoriesService _repos;

        public GetBranchTreeHandler(RepositoriesService repos)
        {
            _repos = repos;
        }

        public async Task<IEnumerable<BranchTreeItem>> Handle(GetBranchTreeQuery request, CancellationToken cancellationToken)
        {
            return _repos.GetBranchTree();
        }
    }
}
