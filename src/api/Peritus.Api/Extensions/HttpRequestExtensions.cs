using Peritus.Types.Identity.Users;

namespace Peritus.Api.Extensions;

public static class HttpRequestExtensions
{
    extension(HttpRequest request)
    {
        public UserAgent GetUserAgent()
        {
            return new UserAgent(request.Headers.UserAgent.ToString());
        }
    }
}
