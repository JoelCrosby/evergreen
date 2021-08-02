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
        private readonly RepositoriesService _repos;

        public FetchHandler(RepositoriesService repositoriesService)
        {
            _repos = repositoriesService;
        }

        public async Task<Result<ExecResult>> Handle(FetchCommand request, CancellationToken cancellationToken)
        {
            return await _repos.Fetch().ConfigureAwait(false);
        }
    }
}
