using System;
using System.Diagnostics.CodeAnalysis;

namespace Exceptionless.Dependency {
    public interface IDependencyResolver : IDisposable {
        object Resolve([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType);
        void Register(Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] Type concreteType);
        void Register(Type serviceType, Func<object> activator);
    }
}
