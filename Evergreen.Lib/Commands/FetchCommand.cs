using Evergreen.Lib.Models;
using Evergreen.Lib.Models.Common;

using MediatR;

namespace Evergreen.Lib.Commands
{
    public record FetchCommand : IRequest<Result<ExecResult>>;
}
