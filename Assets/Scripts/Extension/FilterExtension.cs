public static class FilterExtension {
    public static bool ShouldFilter(this Danmu danmu) {
        if (danmu.Content.Trim().Length == 0)
            return true;

        return false;
    }
}