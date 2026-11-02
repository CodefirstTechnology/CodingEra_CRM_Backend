using System;

namespace ERP.Application.Procurement
{
    public class ProcurementSecurityException : Exception
    {
        public ProcurementSecurityException(string message) : base(message)
        {
        }

        public ProcurementSecurityException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

