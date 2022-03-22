using DFC.Api.Content.Function;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DFC.Api.Content.UnitTests.Functions
{
    public class HealthPingHttpTriggerTests
    {
        private readonly ILogger _logger = A.Fake<ILogger>();

        [Fact]
        public void HealthPingHttpTriggerTestsReturnsOk()
        {
            // Arrange

            // Act
            var result = HealthPingHttpTrigger.Run(new DefaultHttpRequest(new DefaultHttpContext()), _logger);

            // Assert
            Assert.IsType<OkResult>(result);
        }
    }
}
