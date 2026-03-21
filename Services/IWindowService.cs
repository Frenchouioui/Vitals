using Microsoft.UI.Xaml;

namespace Vitals.Services
{
    public interface IWindowService
    {
        void RestoreWindowState(Window window);
        void SaveWindowState(Window window);
        void CenterWindow(Window window);
    }
}

