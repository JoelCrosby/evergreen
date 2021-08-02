using System.Threading;
using System.Threading.Tasks;

using Evergreen.Lib.Git.Models;
using Evergreen.Lib.Queries;
using Evergreen.Lib.Services;

using MediatR;

namespace Evergreen.Lib.Handlers
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
