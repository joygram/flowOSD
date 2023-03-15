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

namespace flowOSD.Hardware;

using System.ComponentModel;
using System.Management;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using flowOSD.Api.Hardware;
using flowOSD.Extensions;
using Microsoft.Win32.SafeHandles;
using static flowOSD.Native.Kernel32;

sealed partial class Atk : IDisposable, IAtk
{
    public const int FEATURE_KBD_REPORT_ID = 0x5a;

    private const uint IO_CONTROL_CODE = 0x0022240C;

    const uint DSTS = 0x53545344;
    const uint DEVS = 0x53564544;

    const uint DEVID_GPU_ECO_MODE = 0x00090020;
    const uint DEVID_THROTTLE_THERMAL_POLICY = 0x00120075;

    public const uint CPU_Fan = 0x00110013;
    public const uint GPU_Fan = 0x00110014;

    private readonly BehaviorSubject<PerformanceMode> performanceModeSubject;
    private readonly BehaviorSubject<GpuMode> gpuModeSubject;

    private SafeFileHandle handle;

    private CompositeDisposable disposable = new CompositeDisposable();
    private readonly object ControlLocker = new object();

    public Atk(PerformanceMode? performanceMode)
    {
        handle = CreateFile(
            @"\\.\\ATKACPI",
            FileAccess.ReadWrite,
            FileShare.ReadWrite,
            IntPtr.Zero,
            FileMode.Open,
            FILE_ATTRIBUTE_NORMAL,
            IntPtr.Zero
        ).DisposeWith(disposable);

        if (handle.IsInvalid)
        {
            throw new ApplicationException("Can't connect to ACPI.");
        }

        performanceModeSubject = new BehaviorSubject<PerformanceMode>(performanceMode ?? Api.Hardware.PerformanceMode.Default);
        gpuModeSubject = new BehaviorSubject<GpuMode>((GpuMode)Get(DEVID_GPU_ECO_MODE));

        PerformanceMode = performanceModeSubject.AsObservable();
        GpuMode = gpuModeSubject.AsObservable();

        SetPerformanceMode(performanceMode ?? Api.Hardware.PerformanceMode.Default);
    }

    public IObservable<PerformanceMode> PerformanceMode { get; }

    public IObservable<GpuMode> GpuMode { get; }

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

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

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
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return outBuffer;
        }
    }
}