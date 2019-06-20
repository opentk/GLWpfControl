## GLWpfControl

A native control for WPF in OpenTK. 

(Probably) faster than GLControl, and solves [the airspace problem](https://stackoverflow.com/questions/8006092/controls-dont-show-over-winforms-host).

## Usage:

1. [Install via NuGet](https://www.nuget.org/packages/OpenTK.GLWpfControl)
2. See [MainWindow.xaml](https://github.com/varon/GLWpfControl/blob/master/src/GLWpfControlExample/MainWindow.xaml#L11) and [MainWindow.xaml.cs](https://github.com/varon/GLWpfControl/blob/master/src/GLWpfControlExample/MainWindow.xaml.cs#L18) in the example project.



## Build instructions

1. Run `build.cmd` or `build.sh`.
2. Develop as normal in whatever IDE you like.


## Planned features

#### Hardware rendering using [NV_DX_interop](https://www.khronos.org/registry/OpenGL/extensions/NV/WGL_NV_DX_interop.txt):

Currently a work in progress. Contributions welcome!


