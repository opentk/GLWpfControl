## GLWpfControl - A fast OpenGL Control for WPF
![Nuget](https://img.shields.io/nuget/v/OpenTK.GLWpfControl.svg?color=green)

A native control for WPF in OpenTK 3.x and 4.x.

Supported configurations:
- .Net Framework for OpenTK 3.x (the 3.x series NuGet packages)
- .Net Core for OpenTK 4.x (the 4.x series NuGet packages)

Since version 3.0.0, we're using full OpenGL/DirectX interop via OpenGL extensions - [NV_DX_interop](https://www.khronos.org/registry/OpenGL/extensions/NV/WGL_NV_DX_interop.txt). This should run almost everywhere with **AMAZING PERFORMANCE** and is fully supported on Intel, AMD and Nvidia graphics.

This offers a way more clean solution than embedding GLControl and totally solves [the airspace problem](https://stackoverflow.com/questions/8006092/controls-dont-show-over-winforms-host). As controls can be layered, nested and structured over your 3D view.

This package is intended to supercede the legacy *GLControl* completely, and we strongly encourage upgrading to this native WPF control instead.

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

### I'm having trouble with Keyboard and Mouse Input!?

The current design has some issues based around polling for keyboard and mouse input due to the way the control was initially designed.

If you want to handle keyboard input for the control when it is not focused, this is a feature we're currently looking into fixing. However, if you just want to handle keyboard input when the control has focus there's a little additional logic that must be implemented:

1. Before calling `Start()` add the following to ensure that you can hook onto events via the control:

```csharp
this.glWpfControl.RegisterToEventsDirectly = false;
this.glWpfControl.CanInvokeOnHandledEvents = false;

this.glWpfControl.MouseDown += this.GlWpfControl_MouseDown;
this.glWpfControl.MouseEnter += this.GlWpfControl_MouseEnter;
this.glWpfControl.MouseLeave += this.GlWpfControl_MouseLeave;
```

2. Next, in the mouse event handlers you want to call `Focus()` for the control:

```csharp
    private void GlWpfControl_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        // When the mouse is leaving, lose focus so the keyboard cannot invoke events.
        var scope = FocusManager.GetFocusScope(this.glWpfControl);
        FocusManager.SetFocusedElement(scope, null);
        System.Windows.Input.Keyboard.ClearFocus();
    }

    private void GlWpfControl_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        this.glWpfControl.Focus();
    }

    private void GlWpfControl_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        this.glWpfControl.Focus();
    }
```

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

It's possible to bypass the RTT that takes place in WPF D3dImage by stealing the actual D3d handle from WPF and drawing manually. This is incredibly challenging, but would offer APEX performance as zero indirection is required. Currently more of an idea than a work in progress. Contributions welcome - Drop by the [Discord](https://discord.gg/6HqD48s) server if you want to give this a shot!
