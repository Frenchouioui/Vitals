using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Vitals.Models;

namespace Vitals.Hardware
{
    public interface IHardwareService : IDisposable
    {
        bool IsInitialized { get; }
        int CurrentInterval { get; }
        int CurrentUps { get; }
        int DetectedHardwareCount { get; }
        int DetectedStorageCount { get; }

        ObservableCollection<HardwareNode> HardwareNodes { get; }

        event EventHandler? TimerTick;
        event EventHandler<int>? UpsUpdated;
        event EventHandler<string>? ExpansionStateChanged;

        Task InitializeAsync(CancellationToken cancellationToken = default);
        Task BuildHardwareStructureAsync(CancellationToken cancellationToken = default);
        Task UpdateSensorValuesAsync(CancellationToken cancellationToken = default);
        Task ForceHardwareRedetectionWithUIAsync(CancellationToken cancellationToken = default);
        Task<string> GenerateDiagnosticReportAsync();

        void StartTimer();
        void StopTimer();
        void ChangeInterval(int newInterval);
    }
}

