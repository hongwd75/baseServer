using System.Runtime.InteropServices;

namespace Project.Base;

/// <summary>
/// 다음은 효율적인 스핀-대기 락 구현에 대한 설명입니다.
///     주의사항
///     이 구조체는 값 형식이므로 클래스의 필드로 사용될 때 매우 효율적으로 작동합니다.
///     박싱을 피하세요. 그렇지 않으면 스레드 안전성을 잃을 수 있습니다.
///     이 구조체는 Jeffrey Richter의 2005년 10월 MSDN 매거진 기사 "Concurrent Affairs"를 기반으로 합니다.
/// </remarks>
public struct SpinWaitLock
{
    private const int LockFree = 0;
    private const int LockOwned = 1;
    private static readonly bool IsSingleCpuMachine = (Environment.ProcessorCount == 1);
    private int _lockState; // Defaults to 0=LockFree

    public void Enter()
    {
        Thread.BeginCriticalRegion();

        while (true)
        {
            // If resource available, set it to in-use and return
            if (Interlocked.Exchange(ref _lockState, LockOwned) == LockFree)
                return;

            // Efficiently spin, until the resource looks like it might 
            // be free. NOTE: Just reading here (as compared to repeatedly 
            // calling Exchange) improves performance because writing 
            // forces all CPUs to update this value
            while (Thread.VolatileRead(ref _lockState) == LockOwned)
            {
                StallThread();
            }
        }
    }

    public void Exit()
    {
        // Mark the resource as available
        Interlocked.Exchange(ref _lockState, LockFree);
        Thread.EndCriticalRegion();
    }

#if LINUX
        private static void StallThread()
        {
            //Linux doesn't support SwitchToThread()
            Thread.SpinWait(1);
        }
#else
    private static void StallThread()
    {
        if (IsSingleCpuMachine)
        {
            // On a single-CPU system, spinning does no good
            SwitchToThread();
        }
        else
        {
            // Multi-CPU system might be hyper-threaded, let other thread run
            Thread.SpinWait(1);
        }
    }

    [DllImport("kernel32", ExactSpelling = true)]
    private static extern void SwitchToThread();
#endif
}