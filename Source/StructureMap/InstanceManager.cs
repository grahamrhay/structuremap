using System;
using System.Collections;
using System.Collections.Specialized;
using NMock;
using StructureMap.Graph;
using StructureMap.Interceptors;

namespace StructureMap
{
    /// <summary>
    /// A collection of IInstanceFactory's.
    /// </summary>
    public class InstanceManager : IEnumerable
    {
        private HybridDictionary _factories;
        private HybridDictionary _filledTypeFactories;
        private bool _failOnException = true;
        private GenericsPluginGraph _genericsGraph;

        /// <summary>
        /// Default constructor
        /// </summary>
        public InstanceManager()
        {
            _factories = new HybridDictionary();
            _filledTypeFactories = new HybridDictionary();
            _genericsGraph = new GenericsPluginGraph();
        }

        /// <summary>
        /// Creates an InstanceManager from the contents of the pluginGraph.  Fails
        /// on any exceptions found
        /// </summary>
        /// <param name="pluginGraph"></param>
        public InstanceManager(PluginGraph pluginGraph) : this(pluginGraph, true)
        {
        }

        /// <summary>
        /// Constructor to create an InstanceManager that traps exceptions.  Used to diagnose
        /// configuration and runtime errors
        /// </summary>
        /// <param name="pluginGraph">PluginGraph containing the instance and type definitions 
        /// for the InstanceManager</param>
        /// <param name="failOnException">Flags the InstanceManager to fail or trap exceptions</param>
        public InstanceManager(PluginGraph pluginGraph, bool failOnException) : this()
        {
            _failOnException = failOnException;

            if (!pluginGraph.IsSealed)
            {
                pluginGraph.Seal();
            }

            foreach (PluginFamily family in pluginGraph.PluginFamilies)
            {
                if (family.IsGeneric)
                {
                    _genericsGraph.AddFamily(family);
                }
                else
                {
                    registerPluginFamily(family);
                }
            }
        }

        private IInstanceFactory registerPluginFamily(PluginFamily family)
        {
            InstanceFactory factory = new InstanceFactory(family, _failOnException);
            IInstanceFactory wrappedFactory = family.InterceptionChain.WrapInstanceFactory(factory);

            RegisterType(wrappedFactory);

            return wrappedFactory;
        }

        /// <summary>
        /// Sets the default instances for all PluginType's managed by the InstanceManager
        /// </summary>
        /// <param name="defaultProfile"></param>
        public void SetDefaults(Profile defaultProfile)
        {
            foreach (InstanceDefault defaultInstance in defaultProfile.Defaults)
            {
                try
                {
                    string pluginTypeName = defaultInstance.PluginTypeName;
                    IInstanceFactory instanceFactory = this[pluginTypeName];
                    instanceFactory.SetDefault(defaultInstance.DefaultKey);
                }
                catch (Exception)
                {
                    if (_failOnException)
                    {
                        throw;
                    }
                }
            }
        }


        /// <summary>
        /// Adds an instance of an IInstanceFactory
        /// </summary>
        /// <param name="instanceFactory"></param>
        public void RegisterType(IInstanceFactory instanceFactory)
        {
            _factories.Add(instanceFactory.PluginType.FullName, instanceFactory);
            instanceFactory.SetInstanceManager(this);
        }

        /// <summary>
        /// Creates the named instance of the PluginType
        /// </summary>
        /// <param name="pluginType"></param>
        /// <param name="instanceKey"></param>
        /// <returns></returns>
        public object CreateInstance(Type pluginType, string instanceKey)
        {
            IInstanceFactory instanceFactory = this[pluginType];
            return instanceFactory.GetInstance(instanceKey);
        }

        /// <summary>
        /// Creates the named instance of the PluginTypeName
        /// </summary>
        /// <param name="pluginTypeName">Fully qualified name of the CLR Type to create</param>
        /// <param name="instanceKey"></param>
        /// <returns></returns>
        public object CreateInstance(string pluginTypeName, string instanceKey)
        {
            IInstanceFactory instanceFactory = this[pluginTypeName];
            return instanceFactory.GetInstance(instanceKey);
        }

