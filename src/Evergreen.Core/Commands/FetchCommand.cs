using Evergreen.Core.Models;
using Evergreen.Core.Models.Common;

using MediatR;

namespace Evergreen.Core.Commands
{
    public record FetchCommand : IRequest<Result<ExecResult>>;
}
