using Kraig.Attributes;

namespace Kraig.Generators.Samples.SingletonObjects
{
    [Singleton]
    public partial class SimpleSingleton
    {
        public Guid Id { get; init; } = Guid.NewGuid();
    }

    [Singleton]
    public partial class SingletonWithConstructor
    {
        public Guid Id { get; init; }
        private SingletonWithConstructor()
        {
            Id = Guid.NewGuid();
        }
    }
}
