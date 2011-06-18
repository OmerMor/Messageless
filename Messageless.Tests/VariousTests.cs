using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using FluentAssertions;
using NUnit.Framework;

//using FluentAssertions;

namespace Messageless.Tests
{
    public class VariousTests
    {
        [Test]
        public void Calling_method_on_proxy_should_be_propogated_to_remote_service_2_containers()
        {
            // arrange
            using (var localContainer = new WindsorContainer().IntegrateMessageless(@".\private$\local"))
            using (var remoteContainer = new WindsorContainer().IntegrateMessageless(@".\private$\remote"))
            {
                localContainer.Register(
                    Component.For<IService>().LifeStyle.Transient.At(@".\private$\remote", "test-service", "test-proxy")
                    );

                remoteContainer.Register(
                    Component.For<IService>().ImplementedBy<Service>().Named("test-service")
                    );

                var proxy = localContainer.Resolve<IService>();
                var service = remoteContainer.Resolve<IService>();

                var signal = new AutoResetEvent(false);
                service.As<Service>().FooImpl = () => signal.Set();

                // act
                proxy.Foo();

                // assert
                var fooCalled = signal.WaitOne(TimeSpan.FromSeconds(1));
                fooCalled.Should().BeTrue();
            }
        }

        [Test]
        public void Poison_message_in_local_queue_should_not_stop_handler()
        {
            using (var container = new WindsorContainer().IntegrateMessageless(@".\private$\local"))
            {
                container.Register(
                    Component.For<IService>().LifeStyle.Transient.At(@".\private$\local", "test-service", "test-proxy"),
                    Component.For<IService>().ImplementedBy<Service>().Named("test-service")
                    );

                var proxy = container.Resolve<IService>("test-proxy");
                proxy.GetType().Should().NotBe(typeof(Service));

                var service = container.Resolve<IService>("test-service");
                service.Should().BeOfType<Service>();
                var signal = new AutoResetEvent(false);
                service.As<Service>().FooImpl = () => signal.Set();

                // act
                var transport = container.Resolve<ITransport>();
                var poisonMsg = new TransportMessage(new byte[] { 1, 2, 3 }, @".\private$\local", "poison");
                transport.OnNext(poisonMsg);
                transport.OnNext(poisonMsg);

                proxy.Foo();

                // assert
                var fooCalled = signal.WaitOne(TimeSpan.FromSeconds(1));
                fooCalled.Should().BeTrue();
            }
        }
        [Test]
        public void Calling_method_on_proxy_should_be_propogated_to_remote_service()
        {
            // arrange
            using (var container = new WindsorContainer().IntegrateMessageless(@".\private$\local"))
            {
                container.Register(
                    Component.For<IService>().LifeStyle.Transient.At(@".\private$\local", "test-service", "test-proxy"),
                    Component.For<IService>().ImplementedBy<Service>().Named("test-service")
                    );

                var proxy = container.Resolve<IService>("test-proxy");
                proxy.GetType().Should().NotBe(typeof(Service));

                var service = container.Resolve<IService>("test-service");
                service.Should().BeOfType<Service>();
                var signal = new AutoResetEvent(false);
                service.As<Service>().FooImpl = () => signal.Set();

                // act
                proxy.Foo();

                // assert
                var fooCalled = signal.WaitOne(TimeSpan.FromSeconds(1));
                fooCalled.Should().BeTrue();
            }
        }

        [Test]
        public void Resolving_a_service_should_not_return_the_proxy()
        {
            // arrange
            using (var container = new WindsorContainer().IntegrateMessageless(@".\private$\local"))
            {

                container.Register(
                    Component.For<IService>().LifeStyle.Transient.At(@".\private$\local", "test-service", "test-proxy"),
                    Component.For<IService>().ImplementedBy<Service>().Named("test-service")
                    );

                // act
                var service = container.Resolve<IService>("test-service");

                // assert
                service.Should().BeOfType<Service>();
            }
        }

