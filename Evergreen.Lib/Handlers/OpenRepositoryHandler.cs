using System.Threading;
using System.Threading.Tasks;

using Evergreen.Lib.Queries;
using Evergreen.Lib.Services;

using MediatR;

namespace Evergreen.Lib.Handlers
{
    public class OpenRepositoryHandler : IRequestHandler<OpenRepositoryQuery>
    {
        private readonly RepositoriesService repos;

        public OpenRepositoryHandler(RepositoriesService repos)
        {
            this.repos = repos;
        }

        public Task<Unit> Handle(OpenRepositoryQuery request, CancellationToken cancellationToken)
        {
            repos.OpenRepository(request.Path);

            return Task.FromResult(Unit.Value);
        }
    }
}
