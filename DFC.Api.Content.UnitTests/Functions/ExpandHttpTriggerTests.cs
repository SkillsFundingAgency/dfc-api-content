using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using DFC.Api.Content.Function;
using DFC.Api.Content.Helpers;
using DFC.Api.Content.Interfaces;
using DFC.Api.Content.Models;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DFC.Api.Content.UnitTests.Functions
{
    public class ExpandHttpTriggerTests
    {
        private readonly Expand _expandFunction;
        private readonly ILogger _log;
        private readonly HttpRequest _request;
        private readonly IDataSourceProvider _dataSource;

        public ExpandHttpTriggerTests()
        {
            var context = new DefaultHttpContext();
            _request = context.Request;

            _dataSource = A.Fake<IDataSourceProvider>();
            _log = A.Fake<ILogger>();
            
            _expandFunction = new Expand(_dataSource, new JsonFormatHelper());
        }

        [Fact]
        public async Task Expand_WhenNoParametersPresent_ReturnsBadRequestObjectResult()
        {
            var result = await RunFunction(string.Empty, Guid.NewGuid());

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.IsType<BadRequestObjectResult>(result);

            var badRequestObjectResult = result as BadRequestObjectResult;
            Assert.Equal((int?)HttpStatusCode.BadRequest, badRequestObjectResult!.StatusCode);
        }

        [Fact]
        public async Task Expand_WhenNoData_ReturnsNotFoundObjectResult()
        {
            var result = await RunFunction("test1", Guid.NewGuid()); ;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.IsType<NotFoundObjectResult>(result);

            var notFoundObjectResult = result as NotFoundObjectResult;
            Assert.Equal((int?)HttpStatusCode.NotFound, notFoundObjectResult!.StatusCode);
        }

        [Fact]
        public async Task Expand_GetPage_ReturnsCorrectJsonResponse()
        {
            var expectedJsonOutput = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/Output/PageRecordOutput_Expand.json");
            
            var recordJsonInputPage = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/Input/PageRecordInput_Expand_Page.json");
            A.CallTo(() => _dataSource.Run(A<GenericQuery>.That.Matches(x => x.ContentType == "page")))
                .Returns(new List<Dictionary<string, object>>
                {
                    JsonConvert.DeserializeObject<Dictionary<string, object>>(recordJsonInputPage)
                });
            
            var recordJsonInputPageLocation1 = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/Input/PageRecordInput_Expand_PageLocation1.json");
            A.CallTo(() => _dataSource.Run(A<GenericQuery>.That.Matches(x => x.QueryText.Contains("c78a4c9c-df64-4ead-a307-ba73165b7286"))))
                .Returns(new List<Dictionary<string, object>>
                {
                    JsonConvert.DeserializeObject<Dictionary<string, object>>(recordJsonInputPageLocation1)
                });
            
            var recordJsonInputPageLocation2 = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/Input/PageRecordInput_Expand_PageLocation2.json");
            A.CallTo(() => _dataSource.Run(A<GenericQuery>.That.Matches(x => x.QueryText.Contains("48b3f8cb-27c5-4e3a-9a53-69b6cfe8e408"))))
                .Returns(new List<Dictionary<string, object>>
                {
                    JsonConvert.DeserializeObject<Dictionary<string, object>>(recordJsonInputPageLocation2)
                });          

            var recordJsonInputPageLocation3 = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/Input/PageRecordInput_Expand_PageLocation3.json");
            A.CallTo(() => _dataSource.Run(A<GenericQuery>.That.Matches(x => x.QueryText.Contains("a6d20c3c-51c0-437a-84c4-ae23cc78c99a"))))
                .Returns(new List<Dictionary<string, object>>
                {
                    JsonConvert.DeserializeObject<Dictionary<string, object>>(recordJsonInputPageLocation3)
                });            
            
            var recordJsonInputTaxonomy = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/Input/PageRecordInput_Expand_Taxonomy.json");
            A.CallTo(() => _dataSource.Run(A<GenericQuery>.That.Matches(x => x.ContentType == "taxonomy")))
                .Returns(new List<Dictionary<string, object>>
                {
                    JsonConvert.DeserializeObject<Dictionary<string, object>>(recordJsonInputTaxonomy)
                });
            
            var result = await RunFunction("page", Guid.Parse("f60c7388-7b89-40d6-9911-52f5ae6b4a41"));
            
            // Assert
            Assert.IsType<OkObjectResult>(result);

            var okObjectResult = result as OkObjectResult;
            var actualJsonOutput = JsonConvert.SerializeObject(okObjectResult!.Value);

            Assert.Equal(
                JsonConvert.SerializeObject(JToken.Parse(expectedJsonOutput)), 
                actualJsonOutput);
        }

        private async Task<IActionResult> RunFunction(string contentType, Guid id)
        {
            return await _expandFunction.Run(_request, contentType, id, _log, "published").ConfigureAwait(false);
        }
    }
}