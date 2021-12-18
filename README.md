# flowOSD

## Disclaimer

There are no warranties. Use this app at your own risk.

## About

flowOSD - is an open source application to show OSD messages on [ASUS ROG Flow x13](https://rog.asus.com/laptops/rog-flow/2021-rog-flow-x13-series/) notebooks. For work it requires only ASUS Optimization Service (which is installed with ASUS System Control Interface drivers). I wrote this app to avoid installing MyASUS and Armoury Crate utilites (this is my personal preference).

This app is tested on **GV301QH** model (120Hz WUXGA display). The proper functionality with other modifications are not guaranteed. 

flowOSD shows the following OSDs:

* Keyboard backlight level changing
* TouchPad is disabled/enabled
* Display refresh rate is changed
* CPU Boost is disabled/enabled (see below)
* Power source is changed (AC/DC)

In addition, flowOSD provides shortcuts for the following utilites:

* Disable/enable CPU Boost (Fn + F5). This option reduces the CPU speed, but can increase battery life time and decreases CPU temperature
* Disable/enable Display High Refresh Rate (Fn + F4). App also can control display refresh rate depending on power source (for example, set 60Hz if notebook is on battery power)
* Disables touchpad when notebook goes into the tablet mode

If ASUS Armoury Crate Interface is disabled (in BIOS), ROG key is used as Print Screen.