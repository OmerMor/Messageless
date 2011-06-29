using System;
using System.Linq;
using System.Reactive.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Messageless.Tests
{
    [TestFixture("MSMQ")]
    public class MsmqTransportTests
    {
        [Test]
        public void Transport_should_work()
        {
            const string queueName = @".\private$\test";
            using (var transport = new MsmqTransport())
            {
                transport.Init(queueName);

                var payload = new byte[] { 1, 2, 3, 4, 5, 6, 6, 6 };
                var firstMsg = transport.Where(message => Enumerable.SequenceEqual(message.Payload, payload)).Take(1);

                const string key = "key";
                transport.OnNext(new TransportMessage(payload, queueName, key));

                var msgReceived = firstMsg
                    .Timeout(TimeSpan.FromSeconds(1))
                    .First();
                msgReceived.Should().NotBeNull();
            }
        }
    }
}