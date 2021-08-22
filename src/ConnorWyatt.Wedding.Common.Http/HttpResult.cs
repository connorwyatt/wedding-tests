using System.Net;

namespace ConnorWyatt.Wedding.Common.Http
{
    public class HttpResult<T> where T : class
    {
        public readonly HttpStatusCode StatusCode;

        public readonly T Value;

        private HttpResult(HttpStatusCode statusCode, T value = null)
        {
            StatusCode = statusCode;
            Value = value;
        }

        public static HttpResult<T> Success(HttpStatusCode statusCode, T value)
        {
            return new HttpResult<T>(statusCode, value);
        }

        public static HttpResult<T> Error(HttpStatusCode statusCode)
        {
            return new HttpResult<T>(statusCode);
        }
    }
}