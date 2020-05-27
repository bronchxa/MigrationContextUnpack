using System;

namespace MigrationContextUnpack.Sources.CSharp
{
    public class CustomException : Exception
    {
        public new string Message { get; }
        public new string StackTrace { get { return stackTrace; } set { stackTrace = value; } }
        private string stackTrace;
        
        public CustomException(string message) : base() { Message = message; stackTrace = Environment.StackTrace; }

        public override string ToString()
        {
            return string.Concat(Message, Environment.NewLine, StackTrace);
        }
    }
}
