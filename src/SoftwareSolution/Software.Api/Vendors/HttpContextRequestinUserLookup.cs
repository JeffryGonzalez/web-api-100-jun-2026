namespace Software.Api.Software;

public class HttpContextRequestinUserLookup(IHttpContextAccessor httpContextAccessor) : ILookupRequestingUsers
{
    public string GetRequestingUserId()
    {
      if(httpContextAccessor.HttpContext is null || httpContextAccessor.HttpContext.User is null)
        {
            throw new InvalidOperationException("Use this in the context of an Http request only");
        }
        return httpContextAccessor.HttpContext.User.Identity?.Name ?? string.Empty;
    }
}
