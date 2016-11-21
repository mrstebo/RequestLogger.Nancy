using System;
using Nancy;
using Nancy.Bootstrapper;

namespace RequestLogger.Nancy
{
    public static class Logger
    {
        public static void Enable(IPipelines pipelines, IRequestLogger requestLogger)
        {
            if (pipelines == null)
            {
                throw new ArgumentNullException("pipelines");
            }

            if (requestLogger == null)
            {
                throw new ArgumentNullException("requestLogger");
            }

            pipelines.AfterRequest.AddItemToEndOfPipeline(GetLogRequestHook(requestLogger));
            pipelines.OnError.AddItemToEndOfPipeline(GetLogErrorHook(requestLogger));
        }

        public static void Enable(INancyModule module, IRequestLogger requestLogger)
        {
            if (module == null)
            {
                throw new ArgumentNullException("module");
            }

            if (requestLogger == null)
            {
                throw new ArgumentNullException("requestLogger");
            }

            module.After.AddItemToEndOfPipeline(GetLogRequestHook(requestLogger));
            module.OnError.AddItemToEndOfPipeline(GetLogErrorHook(requestLogger));
        }

        private static Action<NancyContext> GetLogRequestHook(IRequestLogger requestLogger)
        {
            return ctx =>
            {
                var requestData = ExtractRequestData(ctx.Request);
                var responseData = ExtractResponseData(ctx.Response);

                requestLogger.Log(requestData, responseData);
            };
        }

        private static Func<NancyContext, Exception, dynamic> GetLogErrorHook(IRequestLogger requestLogger)
        {
            return (ctx, ex) =>
            {
                var requestData = ExtractRequestData(ctx.Request);
                var responseData = ExtractResponseData(ctx.Response);

                requestLogger.LogError(requestData, responseData, ex);

                return ctx.Response;
            };
        }

        private static RequestData ExtractRequestData(Request request)
        {
            return new RequestData();
        }

        private static ResponseData ExtractResponseData(Response response)
        {
            return new ResponseData();
        }
    }
}
