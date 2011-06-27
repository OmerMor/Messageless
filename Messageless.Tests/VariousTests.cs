using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using FluentAssertions;
using NUnit.Framework;

namespace Messageless.Tests
{
    public class VariousTests
    {
        private IWindsorContainer m_localContainer;
        private IWindsorContainer m_remoteContainer;
        private const string REMOTE_ADDR = @".\private$\remote";
        private const string LOCAL_ADDR = @".\private$\local";
        private const string SERVICE_KEY = "test-service";
        private const string PROXY_KEY = "test-proxy";

        [SetUp]
        public void SetUp()
        {
            m_localContainer = new WindsorContainer().IntegrateMessageless(LOCAL_ADDR);
            m_remoteContainer = new WindsorContainer().IntegrateMessageless(REMOTE_ADDR);
        }
        [TearDown]
        public void TearDown()
        {
            if (m_localContainer != null) m_localContainer.Dispose();
            if (m_remoteContainer != null) m_remoteContainer.Dispose();
        }

        [Test]
        public void Calling_method_with_return_value_on_proxy_should_throw()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY, PROXY_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>(PROXY_KEY);

            // act
            Action action = () => proxy.GetReturnValue();

            // assert
            action.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void Calling_method_with_out_params_on_proxy_should_throw()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY, PROXY_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>(PROXY_KEY);

            // act
            object param;
            Action action = () => proxy.MethodWithOutParams(out param);

            // assert
            action.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void Calling_method_with_ref_params_on_proxy_should_not_throw()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY, PROXY_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>(PROXY_KEY);

            // act
            object param = null;
            Action action = () => proxy.MethodWithRefParams(ref param);

            // assert
            action.ShouldNotThrow();
        }

