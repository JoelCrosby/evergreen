namespace Evergreen.Lib.Models.Common
{
    public record Result
    {
        public bool IsSuccess { get; init; }

        public string? Message { get; init; }

        public static Result Success() => new()
        {
            IsSuccess = true,
        };

        public static Result Failed(string message) => new()
        {
            IsSuccess = false,
            Message = message,
        };
    }

    public record Result<T>
    {
        public bool IsSuccess { get; init; }

        public string? Message { get; init; }

        public T? Payload { get; init; }

        public static Result<T> Success(T payload = default!)
        {
            return new()
            {
                IsSuccess = true,
                Payload = payload,
            };
        }

        public static Result<T> Failed(string message)
        {
            return new()
            {
                IsSuccess = false,
                Message = message,
            };
        }
    }
}
