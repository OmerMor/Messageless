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
            action.ShouldThrow<Exception>();
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
            catch{}
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
            FooImpl = () => { };
            GetReturnValueImpl = () => null;
        }

        public void Foo()
        {
            Console.WriteLine("Service.Foo() called");
            FooImpl();
        }

        public Action FooImpl { get; set; }

        public object GetReturnValue()
        {
            Console.WriteLine("Service.GetReturnValue() called");
            return GetReturnValueImpl();
        }

        public Func<object> GetReturnValueImpl { get; set; }

        #endregion
    }

    public interface IService
    {
        void Foo();
        object GetReturnValue();
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