using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy.Generators;
using Castle.MicroKernel;
using Castle.MicroKernel.ComponentActivator;
using Castle.MicroKernel.Context;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using FluentAssertions;
using NUnit.Framework;
using System.Reactive.Threading.Tasks;

//using FluentAssertions;

namespace Messageless.Tests
{
    public class VariousTests
    {

        [Test]
        public void Container_should_register2()
        {
            var container = new WindsorContainer().IntegrateMessageless(@".\private$\test");

            container.Register(Component.For<IService>().LifeStyle.Transient.At(@".\private$\test", "test-service"));
            container.Register(Component.For<IService>().ImplementedBy<Service>().Named("test-service"));

            var service1 = container.Resolve<IService>();
            var service2 = container.ResolveRemoteService<IService>(@".\private$\test2");
            
            var transport = container.Resolve<ITransport>();

            transport.Subscribe(message =>
            {
                var formatter = new BinaryFormatter();
                var stream = new MemoryStream(message.Payload);
                var o = formatter.Deserialize(stream);
                Console.WriteLine(o.ToString());
            });

            service1.Foo();
            service2.Foo();

            Thread.Sleep(TimeSpan.FromSeconds(10));
        }

        [Test]
        public void Container_should_register()
        {
            var container = new WindsorContainer();
            container.Register(Component.For<MessagelessInterceptor>().LifeStyle.Transient);
            container.Register(Component.For<IService>().At(@".\private$\test", "service"));

            var resolve = container.Resolve<IService>();
            resolve.Foo();
        }

        [Test]
        public void Foo()
        {
            var factory = new Factory();
            var proxy = factory.CreateProxy<IObj>("app", "service");
            proxy.Should().NotBeNull();
        }

        [Test]
        public void Transport_should_work()
        {
            const string queueName = @".\private$\test";
            ITransport transport = new MsmqTransport();
            transport.Init(queueName);

            var payload = new byte[] { 1, 2, 3, 4, 5, 6, 6, 6 };
            var firstMsg = transport
                .Where(message => message.Payload.SequenceEqual(payload))
                .Take(1);

            const string key = "key";
            transport.OnNext(new TransportMessage(payload, queueName, key));
            
            var msgReceived = firstMsg
                .Timeout(TimeSpan.FromSeconds(1))
                .First();
            msgReceived.Should().NotBeNull();
        }
    }

    public class Service : IService
    {
        #region Implementation of IService

        public void Foo()
        {
            Console.WriteLine("Service.Foo() called");
        }

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
            set 
            {
                m_source.SetResult(value);
            }
        }

        public bool WaitOne(TimeSpan timeout)
        {
            return m_source.Task.Wait(timeout);
        }
    }

    public interface IObj
    {
        void Foo();
    }

    public class Factory
    {
        public T CreateProxy<T>(string appAddress, string serviceName)
        {
            return default(T);
        }
    }
}