        /// <summary>
        /// Creates a new object instance of the requested type
        /// </summary>
        /// <param name="pluginType"></param>
        /// <returns></returns>
        public object CreateInstance(Type pluginType)
        {
            IInstanceFactory instanceFactory = this[pluginType];
            return instanceFactory.GetInstance();
        }

        /// <summary>
        /// Creates a new object instance of the requested type
        /// </summary>
        /// <param name="pluginTypeName">Fully qualified name of the CLR Type to create</param>
        /// <returns></returns>
        public object CreateInstance(string pluginTypeName)
        {
            IInstanceFactory instanceFactory = this[pluginTypeName];
            return instanceFactory.GetInstance();
        }

        /// <summary>
        /// Creates a new instance of the requested type using the InstanceMemento.  Mostly used from other
        /// classes to link children members
        /// </summary>
        /// <param name="pluginTypeName"></param>
        /// <param name="instanceMemento"></param>
        /// <returns></returns>
        public object CreateInstance(string pluginTypeName, InstanceMemento instanceMemento)
        {
            IInstanceFactory instanceFactory = this[pluginTypeName];
            return instanceFactory.GetInstance(instanceMemento);
        }

        /// <summary>
        /// Creates a new instance of the requested type using the InstanceMemento.  Mostly used from other
        /// classes to link children members
        /// </summary>
        /// <param name="pluginType"></param>
        /// <param name="instanceMemento"></param>
        /// <returns></returns>
        public object CreateInstance(Type pluginType, InstanceMemento instanceMemento)
        {
            IInstanceFactory instanceFactory = this[pluginType];
            return instanceFactory.GetInstance(instanceMemento);
        }

        /// <summary>
        /// Creates an array of object instances of the requested type
        /// </summary>
        /// <param name="pluginType"></param>
        /// <param name="instanceMementoes"></param>
        /// <returns></returns>
        public Array CreateInstanceArray(string pluginType, InstanceMemento[] instanceMementoes)
        {
            if (instanceMementoes == null)
            {
                throw new StructureMapException(205, pluginType, "UNKNOWN");
            }

            IInstanceFactory instanceFactory = this[pluginType];
            return instanceFactory.GetArray(instanceMementoes);
        }

        /// <summary>
        /// Sets the default instance for the PluginType
        /// </summary>
        /// <param name="pluginType"></param>
        /// <param name="instanceMemento"></param>
        public void SetDefault(Type pluginType, InstanceMemento instanceMemento)
        {
            IInstanceFactory instanceFactory = this[pluginType];
            instanceFactory.SetDefault(instanceMemento);
        }

        /// <summary>
        /// Sets the default instance for the PluginType
        /// </summary>
        /// <param name="pluginType"></param>
        /// <param name="instanceKey"></param>
        public void SetDefault(Type pluginType, string instanceKey)
        {
            IInstanceFactory instanceFactory = this[pluginType];
            instanceFactory.SetDefault(instanceKey);
        }


        /// <summary>
        /// Sets the default instance for the PluginType
        /// </summary>
        /// <param name="pluginTypeName"></param>
        /// <param name="instanceKey"></param>
        public void SetDefault(string pluginTypeName, string instanceKey)
        {
            IInstanceFactory instanceFactory = this[pluginTypeName];
            instanceFactory.SetDefault(instanceKey);
        }

        public IInstanceFactory this[Type pluginType]
        {
            get
            {
                if (pluginType.IsGenericType && !_factories.Contains(pluginType.FullName))
                {
                    PluginFamily family = _genericsGraph.CreateTemplatedFamily(pluginType);
                    return registerPluginFamily(family);
                }
                
                return this[pluginType.FullName];
            }
            set { this[pluginType.FullName] = value; }
        }

        public IInstanceFactory this[string pluginTypeName]
        {
            get
            {
                if (_factories.Contains(pluginTypeName))
                {
                    return (IInstanceFactory) _factories[pluginTypeName];
                }
                else
                {
                    throw new StructureMapException(208, pluginTypeName);
                }
            }
            set { _factories[pluginTypeName] = value; }
        }


        /// <summary>
        /// Attempts to create a new instance of the requested type.  Automatically inserts the default
        /// configured instance for each dependency in the StructureMap constructor function.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public object FillDependencies(Type type)
        {
            if (type.IsInterface || type.IsAbstract)
            {
                throw new StructureMapException(230);
            }

            InstanceFactory factory = getFilledTypeFactory(type);
            return factory.GetInstance();
        }

