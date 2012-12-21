# Factions Launcher

The legacy game launcher for Factions. It handles updating and installing the game.

![Screenshot](http://i.minus.com/ibjlQAYQQ1C67v.PNG)

## Server Requirements

The launcher works with a standard HTTP server such as Apache or nginx.

## Components

The launcher is divided into two projects; the Redress library and the Factions Launcher application.

### Redress

Redress is a general-purpose application patching library.

A patch operation begins by downloading a manifest file from a server. The manifest contains a list of files in the application version. The library then checks for existing files in the application directory. If the checksum of the existing file does not match with the manifest, a new version of the file is downloaded.

### Factions Launcher

The launcher itself is a WPF application that uses Redress as a data provider.