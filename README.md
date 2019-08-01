## GLWpfControl
![Nuget](https://img.shields.io/nuget/v/OpenTK.GLWpfControl.svg?color=green)

A native control for WPF in OpenTK. 

(Probably) faster than GLControl, and solves [the airspace problem](https://stackoverflow.com/questions/8006092/controls-dont-show-over-winforms-host).

## Getting started:

1. [Install via NuGet](https://www.nuget.org/packages/OpenTK.GLWpfControl)
2. In your window, include GLWpfControl.
    ```XML
        <Window 
            ...
            xmlns:glWpfControl="clr-namespace:OpenTK.Wpf;assembly=GLWpfControl"
            ... >    
    ```
3. Add the control into a container (Grid, StackPanel, etc.) and add a handler dethod to the render event.

    ```XML
        <Grid>
            ...
            <glWpfControl:GLWpfControl 
                x:Name="OpenTkControl" 
                Render="OpenTkControl_OnRender"/>
            ...
        </Grid>
    ```
4. In the code behind add to the constructor, after the InitializeComponents call, a call to the start method of the GLWpfControl.
    ```CS
    public MainWindow() {
            InitializeComponent();
            // [...]
            var settings = new GLWpfControlSettings
            {
                MajorVersion = 3,
                MinorVersion = 6
            };
            OpenTkControl.Start(settings);
    }
    ```
5. You can now render in the OnRender handler.
    ```CS
    private void OpenTkControl_OnRender(TimeSpan delta) {
        GL.ClearColor(Color4.Blue);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }
    ```
For additional examples, see [MainWindow.xaml](https://github.com/varon/GLWpfControl/blob/master/src/GLWpfControlExample/MainWindow.xaml#L11) and [MainWindow.xaml.cs](https://github.com/varon/GLWpfControl/blob/master/src/GLWpfControlExample/MainWindow.xaml.cs#L18) in the example project.



## Build instructions

1. Clone repository 
    ```shell
    $ git clone https://github.com/varon/GLWpfControl.git
    $ cd GLWpfControl
    ```
    or for SSH 
    ```shell
    $ git clone git@github.com:varon/GLWpfControl.git
    $ cd GLWpfControl
    ```
2. Run `build.cmd` or `build.sh`.
3. Develop as normal in whatever IDE you like.


## Planned features

#### Hardware rendering using [NV_DX_interop](https://www.khronos.org/registry/OpenGL/extensions/NV/WGL_NV_DX_interop.txt):

Currently a work in progress. Contributions welcome!


