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
        private readonly RepositoriesService _repos;

        public GetCommitsHandler(RepositoriesService repos)
        {
            _repos = repos;
        }

        public Task<IEnumerable<CommitListItem>> Handle(GetCommitsQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_repos.GetCommits());
        }
    }
}
