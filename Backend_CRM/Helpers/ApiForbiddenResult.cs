using Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Mvc;



namespace CRM.Helpers

{

    /// <summary>

    /// Returns HTTP 403 without requiring ASP.NET authentication middleware

    /// (this CRM uses query-param userId, not JWT/Identity Forbid).

    /// </summary>

    public static class ApiForbiddenResult

    {

        public static ObjectResult Create(

            string message = "You do not have permission to perform this action.",

            object? details = null)

        {

            return new ObjectResult(new { message, details })

            {

                StatusCode = StatusCodes.Status403Forbidden,

            };

        }

    }

}


