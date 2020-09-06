namespace Icing
{
    public class CountedBool
    {
        private int count = 0;
        public bool Value => count > 0;

        public void Set(bool value)
        {
            count += value ? 1 : -1;
        }
        public void Reset()
        {
            count = 0;
        }
    }
}
