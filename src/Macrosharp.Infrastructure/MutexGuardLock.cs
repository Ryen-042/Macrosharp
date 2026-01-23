using ErrorOr;

namespace Macrosharp.Infrastructure;

/// <summary>
/// Manages the acquisition of named mutexes to ensure that resources are not concurrently accessed.
/// </summary>
public class MutexGuardLock
{
    /// <summary>Attempts to acquire a named mutex. If the mutex is already acquired, a conflict error is returned.</summary>
    /// <param name="mutexName">The name of the mutex to acquire.</param>
    /// <returns>An <see cref="ErrorOr{T}"/> containing the acquired <see cref="Mutex"/> if successful, or an error if the mutex is already acquired.</returns>
    public static ErrorOr<Mutex> AcquireMutex(string mutexName)
    {
        var mutex = new Mutex(true, mutexName, out bool newMutexCreated);

        if (!newMutexCreated)
        {
            AudioPlayer.PlayAudio("denied.wav");
            return Error.Conflict("InUse", $"Mutex {mutexName} already acquired.");
        }

        return mutex;
    }
}
