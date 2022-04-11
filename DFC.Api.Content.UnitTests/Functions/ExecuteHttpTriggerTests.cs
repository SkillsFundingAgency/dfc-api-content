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

namespace DFC.Api.Content.UnitTests.Functions
{
    public class ExecuteHttpTriggerTests
    {
        private readonly Execute _executeFunction;
        private readonly ILogger _log;
        private readonly HttpRequest _request;
        private readonly IDataSourceProvider _dataSource;

        public ExecuteHttpTriggerTests()
        {
            var context = new DefaultHttpContext();
            _request = context.Request;

            _dataSource = A.Fake<IDataSourceProvider>();
            _log = A.Fake<ILogger>();
            
            var jsonHelper = new JsonFormatHelper();
            _executeFunction = new Execute(_dataSource, jsonHelper);
        }

        [Fact]
        public async Task Execute_WhenNoParametersPresent_ReturnsBadRequestObjectResult()
        {
            var result = await RunFunction(string.Empty, null);

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.IsType<BadRequestObjectResult>(result);

            var badRequestObjectResult = result as BadRequestObjectResult;
            Assert.Equal((int?)HttpStatusCode.BadRequest, badRequestObjectResult!.StatusCode);
        }

        [Fact]
        public async Task Execute_WhenContentTypePresentInMap_NoData_ReturnsNotFoundObjectResult()
        {
            var result = await RunFunction("test1", null);

            // Assert
            Assert.IsAssignableFrom<IActionResult>(result);
            Assert.IsType<NotFoundObjectResult>(result);

            var notFoundObjectResult = result as NotFoundObjectResult;
            Assert.Equal((int?)HttpStatusCode.NotFound, notFoundObjectResult!.StatusCode);
        }

        [Fact]
        public async Task Execute_GetAllPages_ReturnsCorrectJsonResponse()
        {
            var recordJsonInput = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/Input/PageRecordInput_GetAll.json");
            var expectedJsonOutput = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/Output/PageRecordOutput_GetAll.json");

            var recordJson = ((JObject)JsonConvert.DeserializeObject<Dictionary<string, object>>(recordJsonInput)["data"])
                .ToObject<Dictionary<string, object>>();
            
            A.CallTo(() => _dataSource.Run(A<GenericQuery>.Ignored))
                .Returns(new List<Dictionary<string, object>>
                {
                    recordJson
                });

            var result = await RunFunction("test1", null);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            var okObjectResult = result as OkObjectResult;
            var resultJson = JsonConvert.SerializeObject(okObjectResult!.Value);

            Assert.Equal(JsonConvert.SerializeObject(JToken.Parse(expectedJsonOutput)), resultJson);
        }

        [Fact]
        public async Task Execute_GetPublishedPage_ReturnsCorrectJsonResponse()
        {
            var recordJsonInput = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/Input/PageRecordInput_GetById.json");
            var expectedJsonOutput = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/Output/PageRecordOutput_GetById.json");

            var recordJson = ((JObject)JsonConvert.DeserializeObject<Dictionary<string, object>>(recordJsonInput)["data"])
                .ToObject<Dictionary<string, object>>();

            A.CallTo(() => _dataSource.Run(A<GenericQuery>.Ignored))
                .Returns(new List<Dictionary<string, object>>
                {
                    recordJson
                });

            var result = await RunFunction("test1", Guid.NewGuid());
            
            // Assert
            Assert.IsType<OkObjectResult>(result);
            
            var okObjectResult = result as OkObjectResult;
            var resultJson = JsonConvert.SerializeObject(okObjectResult!.Value);

            Assert.Equal(JsonConvert.SerializeObject(JToken.Parse(expectedJsonOutput)), resultJson);
        }

        [Fact]
        public async Task Execute_GetPreviewPage_ReturnsCorrectJsonResponse()
        {
            var recordJsonInput = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/Input/PageRecordInput_GetById.json");
            var expectedJsonOutput = File.ReadAllText(Directory.GetCurrentDirectory() + "/Files/Output/PageRecordOutput_GetById.json");

            var recordJson = ((JObject)JsonConvert.DeserializeObject<Dictionary<string, object>>(recordJsonInput)["data"])
                .ToObject<Dictionary<string, object>>();

            A.CallTo(() => _dataSource.Run(A<GenericQuery>.Ignored))
                .Returns(new List<Dictionary<string, object>>
                {
                    recordJson
                });

            var result = await RunFunction("test1", Guid.NewGuid(), "preview");

            // Assert
            Assert.IsType<OkObjectResult>(result);

            var okObjectResult = result as OkObjectResult;
            var resultJson = JsonConvert.SerializeObject(okObjectResult!.Value);

            Assert.Equal(JsonConvert.SerializeObject(JToken.Parse(expectedJsonOutput)), resultJson);
        }


        private async Task<IActionResult> RunFunction(string contentType, Guid? id, string databaseType = "published")
        {
            return await _executeFunction.Run(_request, contentType, id, _log, null, databaseType).ConfigureAwait(false);
        }
    }
}