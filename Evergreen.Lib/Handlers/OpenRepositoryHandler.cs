using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Evergreen.Lib.Models;
using Evergreen.Lib.Queries;
using Evergreen.Lib.Services;

using MediatR;

namespace Evergreen.Lib.Handlers
{
    public class OpenRepositoryHandler : IRequestHandler<OpenRepositoryQuery>
    {
        private readonly RepositoriesService _repos;

        public OpenRepositoryHandler(RepositoriesService repos)
        {
            _repos = repos;
        }

        public Task<Unit> Handle(OpenRepositoryQuery request, CancellationToken cancellationToken)
        {
            _repos.OpenRepository(request.path);

            return Task.FromResult(Unit.Value);
        }
    }
}
