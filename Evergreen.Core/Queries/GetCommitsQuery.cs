using System.Collections.Generic;

using Evergreen.Core.Models;

using MediatR;

namespace Evergreen.Core.Queries
{
    public record GetCommitsQuery : IRequest<IEnumerable<CommitListItem>>;
}
