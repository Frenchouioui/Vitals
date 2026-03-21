using Vitals.Models;

namespace Vitals.Services
{
    public interface ISettingsService
    {
        AppSettings Settings { get; }
        void Load();
        void Save();
        void Reset();
    }
}

