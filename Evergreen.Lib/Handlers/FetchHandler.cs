using System.Threading;
using System.Threading.Tasks;

using Evergreen.Lib.Commands;
using Evergreen.Lib.Models;
using Evergreen.Lib.Models.Common;
using Evergreen.Lib.Services;

using MediatR;

namespace Evergreen.Lib.Handlers
{
    public class FetchHandler : IRequestHandler<FetchCommand, Result<ExecResult>>
    {
        private readonly RepositoriesService repos;

        public FetchHandler(RepositoriesService repositoriesService)
        {
            repos = repositoriesService;
        }

        public async Task<Result<ExecResult>> Handle(FetchCommand command, CancellationToken cancellationToken)
        {
            return await repos.Fetch().ConfigureAwait(false);
        }
    }
}
