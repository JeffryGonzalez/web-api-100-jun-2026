namespace Software.Api.Software;

public class HttpContextRequestinUserLookup(IHttpContextAccessor httpContextAccessor) : ILookupRequestingUsers
{
    public string GetRequestingUserId()
    {
      if(httpContextAccessor.HttpContext is null || httpContextAccessor.HttpContext.User is null)
        {
            throw new InvalidOperationException("Use this in the context of an Http request only");
        }
        // Identity.Name x003809

        // insert into userlog (sub, name) values (x003809, Guid.NewGuid())
        return httpContextAccessor.HttpContext.User.Identity?.Name ?? string.Empty;
    }
}


