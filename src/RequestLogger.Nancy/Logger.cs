using System;
using System.IO;
using System.Linq;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Extensions;

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
            var requestData = new RequestData
            {
                Url = request.Url,
                HttpMethod = request.Method,
                Header = request.Headers.ToDictionary(x => x.Key, y => y.Value.ToArray())
            };

            using (var ms = new MemoryStream())
            {
                request.Body.CopyTo(ms);
                requestData.Content = ms.ToArray();
            }

            return requestData;
        }

        private static ResponseData ExtractResponseData(Response response)
        {
            var responseData = new ResponseData();

            if (response == null)
                return responseData;

            responseData.StatusCode = (int) response.StatusCode;
            responseData.ReasonPhrase = response.ReasonPhrase;
            responseData.Header = response.Headers.ToDictionary(x => x.Key, y => new[] {y.Value});

            using (var ms = new MemoryStream())
            {
                response.Contents = stream => stream.CopyTo(ms);
                responseData.Content = ms.ToArray();
            }

            return responseData;
        }
    }
}
