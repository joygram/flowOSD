/*  Copyright © 2021, Albert Akhmetov <akhmetov@live.com>   
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
namespace flowOSD.Services
{
    using System;
    using System.ComponentModel;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Runtime.InteropServices;
    using static Native;

    sealed partial class PowerManagement : IDisposable
    {
        private static Guid GUID_ACDC_POWER_SOURCE = new Guid("5D3E9A59-E9D5-4B00-A6BD-FF34FF516548");

        private static Guid PROCESSOR_SUBGROUP = new Guid("54533251-82be-4824-96c1-47b60b740d00");
        private static Guid BOOST_SETTING = new Guid("be337238-0d82-4146-a960-4f3749d470c7");

        private CompositeDisposable disposable = new CompositeDisposable();

        private BehaviorSubject<bool> isBoostEnabledSubject;
        private BehaviorSubject<bool> isACSubject;

        private Guid activeScheme;

        public PowerManagement()
        {
            UpdateActiveScheme();

            if (!GetSystemPowerStatus(out SYSTEM_POWER_STATUS status))
            {
                throw new Win32Exception();
            }

            var isAC = status.ACLineStatus == 1;
            isACSubject = new BehaviorSubject<bool>(isAC);

            var isBoostEnabled = ReadValueIndex(ref PROCESSOR_SUBGROUP, ref BOOST_SETTING) != 0;
            isBoostEnabledSubject = new BehaviorSubject<bool>(isBoostEnabled);

            IsBoostEnabled = isBoostEnabledSubject.AsObservable();
            IsAC = isACSubject.AsObservable();

            new PowerSettingSubscription(BOOST_SETTING, HandlerCallback).DisposeWith(disposable);
            new PowerSettingSubscription(GUID_ACDC_POWER_SOURCE, HandlerCallback).DisposeWith(disposable);
        }

        void IDisposable.Dispose()
        {
            disposable?.Dispose();
            disposable = null;
        }

        public IObservable<bool> IsBoostEnabled { get; }

        public IObservable<bool> IsAC { get; }

        public void ToggleBoost()
        {
            if (isBoostEnabledSubject.Value)
            {
                DisableBoost();
            }
            else
            {
                EnableBoost();
            }
        }

        public void EnableBoost()
        {
            if (isBoostEnabledSubject.Value)
            {
                return;
            }

            WriteValueIndex(ref PROCESSOR_SUBGROUP, ref BOOST_SETTING, 2);

            PowerSetActiveScheme(IntPtr.Zero, ref activeScheme);
        }

        public void DisableBoost()
        {
            if (!isBoostEnabledSubject.Value)
            {
                return;
            }

            WriteValueIndex(ref PROCESSOR_SUBGROUP, ref BOOST_SETTING, 0);

            PowerSetActiveScheme(IntPtr.Zero, ref activeScheme);
        }

        private void UpdateActiveScheme()
        {
            IntPtr SchemeGuid = IntPtr.Zero;
            if (PowerGetActiveScheme(IntPtr.Zero, ref SchemeGuid) != ERROR_SUCCESS)
            {
                throw new Win32Exception();
            }
            try
            {
                activeScheme = (Guid)Marshal.PtrToStructure(SchemeGuid, typeof(Guid));
            }
            finally
            {
                LocalFree(SchemeGuid);
            }
        }

        private uint ReadValueIndex(ref Guid subgroup, ref Guid setting)
        {
            var value = default(uint);
            var errorCode = isACSubject.Value
                ? PowerReadACValueIndex(IntPtr.Zero, ref activeScheme, ref subgroup, ref setting, ref value)
                : PowerReadDCValueIndex(IntPtr.Zero, ref activeScheme, ref subgroup, ref setting, ref value);

            if (errorCode != ERROR_SUCCESS)
            {
                throw new Win32Exception((int)errorCode);
            }

            return value;
        }

        private void WriteValueIndex(ref Guid subgroup, ref Guid setting, uint value)
        {
            var errorCode = isACSubject.Value
                ? PowerWriteACValueIndex(IntPtr.Zero, ref activeScheme, ref subgroup, ref setting, value)
                : PowerWriteDCValueIndex(IntPtr.Zero, ref activeScheme, ref subgroup, ref setting, value);

            if (errorCode != 0)
            {
                throw new Win32Exception((int)errorCode);
            }
        }

        private int HandlerCallback(IntPtr context, int eventType, IntPtr setting)
        {
            const int PBT_POWERSETTINGCHANGE = 0x8013;

            if (eventType != PBT_POWERSETTINGCHANGE)
            {
                return 0;
            }

            if (Marshal.PtrToStructure(setting, typeof(POWERBROADCAST_SETTING)) is POWERBROADCAST_SETTING pbs)
            {
                if (pbs.PowerSetting == BOOST_SETTING)
                {
                    isBoostEnabledSubject.OnNext(pbs.Data != 0);
                }

                if (pbs.PowerSetting == GUID_ACDC_POWER_SOURCE)
                {
                    isACSubject.OnNext(pbs.Data == 0);
                }
            }

            return 0;
        }

        private sealed class PowerSettingSubscription : IDisposable
        {
            private DEVICENOTIFYPROC callback;
            private IntPtr handle;

            public PowerSettingSubscription(Guid setting, DEVICENOTIFYPROC callback)
            {
                const int DEVICE_NOTIFY_CALLBACK = 0x2;

                this.callback = callback;

                var errorCode = PowerSettingRegisterNotification(
                    ref setting,
                    DEVICE_NOTIFY_CALLBACK,
                    ref this.callback,
                    ref handle);
                if (errorCode != ERROR_SUCCESS)
                {
                    throw new Win32Exception((int)errorCode);
                }
            }

            ~PowerSettingSubscription()
            {
                Dispose(false);
            }

            private void Dispose(bool disposing)
            {
                if (handle != IntPtr.Zero)
                {
                    PowerSettingUnregisterNotification(handle);
                    handle = IntPtr.Zero;
                }
            }

            void IDisposable.Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }
    }
}