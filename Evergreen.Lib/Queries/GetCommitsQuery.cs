using System.Collections.Generic;

using Evergreen.Lib.Models;

using MediatR;

namespace Evergreen.Lib.Queries
{
    public record GetCommitsQuery : IRequest<IEnumerable<CommitListItem>>;
}
