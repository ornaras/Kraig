using Kraig.Attributes;

namespace Kraig.Roslyn.Samples.NotifyChangedObjects
{
    internal partial class Class1
    {
        [NotifyChanged] private int _digit = 10;

        public int Sqrt() => (int)Math.Sqrt(Digit);
    }
}
