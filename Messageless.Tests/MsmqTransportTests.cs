using System;
using System.Linq;
using System.Reactive.Linq;
using FluentAssertions;
using Messageless.Transport;
using NUnit.Framework;

namespace Messageless.Tests
{
    [Category("MSMQ")]
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
                var firstMsg = transport.Where(message => message.Payload.SequenceEqual(payload)).Take(1);

                transport.OnNext(new TransportMessage(payload, queueName));

                var msgReceived = firstMsg
                    .Timeout(TimeSpan.FromSeconds(1))
                    .First();
                msgReceived.Should().NotBeNull();
            }
        }
    }
}