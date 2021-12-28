using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Evergreen.Core.Models;
using Evergreen.Core.Queries;
using Evergreen.Core.Services;

using MediatR;

namespace Evergreen.Core.Handlers
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