        [Test]
        public void Callback_with_return_value_should_throw()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY, PROXY_KEY)
                );

            m_remoteContainer.Register(
                Component.For<IService>().ImplementedBy<Service>().Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>(PROXY_KEY);
            var service = m_remoteContainer.Resolve<IService>(SERVICE_KEY);
            var signal = new AutoResetEvent(false);
            service.As<Service>().MethodWithFuncCallbackImpl = func => { signal.Set(); func(); };

            // act
            Action action = () => proxy.MethodWithFuncCallback(() => 0);
            // ReSharper restore EmptyGeneralCatchClause

            // assert
            action.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void Calling_method_with_return_value_on_proxy_should_not_reach_the_service()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY, PROXY_KEY)
                );

            m_remoteContainer.Register(
                Component.For<IService>().ImplementedBy<Service>().Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>(PROXY_KEY);
            var service = m_remoteContainer.Resolve<IService>(SERVICE_KEY);
            var signal = new AutoResetEvent(false);
            service.As<Service>().GetReturnValueImpl = () => signal.Set();

            // act
            try
            {
                proxy.GetReturnValue();
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch { }
            // ReSharper restore EmptyGeneralCatchClause

            // assert
            var funcCalled = signal.WaitOne(TimeSpan.FromSeconds(1));
            funcCalled.Should().BeFalse();
        }

        [Test]
        public void Calling_method_on_proxy_should_be_propogated_to_remote_service_2_containers()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY, PROXY_KEY)
                );

            m_remoteContainer.Register(
                Component.For<IService>().ImplementedBy<Service>().Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>();
            var service = m_remoteContainer.Resolve<IService>();

            var signal = new AutoResetEvent(false);
            service.As<Service>().FooImpl = () => signal.Set();

            // act
            proxy.Foo();

            // assert
            var fooCalled = signal.WaitOne(TimeSpan.FromSeconds(1));
            fooCalled.Should().BeTrue();
        }
        [Test]
        public void Calling_generic_method_on_proxy_should_be_propogated_to_remote_service()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY, PROXY_KEY)
                );

            m_remoteContainer.Register(
                Component.For<IService>().ImplementedBy<Service>().Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>();
            var service = m_remoteContainer.Resolve<IService>();

            var waitableValue = new WaitableValue<int>();
            service.As<Service>().GenericMethodImpl = arg => { waitableValue.Value = (int) arg; };
            const int magicNumber = 666;
            // act
            proxy.GenericMethod(magicNumber);

            // assert
            var methodCalled = waitableValue.WaitOne(TimeSpan.FromSeconds(1));
            methodCalled.Should().BeTrue();
            waitableValue.Value.Should().Be(magicNumber);
        }

        [Test]
        public void Nested_callbacks_should_be_able_to_round_trip()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY, PROXY_KEY)
                );

            m_remoteContainer.Register(
                Component.For<IService>().ImplementedBy<Service>().Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>();
            var service = m_remoteContainer.Resolve<IService>();

            const int magicNumber = 666;
            service.As<Service>().MethodWithNestedCallbackImpl = cb1 => cb1(cb2 => cb2(magicNumber));
            var result = new WaitableValue<int>();

            // act
            proxy.MethodWithNestedCallback(cb => cb(x => result.Value = x));

            // assert
            var callbackCalled = result.WaitOne(TimeSpan.FromSeconds(1));
            callbackCalled.Should().BeTrue();

            result.Value.Should().Be(magicNumber);
        }
        [Test]
        public void Generic_nested_callbacks_should_be_able_to_round_trip()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY, PROXY_KEY)
                );

            m_remoteContainer.Register(
                Component.For<IService>().ImplementedBy<Service>().Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>();
            var service = m_remoteContainer.Resolve<IService>();

            const int magicNumber = 666;
            service.As<Service>().GenericMethodWithNestedCallbackImpl = (o,cb1) => cb1(cb2 => ((Action<int>)cb2)((int) o));
            var result = new WaitableValue<int>();

            // act
            proxy.GenericMethodWithNestedCallback(magicNumber, cb => cb(x => result.Value = x));

            // assert
            var callbackCalled = result.WaitOne(TimeSpan.FromSeconds(1));
            callbackCalled.Should().BeTrue();

            result.Value.Should().Be(magicNumber);
        }

        [Test]
        public void Calling_method_with_null_callback_on_proxy_should_not_fail()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY, PROXY_KEY)
                );

            m_remoteContainer.Register(
                Component.For<IService>().ImplementedBy<Service>().Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>();
            var service = m_remoteContainer.Resolve<IService>();

            var isNullCallback = new WaitableValue<bool>();
            service.As<Service>().AddImpl = (x, y, cb) => isNullCallback.Value = (cb == null);

            // act
            proxy.Add(111, 222, null);

            // assert
            var methodWasCalled = isNullCallback.WaitOne(TimeSpan.FromSeconds(1));
            methodWasCalled.Should().BeTrue();

            isNullCallback.Value.Should().BeTrue();
        }
        [Test]
        public void Calling_method_with_parameterless_callback_on_proxy_should_not_fail()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY, PROXY_KEY)
                );

            m_remoteContainer.Register(
                Component.For<IService>().ImplementedBy<Service>().Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>();
            var service = m_remoteContainer.Resolve<IService>();

            var result = new WaitableValue<int>();
            const int magicNumber = 666;
            service.As<Service>().MethodWithParameterlessCallbackImpl = cb => cb();

            // act
            proxy.MethodWithParameterlessCallback(() => result.Value = magicNumber);

            // assert
            var callbackWasCalled = result.WaitOne(TimeSpan.FromSeconds(1));
            callbackWasCalled.Should().BeTrue();
            result.Value.Should().Be(magicNumber);
        }
        [Test]
        public void Calling_method_with_callback_on_proxy_should_not_fail()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY, PROXY_KEY)
                );

            m_remoteContainer.Register(
                Component.For<IService>().ImplementedBy<Service>().Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>();
            var service = m_remoteContainer.Resolve<IService>();

            var result = new WaitableValue<int>();
            service.As<Service>().AddImpl = (x, y, cb) => cb(x + y);

            // act
            proxy.Add(111, 222, i => result.Value = i);

            // assert
            var methodWasCalled = result.WaitOne(TimeSpan.FromSeconds(1));
            methodWasCalled.Should().BeTrue();

            result.Value.Should().Be(111 + 222);
        }

        [Test]
        public void Poison_message_in_local_queue_should_not_stop_handler()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(LOCAL_ADDR, SERVICE_KEY, PROXY_KEY),
                Component.For<IService>().ImplementedBy<Service>().Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>(PROXY_KEY);
            var service = m_localContainer.Resolve<IService>(SERVICE_KEY);
            var signal = new AutoResetEvent(false);
            service.As<Service>().FooImpl = () => signal.Set();

            // act
            var transport = m_localContainer.Resolve<ITransport>();
            var poisonMsg = new TransportMessage(new byte[] { 1, 2, 3 }, LOCAL_ADDR, key:"poison");
            transport.OnNext(poisonMsg);
            transport.OnNext(poisonMsg);

            proxy.Foo();

            // assert
            var fooCalled = signal.WaitOne(TimeSpan.FromSeconds(1));
            fooCalled.Should().BeTrue();
        }
        [Test]
        public void Calling_method_on_proxy_should_be_propogated_to_remote_service()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(LOCAL_ADDR, SERVICE_KEY, PROXY_KEY),
                Component.For<IService>().ImplementedBy<Service>().Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>(PROXY_KEY);
            var service = m_localContainer.Resolve<IService>(SERVICE_KEY);
            var signal = new AutoResetEvent(false);
            service.As<Service>().FooImpl = () => signal.Set();

            // act
            proxy.Foo();

            // assert
            var fooCalled = signal.WaitOne(TimeSpan.FromSeconds(1));
            fooCalled.Should().BeTrue();
        }

        [Test]
        public void Resolving_a_proxy_should_not_return_the_service()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(LOCAL_ADDR, SERVICE_KEY, PROXY_KEY),
                Component.For<IService>().ImplementedBy<Service>().Named(SERVICE_KEY)
                );

            // act
            var proxy = m_localContainer.Resolve<IService>(PROXY_KEY);

            // assert
            proxy.GetType().Should().NotBe(typeof(Service));
        }

        [Test]
        public void Resolving_a_service_should_not_return_the_proxy()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(LOCAL_ADDR, SERVICE_KEY, PROXY_KEY),
                Component.For<IService>().ImplementedBy<Service>().Named(SERVICE_KEY)
                );

            // act
            var service = m_localContainer.Resolve<IService>(SERVICE_KEY);

            // assert
            service.Should().BeOfType<Service>();
        }

        [Test]
        public void Calling_method_on_proxy_should_not_be_propogated_to_remote_service_in_different_address()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY, PROXY_KEY),
                Component.For<IService>().ImplementedBy<Service>().Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>(PROXY_KEY);
            var service = m_localContainer.Resolve<IService>(SERVICE_KEY);
            var signal = new AutoResetEvent(false);
            service.As<Service>().FooImpl = () => signal.Set();

            // act
            proxy.Foo();

            // assert
            var fooCalled = signal.WaitOne(TimeSpan.FromSeconds(1));
            fooCalled.Should().BeFalse();
        }

        [Test]
        public void Resolving_proxy_with_dynamic_address_should_not_use_static_address()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(LOCAL_ADDR, SERVICE_KEY, PROXY_KEY),
                Component.For<IService>().ImplementedBy<Service>().Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.ResolveRemoteService<IService>(REMOTE_ADDR);
            var service = m_localContainer.Resolve<IService>(SERVICE_KEY);
            var signal = new AutoResetEvent(false);
            service.As<Service>().FooImpl = () => signal.Set();

            // act
            proxy.Foo();

            // assert
            var fooCalled = signal.WaitOne(TimeSpan.FromSeconds(1));
            fooCalled.Should().BeFalse();
        }

        [Test]
        public void Invoking_a_method_on_a_proxy_should_send_message()
        {
            // arrange
            m_localContainer.Register(Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY));

            var service = m_localContainer.ResolveRemoteService<IService>(@".\private$\tmp");
            var destinationTransport = new MsmqTransport();
            destinationTransport.Init(@".\private$\tmp");

            // act
            service.Foo();

            // assert
            var msgReceived = destinationTransport
                .Take(1)
                .Timeout(TimeSpan.FromSeconds(1))
                .First();
            msgReceived.Should().NotBeNull();
        }

        [Test]
        public void Invoking_method_on_proxy_should_not_throw()
        {
            // arrange
            m_localContainer.Register(Component.For<IService>().At(REMOTE_ADDR, SERVICE_KEY));

            var resolve = m_localContainer.Resolve<IService>();
            
            // act
            resolve.Foo();

            // assert
            // nothing is thrown...
        }

        [Test]
        public void Transport_should_work()
        {
            const string queueName = @".\private$\test";
            using (var transport = new MsmqTransport())
            {
                transport.Init(queueName);

                var payload = new byte[] {1, 2, 3, 4, 5, 6, 6, 6};
                var firstMsg = transport.Where(message => message.Payload.SequenceEqual(payload)).Take(1);

                const string key = "key";
                transport.OnNext(new TransportMessage(payload, queueName, key));

                var msgReceived = firstMsg
                    .Timeout(TimeSpan.FromSeconds(1))
                    .First();
                msgReceived.Should().NotBeNull();
            }
        }
    }

    public class Service : IService
    {
        #region Implementation of IService

        public Service()
        {
            FooImpl = delegate { };
            GetReturnValueImpl = () => null;
            AddImpl = delegate { };
            MethodWithNestedCallbackImpl = delegate { };
            MethodWithParameterlessCallbackImpl = delegate { };
            GenericMethodImpl = delegate { };
        }

        public void Foo()
        {
            Console.WriteLine("Service.Foo() called");
            FooImpl();
        }

        public void GenericMethod<T>(T arg)
        {
            Console.WriteLine("Service.GenericMethod<T>() called");
            GenericMethodImpl(arg);
        }

        public Action FooImpl { get; set; }

        public object GetReturnValue()
        {
            Console.WriteLine("Service.GetReturnValue() called");
            return GetReturnValueImpl();
        }

        public void MethodWithOutParams(out object param)
        {
            Console.WriteLine("Service.MethodWithOutParams() called");
            param = GetReturnValueImpl();
        }

        // ReSharper disable RedundantAssignment
        public void MethodWithRefParams(ref object param)
        // ReSharper restore RedundantAssignment
        {
            Console.WriteLine("Service.MethodWithRefParams() called");
            param = GetReturnValueImpl();
        }

        public void Add(int x, int y, Action<int> callback)
        {
            Console.WriteLine("Service.Add() called");
            AddImpl(x, y, callback);
        }

        public void MethodWithFuncCallback(Func<int> callback)
        {
            Console.WriteLine("Service.MethodWithFuncCallback() called");
            MethodWithFuncCallbackImpl(callback);
        }

        public void MethodWithNestedCallback(Action<Action<Action<int>>> callback)
        {
            Console.WriteLine("Service.MethodWithNestedCallback() called");
            MethodWithNestedCallbackImpl(callback);
        }

        public void GenericMethodWithNestedCallback<T>(T value, Action<Action<Action<T>>> callback)
        {
            Console.WriteLine("Service.GenericMethodWithNestedCallback() called");
            GenericMethodWithNestedCallbackImpl(value, callback);
        }

        public void MethodWithParameterlessCallback(Action callback)
        {
            Console.WriteLine("Service.MethodWithParameterlessCallback() called");
            MethodWithParameterlessCallbackImpl(callback);
        }

        public Func<object> GetReturnValueImpl { get; set; }
        public Action<int,int,Action<int>> AddImpl { get; set; }
        public Action<Action> MethodWithParameterlessCallbackImpl { get; set; }
        public Action<Func<int>> MethodWithFuncCallbackImpl { get; set; }
        public Action<Action<Action<Action<int>>>> MethodWithNestedCallbackImpl { get; set; }
        public Action<object,Action<Action<Delegate>>> GenericMethodWithNestedCallbackImpl { get; set; }
        public Action<object> GenericMethodImpl { get; set; }

        #endregion
    }

    public interface IService
    {
        void Foo();
        void GenericMethod<T>(T arg);
        object GetReturnValue();
        void MethodWithOutParams(out object param);
        void MethodWithRefParams(ref object param);
        void Add(int x, int y, Action<int> callback);
        void MethodWithFuncCallback(Func<int> callback);
        void MethodWithNestedCallback(Action<Action<Action<int>>> callback);
        void GenericMethodWithNestedCallback<T>(T value, Action<Action<Action<T>>> callback);
        void MethodWithParameterlessCallback(Action callback);
    }

    public class WaitableValue<T>
    {
        private readonly TaskCompletionSource<T> m_source = new TaskCompletionSource<T>();

        public T Value
        {
            get { return m_source.Task.Result; }
            set { m_source.SetResult(value); }
        }

        public bool WaitOne(TimeSpan timeout)
        {
            return m_source.Task.Wait(timeout);
        }
    }
}