using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using DFC.Api.Content.Helpers;
using DFC.Api.Content.Models.Cypher;
using DFC.ServiceTaxonomy.ApiFunction.Function;
using DFC.ServiceTaxonomy.ApiFunction.Models;
using DFC.ServiceTaxonomy.Neo4j.Services;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
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
        private readonly IOptionsMonitor<ContentTypeSettings> _ContentTypeNameMapConfig;
        private readonly IGraphDatabase _graphDatabase;
        private readonly IJsonFormatHelper _jsonHelper;

        public ExecuteHttpTriggerTests()
        {
            var context = new DefaultHttpContext();
            _request = context.Request;

            _ContentTypeNameMapConfig = A.Fake<IOptionsMonitor<ContentTypeSettings>>();
            A.CallTo(() => _ContentTypeNameMapConfig.CurrentValue).Returns(new ContentTypeSettings
            {
                OverrideUri = false,
                ContentTypeNameMap = new Dictionary<string, string>() { { "test1", "Test2" }, { "test2", "Test3" } }
            }); ;

            _log = A.Fake<ILogger>();
            _graphDatabase = A.Fake<IGraphDatabase>();
            _jsonHelper = A.Fake<IJsonFormatHelper>();

            _jsonHelper = new JsonFormatHelper(_ContentTypeNameMapConfig);
            _executeFunction = new Execute(_ContentTypeNameMapConfig, _graphDatabase, _jsonHelper);
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
        public async Task Execute_WhenContentTypeNotPresentInMap_ReturnsBadRequestObjectResult()
        {
            var result = await RunFunction("abcdefghi", null);

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
        public async Task Execute_GetAllJobProfiles_ReturnsCorrectJsonResponse()
        {
            var recordJsonInput = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/Input/JobProfileRecordInput_GetAll.json");
            var expectedJsonOutput = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/Output/JobProfileRecordOutput_GetAll.json");

            A.CallTo(() => _graphDatabase.Run(A<GenericCypherQuery>.Ignored)).Returns(new List<IRecord>() { new Api.Content.UnitTests.Models.Record(new string[] { "data.properties" }, new object[] { JsonConvert.DeserializeObject<Dictionary<string, object>>(recordJsonInput.ToString()) }) });

            var result = await RunFunction("test1", null);
            var okObjectResult = result as OkObjectResult;

            // Assert
            Assert.True(result is OkObjectResult);

            var resultJson = okObjectResult.Value.ToString();

            var equal = JToken.DeepEquals(JToken.Parse(expectedJsonOutput), JToken.Parse(resultJson));
            Assert.True(equal);
        }

        [Fact]
        public async Task Execute_GetJobProfile_ReturnsCorrectJsonResponse()
        {
            var recordJsonInput = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/Input/JobProfileRecordInput_GetById.json");
            var expectedJsonOutput = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/Output/JobProfileRecordOutput_GetById.json");

            var driverRecords = new List<IRecord>() { new Api.Content.UnitTests.Models.Record(new string[] { "values" }, new object[] { JsonConvert.DeserializeObject<Dictionary<string, object>>(recordJsonInput.ToString()) }) };

            A.CallTo(() => _graphDatabase.Run(A<GenericCypherQuery>.Ignored)).Returns(driverRecords);

            var result = await RunFunction("test1", Guid.NewGuid());
            var okObjectResult = result as OkObjectResult;

            // Assert
            Assert.True(result is OkObjectResult);

            var equal = JToken.DeepEquals(JToken.Parse(okObjectResult.Value.ToString()), JToken.Parse(expectedJsonOutput));
            Assert.True(equal);
        }

        private async Task<IActionResult> RunFunction(string contentType, Guid? id)
        {
            return await _executeFunction.Run(_request, contentType, id, _log).ConfigureAwait(false);
        }
    }
}