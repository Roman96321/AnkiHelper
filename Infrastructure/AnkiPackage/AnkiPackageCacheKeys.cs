namespace Infrastructure.AnkiPackage;

public static class AnkiPackageCacheKeys
{
    public static string GetAnkiPackageCacheKey(Guid userId) => $"anki-package-{userId}";
}
