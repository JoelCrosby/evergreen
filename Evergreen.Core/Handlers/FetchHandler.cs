using System.Threading;
using System.Threading.Tasks;

using Evergreen.Core.Commands;
using Evergreen.Core.Models;
using Evergreen.Core.Models.Common;
using Evergreen.Core.Services;

using MediatR;

namespace Evergreen.Core.Handlers
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
