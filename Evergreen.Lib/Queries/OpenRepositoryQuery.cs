using MediatR;

namespace Evergreen.Lib.Queries
{
    public record OpenRepositoryQuery(string Path) : IRequest;
}
