using System.Net;
using System.Text.Json;

namespace Turbo.Maui.Services.Models
{
    public class HttpResponse
    {
        public HttpResponse() { }
        public HttpResponse(HttpResponseMessage r)
        {
            WasSuccessful = r.IsSuccessStatusCode;
            ReasonPhrase = r.ReasonPhrase ?? "";
            StatusCode = r.StatusCode;
        }

        public bool WasSuccessful { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string ReasonPhrase { get; set; } = "";
    }

    public class HttpResponseData<T> : HttpResponse
    {
        public T? Data { get; set; }

        public static async Task<HttpResponseData<T>> Create(HttpResponseMessage r)
        {
            var response = new HttpResponseData<T>
            {
                WasSuccessful = r.IsSuccessStatusCode
            };

            if (r.IsSuccessStatusCode)
            {
                var content = await r.Content.ReadAsStringAsync();
                try
                {
                    if (IsSimple(typeof(T)))
                        response.Data = (T)Convert.ChangeType(content, typeof(T));
                    else
                        response.Data = string.IsNullOrWhiteSpace(content) ? default : JsonSerializer.Deserialize<T>(content);
                }
                catch (Exception)
                {
                    response.Data = default;
                }
            }
            else
            {
                response.StatusCode = r.StatusCode;
            }

            return response;
        }

        private static bool IsSimple(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // nullable type, check if the nested type is simple.
                return IsSimple(type.GetGenericArguments()[0]);
            }
            return type.IsPrimitive
                   || type.IsEnum
                   || type == typeof(string)
                   || type == typeof(decimal);
        }
    }
}

