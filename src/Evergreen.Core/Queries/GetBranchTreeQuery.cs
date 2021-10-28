using Evergreen.Core.Git.Models;

using MediatR;

namespace Evergreen.Core.Queries
{
    public record GetBranchTreeQuery : IRequest<BranchTree>;
}
