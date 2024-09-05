<hr />
<p align="center">
<img id="charmsbarPort" src="resource/logo.png"/>
</p>
<hr />

<blockquote>
"Your most unhappy customers are your greatest source of learning."<br />— Bill Gates
</blockquote>

[![GitHub release](https://img.shields.io/github/release/Icepenguins101/charms-bar-port/all.svg)](https://github.com/Icepenguins101/charms-bar-port/releases)
[![GitHub all releases](https://img.shields.io/github/downloads/Icepenguins101/charms-bar-port/total.svg)](https://github.com/Icepenguins101/charms-bar-port/releases)
[![GitHub stars](https://img.shields.io/github/stars/Icepenguins101/charms-bar-port.svg)](https://github.com/Icepenguins101/charms-bar-port/stargazers)
[![Documentation](https://img.shields.io/badge/Docs-WIP-red.svg)](https://github.com/Icepenguins101/charms-bar-port/wiki)
[![Issues](https://img.shields.io/github/issues/Icepenguins101/charms-bar-port.svg)](https://github.com/Icepenguins101/charms-bar-port/issues)

## Contents
- [About](#about)
- [How does it work?](#how-does-it-work)
- [Requirements](#requirements)
- [Features](#features)
- [TODOs](#todos)
- [Supported languages](#supported-languages)
- [Screenshots](#screenshots)
- [Q&As](#qas)
- [Disclaimer](#disclaimer)
- [Support](#support)
  
## About
**Charms Bar Port** will help you bring back the Windows 8.x Charms Bar to Windows 10 and Windows 11.

Forked and edited from [this project](https://github.com/Icepenguins101/charms-bar-port), which is a fork of [CharmsBarRevived](https://github.com/Jerhynh/CharmsBarRevived).

## How does it work?

Using a bunch of Windows methods, functions for the bar's functions.

## Requirements

* Windows 10 17763 or higher. Windows 11 is in fact 10 with WinUI 3 as primary GUI toolkit...
* <a href="https://dotnet.microsoft.com/en-us/download/dotnet/8.0">.NET 8.0</a>
* Visual Studio 2022 for building from source.

## Features

* Touch-friendly
* Multi-monitor support
* Portable
* Localizations
* 12/24 hours clock
* Windows 7/8 Beta/8.x/11 icons
* Customizable: see below

## TODOs

* Futher optimize CharmsBar.xaml.cs
* More customizations
* Remove WinForms usage, if able to
* Finish in-code TODOs
* CharmsClock: Merge all things in the same place into one

## Supported languages

* English
* German (Deutsch)
* Japanese (日本語)
* Vietnamese
* Polish (thank you [@oliik2013](https://github.com/oliik2013)!)

Want to add more? Look at #Support below.

## Screenshots

<img src="resource/preview.png"/>
<img src="resource/previewdark.png"/>
<img src="resource/previewhighcontrast.png"/>

Matches the original isn't it:)?

## Download

Go to Actions tab of the repository and get the latest successful (and unexpired) build.

For the best experience use Release variant.

## Customizations

> TODO: More Windows 8 registry keys, or custom one?
> This is initial work. Please find the original version of this project above for the exact things to change.

Everything is stored in ``HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell\EdgeUi`` registry key.

Don't mess with anything there that you do NOT know what they do. Remember?

We have no responsibility for any damage you've done to your computer by messing with Registry.

* `DisableCharmsHint`: Do not peek the charm bar on mouse hover. A DWORD.
* `AccentColor`: Accent color for... the start button. A hex string. Not yet implemented.
* `Delay`: Delay (in milliseconds) for the charm bar to be show (both normal and peek)
* `Use24HoursFormat`: Force 24 hours clock format. A DWORD.

For options that only do true-false task (e.g `DisableCharmsHint`), they use DWORD type, setting to 1 will enable that option, 0 will do the opposite.

Some changes are immediately taken, else require a program restart.

## Q&As
Q: Why do you "revive" this failure?
A:
	- We love it. Just that. Don't waste time arguing about somebody else's interest (unless it's not something right to the world and the law)
	- There are other things that do the same but most are unmaintained

Saying Charm Bar a failure is true at some point - at least to me :
* Who would move the mouse to right corners of the screen just to open Start?
* Who would open Settings with Charm Bar while we literally have a shortcut (is that... true?) for that?

The answer for these would be touch screens.

But to be fair, we're here not just to make a 1:1 (or closer) clone, but add more things to it right?

Q: Why does this require my run it as administrator?

A: For registry access (some don't require admin though). Currently you don't have to run as admin.

Q: Are forked repositories the complete edition of Charms Bar Port?

A: Yes or no, depending on the work on that repository. Always be careful when you surf the internet.

Q: When will this be released?

A: No ETA.

Q: I'm using a touch screen, why does the Action Center always open with the charms bar?

A: This is because the action center uses the same gesture. Do what you want & need.

Q: Win+C is taken, can you use another hotkey?

A: No, unless you edit the source code yourself.

Q: Is this safe to use?

A: Yes, it should be. But still be careful with stuff on internet.

Q: Why are the animations stiff?

A: I'm new to C#, so the animations may not match.

Q: Why does this program not support Windows 7 and Windows 8.1?

A: This program is not meant to be made for Windows 7 (not tested). 8/8.1? You already have the REAL one there.

Q: How does multi-monitor support work?

A: If you have two or more monitors, moving your mouse to the next monitor(s) will increase the activeScreen parameter (activeScreen = 0 is monitor 1, activeScreen = 1 is monitor 2, vice versa), forcing the Charms Bar to be moved over to the next screen. If it's activated by mouse but not completely "spread-out", moving to the next monitor will force the Charms Bar to deactivate, to fix a bug that the original version had (if you activated it on monitor 1 and moved your cursor to monitor 2 it will stay on the screen).

Q: I'm trying to ALT+F4 the program but it won't let me. Why?

A: No you don't... Find another ways, like Task Manager or `taskkill` command.

Q: Why is it lagging on my machine?

A: C# and WinForm skill issues. Just that.

Q: Why is it flickering on my machine?

A: This is a hardware specific problem. I'm planning to outsource this program to another developer to see if they can fix this better than I can. If you want to assist, please <a href="mailto:jaydenwmontoya@icloud.com">email me</a>.

Q: Can I fork this repository to release your work now?

A: No. You can only fork it to make changes and released developer builds are not to be used for local purposes.

Q: Will you do more ports from Windows 8.1?

A: I really would love to make more ports from Windows 8.1 as I'm considering to make an App Switcher and Start Screen ports, but I would really need assistance for the start screen port to replace the default Windows start menu (or screen, if you're in 10 with the "fullscreen Start" option switched on).

Q: Will there be a version for Mac OS X and Linux?

A: **No.** Microsoft owns full ownership of the charms bar and it would be infringement to create Charms Bar Port on those systems.

Actually there are some attempts:

* [Retiled for Linux, though not Charm Bar](https://codeberg.org/DrewNaylor/Retiled) - primary for Mobile devices (can't run Android apps there? Use Waydroid!)
* [This Rofi theme, for Linux too](https://github.com/Dartegnian/rofi-metro)
* And a lot of Android launchers which mimics Windows 8.x/10 Start Screen, and even MetroUI!

Q: How can I contact you?

A: You can <a href="mailto:jaydenwmontoya@icloud.com">email me</a> for any assistance regarding Charms Bar Port and other products I have created.

## Disclaimer
I'm not officially affiliated with Microsoft; I moved to Mac OS X in March 3rd of 2017 for better stability and UI, and have temporarily returned to Windows 10 for better performance using web development with the announcement of macOS Sonoma. I will still be using Mac OS X as a daily driver, so there may not be enough focus given to Charms Bar Port.

## Support
Are you a fan of the Charms Bar Port program and want to help out? here are some options...

#### Programmer
Code contributions are welcome. If you are able to port Windows 8.1 features better than I can, or if you want to improve some features (especially multi-monitor support), please <a href="mailto:jaydenwmontoya@icloud.com">email me</a>.

#### Localization
Help translate Charms Bar Port to more languages. If there's a language that isn't present in Charms Bar Port please <a href="mailto:jaydenwmontoya@icloud.com">email me</a>.

Or:

1: Copy Strings.resx from this repository, modify texts:
	```resx
	<!-- Only for values inside data tags! -->
	<data ...>
		<value>What to translate</value>
	</data>
	```
2: Add `.<your language code>` suffix to the file, after `Strings` and BEFORE `.resx`.
3: Send it to me or create an Issues where you attach your file.

Have any problems? Look at how other languages are available there.

#### Suggestions & Bug Report
Suggest new features or file bug reports to improve Charms Bar Port, [see here.](https://github.com/Icepenguins101/charms-bar-port/issues)

#### Spread the word
Star this repository, leave a review of the program anywhere on your website or share it to others that want the Windows 8.x experience back!