        [Test]
        public void Resolving_a_proxy_should_not_return_the_service()
        {
            // arrange
            using (var container = new WindsorContainer().IntegrateMessageless(@".\private$\local"))
            {

                container.Register(
                    Component.For<IService>().LifeStyle.Transient.At(@".\private$\local", "test-service", "test-proxy"),
                    Component.For<IService>().ImplementedBy<Service>().Named("test-service")
                    );

                // act
                var proxy = container.Resolve<IService>("test-proxy");

                // assert
                proxy.GetType().Should().NotBe(typeof (Service));
            }
        }

        [Test]
        public void Calling_method_on_proxy_should_not_be_propogated_to_remote_service_in_different_address()
        {
            // arrange
            using (var container = new WindsorContainer().IntegrateMessageless(@".\private$\local"))
            {

                container.Register(
                    Component.For<IService>().LifeStyle.Transient.At(@".\private$\remote", "test-service", "test-proxy"),
                    Component.For<IService>().ImplementedBy<Service>().Named("test-service")
                    );

                var proxy = container.Resolve<IService>("test-proxy");
                proxy.GetType().Should().NotBe(typeof (Service));

                var service = container.Resolve<IService>("test-service");
                service.Should().BeOfType<Service>();
                var signal = new AutoResetEvent(false);
                service.As<Service>().FooImpl = () => signal.Set();

                // act
                proxy.Foo();

                // assert
                var fooCalled = signal.WaitOne(TimeSpan.FromSeconds(1));
                fooCalled.Should().BeFalse();
            }
        }

        [Test]
        public void Resolving_proxy_with_dynamic_address_should_not_use_static_address()
        {
            // arrange
            using (var container = new WindsorContainer().IntegrateMessageless(@".\private$\local"))
            {

                container.Register(
                    Component.For<IService>().LifeStyle.Transient.At(@".\private$\local", "test-service", "test-proxy"),
                    Component.For<IService>().ImplementedBy<Service>().Named("test-service")
                    );

                var proxy = container.ResolveRemoteService<IService>(@".\private$\remote");
                proxy.GetType().Should().NotBe(typeof (Service));

                var service = container.Resolve<IService>("test-service");
                service.Should().BeOfType<Service>();
                var signal = new AutoResetEvent(false);
                service.As<Service>().FooImpl = () => signal.Set();

                // act
                proxy.Foo();

                // assert
                var fooCalled = signal.WaitOne(TimeSpan.FromSeconds(1));
                fooCalled.Should().BeFalse();
            }
        }

        [Test]
        public void Invoking_a_method_on_a_proxy_should_send_message()
        {
            // arrange
            using (var container = new WindsorContainer().IntegrateMessageless(@".\private$\local"))
            {

                container.Register(Component.For<IService>().LifeStyle.Transient.At(@".\private$\remote", "test-service"));

                var service = container.Resolve<IService>();
                var destinationTransport = new MsmqTransport();
                destinationTransport.Init(@".\private$\remote");

                // act
                service.Foo();

                // assert
                var msgReceived = destinationTransport
                    .Take(1)
                    .Timeout(TimeSpan.FromSeconds(1))
                    .First();
                msgReceived.Should().NotBeNull();
            }
        }

        [Test]
        public void Invoking_method_on_proxy_should_not_throw()
        {
            using (var container = new WindsorContainer().IntegrateMessageless(@".\private$\local"))
            {
                container.Register(Component.For<IService>().At(@".\private$\remote", "service"));

                var resolve = container.Resolve<IService>();
                resolve.Foo();
            }
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

        public void Foo()
        {
            Console.WriteLine("Service.Foo() called");
            FooImpl();
        }

        public Service()
        {
            FooImpl = () => { };
        }

        public Action FooImpl { get; set; }

        #endregion
    }

    public interface IService
    {
        void Foo();
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