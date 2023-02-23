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

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using flowOSD.Api;
using static Native;

sealed partial class Gpu : IGpu, IDisposable
{
    private readonly BehaviorSubject<bool> isEnabledSubject;

    private IAtk atk;
    private CompositeDisposable disposable;

    public Gpu(IAtk atk)
    {
        disposable = new CompositeDisposable();

        this.atk = atk;

        var isEnabled = IsGpuEnabled();

        isEnabledSubject = new BehaviorSubject<bool>(isEnabled);
        IsEnabled = isEnabledSubject.DistinctUntilChanged().AsObservable();
    }

    void IDisposable.Dispose()
    {
        if (disposable != null)
        {
            disposable.Dispose();
            disposable = null;
        }
    }

    public IObservable<bool> IsEnabled
    {
        get;
    }

    public void Enable()
    {
        if (!IsGpuEnabled())
        {
            atk.Set(GPU_ECO_MODE, 0);
            isEnabledSubject.OnNext(true);
        }
    }

    public void Disable()
    {
        if (IsGpuEnabled())
        {
            atk.Set(GPU_ECO_MODE, 1);
            isEnabledSubject.OnNext(false);
        }
    }

    public void Toggle()
    {
        if (IsGpuEnabled())
        {
            Disable();
        }
        else
        {
            Enable();
        }
    }

    private bool IsGpuEnabled()
    {
        return atk.Get(GPU_ECO_MODE) == 0;
    }
}
