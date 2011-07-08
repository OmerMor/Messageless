using System;
using System.Messaging;
using System.Reactive.Linq;
using System.Runtime.Serialization.Formatters;
using System.Security.Principal;

namespace Messageless.Transport
{
    public class MsmqTransport : AbstractTransport
    {
        private MessageQueue m_messageQueue;
        private IObservable<TransportMessage> m_msgs;

        public override void Init(string path)
        {
            base.Init(path);

            m_messageQueue = getIncomingQueue(LocalPath);
            var receiveAsync = Observable.FromAsyncPattern<Message>(
                (cb,obj) => m_messageQueue.BeginReceive(MessageQueue.InfiniteTimeout, obj, cb), 
                m_messageQueue.EndReceive);

            m_msgs = Observable.Defer(receiveAsync)
                .Repeat()
                .Publish()
                .RefCount()
                .Select(msg => msg.Body)
                .OfType<TransportMessage>();
        }

        private static MessageQueue getIncomingQueue(string path)
        {
            createQueueIfNeeded(path);
            var queue = new MessageQueue(path, sharedModeDenyReceive: true, enableCache: false,
                                         accessMode: QueueAccessMode.Receive)
                        {
                            Formatter =
                                new BinaryMessageFormatter(FormatterAssemblyStyle.Full,
                                                           FormatterTypeStyle.TypesAlways)
                        };
            //queue.FormatName
            return queue;
        }

        public override void OnNext(TransportMessage value)
        {
            using (var mq = getOutgoingQueue(value.RecipientPath))
            {
                value.SenderPath = LocalPath;
                mq.Send(value);
            }
        }

        private static MessageQueue getOutgoingQueue(string path)
        {
            createQueueIfNeeded(path);
            return new MessageQueue(path, sharedModeDenyReceive: false,
                                    enableCache: true, accessMode: QueueAccessMode.Send)
                   {
                       Formatter =
                           new BinaryMessageFormatter(FormatterAssemblyStyle.Full,
                                                      FormatterTypeStyle.TypesAlways)
                   };
        }

        private static void createQueueIfNeeded(string path)
        {
            var mqExists = MessageQueue.Exists(path);
            if (mqExists) return;

            var queue = MessageQueue.Create(path, false);

            setFullPermissions(queue, WellKnownSidType.BuiltinAdministratorsSid);
            setFullPermissions(queue, WellKnownSidType.WorldSid);
            setFullPermissions(queue, WellKnownSidType.AnonymousSid);
        }

        private static void setFullPermissions(MessageQueue queue, WellKnownSidType wellKnownSidType)
        {
            var administratorsGroupName = new SecurityIdentifier(wellKnownSidType, null)
                .Translate(typeof(NTAccount))
                .ToString();
            queue.SetPermissions(administratorsGroupName, MessageQueueAccessRights.FullControl, AccessControlEntryType.Allow);
        }

        public override IDisposable Subscribe(IObserver<TransportMessage> observer)
        {
            return m_msgs.Subscribe(observer);
        }

        #region Implementation of IDisposable

        public override void Dispose()
        {
            Console.WriteLine("Disposing MQ");
            m_messageQueue.Dispose();

            base.Dispose();
        }

        #endregion
    }
}