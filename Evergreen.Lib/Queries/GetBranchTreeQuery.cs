using Evergreen.Lib.Git.Models;

using MediatR;

namespace Evergreen.Lib.Queries
{
    public record GetBranchTreeQuery : IRequest<BranchTree>;
}
