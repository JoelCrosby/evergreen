using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Evergreen.Lib.Models;
using Evergreen.Lib.Queries;
using Evergreen.Lib.Services;

using MediatR;

namespace Evergreen.Lib.Handlers
{
    public class GetCommitsHandler : IRequestHandler<GetCommitsQuery, IEnumerable<CommitListItem>>
    {
        private readonly RepositoriesService repos;

        public GetCommitsHandler(RepositoriesService repos)
        {
            this.repos = repos;
        }

        public async Task<IEnumerable<CommitListItem>> Handle(GetCommitsQuery request, CancellationToken cancellationToken)
        {
            return repos.GetCommits();
        }
    }
}
