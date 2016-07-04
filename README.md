# ParaUnity

This project illustrates the combination of ParaView and Virtual Reality by using the Unity gaming engine.

It is possible to either export your rendered Data from ParaView into a running (and prepared) Unity Project or
visualize it in a separate Unity Player Window

## Development Setup (Windows)

The information listed here is specific to Windows machines. However, the source code is cross platform compatible
and the development setup on other systems should be quite similar (and even less problematic).

### Required Components

Um die ParaView-Erweiterung zu übersetzen bzw. weiterzuentwickeln sind folgende \pbw{Entwicklungswerkzeuge} notwendig:

| Software                                                                          | Recommended Version |
| --------------------------------------------------------------------------------- | ------------------- |
| [Unity SDK](https://unity3d.com/get-unity)                                        | \>= 5.3.2f1         |
| [CMake](https://cmake.org/download)                                               | \>= 3               |
| [Visual Studio](https://www.visualstudio.com/en-us/news/vs2013-community-vs.aspx) | 2013                |
| [Qt](https://download.qt.io/archive/qt/4.8)                                       | 4.8                 |
| [ParaView](http://www.paraview.org/download)                                      | 5                   |

**Warning:** Make sure you use the same Visual Studio version in all your build targets / products. Mixing up different
versions of the Visual Studio runtime is the main source for runtime failures.

### Compilation


1. **Compile Qt (optional):** unpack the Qt source code to your preferred location and run the following commands inside a
   Visual Studio command prompt:

   ```
   ./configure.exe -prefix "<QT_DIR>" -debug-and-release -nomake examples -nomake tests -nomake demos -opensource -confirm-license  -platform win32-msvc2013
   nmake
   nmake install
   ```

   The last command will copy all binaries into `<QT_DIR>`.

   You may need to add `<QT_DIR>\bin` into the `PATH` environment variable

   This step is not required unless you want to build the ParaView plugin with Qt 4.8 and Visual Studio 2013

2. [**Compile ParaView:**](http://www.paraview.org/Wiki/ParaView:Build_And_Install) unpack the ParaView source code to your preferred location and run the following commands inside
   a terminal (e.g. cmd.exe):

   ```
   mkdir build
   cd build
   cmake -G "Visual Studio 12 2013" -DQT_QMAKE_EXECUTABLE="<QT_DIR>\bin\qmake.exe"  ..
   nmake
   ```

3. **Compile the ParaView plugin:** Navigate to the subfolder `ParaView/Unity3DPlugin` and run the folowing commands
  inside a terminal (e.g. cmd.exe):

   ```
   mkdir build
   cd build
   cmake -G "Visual Studio 12 2013" -DParaView_DIR="<PARAVIEW_DIR>\build" ..
   nmake
   ```

   `PARAVIEW_DIR` is the directory where you unpacked and compiled ParaView source reside.
4. **Compile the Unity Player component:** Navigate to the subfolder `Unity/ParaViewEmbeddedPlayer`
   And open the project with the Unity Editor.

   In the Editor UI, click `File -> Build Settings... -> Build` to build
   the player executable.

   Make sure you use `unity_player` as name for the executable / bundle and copy it to same location where
   the ParaView plugin resides (i.g. the same folder where the plugin .dll file is located).
