namespace api.Services.Cmdb;

public static class CmdbEmployerResolver
{
    private const string GenericEntity = "COMMUNAUTAIRE";

    public static string? Resolve(
        string? entityPath,
        string? responsibleEmployer)
    {
        var entity = ExtractRootEntity(entityPath);
        if (!string.IsNullOrWhiteSpace(entity) &&
            !entity.Equals(GenericEntity, StringComparison.OrdinalIgnoreCase))
        {
            return entity;
        }

        var responsible = NullIfWhiteSpace(responsibleEmployer);
        return responsible ?? entity;
    }

    public static string? ExtractRootEntity(string? entityPath)
    {
        if (string.IsNullOrWhiteSpace(entityPath))
        {
            return null;
        }

        var trimmedPath = entityPath.Trim();
        var separatorIndex = trimmedPath.IndexOf('/');
        var entity = separatorIndex >= 0
            ? trimmedPath[..separatorIndex]
            : trimmedPath;

        return NullIfWhiteSpace(entity);
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
