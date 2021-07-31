namespace Evergreen.Lib.Models
{
    public class CommitListItem
    {
        public string? Message { get; init; }
        public string? Author { get; init; }
        public string? Sha { get; init; }
        public string? CommitDate { get; init; }
        public string? Id { get; init; }
    }
}