        private InstanceFactory getFilledTypeFactory(Type type)
        {
            if (!_filledTypeFactories.Contains(type))
            {
                lock (this)
                {
                    if (!_filledTypeFactories.Contains(type))
                    {
                        PluginFamily family = PluginFamily.CreateAutoFilledPluginFamily(type);
                        InstanceFactory factory = new InstanceFactory(family, true);
                        factory.SetInstanceManager(this);
                        _filledTypeFactories.Add(type, factory);
                    }
                }
            }

            return (InstanceFactory) _filledTypeFactories[type];
        }

        #region mocking

        /// <summary>
        /// When called, returns an NMock.IMock instance for the TargetType.  Until UnMocked, calling 
        /// GetInstance(Type TargetType) will return the MockInstance member of the IMock
        /// </summary>
        /// <param name="TargetType"></param>
        /// <returns></returns>
        public IMock Mock(Type TargetType)
        {
            if (IsMocked(TargetType))
            {
                string msg = string.Format("The Type {0} is already mocked", TargetType.FullName);
                throw new InvalidOperationException(msg);
            }

            IInstanceFactory factory = this[TargetType];
            MockInstanceFactory mockFactory = new MockInstanceFactory(factory);
            IMock returnValue = mockFactory.GetMock();

            lock (this)
            {
                this[TargetType] = mockFactory;
            }

            return returnValue;
        }

        /// <summary>
        /// Is the specified TargetType currently setup as an IMock
        /// </summary>
        /// <param name="TargetType"></param>
        /// <returns></returns>
        public bool IsMocked(Type TargetType)
        {
            bool returnValue = false;

            returnValue = isInstanceFamilyMocked(this[TargetType]);

            return returnValue;
        }

        private bool isInstanceFamilyMocked(IInstanceFactory instanceFactory)
        {
            bool returnValue = false;

            InstanceFactoryInterceptor interceptor = instanceFactory as InstanceFactoryInterceptor;
            if (interceptor != null)
            {
                returnValue = interceptor.IsMockedOrStubbed;
            }

            return returnValue;
        }

        /// <summary>
        /// Release the NMock behavior of TargetType
        /// </summary>
        /// <param name="TargetType"></param>
        public void UnMock(Type TargetType)
        {
            if (IsMocked(TargetType))
            {
                InstanceFactoryInterceptor instanceFactory = (InstanceFactoryInterceptor) this[TargetType];
                IInstanceFactory innerFactory = instanceFactory.InnerInstanceFactory;
                lock (this)
                {
                    this[TargetType] = innerFactory;
                }
            }
        }

        /// <summary>
        /// Calls UnMock() on all IInstanceFactory's 
        /// </summary>
        public void UnMockAll()
        {
            ArrayList typeList = new ArrayList();
            lock (this)
            {
                foreach (IInstanceFactory factory in _factories.Values)
                {
                    if (isInstanceFamilyMocked(factory))
                    {
                        typeList.Add(factory.PluginType);
                    }
                }


                foreach (Type type in typeList)
                {
                    UnMock(type);
                }
            }
        }

        /// <summary>
        /// Sets up the InstanceManager to return the object in the "stub" argument anytime
        /// any instance of the PluginType is requested
        /// </summary>
        /// <param name="pluginType"></param>
        /// <param name="stub"></param>
        public void InjectStub(Type pluginType, object stub)
        {
            if (!Plugin.CanBeCast(pluginType, stub.GetType()))
            {
                throw new StructureMapException(220, pluginType.FullName, stub.GetType().FullName);
            }

            IInstanceFactory innerFactory = this[pluginType];
            StubbedInstanceFactory stubbedFactory = new StubbedInstanceFactory(innerFactory, stub);
            lock (this)
            {
                this[pluginType] = stubbedFactory;
            }
        }

        #endregion

        public IEnumerator GetEnumerator()
        {
            return _factories.Values.GetEnumerator();
        }

        public IList GetAllInstances(Type type)
        {
            return this[type].GetAllInstances();
        }
    }
}