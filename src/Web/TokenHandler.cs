using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

public class TokenHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (httpContextAccessor.HttpContext is not null)
        {
            var result = await httpContextAccessor.HttpContext.GetTokenAsync("access_token");

            if (result is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result);

            }
            return await base.SendAsync(request, ct);

        }
        return await base.SendAsync(request, ct);
    }
}
