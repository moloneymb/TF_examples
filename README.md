# Tensorflow Examples in F#

List of examples
* Resnet50 for image classification

## Resnet50
Steps to run:
* Make sure FSI is 64bit and does not shadow assemblies<sup>1</sup>
* Packet Install
* Build ./src/Resnet50/Resnet50.fsproj
* Run script ./src/Resnet50/script.fsx in F# interactive


### 1: Making sure FSI is 64bit and does not shadow
For Visual Studio Code:
* On Ubuntu: it should work with default settings
* On Windows: if FSI is defaulting to 32bit this will need to be changed to 64bit by setting the Ionide property for fsiFilePath to the full path to the fsiAnyCpu.exe.

`"FSharp.fsiFilePath": "C:\..fsiAnyCpu.exe"`

* On Mac: this is currently untested

For Visual Studio both 64bit needs to be enabled and shadow copy assemblies needs to be switched off. Tools -> Options -> F# Tools -> F# Interactive





