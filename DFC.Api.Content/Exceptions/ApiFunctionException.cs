using System;
using System.Web.Http;
using Microsoft.AspNetCore.Mvc;

namespace DFC.ServiceTaxonomy.ApiFunction.Exceptions
{
    public class ApiFunctionException : Exception
    {
        public ActionResult ActionResult { get; }
        
        // could split into StatusCodeResult & ObjectResult
        
        public ApiFunctionException(ActionResult actionResult, string message)
            : base(message)
        {
            ActionResult = actionResult;
        }
        
        public ApiFunctionException(ActionResult actionResult, string message, Exception innerException)
        : base(message, innerException)
        {
            ActionResult = actionResult;
        }

        public static ApiFunctionException BadRequest(string message, Exception innerException = null)
        {
            return new ApiFunctionException(new BadRequestObjectResult(message), message, innerException);
        }

        public static ApiFunctionException UnprocessableEntityObjectResult(string message, Exception innerException = null)
        {
            return new ApiFunctionException(new UnprocessableEntityObjectResult(message), message, innerException);
        }
        
        public static ApiFunctionException InternalServerError(string message, Exception innerException = null)
        {
            return new ApiFunctionException(new InternalServerErrorResult(), message, innerException);
        }
    }
}