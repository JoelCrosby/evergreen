namespace Evergreen.Lib.Models.Common
{
    public record Result
    {
        public bool IsSuccess { get; init; }

        public string Message { get; set; }

        public static Result Success()
        {
            return new Result
            {
                IsSuccess = true,
            };
        }

        public static Result Failed(string message)
        {
            return new Result
            {
                IsSuccess = false,
                Message = message,
            };
        }
    }

    public record Result<T>
    {
        public bool IsSuccess { get; init; }

        public string Message { get; set; }

        public T Payload { get; set; }

        public static Result<T> Success(T payload = default)
        {
            return new Result<T>
            {
                IsSuccess = true,
                Payload = payload,
            };
        }

        public static Result<T> Failed(string message)
        {
            return new Result<T>
            {
                IsSuccess = false,
                Message = message,
            };
        }
    }
}
