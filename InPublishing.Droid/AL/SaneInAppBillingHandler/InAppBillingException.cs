using System;

namespace InPublishing
{
    public class InAppBillingException : Exception
    {
        public InAppBillingException(string message)
            : base(message)
        { }
    }
}