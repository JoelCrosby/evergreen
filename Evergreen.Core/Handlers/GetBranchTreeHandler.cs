using System.Threading;
using System.Threading.Tasks;

using Evergreen.Core.Git.Models;
using Evergreen.Core.Queries;
using Evergreen.Core.Services;

using MediatR;

namespace Evergreen.Core.Handlers
{
    public class GetBranchTreeHandler : IRequestHandler<GetBranchTreeQuery, BranchTree>
    {
        private readonly RepositoriesService _repos;

        public GetBranchTreeHandler(RepositoriesService repositoriesService)
        {
            _repos = repositoriesService;
        }

        public Task<BranchTree> Handle(GetBranchTreeQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_repos.GetBranchTree());
        }
    }
}
