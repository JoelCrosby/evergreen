using System;

namespace Evergreen.Core.Git
{
    public class GitException : Exception
    {
        public GitException()
        {
        }

        public GitException(string message)
            : base(message)
        {
        }

        public GitException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
