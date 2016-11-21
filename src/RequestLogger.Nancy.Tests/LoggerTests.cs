using Moq;
using Nancy;
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
        public void Enable_Should_LogRequest_For_Module()
        {
            var browser = new Browser(new ConfigurableBootstrapper(config =>
            {
                config
                    .Module<TestModule>()
                    .Dependency(_requestLogger.Object);
            }));

            browser.Get("/test", config => config.HttpRequest());

            _requestLogger.Verify(x => x.Log(It.IsAny<RequestData>(), It.IsAny<ResponseData>()), Times.Once);
        }

        [Test]
        public void Enable_Should_LogRequest_For_Application()
        {
            var browser = new Browser(new ConfigurableBootstrapper(config =>
            {
                config.ApplicationStartup((container, pipelines) => Logger.Enable(pipelines, _requestLogger.Object));
                config
                    .Module<TestModule>();
            }));

            browser.Get("/test", config => config.HttpRequest());

            _requestLogger.Verify(x => x.Log(It.IsAny<RequestData>(), It.IsAny<ResponseData>()), Times.Once);
        }

        private class TestModule : NancyModule
        {
            public TestModule()
                : base("/test")
            {
                Get["/"] = _ => "GET";
                Post["/"] = _ => "POST";
                Put["/"] = _ => "PUT";
                Delete["/"] = _ => "DELETE";
            }

            public TestModule(IRequestLogger requestLogger)
                : base("/test")
            {
                Logger.Enable(this, requestLogger);

                Get["/"] = _ => "GET";
                Post["/"] = _ => "POST";
                Put["/"] = _ => "PUT";
                Delete["/"] = _ => "DELETE";
            }
        }
    }
}
