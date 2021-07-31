using System.Collections.Generic;

using Evergreen.Lib.Git.Models;

using MediatR;

namespace Evergreen.Lib.Queries
{
    public record GetBranchTreeQuery : IRequest<IEnumerable<BranchTreeItem>>;
}
