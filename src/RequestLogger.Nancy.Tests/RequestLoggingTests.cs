using System;
using System.Linq;
using System.Text;
using Moq;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Testing;
using NUnit.Framework;

namespace RequestLogger.Nancy.Tests
{
    [TestFixture]
    [Parallelizable]
    public class RequestLoggingTests
    {
        [SetUp]
        public void SetUp()
        {
            _requestLogger = new Mock<IRequestLogger>();
        }

        private Mock<IRequestLogger> _requestLogger;

        private class TestModule : NancyModule
        {
            public TestModule()
                : base("/test")
            {
                Get["/"] = GetSuccessResponse;
                Get["/error"] = GetErrorResponse;
                Post["/"] = GetSuccessResponse;
                Put["/"] = GetSuccessResponse;
                Delete["/"] = GetSuccessResponse;
            }

            private Response GetSuccessResponse(dynamic parameters)
            {
                var response = Response.AsText("TEST", "text/html")
                        .WithHeader("X-Test", "TEST")
                        .WithStatusCode(200);
                response.ReasonPhrase = "OK";
                return response;
            }

            private static Response GetErrorResponse(dynamic parameters)
            {
                throw new Exception("ERROR");
            }
        }

        private class TestWithLoggerModule : TestModule
        {
            public TestWithLoggerModule(IRequestLogger requestLogger)
            {
                RequestLogging.Enable(this, requestLogger);
            }
        }

        [Test]
        public void Enable_Should_LogError_For_Application()
        {
            var browser = new Browser(new ConfigurableBootstrapper(config =>
            {
                config.ApplicationStartup((container, pipelines) => RequestLogging.Enable(pipelines, _requestLogger.Object));
                config
                    .Module<TestModule>();
            }));

            Assert.Throws<Exception>(() => browser.Get("/test/error", config =>
            {
                config.HostName("localhost");
                config.HttpRequest();
            }));

            _requestLogger.Verify(x => x.LogError(
                    It.IsAny<RequestData>(),
                    It.IsAny<ResponseData>(),
                    It.IsAny<Exception>()),
                Times.Once);
        }

        [Test]
        public void Enable_Should_LogError_For_Module()
        {
            var browser = new Browser(new ConfigurableBootstrapper(config =>
            {
                config
                    .Module<TestWithLoggerModule>()
                    .Dependency(_requestLogger.Object);
            }));

            Assert.Throws<Exception>(() => browser.Get("/test/error", config =>
            {
                config.HostName("localhost");
                config.HttpRequest();
            }));

            _requestLogger.Verify(x => x.LogError(
                    It.IsAny<RequestData>(),
                    It.IsAny<ResponseData>(),
                    It.IsAny<Exception>()),
                Times.Once);
        }

        [Test]
        public void Enable_Should_Log_GET_Request_For_Application()
        {
            var browser = new Browser(new ConfigurableBootstrapper(config =>
            {
                config.ApplicationStartup((container, pipelines) => RequestLogging.Enable(pipelines, _requestLogger.Object));
                config
                    .Module<TestModule>();
            }));

            browser.Get("/test", config =>
            {
                config.HostName("localhost");
                config.HttpRequest();
                config.Header("Accept", "text/html");
            });

            _requestLogger.Verify(x => x.Log(
                    It.Is<RequestData>(r =>
                        r.Url == new Uri("http://localhost/test") &&
                        r.HttpMethod == "GET" &&
                        r.Header.ContainsKey("Accept") &&
                        r.Header["Accept"].Any(y => y.Contains("text/html")) &&
                        r.Content.Length == 0),
                    It.Is<ResponseData>(r =>
                        r.StatusCode == 200 &&
                        r.ReasonPhrase == "OK" &&
                        r.Header.ContainsKey("X-Test") &&
                        r.Header["X-Test"][0] == "TEST" &&
                        r.Content.SequenceEqual(Encoding.UTF8.GetBytes("TEST")))),
                Times.Once);
        }

        [Test]
        public void Enable_Should_Log_GET_Request_For_Module()
        {
            var browser = new Browser(new ConfigurableBootstrapper(config =>
            {
                config
                    .Module<TestWithLoggerModule>()
                    .Dependency(_requestLogger.Object);
            }));

            browser.Get("/test", config =>
            {
                config.HostName("localhost");
                config.HttpRequest();
                config.Header("Accept", "text/html");
            });

            _requestLogger.Verify(x => x.Log(
                    It.Is<RequestData>(r =>
                        r.Url == new Uri("http://localhost/test") &&
                        r.HttpMethod == "GET" &&
                        r.Header.ContainsKey("Accept") &&
                        r.Header["Accept"].Any(y => y.Contains("text/html")) &&
                        r.Content.Length == 0),
                    It.Is<ResponseData>(r =>
                        r.StatusCode == 200 &&
                        r.ReasonPhrase == "OK" &&
                        r.Header.ContainsKey("X-Test") &&
                        r.Header["X-Test"][0] == "TEST" &&
                        r.Content.SequenceEqual(Encoding.UTF8.GetBytes("TEST")))),
                Times.Once);
        }

        [Test]
        public void Enable_When_Pipelines_IsNull_ShouldThrow_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => RequestLogging.Enable((IPipelines) null, _requestLogger.Object));
            Assert.Throws<ArgumentNullException>(() => RequestLogging.Enable((NancyModule) null, _requestLogger.Object));
        }

        [Test]
        public void Enable_When_RequestLogger_IsNull_ShouldThrow_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => RequestLogging.Enable(new Pipelines(), null));
            Assert.Throws<ArgumentNullException>(() => RequestLogging.Enable(new ConfigurableNancyModule(), null));
        }
    }
}