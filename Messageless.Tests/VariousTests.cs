using System;
using System.Reactive.Linq;
using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using FluentAssertions;
using NUnit.Framework;
using Rhino.Mocks;

namespace Messageless.Tests
{
    //[TestFixture(typeof(MsmqTransport), Category = "MSMQ")]
    //[TestFixture(typeof(InProcTransport))]
    //public class VariousTests<TTransport> where TTransport : ITransport, new()
    public class VariousTests
    {
        private IWindsorContainer m_localContainer;
        private IWindsorContainer m_remoteContainer;
        private IService m_service;
        private const string REMOTE_ADDR = @".\private$\remote";
        private const string LOCAL_ADDR = @".\private$\local";
        private const string SERVICE_KEY = "test-service";
        private const string PROXY_KEY = "test-proxy";

        [SetUp]
        public void SetUp()
        {
            m_localContainer = new WindsorContainer(); //.IntegrateMessageless(LOCAL_ADDR);
            m_remoteContainer = new WindsorContainer(); //.IntegrateMessageless(REMOTE_ADDR);
            InProcTransport.Start();
            m_localContainer.AddFacility<MessagelessTestFacility<InProcTransport>>(
                facility => facility.Init(LOCAL_ADDR));
            m_remoteContainer.AddFacility<MessagelessTestFacility<InProcTransport>>(
                facility => facility.Init(REMOTE_ADDR));
            m_service = MockRepository.GenerateStub<IService>();
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
            var action = proxy.Invoking(p => p.GetReturnValue());

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
            var action = proxy.Invoking(p => p.MethodWithOutParams(out param));

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
            var action = proxy.Invoking(p => p.MethodWithRefParams(ref param));

            // assert
            action.ShouldNotThrow();
        }

