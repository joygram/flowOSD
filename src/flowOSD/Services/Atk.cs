/*  Copyright © 2021-2023, Albert Akhmetov <akhmetov@live.com>   
 *
 *  This file is part of flowOSD.
 *
 *  flowOSD is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  flowOSD is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with flowOSD. If not, see <https://www.gnu.org/licenses/>.   
 *
 */
namespace flowOSD.Services;

using System.ComponentModel;
using System.Management;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using flowOSD.Api;
using static Native;

sealed partial class Atk : IAtk, IDisposable
{
    public readonly static int WM_ACPI = (int)RegisterWindowMessage("ACPI Notification through ATKHotkey from BIOS");

    private const int AK_BACKLIGHT_DOWN = 0xC5;
    private const int AK_BACKLIGHT_UP = 0xC4;
    private const int AK_AURA = 0xB3;
    private const int AK_FAN = 0xAE;
    private const int AK_TOUCHPAD = 0x6B;
    private const int AK_ROG = 0x38;
    private const int AK_MUTE_MIC = 0x7C;
    private const int AK_FN_C = 0x9E;
    private const int AK_FN_V = 0x8A;
    private const int AK_TABLET_STATE = 0xBD;
    private const int AK_CHARGER = 0x7B;

    private const uint IO_CONTROL_CODE = 0x0022240C;

    const uint DSTS = 0x53545344;
    const uint DEVS = 0x53564544;

    const uint DEVID_GPU_ECO_MODE = 0x00090020;
    const uint DEVID_THROTTLE_THERMAL_POLICY = 0x00120075;
    const uint DEVID_CHARGER = 0x0012006c;
    const uint DEVID_TABLET = 0x00060077;

    public const uint CPU_Fan = 0x00110013;
    public const uint GPU_Fan = 0x00110014;

    const int POWER_SOURCE_BATTERY = 0x00;
    const int POWER_SOURCE_LOW = 0x22;
    const int POWER_SOURCE_FULL = 0x2A;

    private readonly Dictionary<int, AtkKey> codeToKey;
    private readonly Subject<AtkKey> keyPressedSubject;
    private readonly BehaviorSubject<PerformanceMode> performanceModeSubject;
    private readonly BehaviorSubject<GpuMode> gpuModeSubject;
    private readonly BehaviorSubject<ChargerType> chargerTypeSubject;
    private readonly BehaviorSubject<TabletMode> tabletModeSubject;

    private readonly CountableSubject<uint> cpuTemperatureSubject;

    private IntPtr handle;

    private CompositeDisposable disposable = new CompositeDisposable();
    private readonly object ControlLocker = new object();

    private IDisposable updateSubscription;

    public Atk(PerformanceMode performanceMode, IMessageQueue messageQueue)
    {
        handle = CreateFile(
            @"\\.\\ATKACPI",
            GENERIC_READ | GENERIC_WRITE,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_ATTRIBUTE_NORMAL,
            IntPtr.Zero
        );

        if (handle == -1)
        {
            throw new ApplicationException("Can't connect to ACPI.");
        }

        codeToKey = new Dictionary<int, AtkKey>();
        codeToKey[AK_BACKLIGHT_DOWN] = AtkKey.BacklightDown;
        codeToKey[AK_BACKLIGHT_UP] = AtkKey.BacklightUp;
        codeToKey[AK_AURA] = AtkKey.Aura;
        codeToKey[AK_FAN] = AtkKey.Fan;
        codeToKey[AK_TOUCHPAD] = AtkKey.TouchPad;
        codeToKey[AK_ROG] = AtkKey.Rog;
        codeToKey[AK_MUTE_MIC] = AtkKey.MuteMic;
        codeToKey[AK_FN_C] = AtkKey.Copy;
        codeToKey[AK_FN_V] = AtkKey.Paste;

        keyPressedSubject = new Subject<AtkKey>();
        performanceModeSubject = new BehaviorSubject<PerformanceMode>(performanceMode);
        gpuModeSubject = new BehaviorSubject<GpuMode>((GpuMode)Get(DEVID_GPU_ECO_MODE));
        chargerTypeSubject = new BehaviorSubject<ChargerType>(GetChargerType());
        tabletModeSubject = new BehaviorSubject<TabletMode>(GetTabletMode());

        cpuTemperatureSubject = new CountableSubject<uint>(GetCpuTemperature());

        KeyPressed = keyPressedSubject.Throttle(TimeSpan.FromMilliseconds(5)).AsObservable();
        PerformanceMode = performanceModeSubject.AsObservable();
        GpuMode = gpuModeSubject.AsObservable();
        ChargerType = chargerTypeSubject.AsObservable();
        TabletMode = tabletModeSubject.AsObservable();
        CpuTemperature = cpuTemperatureSubject.AsObservable();

        cpuTemperatureSubject.Count
            .Subscribe(sum =>
            {
                if (sum == 0 && updateSubscription != null)
                {
                    updateSubscription.Dispose();
                    updateSubscription = null;
                }

                if (sum > 0 && updateSubscription == null)
                {
                    updateSubscription = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(_ => UpdateCpuTemperature());
                }
            })
            .DisposeWith(disposable);

        messageQueue.Subscribe(WM_ACPI, ProcessMessage).DisposeWith(disposable);

        SetPerformanceMode(performanceMode);
    }

