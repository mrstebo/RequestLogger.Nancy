using System;
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

        private class TestWithLoggerModule : NancyModule
        {
            public TestWithLoggerModule(IRequestLogger requestLogger)
                : base("/test")
            {
                RequestLogging.Enable(this, requestLogger);

                Get["/"] = _ => Response.AsText("TEST", "text/html").WithHeader("X-Test", "Hello");
                Get["/error"] = _ => { throw new Exception("ERROR"); };
            }
        }

        private class TestWithoutLoggerModule : NancyModule
        {
            public TestWithoutLoggerModule()
                : base("/test")
            {
                Get["/"] = _ => Response.AsText("TEST", "text/html").WithHeader("X-Test", "Hello");
                Get["/error"] = _ => { throw new Exception("ERROR"); };
            }
        }

        private class TestModel
        {
            public string Message { get; set; }
        }

        [Test]
        public void Enable_Should_LogError_For_Application()
        {
            var browser = new Browser(new ConfigurableBootstrapper(config =>
            {
                config.ApplicationStartup((container, pipelines) => RequestLogging.Enable(pipelines, _requestLogger.Object));
                config
                    .Module<TestWithoutLoggerModule>();
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
        public void Enable_Should_LogRequest_For_Application()
        {
            var browser = new Browser(new ConfigurableBootstrapper(config =>
            {
                config.ApplicationStartup((container, pipelines) => RequestLogging.Enable(pipelines, _requestLogger.Object));
                config
                    .Module<TestWithoutLoggerModule>();
            }));

            browser.Get("/test", config =>
            {
                config.HostName("localhost");
                config.HttpRequest();
            });

            _requestLogger.Verify(x => x.Log(
                    It.IsAny<RequestData>(),
                    It.IsAny<ResponseData>()),
                Times.Once);
        }

        [Test]
        public void Enable_Should_LogRequest_For_Module()
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
            });

            _requestLogger.Verify(x => x.Log(
                    It.IsAny<RequestData>(),
                    It.IsAny<ResponseData>()),
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