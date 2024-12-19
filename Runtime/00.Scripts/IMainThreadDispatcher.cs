using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Hian.Threading
{
    /// <summary>
    /// Unity 메인 스레드에서 작업을 실행하기 위한 인터페이스입니다.
    /// </summary>
    public interface IMainThreadDispatcher
    {
        /// <summary>
        /// 작업을 메인 스레드 큐에 추가합니다.
        /// </summary>
        /// <param name="action">실행할 작업</param>
        void Enqueue(Action action);

        /// <summary>
        /// 코루틴을 메인 스레드 큐에 추가합니다.
        /// </summary>
        /// <param name="coroutine">실행할 코루틴</param>
        void Enqueue(IEnumerator coroutine);

        /// <summary>
        /// 작업을 메인 스레드에서 비동기적으로 실행합니다.
        /// </summary>
        /// <param name="action">실행할 작업</param>
        /// <returns>작업 완료를 나타내는 Task</returns>
        Task EnqueueAsync(Action action);

        /// <summary>
        /// 작업을 메인 스레드에서 비동기적으로 실행하고 결과를 반환합니다.
        /// </summary>
        /// <typeparam name="T">반환 값의 타입</typeparam>
        /// <param name="function">실행할 함수</param>
        /// <returns>함수의 결과를 포함하는 Task</returns>
        Task<T> EnqueueAsync<T>(Func<T> function);

        /// <summary>
        /// 현재 큐에 있는 모든 작업을 취소합니다.
        /// </summary>
        void ClearQueue();

        /// <summary>
        /// 현재 큐에 있는 작업의 수를 반환합니다.
        /// </summary>
        int QueuedActions { get; }

#if UNITY_2023_1_OR_NEWER
        /// <summary>
        /// Unity Awaitable 작업을 메인 스레드에서 실행합니다.
        /// </summary>
        /// <param name="awaitable">실행할 Unity Awaitable 작업</param>
        /// <typeparam name="T">Awaitable 결과 타입</typeparam>
        /// <returns>작업의 결과를 포함하는 Task</returns>
        Task<T> EnqueueAwaitable<T>(Awaitable<T> awaitable);

        /// <summary>
        /// Unity Awaitable 작업을 메인 스레드에서 실행합니다.
        /// </summary>
        /// <param name="awaitable">실행할 Unity Awaitable 작업</param>
        /// <returns>작업 완료를 나타내는 Task</returns>
        Task EnqueueAwaitable(Awaitable awaitable);
#endif

        void Post(Action action, object state);
    }
}
