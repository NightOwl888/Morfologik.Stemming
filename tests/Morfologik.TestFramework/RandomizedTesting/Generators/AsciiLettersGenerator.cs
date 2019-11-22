namespace Morfologik.TestFramework.RandomizedTesting.Generators
{
    /// <summary>
    /// A generator emitting simple ASCII characters from the set
    /// (newlines not counted):
    /// <code>
    /// abcdefghijklmnopqrstuvwxyz
    /// ABCDEFGHIJKLMNOPQRSTUVWXYZ
    /// </code>
    /// </summary>
    public class AsciiLettersGenerator : CodepointSetGenerator
    {
        private readonly static char[] Chars =
          ("abcdefghijklmnopqrstuvwxyz" +
           "ABCDEFGHIJKLMNOPQRSTUVWXYZ").ToCharArray();

        public AsciiLettersGenerator()
            : base(Chars)
        {
        }
    }
}
