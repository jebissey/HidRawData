namespace Djlastnight.Hid;

using Djlastnight.Win32.Win32RawInput;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;

/// <summary>
/// The HidRawData.dll main class. It is intended to read the raw data from all the connected HID devices.
/// </summary>
public class HidDataReader : IDisposable
{
    private readonly Window window;
    private HidHandler handler;

    /// <summary>
    /// Creates new HID data reader.
    /// </summary>
    /// <param name="window">Window instance, which will reveice the WM_INPUT messages</param>
    public HidDataReader(Window window)
    {
        this.window = window ?? throw new ArgumentNullException("window");
        window.Closed += OnWindowClosed;
        var handle = new WindowInteropHelper(window).Handle;
        var source = HwndSource.FromHwnd(handle);
        source.AddHook(WndProc);

        var devices = RawInputHelper.GetDevices();

        int i = 0;
        RAWINPUTDEVICE[] rids = new RAWINPUTDEVICE[devices.Count];

        // Setting handle to each rid device to receive the WM_INPUT message
        foreach (var device in devices)
        {
            rids[i].usUsagePage = device.UsagePage;
            rids[i].usUsage = device.UsageCollection;
            rids[i].dwFlags = RawInputDeviceFlags.RIDEV_INPUTSINK;
            rids[i].hwndTarget = handle;
            i++;
        }

        handler = new HidHandler(rids);
        handler.OnHidEvent += OnHidEvent;
    }

    /// <summary>
    /// Raised, when data from HID device is received
    /// </summary>
    public event HidEventHandler HidDataReceived;

    public static List<Device> GetDevices()
    {
        var devices = RawInputHelper.GetDevices();
        return devices;
    }

    public void Dispose()
    {
        if (handler != null)
        {
            handler.OnHidEvent -= OnHidEvent;
            handler.Dispose();
            handler = null;

            if (window != null)
            {
                var windowHandle = new WindowInteropHelper(window).Handle;
                if (windowHandle != IntPtr.Zero)
                {
                    var source = HwndSource.FromHwnd(windowHandle);
                    source.RemoveHook(WndProc);
                }
            }
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wordParam, IntPtr longParam, ref bool handled)
    {
        if (window == null || msg != Djlastnight.Win32.Contants.WM_INPUT)
        {
            return IntPtr.Zero;
        }

        var message = new System.Windows.Forms.Message();
        message.HWnd = hwnd;
        message.Msg = msg;
        message.WParam = wordParam;
        message.LParam = longParam;
        handler.ProcessInput(ref message);

        return IntPtr.Zero;
    }

    private void OnHidEvent(object sender, HidEvent e)
    {
        if (HidDataReceived == null ||
            window == null ||
            window.Dispatcher == null)
        {
            return;
        }

        window.Dispatcher.BeginInvoke((Action)delegate
        {
            HidDataReceived(sender, e);
        });
    }

    private void OnWindowClosed(object sender, EventArgs e)
    {
        Dispose();
    }
}