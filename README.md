## GLWpfControl - A fast OpenGL Control for WPF
![Nuget](https://img.shields.io/nuget/v/OpenTK.GLWpfControl.svg?color=green)

A native control for WPF in OpenTK 3.x and 4.x.

Supported configurations:
- .Net Framework for OpenTK 3.x (the 3.x series NuGet packages)
- .Net Core for OpenTK 4.x (the 4.x series NuGet packages)

Since version 3.0.0, we're using full OpenGL/DirectX interop via OpenGL extensions - [NV_DX_interop](https://www.khronos.org/registry/OpenGL/extensions/NV/WGL_NV_DX_interop.txt).
This should run almost everywhere with **AMAZING PERFORMANCE** and is fully supported on Intel, AMD and Nvidia graphics.

This offers a way more clean solution than embedding GLControl and totally solves [the airspace problem](https://stackoverflow.com/questions/8006092/controls-dont-show-over-winforms-host).
As controls can be layered, nested and structured over your 3D view.

## Getting started:

1. [Install via NuGet](https://www.nuget.org/packages/OpenTK.GLWpfControl)
2. In your window, include GLWpfControl.
    ```XML
        <Window 
            ...
            xmlns:glWpfControl="clr-namespace:OpenTK.Wpf;assembly=GLWpfControl"
            ... >    
    ```
3. Add the control into a container (Grid, StackPanel, etc.) and add a handler method to the render event.

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
For additional examples, see [MainWindow.xaml](https://github.com/opentk/GLWpfControl/blob/master/src/Example/MainWindow.xaml) and [MainWindow.xaml.cs](https://github.com/opentk/GLWpfControl/blob/master/src/Example/MainWindow.xaml.cs) in the example project.

### I can't receive keyboard input when my control doesn't have keyboard focus!

WPF by design only sends keyboard events to the control that has keybaord focus. To be able to get keyboard focus a control needs to have `Focusable==true` (this is the default for GLWpfControl) and `IsVisible==true`.

If you however need to get keyboard events idependent of keyboard focus you will have to use the `Keyboard.AddPreview*` functions.
These functions allow you to register a preview event that is called before the control with keyboard focus gets the keyboard event.

This replaces the old `CanInvokeOnHandledEvents` and `RegisterToEventsDirectly` settings.

See [Example](./src/Example/TabbedMainWindowTest.xaml.cs) for an example how to set this up.

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

#### DX-Hijacking rendering

It's possible to bypass the RTT that takes place in WPF D3dImage by stealing the actual D3d handle from WPF and drawing manually.
This is incredibly challenging, but would offer APEX performance as zero indirection is required.
Currently more of an idea than a work in progress.
Contributions welcome - Drop by the [Discord](https://discord.gg/6HqD48s) server if you want to give this a shot!
