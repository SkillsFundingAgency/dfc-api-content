using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using DFC.Api.Content.Models.Cypher;
using DFC.ServiceTaxonomy.ApiFunction.Function;
using DFC.ServiceTaxonomy.ApiFunction.Models;
using DFC.ServiceTaxonomy.Neo4j.Services;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
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
        private const string DefaultFunctionName = "GetAllSkills";
        private readonly Execute _executeFunction;
        private readonly ILogger _log;
        private readonly HttpRequest _request;
        private readonly ExecutionContext _executionContext;
        private readonly IOptionsMonitor<ServiceTaxonomyApiSettings> _graphConfig;
        private readonly IOptionsMonitor<ContentTypeMapSettings> _contentTypeMapConfig;
        private readonly IResultSummary _resultSummary;
        private readonly IGraphDatabase _graphDatabase;

        public ExecuteHttpTriggerTests()
        {
            var context = new DefaultHttpContext();
            _request = context.Request;

            _executionContext = new ExecutionContext();

            _graphConfig = A.Fake<IOptionsMonitor<ServiceTaxonomyApiSettings>>();
            A.CallTo(() => _graphConfig.CurrentValue).Returns(new ServiceTaxonomyApiSettings
            {
                Function = DefaultFunctionName,
                Neo4jUrl = "bolt://localhost:11002",
                Neo4jUser = "NeoUser",
                Neo4jPassword = "NeoPass",
                Scheme = "https://",
                ApplicationName = "ServiceTaxonomy"
            });

            _contentTypeMapConfig = A.Fake<IOptionsMonitor<ContentTypeMapSettings>>();
            A.CallTo(() => _contentTypeMapConfig.CurrentValue).Returns(new ContentTypeMapSettings
            {
                OverrideUri = false,
                ContentTypeMap = new Dictionary<string, string>() { { "test1", "Test2" }, { "test2", "Test3" } }
            }); ;

            _log = A.Fake<ILogger>();
            _resultSummary = A.Fake<IResultSummary>();
            _graphDatabase = A.Fake<IGraphDatabase>();

            //A.CallTo(() => _neo4JHelper.GetResultSummaryAsync()).Returns(_resultSummary);

            _executeFunction = new Execute(_contentTypeMapConfig, _graphDatabase);
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
        public async Task Execute_WhenContentTypePresentInMap_NoGraphData_ReturnsNoContentResult()
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
            var recordJson = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/JobProfileRecordResponse_1.json");

            var cypherQuery = new GenericCypherQuery("");

            A.CallTo(() => _graphDatabase.Run(A<GenericCypherQuery>.Ignored)).Returns(new List<IRecord>() { new Api.Content.UnitTests.Models.Record(new string[] { "data.properties" }, new object[] { JsonConvert.DeserializeObject<Dictionary<string, object>>(recordJson) }) });

            var result = await RunFunction("test1", null);
            var okObjectResult = result as OkObjectResult;

            // Assert
            Assert.True(result is OkObjectResult);

            var resultJson = okObjectResult.Value.ToString();

            var equal = JToken.DeepEquals(JToken.Parse(recordJson), JToken.Parse(resultJson));
            Assert.True(equal);
        }

        [Fact]
        public async Task Execute_GetJobProfile_ReturnsCorrectJsonResponse()
        {
            var recordJson = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/JobProfileRecordResponse_2.json");

            var cypherQuery = new GenericCypherQuery("");

            A.CallTo(() => _graphDatabase.Run(A<GenericCypherQuery>.Ignored)).Returns(new List<IRecord>() { new Api.Content.UnitTests.Models.Record(new string[] { "values" }, new object[] { JsonConvert.DeserializeObject<Dictionary<string, object>>(recordJson) }) });

            var result = await RunFunction("test1", Guid.NewGuid());
            var okObjectResult = result as OkObjectResult;

            // Assert
            Assert.True(result is OkObjectResult);

            var resultJson = okObjectResult.Value.ToString();

            var equal = JToken.DeepEquals(JToken.Parse(recordJson), JToken.Parse(resultJson));
        }


        private async Task<IActionResult> RunFunction(string contentType, Guid? id)
        {
            return await _executeFunction.Run(_request, contentType, id, _log).ConfigureAwait(false);
        }
    }
}