        [Test]
        public void Generic_callback_resolution()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY, PROXY_KEY)
                );

            m_remoteContainer.Register(
                Component.For<IService>().Instance(m_service).Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>(PROXY_KEY);
            var signal = new AutoResetEvent(false);
            m_service.Stub(s => s.MethodWithGenericCallback<Action<Action<Derived>>>(null))
                .IgnoreArguments()
                .Call().Action<Action<Action<Action<Derived>>>>(cb1 => cb1(cb2 => cb2(null)));

            // act
            Action<Action<Action<Base>>> localCb = remoteCbProxy => remoteCbProxy(x => signal.Set());
            proxy.MethodWithGenericCallback<Action<Action<Derived>>>(localCb);

            // assert
            var callbackCalled = signal.WaitOne(TimeSpan.FromSeconds(1));
            callbackCalled.Should().BeTrue();
        }

        [Test]
        public void Callback_with_return_value_should_throw()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY, PROXY_KEY)
                );

            m_remoteContainer.Register(
                Component.For<IService>().Instance(m_service).Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>(PROXY_KEY);
            var signal = new AutoResetEvent(false);
            m_service.Stub(s => s.MethodWithFuncCallback(null))
                .IgnoreArguments()
                .Call().Action<Func<int>>(func =>
                {
                    signal.Set();
                    func();
                });

            // act
            var action = proxy.Invoking(p => p.MethodWithFuncCallback(() => 0));

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
                Component.For<IService>().Instance(m_service).Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>(PROXY_KEY);
            var signal = new AutoResetEvent(false);
            m_service.Stub(s => s.GetReturnValue()).Call().Func(signal.Set);

            // act
            try
            {
                proxy.GetReturnValue();
            }
                // ReSharper disable EmptyGeneralCatchClause
            catch
            {
            }
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
                Component.For<IService>().Instance(m_service).Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>();

            var signal = new AutoResetEvent(false);
            m_service.Stub(s => s.Foo()).Call().Action(() => signal.Set());

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
                Component.For<IService>().Instance(m_service).Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>();

            var waitableValue = new WaitableValue<int>();
            const int magicNumber = 666;
            m_service.Stub(s => s.GenericMethod(magicNumber))
                .Call().Action<int>(arg => { waitableValue.Value = arg; });

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
                Component.For<IService>().Instance(m_service).Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>();

            const int magicNumber = 666;
            m_service.Stub(s => s.MethodWithNestedCallback(null))
                .IgnoreArguments()
                .Call().Action<Action<Action<Action<int>>>>(cb1 => cb1(cb2 => cb2(magicNumber)));
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
                Component.For<IService>().Instance(m_service).Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>();

            const int magicNumber = 666;
            m_service.Stub(s => s.GenericMethodWithNestedCallback(magicNumber, null))
                .IgnoreArguments()
                .Call().Action((int x, Action<Action<Action<int>>> cb1) => cb1(cb2 => cb2(x)));
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
                Component.For<IService>().Instance(m_service).Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>();

            var isNullCallback = new WaitableValue<bool>();
            m_service.Stub(s => s.Add(0, 0, null))
                .IgnoreArguments()
                .Call().Action((int x, int y, Action<int> cb) => isNullCallback.Value = (cb == null));

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
                Component.For<IService>().Instance(m_service).Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>();

            var result = new WaitableValue<int>();
            const int magicNumber = 666;
            m_service.Stub(s => s.MethodWithParameterlessCallback(null))
                .IgnoreArguments()
                .Call().Action<Action>(cb => cb());

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
                Component.For<IService>().Instance(m_service).Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>();

            var result = new WaitableValue<int>();
            m_service.Stub(s => s.Add(0, 0, null))
                .IgnoreArguments()
                .Call().Action((int x, int y, Action<int> cb) => cb(x + y));

            // act
            proxy.Add(111, 222, i => result.Value = i);

            // assert
            var methodWasCalled = result.WaitOne(TimeSpan.FromSeconds(1));
            methodWasCalled.Should().BeTrue();

            result.Value.Should().Be(111 + 222);
        }

        [Test]
        public void Ignoring_a_callback_with_timeout_should_invoke_the_callback_with_timeout_context()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY, PROXY_KEY)
                );

            m_remoteContainer.Register(
                Component.For<IService>().Instance(m_service).Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>();

            var result = new WaitableValue<bool>();
            Action<int, int, Action<int>> ignoreCallback = (x, y, cb) => { };
            m_service.Stub(s => s.Add(0, 0, null))
                .IgnoreArguments()
                .Call().Action(ignoreCallback);

            // act
            MessagelessContext.Execute(
                ctx => proxy.Add(111, 222, i =>
                {
                    result.Value = MessagelessContext.CurrentContext.CallbackTimedOut;
                }),
                timeout: TimeSpan.FromSeconds(0.5));


            // assert
            var calledWithTimeoutContext = result.WaitOne(TimeSpan.FromSeconds(1));
            calledWithTimeoutContext.Should().BeTrue();

            result.Value.Should().BeTrue();
        }
        [Test]
        public void Invocation_of_callback_after_timeout_should_invoke_the_callback_with_timeout_context()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY, PROXY_KEY)
                );

            m_remoteContainer.Register(
                Component.For<IService>().Instance(m_service).Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>();

            var timeoutResult = new WaitableValue<bool>();
            var addResult = new WaitableValue<int>();
            Action<int, int, Action<int>> delayedCallbackInvocation =
                (x, y, cb) => Observable.Timer(TimeSpan.FromSeconds(0.6)).Subscribe(_ => cb(x + y));
            m_service.Stub(s => s.Add(0, 0, null))
                .IgnoreArguments()
                .Call().Action(delayedCallbackInvocation);

            // act
            MessagelessContext.Execute(
                ctx => proxy.Add(111, 222, result =>
                {
                    if (MessagelessContext.CurrentContext.CallbackTimedOut)
                        timeoutResult.Value = true;
                    else
                        addResult.Value = result;
                }),
                timeout: TimeSpan.FromSeconds(0.5));


            // assert
            var calledWithTimeoutContext = timeoutResult.WaitOne(TimeSpan.FromSeconds(1));
            calledWithTimeoutContext.Should().BeTrue();
            timeoutResult.Value.Should().BeTrue();

            var calledWithResult = addResult.WaitOne(TimeSpan.FromSeconds(0.5));
            calledWithResult.Should().BeFalse();
        }

        [Test]
        public void Invocation_of_callback_before_timeout_should_not_invoke_the_callback_with_timeout_context()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY, PROXY_KEY)
                );

            m_remoteContainer.Register(
                Component.For<IService>().Instance(m_service).Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>();

            var timeoutResult = new WaitableValue<bool>();
            var addResult = new WaitableValue<int>();
            Action<int, int, Action<int>> delayedCallbackInvocation =
                (x, y, cb) => Observable.Timer(TimeSpan.FromSeconds(0.3)).Subscribe(_ => cb(x + y));
            m_service.Stub(s => s.Add(0, 0, null))
                .IgnoreArguments()
                .Call().Action(delayedCallbackInvocation);

            // act
            MessagelessContext.Execute(
                ctx => proxy.Add(111, 222, result =>
                {
                    if (MessagelessContext.CurrentContext.CallbackTimedOut)
                        timeoutResult.Value = true;
                    else
                        addResult.Value = result;
                }),
                timeout: TimeSpan.FromSeconds(0.5));


            // assert
            var calledWithTimeoutContext = timeoutResult.WaitOne(TimeSpan.FromSeconds(1));
            calledWithTimeoutContext.Should().BeFalse();

            var calledWithResult = addResult.WaitOne(TimeSpan.FromSeconds(0.1));
            calledWithResult.Should().BeTrue();
            addResult.Value.Should().Be(111+222);
        }

        [Test]
        public void Callback_should_be_stored_for_single_invocation_only()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY, PROXY_KEY)
                );

            m_remoteContainer.Register(
                Component.For<IService>().Instance(m_service).Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>();

            var signal = new AutoResetEvent(false);
            m_service.Stub(s => s.Add(0, 0, null))
                .IgnoreArguments()
                .Call().Action((int x, int y, Action<int> cb) =>
                {
                    cb(x + y); 
                    cb(x + y);
                });
            var counter = 0;
            // act
            proxy.Add(111, 222, i =>
            {
                var count = Interlocked.Increment(ref counter);
                if (count == 2)
                    signal.Set();
            });

            // assert
            var methodWasCalledTwice = signal.WaitOne(TimeSpan.FromSeconds(1));
            methodWasCalledTwice.Should().BeFalse();
        }

        [Test]
        public void Poison_message_in_local_queue_should_not_stop_handler()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(LOCAL_ADDR, SERVICE_KEY, PROXY_KEY),
                Component.For<IService>().Instance(m_service).Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>(PROXY_KEY);
            var signal = new AutoResetEvent(false);
            m_service.Stub(s => s.Foo())
                .IgnoreArguments()
                .Call().Action(() => signal.Set());

            // act
            var transport = m_localContainer.Resolve<ITransport>();
            var poisonMsg = new TransportMessage(new byte[] {1, 2, 3}, LOCAL_ADDR);
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
                Component.For<IService>().Instance(m_service).Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>(PROXY_KEY);
            var signal = new AutoResetEvent(false);
            m_service.Stub(s => s.Foo())
                .IgnoreArguments()
                .Call().Action(() => signal.Set());

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
                Component.For<IService>().Instance(m_service).Named(SERVICE_KEY)
                );

            // act
            var proxy = m_localContainer.Resolve<IService>(PROXY_KEY);

            // assert
            proxy.Should().NotBe(m_service);
        }

        [Test]
        public void Resolving_a_service_should_not_return_the_proxy()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(LOCAL_ADDR, SERVICE_KEY, PROXY_KEY),
                Component.For<IService>().Instance(m_service).Named(SERVICE_KEY)
                );

            // act
            var service = m_localContainer.Resolve<IService>(SERVICE_KEY);

            // assert
            service.Should().Be(m_service);
        }

        [Test]
        public void Calling_method_on_proxy_should_not_be_propogated_to_remote_service_in_different_address()
        {
            // arrange
            m_localContainer.Register(
                Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY, PROXY_KEY),
                Component.For<IService>().Instance(m_service).Named(SERVICE_KEY)
                );

            var proxy = m_localContainer.Resolve<IService>(PROXY_KEY);
            var signal = new AutoResetEvent(false);
            m_service.Stub(s => s.Foo())
                .IgnoreArguments()
                .Call().Action(() => signal.Set());

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
                Component.For<IService>().Instance(m_service).Named(SERVICE_KEY)
                );
            var proxy = m_localContainer.ResolveRemoteService<IService>(REMOTE_ADDR);
            var signal = new AutoResetEvent(false);
            m_service.Stub(s => s.Foo())
                .IgnoreArguments()
                .Call().Action(() => signal.Set());

            // act
            proxy.Foo();

            // assert
            //signal.AssertWasCalled(@event => @event.WaitOne(), opt => opt.Constraints(new ));
            var fooCalled = signal.WaitOne(TimeSpan.FromSeconds(1));
            fooCalled.Should().BeFalse();
        }

        [Test]
        public void Invoking_a_method_on_a_proxy_should_send_message()
        {
            // arrange
            m_localContainer.Register(Component.For<IService>().LifeStyle.Transient.At(REMOTE_ADDR, SERVICE_KEY));

            var proxy = m_localContainer.ResolveRemoteService<IService>(@".\private$\tmp");
            ITransport destinationTransport = new InProcTransport();
            destinationTransport.Init(@".\private$\tmp");

            // act
            proxy.Foo();

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
    }

    public class Base
    {
    }

    public class Derived : Base
    {
    }
}