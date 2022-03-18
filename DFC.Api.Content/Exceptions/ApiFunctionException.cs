using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Web.Http;
using Microsoft.AspNetCore.Mvc;

namespace DFC.Api.Content.Exceptions
{
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class ApiFunctionException : Exception
    {
        public ActionResult? ActionResult { get; }

        public ApiFunctionException()
        {
            ActionResult = null;
        }

        public ApiFunctionException(ActionResult actionResult, string message)
            : base(message)
        {
            ActionResult = actionResult;
        }

        public ApiFunctionException(ActionResult actionResult, string message, Exception? innerException)
            : base(message, innerException)
        {
            ActionResult = actionResult;
        }

        public static ApiFunctionException BadRequest(string message, Exception? innerException = null)
        {
            return new ApiFunctionException(new BadRequestObjectResult(message), message, innerException);
        }

        public static ApiFunctionException UnprocessableEntityObjectResult(string message, Exception? innerException = null)
        {
            return new ApiFunctionException(new UnprocessableEntityObjectResult(message), message, innerException);
        }

        public static ApiFunctionException InternalServerError(string message, Exception? innerException = null)
        {
            return new ApiFunctionException(new InternalServerErrorResult(), message, innerException);
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        protected ApiFunctionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ActionResult = null;
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            // MUST call through to the base class to let it save its own state
            base.GetObjectData(info, context);
        }
    }
}