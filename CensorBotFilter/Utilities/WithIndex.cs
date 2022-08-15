namespace CensorBotFilter.Utilities
{
    public static class WithIndexEnumExtension
    {
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)
            => self.Select((item, index) => (item, index));

        public static IEnumerable<(T item, T? nextItem)> WithNext<T>(this IList<T> self) where T : class
            => self.Select((item, index) => (item, index + 1 < self.Count() ? self[index + 1] : null));

        public static IEnumerable<(T item, T? nextItem, int index)> WithIndexAndNext<T>(this IList<T> self) where T : class
            => self.Select((item, index) => (item, index + 1 < self.Count() ? self[index + 1] : null, index));
    }
}
