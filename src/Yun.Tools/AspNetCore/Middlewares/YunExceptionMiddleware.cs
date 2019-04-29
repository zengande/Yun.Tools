using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yun.Tools.Core.AspNetCore.Middlewares
{
    public class YunExceptionMiddleware
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly RequestDelegate _next;

        /// <summary>
        /// 配置
        /// </summary>
        private readonly YunExceptionMiddlewareOptions _options;

        /// <summary>
        /// 需要处理的状态码字典
        /// </summary>
        private IDictionary<int, string> _exceptionStatusCodeDic;


        public YunExceptionMiddleware(RequestDelegate next,
            YunExceptionMiddlewareOptions options)
        {
            _next = next;
            _options = options;

            _exceptionStatusCodeDic = new Dictionary<int, string>
            {
                { 401, "未授权的请求" },
                { 404, "找不到该页面" },
                { 403, "访问被拒绝" },
                { 500, "服务器发生意外的错误" }
                //其余状态自行扩展
            };
        }

        public async Task Invoke(HttpContext context, ILogger<YunExceptionMiddleware> logger)
        {
            Exception exception = null;
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception e)
            {
                context.Response.Clear();
                context.Response.StatusCode = 500;
                exception = e;

                // TODO : 记录log
                logger.LogError(e.Message);
            }
            finally
            {
                if (_exceptionStatusCodeDic.ContainsKey(context.Response.StatusCode) &&
                    !context.Items.ContainsKey("ExceptionHandled"))  //预处理标记
                {
                    var errorMsg = string.Empty;
                    if (context.Response.StatusCode == 500 && exception != null)
                    {
                        errorMsg = $"{_exceptionStatusCodeDic[context.Response.StatusCode]}\r\n{(exception.InnerException != null ? exception.InnerException.Message : exception.Message)}";
                    }
                    else
                    {
                        errorMsg = _exceptionStatusCodeDic[context.Response.StatusCode];
                    }
                    exception = new Exception(errorMsg);
                }

                if (exception != null)
                {
                    var handleType = _options.HandleType;
                    if (handleType == YunExceptionTypes.Both)   //根据Url关键字决定异常处理方式
                    {
                        var requestPath = context.Request.Path;
                        handleType = (_options.JsonHandleUrlKeys != null &&
                            _options.JsonHandleUrlKeys.Count(k => requestPath.StartsWithSegments(k, StringComparison.CurrentCultureIgnoreCase)) > 0) ?
                            YunExceptionTypes.Json :
                            YunExceptionTypes.Page;
                    }
                    if (handleType == YunExceptionTypes.Json)
                    {
                        await JsonHandleAsync(context, exception);
                    }
                    else
                    {
                        await PageHandleAsync(context, exception, _options.ErrorHandingPath);
                    }
                }
            }
        }

        private async Task PageHandleAsync(HttpContext context, Exception exception, PathString errorHandingPath)
        {
            context.Items.Add("Exception", exception);
            var originPath = context.Request.Path;
            context.Request.Path = errorHandingPath;   //设置请求页面为错误跳转页面
            try
            {
                await _next(context);
            }
            catch { }
            finally
            {
                context.Request.Path = originPath;   //恢复原始请求页面
            }
        }

        private async Task JsonHandleAsync(HttpContext context, Exception exception)
        {
            var apiResponse = GetApiResponse(exception);
            var serialzeStr = JsonConvert.SerializeObject(apiResponse, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(serialzeStr, Encoding.UTF8);
        }

        private ApiResponse GetApiResponse(Exception ex) => new ApiResponse() { Success = false, Message = ex.Message };
    }+

    public static class YunExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseMoMoExceptionHandler(this IApplicationBuilder app, YunExceptionMiddlewareOptions options = null) =>
            app.UseMiddleware<YunExceptionMiddleware>(options ?? YunExceptionMiddlewareOptions.Default);
    }

    public class YunExceptionMiddlewareOptions
    {
        public static YunExceptionMiddlewareOptions Default => new YunExceptionMiddlewareOptions(
            YunExceptionTypes.Both,
            jsonHandleUrlKeys: new PathString[] { "/api" },
            errorHandingPath: "/home/error");

        public YunExceptionMiddlewareOptions(YunExceptionTypes handleType = YunExceptionTypes.Json,
            IList<PathString> jsonHandleUrlKeys = null,
            string errorHandingPath = "")
        {
            HandleType = handleType;
            JsonHandleUrlKeys = jsonHandleUrlKeys;
            ErrorHandingPath = errorHandingPath;
        }

        /// <summary>
        /// 异常处理方式
        /// </summary>
        public YunExceptionTypes HandleType { get; set; }

        /// <summary>
        /// Json处理方式的Url关键字
        /// <para>仅 HandleType = Both 时生效</para>
        /// </summary>
        public IList<PathString> JsonHandleUrlKeys { get; set; }

        /// <summary>
        /// 错误跳转页面
        /// <para>仅 HandleType = Page 时生效</para>
        /// </summary>
        public PathString ErrorHandingPath { get; set; }
    }

    public enum YunExceptionTypes
    {
        Json = 0,
        Page = 2,
        Both = 4
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
}
