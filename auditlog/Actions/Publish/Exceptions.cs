using System;

namespace auditlog
{
    public class InvalidTargetSettingsException : Exception
    {
        public InvalidTargetSettingsException()
        {
        }

        public InvalidTargetSettingsException(string message)
            : base(message)
        {
        }

        public InvalidTargetSettingsException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
