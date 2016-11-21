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
    public class LoggerTests
    {
        [SetUp]
        public void SetUp()
        {
            _requestLogger = new Mock<IRequestLogger>();
        }

        private Mock<IRequestLogger> _requestLogger;

        [Test]
        public void Enable_When_Pipelines_IsNull_ShouldThrow_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Logger.Enable((IPipelines) null, _requestLogger.Object));
            Assert.Throws<ArgumentNullException>(() => Logger.Enable((NancyModule) null, _requestLogger.Object));
        }

        [Test]
        public void Enable_When_RequestLogger_IsNull_ShouldThrow_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Logger.Enable(new Pipelines(), null));
            Assert.Throws<ArgumentNullException>(() => Logger.Enable(new ConfigurableNancyModule(), null));
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

            browser.Get("/test", config => config.HttpRequest());

            _requestLogger.Verify(x => x.Log(
                    It.IsAny<RequestData>(),
                    It.IsAny<ResponseData>()),
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

            Assert.Throws<Exception>(() => browser.Get("/test/error", config => config.HttpRequest()));

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
                config.ApplicationStartup((container, pipelines) => Logger.Enable(pipelines, _requestLogger.Object));
                config
                    .Module<TestWithoutLoggerModule>();
            }));

            browser.Get("/test", config => config.HttpRequest());

            _requestLogger.Verify(x => x.Log(
                    It.IsAny<RequestData>(),
                    It.IsAny<ResponseData>()),
                Times.Once);
        }

        [Test]
        public void Enable_Should_LogError_For_Application()
        {
            var browser = new Browser(new ConfigurableBootstrapper(config =>
            {
                config.ApplicationStartup((container, pipelines) => Logger.Enable(pipelines, _requestLogger.Object));
                config
                    .Module<TestWithoutLoggerModule>();
            }));

            Assert.Throws<Exception>(() => browser.Get("/test/error", config => config.HttpRequest()));

            _requestLogger.Verify(x => x.LogError(
                    It.IsAny<RequestData>(),
                    It.IsAny<ResponseData>(),
                    It.IsAny<Exception>()),
                Times.Once);
        }

        private class TestWithLoggerModule : NancyModule
        {
            public TestWithLoggerModule(IRequestLogger requestLogger)
                : base("/test")
            {
                Logger.Enable(this, requestLogger);

                Get["/"] = _ => "GET";
                Get["/error"] = _ => { throw new Exception("ERROR"); };
                Post["/"] = _ => "POST";
                Put["/"] = _ => "PUT";
                Delete["/"] = _ => "DELETE";
            }
        }

        private class TestWithoutLoggerModule : NancyModule
        {
            public TestWithoutLoggerModule()
                : base("/test")
            {
                Get["/"] = _ => "GET";
                Get["/error"] = _ => { throw new Exception("ERROR"); };
                Post["/"] = _ => "POST";
                Put["/"] = _ => "PUT";
                Delete["/"] = _ => "DELETE";
            }

        }
    }
}
