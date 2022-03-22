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
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DFC.Api.Content.UnitTests
{
    public class ExpandHttpTriggerTests
    {
        private readonly Expand _expandFunction;
        private readonly ILogger _log;
        private readonly HttpRequest _request;
        private readonly IOptionsMonitor<ContentApiOptions> _ContentTypeNameMapConfig;
        private readonly IDataSourceProvider _dataSource;

        public ExpandHttpTriggerTests()
        {
            var context = new DefaultHttpContext();
            _request = context.Request;

            _ContentTypeNameMapConfig = A.Fake<IOptionsMonitor<ContentApiOptions>>();
            A.CallTo(() => _ContentTypeNameMapConfig.CurrentValue).Returns(new ContentApiOptions
            {
                ContentTypeNameMap = new Dictionary<string, string> { { "test1", "Test2" }, { "test2", "Test3" } }
            }); 

            _dataSource = A.Fake<IDataSourceProvider>();

            _log = A.Fake<ILogger>();

            _expandFunction = new Expand(_dataSource);
        }

        [Fact]
        public async Task Expand_WhenNoParametersPresent_ReturnsBadRequestObjectResult()
        {
            var result = await RunFunction("", Guid.NewGuid());

            var badRequestObjectResult = result as BadRequestObjectResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.True(result is BadRequestObjectResult);
            Assert.Equal((int?)HttpStatusCode.BadRequest, badRequestObjectResult.StatusCode);
        }

        [Fact]
        public async Task Expand_WhenContentTypePresentInMap_NoGraphData_ReturnsNotFoundObjectResult()
        {
            var result = await RunFunction("test1", Guid.NewGuid());

            var notFoundObjectResult = result as NotFoundObjectResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.True(result is NotFoundObjectResult);
            Assert.Equal((int?)HttpStatusCode.NotFound, notFoundObjectResult.StatusCode);
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
            
            var result = await RunFunction("page", Guid.NewGuid());
            var okObjectResult = result as OkObjectResult;

            // Assert
            Assert.True(result is OkObjectResult);
            
            var resultJson = JsonConvert.SerializeObject(okObjectResult.Value, Formatting.Indented, 
                new JsonSerializerSettings 
                { 
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });

            var equal = JToken.DeepEquals(JToken.Parse(expectedJsonOutput), JToken.Parse(resultJson));
            Assert.True(equal);
        }

        private async Task<IActionResult> RunFunction(string contentType, Guid id)
        {
            return await _expandFunction.Run(_request, contentType, id, _log, null, "published").ConfigureAwait(false);
        }
    }
}