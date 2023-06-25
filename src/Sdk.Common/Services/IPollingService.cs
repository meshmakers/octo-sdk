using System;
using System.Threading.Tasks;

namespace Meshmakers.Octo.Sdk.Common.Services;

public interface IPollingService
{
    void AddCallback(TimeSpan interval, Func<Task> callback);

    void Start();

    void Stop();
}