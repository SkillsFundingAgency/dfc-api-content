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

namespace DFC.ServiceTaxonomy.ApiFunction.Tests
{
    public class ExecuteHttpTriggerTests
    {
        private readonly Execute _executeFunction;
        private readonly ILogger _log;
        private readonly HttpRequest _request;
        private readonly IOptionsMonitor<ContentApiOptions> _ContentTypeNameMapConfig;
        private readonly IDataSourceProvider _dataSource;
        private readonly IJsonFormatHelper _jsonHelper;

        public ExecuteHttpTriggerTests()
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
            _jsonHelper = A.Fake<IJsonFormatHelper>();

            _jsonHelper = new JsonFormatHelper();
            _executeFunction = new Execute(_ContentTypeNameMapConfig, _dataSource, _jsonHelper);
        }

        [Fact]
        public async Task Execute_WhenNoParametersPresent_ReturnsBadRequestObjectResult()
        {
            var result = await RunFunction("", null);

            var badRequestObjectResult = result as BadRequestObjectResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.True(result is BadRequestObjectResult);
            Assert.Equal((int?)HttpStatusCode.BadRequest, badRequestObjectResult.StatusCode);
        }

        [Fact]
        public async Task Execute_WhenContentTypePresentInMap_NoGraphData_ReturnsNotFoundObjectResult()
        {
            var result = await RunFunction("test1", null);

            var notFoundObjectResult = result as NotFoundObjectResult;

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.True(result is NotFoundObjectResult);
            Assert.Equal((int?)HttpStatusCode.NotFound, notFoundObjectResult.StatusCode);
        }

        [Fact]
        public async Task Execute_GetAllPages_ReturnsCorrectJsonResponse()
        {
            var recordJsonInput = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/Input/PageRecordInput_GetAll.json");
            var expectedJsonOutput = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/Output/PageRecordOutput_GetAll.json");

            A.CallTo(() => _dataSource.Run(A<GenericQuery>.Ignored))
                .Returns(new List<Dictionary<string, object>>
                {
                    JsonConvert.DeserializeObject<Dictionary<string, object>>(recordJsonInput)
                });

            var result = await RunFunction("test1", null);
            var okObjectResult = result as OkObjectResult;

            // Assert
            Assert.True(result is OkObjectResult);

            var resultJson = JsonConvert.SerializeObject(okObjectResult.Value);

            var equal = JToken.DeepEquals(JToken.Parse(expectedJsonOutput), JToken.Parse(resultJson));
            Assert.True(equal);
        }

        [Fact]
        public async Task Execute_GetPage_ReturnsCorrectJsonResponse()
        {
            var recordJsonInput = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/Input/PageRecordInput_GetById.json");
            var expectedJsonOutput = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/Output/PageRecordOutput_GetById.json");

            var driverRecords = new List<Dictionary<string, object>> { JsonConvert.DeserializeObject<Dictionary<string, object>>(recordJsonInput) };

            A.CallTo(() => _dataSource.Run(A<GenericQuery>.Ignored)).Returns(driverRecords);

            var result = await RunFunction("test1", Guid.NewGuid());
            var okObjectResult = result as OkObjectResult;

            // Assert
            Assert.True(result is OkObjectResult);

            var equal = JToken.DeepEquals(JToken.Parse(okObjectResult.Value.ToString()), JToken.Parse(expectedJsonOutput));
            Assert.True(equal);
        }

        private async Task<IActionResult> RunFunction(string contentType, Guid? id)
        {
            return await _executeFunction.Run(_request, contentType, id, null, _log).ConfigureAwait(false);
        }
    }
}