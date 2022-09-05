/*  Copyright © 2021-2022, Albert Akhmetov <akhmetov@live.com>   
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

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using flowOSD.Api;

sealed partial class Audio
{
    private IAudioEndpointVolume GetMasterVolumeObject(EDataFlow dataFlow)
    {
        IMMDeviceEnumerator deviceEnumerator = null;
        IMMDevice mic = null;
        try
        {
            deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
            deviceEnumerator.GetDefaultAudioEndpoint(dataFlow, ERole.eMultimedia, out mic);

            Guid IID_IAudioEndpointVolume = typeof(IAudioEndpointVolume).GUID;
            mic.Activate(ref IID_IAudioEndpointVolume, 0, IntPtr.Zero, out object o);
            IAudioEndpointVolume masterVol = (IAudioEndpointVolume)o;

            return masterVol;
        }
        finally
        {
            if (mic != null) Marshal.ReleaseComObject(mic);
            if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
        }
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumerator
    {
    }

    private enum EDataFlow
    {
        eRender,
        eCapture,
        eAll,
        EDataFlow_enum_count
    }

    private enum ERole
    {
        eConsole,
        eMultimedia,
        eCommunications,
        ERole_enum_count
    }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        int NotImpl1();

        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppDevice);
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
    }

    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        int NotImpl1();

        int NotImpl2();

        int GetChannelCount(out UInt32 channelCount);

        int SetMasterVolumeLevel(float level, Guid eventContext);

        int SetMasterVolumeLevelScalar(float level, Guid eventContext);

        int GetMasterVolumeLevel(out float level);

        int GetMasterVolumeLevelScalar(out float level);

        int SetChannelVolumeLevel(UInt32 channelNumber, float level, Guid eventContext);

        int SetChannelVolumeLevelScalar(UInt32 channelNumber, float level, Guid eventContext);

        int GetChannelVolumeLevel(UInt32 channelNumber, out float level);

        int GetChannelVolumeLevelScalar(UInt32 channelNumber, out float level);

        int SetMute(Boolean isMuted, Guid eventContext);

        int GetMute(out Boolean isMuted);

        int GetVolumeStepInfo(out UInt32 step, out UInt32 stepCount);
    }
}