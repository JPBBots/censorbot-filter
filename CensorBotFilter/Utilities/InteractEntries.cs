namespace CensorBotFilter.Utilities
{
    public class InteractEntry<T>
    {
        public int Index { get; private set; }
        private List<T> List { get; set; }

        public InteractEntry (List<T> list, int index)
        {
            List = list;
            Index = index;
        }

        public InteractEntry<T> Next ()
        {
            return new InteractEntry<T>(List, Index + 1);
        }

        public InteractEntry<T> Previous()
        {
            return new InteractEntry<T>(List, Index - 1);
        }

        public void SetValue(T value)
        {
            List[Index] = value;
        }

        public T Value => List[Index];
    }

    public static class ListExtension
    {
        public static List<InteractEntry<T>> Interactions <T> (this List<T> list)
        {
            return list.Select((value, index) => new InteractEntry<T>(list, index)).ToList();
        }
    }
}
