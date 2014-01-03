Description
===========

This is a tactical planning tool (like Advanced Tactical Center) but specifically for World of Tanks players! You can load any maps of WoT, then draw your tactic with tools like in paint. If you want to animate every tank movements in real time then just add some tanks and drag them to the position what you want.

Official home page
------------------

<http://tacticplanner.rutkai.hu/>

Developer notes
===============

XML files
---------

All tanks and maps can be found in the XML files, if you want to add new ones, you have to add them to the XML files too!

The icon ID-s (in maps.xml) can be found in the icons.xml file.

The icon coordinates (in maps.xml) can be written out by the VS. You have to run the project within the VS in debug mode, add a static icon, drag it, then you can see the coordinates in the Output window.

Required programs
-----------------

* Microsoft Visual Studio 2012 or newer
* NSIS <http://nsis.sourceforge.net/>

Libraries
---------

The project includes the following libraries:

* AvalonDock <http://avalondock.codeplex.com/>
* Extended WPF Toolkit <https://wpftoolkit.codeplex.com/>
* WriteableBitmapEx <http://writeablebitmapex.codeplex.com/>

Debugging
---------

In order to run the project within the VS you have to copy the content of the installer folder (except *.nsi files) in the bin/debug folder.

Making installer
----------------

1. Compile a release executable
2. Copy/move it to the installer folder
3. Compile installer.nsi file

Versions
--------

Don't forget to increase the executable version if you make a new version.