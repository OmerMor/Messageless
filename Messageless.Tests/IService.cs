using System;

namespace Messageless.Tests
{
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
        void MethodWithGenericCallback<T>(Action<T> callback);
    }
}