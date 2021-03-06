
## RailEssentials - MIT License

[![Build and UnitTests](https://github.com/cbries/railessentials/actions/workflows/msbuild.yml/badge.svg)](https://github.com/cbries/railessentials/actions/workflows/msbuild.yml)
[![Deploy Nightly](https://github.com/cbries/railessentials/actions/workflows/dailyBuild.yml/badge.svg)](https://github.com/cbries/railessentials/actions/workflows/dailyBuild.yml)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

<a href="https://www.buymeacoffee.com/cbries" target="_blank"><img src="https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png" alt="Buy Me A Coffee" style="height: 41px !important;width: 174px !important;box-shadow: 0px 3px 2px 0px rgba(190, 190, 190, 0.5) !important;-webkit-box-shadow: 0px 3px 2px 0px rgba(190, 190, 190, 0.5) !important;" ></a>

**~~RailwayEssentials~~** is renamed to **RailEssentials**.

**[`RailEssentials`](http://www.railessentials.net)** is a modern and intuitive software for creating and controling
model railways. Currently, the only supported control unit 
is [ESU's ECoS 50210/50200](http://www.esu.eu/produkte/digitale-steuerung/ecos-50210-zentrale/was-ecos-kann/). 
The software is based on a client-server architecture, i.e the server is the brain and bridge between the control unit
and any conntected webclient. The server is written in C# and .NET Framework and the client is written in JavaScript/TypeScript and can be
used in [Chrome](https://www.google.com/chrome/), [Firefox](https://www.mozilla.org/en-US/firefox/new/), 
and [Vivaldi](https://vivaldi.com/). 

# Table of Contents
- [Tutorial: Welcome / First Steps](#tutorial-welcome--first-steps)
- [Daily/Nightly Builds](#dailynightly-builds)
- [Releases](#releases) 
- [Who is Using It?](#who-is-using-it)
- [Contribute](#contribute)
- [Book](#book)
- [Features](#features)
- [Visual Impressions](#visual-impressions)
- [Wiki/Documentation](https://github.com/cbries/railessentials/wiki/Table-of-Content)

<br>

# Demonstration of RailEssentials (i.e. YouTube video, no sound)

I have created a small video of my modelrailway in my basement to demonstrate that `RailEssentials` just work. Just click the preview to jump directly to YouTube.

[![RailEssentials - Live demonstration with real hardware in my basement.](https://img.youtube.com/vi/ieJDzE32IBk/0.jpg)](https://youtu.be/ieJDzE32IBk)

# Tutorial: Welcome / First Steps

For making your first steps easier with `RailEssentials`, a welcome tutorial has been provided. 

It is part of the [Wiki Page](https://github.com/cbries/railessentials/wiki/Tutorial-Welcome)

<br>

# Daily/Nightly Builds

## Setup/Installer

`RailEssentials` is automatically build everyday. During this build an always up-to-date Setup/Installer is generated and can be used to deploy the latest features on your personal computer. Currently, only Windows is supported, for other operating system the source must be used and build. The latest setup/installer is provided under the `Release`tab: [`Daily Setup`](https://github.com/cbries/railessentials/releases/tag/dailybuild)
- download the Setup of your choice (e.g. `Setup-20210322-4d9eb4.exe`, where `20210322` stands for 22nd March 2021)
- doubleblick the Setup and follow the instructions

![Setup Screen Capture](docs/assets/captures/RailEssentials2.gif)

## Portable Zip-Archives

`RailEssentials` is automatically build everyday. The always latest version is provided under the `Release` tab: [`Daily Build`](https://github.com/cbries/railessentials/releases/tag/dailybuild)
- download the archive `railessentials-dailybuild-*.zip`
- after unzip just call the included script `startRailEssentials.bat`

As default the workspace `Basement` is loaded and provided and can be used directly in simulation mode.

![Locomotives2]

<br>

# Releases

We do not provide any official releases yet, because the software is still under development, but we provide a [`Daily Build`](https://github.com/cbries/railessentials/releases/tag/dailybuild). In January'21 the RailEssentials team decided to do a full rewrite. To use this software, checkout the source, and just click "Build and Run (F5)" in VisualStudio.

<br>

# Contribute

## Quick Start for Developer

- install [`VisualStudio 2019`](https://visualstudio.microsoft.com/vs/) or the old variant [`VisualStudio 2017`](https://visualstudio.microsoft.com/vs/older-downloads/)
- clone the RailEssentials repository `git clone https://github.com/cbries/railessentials.git` or download the latest archive [master.zip](https://github.com/cbries/railessentials/archive/master.zip)
- open the solution `railessentials.sln`
- set `railessentials` as _Startup Project_
- click `F5` or call `VisualStudio Toolbar / Debug / Start Debugging`

As a result the software will build and all relevant files are copied to the `Output Directory`, in general this is `railessentials \ bin \ Debug|Release`. `railessentials.exe` will be executed, a command prompt opens and shows some status information. If not browser opens automatially, go to your browser and open `http://localhost:8081/?workspace=Basement`.

Finally, you should see something like this:
![firstImpressionsAfterBuild]

[firstImpressionsAfterBuild]: docs/assets/img/firstImpressionAfterBuild.png "Welcome View of RailEssentials after Build and Run"

## System Requirements

- Windows 10
- .NET Framework 4.8 or higher
- `Chrome`, `Firefox` or `Vivaldi`

## Bug Tracking

Have a bug or a feature request? Please open an issue here [https://github.com/cbries/railessentials/issues](https://github.com/cbries/railessentials/issues). Please make sure that the same issue was not previously submitted by someone else.

## Third-Party Components at Their Best

- [`w2ui`](http://w2ui.com/web/) JavaScript UI Library for the Modern Web
- [`WebSocket4Net`](https://github.com/kerryjiang/WebSocket4Net) A popular .NET WebSocket Client
- [`SuperSocket`](https://github.com/kerryjiang/SuperSocket) SuperSocket is a light weight extensible socket application framework.
- [`Newtonsoft`](https://www.newtonsoft.com/json) Popular high-performance JSON framework for .NET
- [`jQuery`](https://jquery.com/) jQuery is a fast, small, and feature-rich JavaScript library.
- [`jQuery colorpicker`](https://github.com/vanderlee/colorpicker) A full-featured colorpicker for jQueryUI with full theming support.
- [`jQuery Boxfit`](https://github.com/michikono/boxfit) Boxfit is a jQuery plugin for resizing text.
- [`Contextual.js`](https://github.com/LucasReade/Contextual.js) Javascript contextual menu library
- [`select2`](https://github.com/select2/select2) Select2 is a jQuery based replacement for select boxes. It supports searching, remote data sets, and infinite scrolling of results.
- [`FontAwesome`](https://fontawesome.com/) Get vector icons and social logos on your website with Font Awesome, the web's most popular icon set and toolkit.
- [`emgucv`](https://github.com/emgucv/emgucv) A cross platform .Net wrapper for the Open CV image-processing library.

<br>

# Book

In the late of 2018 I wrote a book about programming the ESU's ECoS 50210.

[![Book](docs/assets/img/ecosbook.jpg)](https://www.christianbenjaminries.de/ecos) 

* ISBN: **978-1-790-36403-9**    
* ASIN (Kindle): **B07L4Z4MYL**

The book introduces the basics of managing commands for remote control of your model railway by use of C# and .NET Framework/.net Core. The included examples and API information are all used in [`ecos`](https://github.com/cbries/ecos), a software library hosted on GitHub which is completly used by `RailEssentials` (with few improvements and bug fixes). The source is completly merged into `RailEssentials`, i.e. check out the subdirectory [`ecoslibNet48`](https://github.com/cbries/railessentials/tree/master/ecoslibNet48).

<br>

# Features

## Ui
 - *Modern Web Frontend* based on HTML5, JavaScript, Websocket-Communication
 - *Frontend UI usable with Chrome, Firefox, Vivaldi, Edge* on any device
 - *Server-based* architecture (currently the server is only running on Windows)
 - *WebCam/Video* support included by default for monitoring hidden yards (i.e. includes stream server and web viewer) [Wiki](https://github.com/cbries/railessentials/wiki/RailEssentials'-WebCam-Video-Surveilance-for-Fiddle-Yards)
 - *Import of Rocrail* plans [Wiki](https://github.com/cbries/railessentials/wiki/Import-RocRail-plan.xml)
 - *Workspace* support, to switch between different plan at any time
 - *Track Designer* based on modern web technology
 - *Toolbox-support* with drag&drop
 - *Plan Item* transformation directly by single clicks (i.e. move, rotate, remove)
 - *Text Item* support (i.e. bold, italic, underline, font size, font color)
 - *Labels* for Plan Items
 - *Accessory* can be changed directly by clicking the individual plan item at any time
 - *Output/Button* grouping [Wiki](https://github.com/cbries/railessentials/wiki/Group-Buttons-or-Output)
 - *Report* for Locomotives, Accessories, and Sensors [Wiki](https://github.com/cbries/railessentials/wiki/Report-of-Locomotives,-Accessories,-and-Sensors), [Issue #9](https://github.com/cbries/railessentials/issues/95)

## Routing

 - *Block-based* automatic mode, any train/locomotive can be controlled by a smart algorithmen and can travel between blocks without crossing routes
 - *Accessory state change* during AutoMode for individual routes [Wiki](https://github.com/cbries/railessentials/wiki/Adding-Signals-to-Routes-used-in-AutoMode)
 - *Route detection* between blocks, any feasible route will be found and provided for your individual setup
 - *Routes with direction* are recognized
 - *Route* disabling/enabling on-demand
 - *Block* disabling/enabling on-demand
 
## Feedback

- *S88 Viewer*

## Locomotives
 
 - *Locomotive* lock/unlock
 - *Locomotive* direct control via mouse over controls in the plan
 - *Locomotive* speed curve editor (i.e. individual acceleration/deceleration)
 - *Locomotive* image visualization [Wiki](https://github.com/cbries/railessentials/wiki/Adding-Locomotive-Images-Photos)
 - *Drag&Drop of Locomotives* to Blocks for assignment
 - *Locomotive Options* for `AutoMode` (e.g. change of direction, different kind of trains, etc.)
 - *SpeedLevel* support like in [RocRail](https://wiki.rocrail.net/doku.php?id=loc-speed-en) (e.g. `v_min` is `Level 1`, `v_mid` is `Level 2`, `v_cruise` is `Level 3`, and `v_max` is `Level 4`) [Issue #80](https://github.com/cbries/railessentials/issues/80)
 - *Kick Start* to provide an additional nudge to start the motor during `AutoMode` [Issue #90](https://github.com/cbries/railessentials/issues/90)
 
## AutoMode

 - *Auto Mode* of the whole model railway, work on your landscape during run of all your trains
 - *Simulation Mode* to test any functionality
 - *Block entering* visualization, see directly on which side the locomotive has entered the block and on which side it will leave the block 
 - *Statistics* of accessory usage and when any Locomotive has been started/stopped (e.g. supports scheduled maintenance)
 - *Accessory to Route* weaving for changing accessory states on start/delay of Locomotive traveling [Wiki](https://github.com/cbries/railessentials/wiki/Adding-Signals-to-Routes-used-in-AutoMode)
 - *Lock Additional Blocks during Locomotive Traveling* [Wiki](https://github.com/cbries/railessentials/wiki/Lock-Additional-Blocks-during-Locomotive-Traveling)
 - *Maintenance Mode* - accessories can be set into maintenance mode and all routes which include the relevant accessories are not selected anymore [Issue #79](https://github.com/cbries/railessentials/issues/79)
 - *Wait for Locomotives* if AutoMode has stopped, i.e. finish all current journeys [Issue #75](https://github.com/cbries/railessentials/issues/75)
 - *Drag & Drop* of locomotives to assign a new block [Issue #100](https://github.com/cbries/railessentials/issues/100), [Wiki](https://github.com/cbries/railessentials/wiki/AutoMode-By-DragAndDrop)
 - *Cleaning Robot / Cleaner* mode, specific Locomotive have full access to all blocks/routes, can change direction at any time. [Issue #20](https://github.com/cbries/railessentials/issues/20)
 - *Define Wait-period* of locomotives when they reach target block of a route. [Issue #107](https://github.com/cbries/railessentials/issues/107)
 - *Ghost Train* detection [Issue #104](https://github.com/cbries/railessentials/issues/104)

## Individual Supports for my personal needs

 - *Light Control* (i.e. currently hardcoded variant for my personal model railway, it is based on an invidual solution with LED-RGB-Stripes and Websocket-communication [modeling repository](https://github.com/cbries/modeling/tree/master/Konfigurationen/Beleuchtung-RGB-Stripe/ESP8266_LedStripe_WS)).

<br>

# Visual Impressions

## Locomotive Control

![LocomotivesControl]

![LocomotivesView]

![LocomotivesSpeedCurve]

## Accessories

![Accessories]

## WebCam/Video Surveilance for Fiddle Yards

![WebCam]

## Blocks and S88

![BlocksS88]

![S88Viewer]

## Routing

![Routes]

![RoutesAnalyzing]

## Edit

![Labels]

![Toolbox]

![WorkspaceSelection]

[Locomotives]: Screenshots/Impressions/RailEssentials-Locomotives.png "Locomotives View and Handling"
[Locomotives2]: Screenshots/Impressions/RailEssentials-Locomotives02.png "Locomotives View and Handling"
[LocomotivesControl]: Screenshots/Impressions/RailEssentials-LocomotivesControl.png "Locomotives Control directly in the Plan"
[LocomotivesView]: Screenshots/Impressions/RailEssentials-LocomotivesView.png "Locomotives View"
[LocomotivesSpeedCurve]: Screenshots/Impressions/RailEssentials-SpeedCurve.png "Locomotives SpeedCurve"


[Accessories]: Screenshots/Impressions/RailEssentials-Accessories.png "Accessories"

[WebCam]: Screenshots/Impressions/railessentials01.png "WebCam0"

[BlocksS88]: Screenshots/Impressions/RailEssentials-BlocksS88.png "Blocks and S88"
[S88Viewer]: Screenshots/Impressions/RailEssentials-S88Viewer.png "S88 Viewer"

[Routes]: Screenshots/Impressions/RailEssentials-Routes.png "Routes"
[RoutesAnalyzing]: Screenshots/Impressions/RailEssentials-RoutesAnalyzing.png "Routes Analyzing"

[Labels]: Screenshots/Impressions/RailEssentials-Labels.png "Labels in Track"
[Toolbox]: Screenshots/Impressions/RailEssentials-Toolbox.png "Toolbox to create any Plan individually"
[WorkspaceSelection]: Screenshots/Impressions/RailEssentials-WorkspaceSelection.png "Workspace Selection"
