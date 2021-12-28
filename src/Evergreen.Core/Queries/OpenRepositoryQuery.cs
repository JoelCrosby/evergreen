using MediatR;

namespace Evergreen.Core.Queries
{
    public record OpenRepositoryQuery(string Path) : IRequest;
}
