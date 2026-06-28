using Microsoft.AspNetCore.Http;
using Peritus.Types.Identity.Users;

namespace Peritus.AspNetCore.Extensions;

public static class HttpContextExtensions
{
    extension(HttpContext context)
    {
        public string GetRequestHeader(string key)
        {
            return context.Request.Headers[key]!;
        }

        public Guid GetRequestHeaderAsGuid(string key)
        {
            return Guid.Parse(context.Request.Headers[key]!);
        }

        public T GetRequestHeaderAs<T>(string key)
        {
            return (T)Convert.ChangeType(context.Request.Headers[key], typeof(T));
        }

        public IpAddress? GetRemoteIpAddress()
        {
            var ipAddress = context.Connection.RemoteIpAddress;
            return ipAddress == null ? null : new IpAddress(ipAddress.ToString());
        }
    }
}
