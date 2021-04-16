namespace Evergreen.Lib.Git
{
    public record ExecResult
    {
        public bool IsSuccess { get; init;}

        public string Message { get; init; }

        protected ExecResult()
        {
        }

        public static ExecResult Success()
        {
            return new ExecResult
            {
                IsSuccess = true,
            };
        }

        public static ExecResult Failed(string message)
        {
            return new ExecResult
            {
                IsSuccess = false,
                Message = message,
            };
        }
    }
}
