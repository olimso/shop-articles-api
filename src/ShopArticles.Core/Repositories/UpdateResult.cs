namespace ShopArticles.Core.Repositories;

public enum UpdateStatus
{
    Updated,
    NotFound,
    // The article exists, but its version is no longer the one the client sent.
    Conflict,
}

// Value is the updated item when Status is Updated, otherwise null.
public record UpdateResult<T>(UpdateStatus Status, T? Value = null) where T : class;
