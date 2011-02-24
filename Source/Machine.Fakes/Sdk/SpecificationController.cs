using System;
using System.Collections.Generic;
using Machine.Fakes.Internal;
using Machine.Specifications;

namespace Machine.Fakes.Sdk
{
    /// <summary>
    /// Shortcut for <see cref="SpecificationController{TSubject}"/> which
    /// supplies the type of the fake engine to be used via a generic type parameter.
    /// </summary>
    /// <typeparam name="TSubject">
    /// The subject for the specification. This is the type that is created by the
    /// specification for you.
    /// </typeparam>
    /// <typeparam name="TFakeEngine">
    /// Specifies the type of the fake engine which will be used.
    /// </typeparam>
    public class SpecificationController<TSubject, TFakeEngine> : SpecificationController<TSubject> 
        where TSubject : class 
        where TFakeEngine : IFakeEngine, new()
    {
        /// <summary>
        /// Creates a new instance of the <see cref="SpecificationController{TSubject, TFakeEngine}"/> class.
        /// </summary>
        public SpecificationController() : base(new TFakeEngine())
        {
        }
    }

    /// <summary>
    /// Controller that implements all the core capabilities of Machine.Fakes.
    /// This includes filling a subject with fakes and providing all the handy helper methods
    /// for interacting with fakes in a specification.
    /// </summary>
    /// <typeparam name="TSubject">
    /// The subject for the specification. This is the type that is created by the
    /// specification for you.
    /// </typeparam>
    public class SpecificationController<TSubject> : IFakeAccessor, IDisposable where TSubject : class
    {
        private readonly List<IBehaviorConfig> _behaviorConfigs = new List<IBehaviorConfig>();
        private TSubject _specificationSubject;
        private readonly AutoFakeContainer<TSubject> _container;
        
        /// <summary>
        /// Creates a new instance of the <see cref="SpecificationController{TSubject}"/> class.
        /// </summary>
        /// <param name="fakeEngine">
        /// Specifies the <see cref="IFakeEngine"/> that is used for creating specifications.
        /// </param>
        public SpecificationController(IFakeEngine fakeEngine)
        {
            Guard.AgainstArgumentNull(fakeEngine, "fakeEngine");

            _container = new AutoFakeContainer<TSubject>(fakeEngine);
            FakeEngineGateway.EngineIs(_container);
        }

        /// <summary>
        /// Gives access to the subject under specification. On first access
        /// the spec tries to create an instance of the subject type by itself.
        /// Override this behavior by manually setting a subject instance.
        /// </summary>
        public TSubject Subject
        {
            get { return _specificationSubject ?? (_specificationSubject = _container.CreateSubject()); }
            set { _specificationSubject = value; }
        }

        /// <summary>
        ///   Uses the instance supplied by <paramref name = "instance" /> during the
        ///   creation of the sut. The specified instance will be injected into the constructor.
        /// </summary>
        /// <typeparam name = "TInterfaceType">Specifies the interface type.</typeparam>
        /// <param name = "instance">Specifies the instance to be used for the specification.</param>
        public void Use<TInterfaceType>(TInterfaceType instance) where TInterfaceType : class
        {
            _container.Inject(typeof (TInterfaceType), instance);
        }

        /// <summary>
        ///   Configures the specification to execute the <see cref = "IBehaviorConfig" /> specified
        ///   by <typeparamref name = "TBehaviorConfig" /> before the action on the sut is executed (<see cref = "Because" />).
        /// </summary>
        /// <typeparam name = "TBehaviorConfig">
        ///   Specifies the type of the config to be executed.
        /// </typeparam>
        public TBehaviorConfig With<TBehaviorConfig>() where TBehaviorConfig : IBehaviorConfig, new()
        {
            var behaviorConfig = new TBehaviorConfig();
            With(behaviorConfig);
            return behaviorConfig;
        }

        /// <summary>
        ///   Configures the specification to execute the <see cref = "IBehaviorConfig" /> specified
        ///   by <paramref name = "behaviorConfig" /> before the action on the sut is executed (<see cref = "Because" />).
        /// </summary>
        /// <param name = "behaviorConfig">
        ///   Specifies the behavior config to be executed.
        /// </param>
        public void With(IBehaviorConfig behaviorConfig)
        {
            Guard.AgainstArgumentNull(behaviorConfig, "behaviorConfig");

            _behaviorConfigs.Add(behaviorConfig);

            behaviorConfig.EstablishContext(this);
        }

        /// <summary>
        ///   Creates a fake of the type specified by <typeparamref name = "TInterfaceType" />.
        /// </summary>
        /// <typeparam name = "TInterfaceType">The type to create a fake for. (Should be an interface or an abstract class)</typeparam>
        /// <returns>
        ///   An newly created fake implementing <typeparamref name = "TInterfaceType" />.
        /// </returns>
        public TInterfaceType An<TInterfaceType>() where TInterfaceType : class
        {
            return (TInterfaceType)_container.CreateFake(typeof(TInterfaceType));
        }

        /// <summary>
        ///   Creates a fake of the type specified by <typeparamref name = "TInterfaceType" />.
        ///   This method reuses existing instances. If an instance of <typeparamref name = "TInterfaceType" />
        ///   was already requested it's returned here. (You can say this is kind of a singleton behavior)
        ///   Besides that, you can obtain a reference to automatically injected fakes with this
        ///   method.
        /// </summary>
        /// <typeparam name = "TInterfaceType">The type to create a fake for. (Should be an interface or an abstract class)</typeparam>
        /// <returns>
        ///   An instance implementing <typeparamref name="TInterfaceType" />.
        /// </returns>
        public TInterfaceType The<TInterfaceType>() where TInterfaceType : class
        {
            return _container.Get<TInterfaceType>();
        }

        /// <summary>
        ///   Creates a list containing 3 fake instances of the type specified
        ///   via <typeparamref name = "TInterfaceType" />.
        /// </summary>
        /// <typeparam name = "TInterfaceType">Specifies the item type of the list. This should be an interface or an abstract class.</typeparam>
        /// <returns>An <see cref = "IList{T}" />.</returns>
        public IList<TInterfaceType> Some<TInterfaceType>() where TInterfaceType : class
        {
            return _container.CreateFakeCollectionOf<TInterfaceType>();
        }

        /// <summary>
        /// Performs cleanup. Exuecutes the Cleanup functionality of all configured behavior configs.
        /// </summary>
        public void Dispose()
        {
            _behaviorConfigs.ForEach(x => x.CleanUp(_specificationSubject));
            _behaviorConfigs.Clear();
        }
    }
}