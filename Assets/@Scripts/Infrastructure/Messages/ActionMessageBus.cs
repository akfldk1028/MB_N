using System;
using System.Collections.Generic;
using MB.Infrastructure.Messages;
using UnityEngine;

namespace MB.Infrastructure.Messages
{
    public sealed class ActionMessageBus : IMessageChannel<ActionMessage>
    {
        private readonly MessageChannel<ActionMessage> _channel = new MessageChannel<ActionMessage>();

        public bool IsDisposed => _channel.IsDisposed;

        public void Publish(ActionMessage message)
        {
            if (IsDisposed)
            {
                Debug.LogWarning("[ActionMessageBus] Tried to publish after disposal.");
                return;
            }

            // ✅ 디버깅: System_Update 발행 확인 (첫 5프레임)
            if (message.Id == ActionId.System_Update && Time.frameCount <= 5)
            {
                Debug.Log($"[ActionMessageBus] System_Update 발행! (프레임: {Time.frameCount})");
            }

            _channel.Publish(message);
        }

        public IDisposable Subscribe(Action<ActionMessage> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            return _channel.Subscribe(handler);
        }

        public IDisposable Subscribe(ActionId actionId, Action handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            return Subscribe(actionId, _ => handler());
        }

        public IDisposable Subscribe(ActionId actionId, Action<ActionMessage> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            // ✅ 디버깅: System_Update 구독 확인
            if (actionId == ActionId.System_Update)
            {
                Debug.Log($"[ActionMessageBus] System_Update 구독됨! Handler: {handler.Method.Name}");
            }

            return _channel.Subscribe(message =>
            {
                if (message.Id == actionId)
                {
                    // ✅ 디버깅: System_Update 메시지 수신 확인 (첫 5프레임)
                    if (actionId == ActionId.System_Update && Time.frameCount <= 5)
                    {
                        Debug.Log($"[ActionMessageBus] System_Update 수신! → {handler.Method.Name} 호출");
                    }
                    handler(message);
                }
            });
        }

        public IDisposable Subscribe(Action<ActionMessage> handler, params ActionId[] actionIds) =>
            Subscribe((IEnumerable<ActionId>)actionIds, handler);

        public IDisposable Subscribe(IEnumerable<ActionId> actionIds, Action handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            return Subscribe(actionIds, _ => handler());
        }

        public IDisposable Subscribe(IEnumerable<ActionId> actionIds, Action<ActionMessage> handler)
        {
            if (actionIds == null) throw new ArgumentNullException(nameof(actionIds));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var filter = new HashSet<ActionId>(actionIds);
            if (filter.Count == 0)
                throw new ArgumentException("At least one ActionId is required.", nameof(actionIds));

            return _channel.Subscribe(message =>
            {
                if (filter.Contains(message.Id))
                {
                    handler(message);
                }
            });
        }

        public void Unsubscribe(Action<ActionMessage> handler)
        {
            if (handler == null) return;
            _channel.Unsubscribe(handler);
        }

        public void Dispose() => _channel.Dispose();
    }
}


