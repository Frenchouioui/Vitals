using System;
using Vitals.Shared;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;

namespace Vitals.Services
{
    public class WindowService : IWindowService
    {
        private readonly ISettingsService _settingsService;
        private readonly ILogger _logger;

        public WindowService(ISettingsService settingsService, ILogger logger)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void RestoreWindowState(Window window)
        {
            if (window == null) return;

            try
            {
                var appWindow = GetAppWindow(window);
                if (appWindow == null) return;

                var settings = _settingsService.Settings;

                // Check for first launch or invalid position
                if (settings.WindowX == -1 || settings.WindowY == -1)
                {
                    _logger.LogInfo("First launch detected, centering window");
                    CenterWindow(window);
                }
                // Check if we have valid dimensions and the position is on a screen
                else if (settings.WindowWidth > 100 && settings.WindowHeight > 100)
                {
                    if (IsPositionOnScreen(settings.WindowX, settings.WindowY, settings.WindowWidth, settings.WindowHeight))
                    {
                        appWindow.MoveAndResize(new RectInt32(settings.WindowX, settings.WindowY, settings.WindowWidth, settings.WindowHeight));
                        _logger.LogInfo($"Window state restored to {settings.WindowX},{settings.WindowY} {settings.WindowWidth}x{settings.WindowHeight}");
                    }
                    else
                    {
                        // Position is off-screen, but size is valid. Restore size and center.
                        appWindow.Resize(new SizeInt32(settings.WindowWidth, settings.WindowHeight));
                        CenterWindow(window, preserveSize: true);
                        _logger.LogWarning("Window position was off-screen, centered while preserving size");
                    }
                }
                else
                {
                    CenterWindow(window);
                }

                if (settings.IsMaximized)
                {
                    (appWindow.Presenter as OverlappedPresenter)?.Maximize();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to restore window state", ex);
                CenterWindow(window);
            }
        }

        public void SaveWindowState(Window window)
        {
            if (window == null) return;

            try
            {
                var appWindow = GetAppWindow(window);
                if (appWindow == null) return;

                var settings = _settingsService.Settings;
                var presenter = appWindow.Presenter as OverlappedPresenter;

                // ONLY save if the window is in normal state (not maximized or minimized)
                if (presenter?.State == OverlappedPresenterState.Restored)
                {
                    settings.WindowX = appWindow.Position.X;
                    settings.WindowY = appWindow.Position.Y;
                    settings.WindowWidth = appWindow.Size.Width;
                    settings.WindowHeight = appWindow.Size.Height;
                    _logger.LogInfo($"Window state saved: {settings.WindowX},{settings.WindowY} {settings.WindowWidth}x{settings.WindowHeight}");
                }

                settings.IsMaximized = presenter?.State == OverlappedPresenterState.Maximized;

                _settingsService.Save();
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to save window state", ex);
            }
        }

        public void CenterWindow(Window window) => CenterWindow(window, false);

        private void CenterWindow(Window window, bool preserveSize)
        {
            if (window == null) return;

            try
            {
                var appWindow = GetAppWindow(window);
                if (appWindow == null) return;

                var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
                if (displayArea == null) return;

                var workArea = displayArea.WorkArea;
                
                if (!preserveSize)
                {
                    var adaptiveSize = CalculateAdaptiveSize(workArea);
                    appWindow.Resize(adaptiveSize);
                }

                int centerX = workArea.X + (workArea.Width - appWindow.Size.Width) / 2;
                int centerY = workArea.Y + (workArea.Height - appWindow.Size.Height) / 2;

                appWindow.Move(new PointInt32(centerX, centerY));
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to center window", ex);
            }
        }

        public SizeInt32 CalculateAdaptiveSize(RectInt32 workArea)
        {
            int width = (int)(workArea.Width * 0.8);
            int height = (int)(workArea.Height * 0.8);
            
            const int maxWidth = 1200;
            const int maxHeight = 900;
            const int minWidth = 600;
            const int minHeight = 400;
            
            width = Math.Clamp(width, minWidth, maxWidth);
            height = Math.Clamp(height, minHeight, maxHeight);
            
            return new SizeInt32(width, height);
        }

        internal static AppWindow? GetAppWindow(Window window)
        {
            try
            {
                var hWnd = WindowNative.GetWindowHandle(window);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
                return AppWindow.GetFromWindowId(windowId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool IsPositionOnScreen(int x, int y, int width, int height)
        {
            try
            {
                // Check if the top-left corner is on any screen
                foreach (var displayArea in DisplayArea.FindAll())
                {
                    var workArea = displayArea.WorkArea;
                    
                    // We check if at least a decent portion of the window title bar (top area) is visible
                    // This is more robust than checking if the exact (x,y) is inside.
                    if (x + width > workArea.X + 50 && x < workArea.X + workArea.Width - 50 &&
                        y + 32 > workArea.Y && y < workArea.Y + workArea.Height - 32)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to check screen position: {ex.Message}");
                return true; // Default to true to try and restore anyway
            }
        }
    }
}