    ~Atk()
    {
        Dispose(false);
    }

    void IDisposable.Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public IObservable<AtkKey> KeyPressed { get; }

    public IObservable<PerformanceMode> PerformanceMode { get; }

    public IObservable<GpuMode> GpuMode { get; }

    public IObservable<ChargerType> ChargerType { get; }

    public IObservable<TabletMode> TabletMode { get; }

    public IObservable<uint> CpuTemperature { get; }

    public int Get(uint deviceId)
    {
        var args = new byte[8];
        BitConverter.GetBytes(deviceId).CopyTo(args, 0);

        return BitConverter.ToInt32(Invoke(DSTS, args), 0) - 65536;
    }

    public void Set(uint deviceId, uint status)
    {
        var args = new byte[8];
        BitConverter.GetBytes(deviceId).CopyTo(args, 0);
        BitConverter.GetBytes(status).CopyTo(args, 4);

        Invoke(DEVS, args);
    }

    public void SetPerformanceMode(PerformanceMode performanceMode)
    {
        Set(DEVID_THROTTLE_THERMAL_POLICY, (uint)performanceMode);

        performanceModeSubject.OnNext(performanceMode);
    }

    public void SetGpuMode(GpuMode gpuMode)
    {
        var currentGpuMode = (GpuMode)Get(DEVID_GPU_ECO_MODE);

        if (currentGpuMode != gpuMode)
        {
            Set(DEVID_GPU_ECO_MODE, (uint)gpuMode);
            gpuModeSubject.OnNext(gpuMode);
        }
    }

    private void Dispose(bool disposing)
    {
        if (disposing && disposable != null)
        {
            disposable?.Dispose();
            disposable = null;
        }

        if (handle >= 0)
        {
            CloseHandle(handle);
            handle = -1;
        }
    }

    private void ProcessMessage(int messageId, IntPtr wParam, IntPtr lParam)
    {
        if (messageId == WM_ACPI)
        {
            var code = (int)wParam;

            if (code == AK_TABLET_STATE)
            {
                tabletModeSubject.OnNext(GetTabletMode());
            }
            else if (code == AK_CHARGER)
            {
                chargerTypeSubject.OnNext(GetChargerType());
            }
            else if (codeToKey.ContainsKey(code))
            {
                keyPressedSubject.OnNext(codeToKey[code]);
            }
        }
    }

    private byte[] Invoke(uint MethodId, byte[] args)
    {
        lock (ControlLocker)
        {
            var acpiBuffer = new byte[8 + args.Length];
            var outBuffer = new byte[20];

            BitConverter.GetBytes(MethodId).CopyTo(acpiBuffer, 0);
            BitConverter.GetBytes(args.Length).CopyTo(acpiBuffer, 4);
            Array.Copy(args, 0, acpiBuffer, 8, args.Length);

            uint lpBytesReturned = 0;
            if (!DeviceIoControl(
                handle,
                IO_CONTROL_CODE,
                acpiBuffer,
                (uint)acpiBuffer.Length,
                outBuffer,
                (uint)outBuffer.Length,
                ref lpBytesReturned,
                IntPtr.Zero))
            {
                throw new Win32Exception((int)GetLastError());
            }

            return outBuffer;
        }
    }

    private TabletMode GetTabletMode()
    {
        return (TabletMode)Get(DEVID_TABLET);
    }

    private ChargerType GetChargerType()
    {
        switch (Get(DEVID_CHARGER))
        {
            case POWER_SOURCE_BATTERY:
                return Api.ChargerType.None;

            case POWER_SOURCE_LOW:
                return Api.ChargerType.LowPower;

            case POWER_SOURCE_FULL:
                return Api.ChargerType.FullPower;

            default:
                throw new NotSupportedException("Charger type isn't supported");
        }
    }

    private void UpdateCpuTemperature()
    {
        cpuTemperatureSubject.OnNext(GetCpuTemperature());
    }

    private uint GetCpuTemperature()
    {
        var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PerfFormattedData_Counters_ThermalZoneInformation");
        foreach (ManagementObject obj in searcher.Get())
        {
            if (obj["Temperature"] is uint temperature)
            {
                return temperature - 273;
            }
        }

        return 0;
    }
}