﻿using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Wpf;
using System;
using System.Diagnostics;
using System.Windows.Input;

namespace Example
{
    /// <summary>
    /// Interaction logic for TabbedMainWindowTest.xaml
    /// </summary>
    public sealed partial class TabbedMainWindowTest
    {
        // FIXME: Make this example make use of context sharing...
        ExampleScene scene1 = new ExampleScene();
        ExampleScene scene2 = new ExampleScene();
        ExampleScene scene3 = new ExampleScene();

        public TabbedMainWindowTest()
        {
            InitializeComponent();
            GLWpfControlSettings mainSettings = new GLWpfControlSettings { MajorVersion = 4, MinorVersion = 1, Profile = ContextProfileMask.ContextCompatibilityProfileBit, ContextFlags = GraphicsContextFlags.Debug };
            // Start() makes the controls context current.
            Control1.Start(mainSettings);
            // We call Context.MakeCurrent() to make this explicitly clear.
            Control1.MakeCurrent();
            scene1.Initialize();

            GLWpfControlSettings insetSettings = new GLWpfControlSettings { MajorVersion = 4, MinorVersion = 1, Profile = ContextProfileMask.ContextCompatibilityProfileBit, ContextFlags = GraphicsContextFlags.Debug, Samples = 8, ContextToUse = Control1.Context, WindowInfo = Control1.WindowInfo };
            Control2.Start(insetSettings);
            Control2.MakeCurrent();
            scene2.Initialize();

            GLWpfControlSettings transparentSettings = new GLWpfControlSettings { MajorVersion = 4, MinorVersion = 1, Profile = ContextProfileMask.ContextCompatibilityProfileBit, ContextFlags = GraphicsContextFlags.Debug, TransparentBackground = true };
            Control3.Start(transparentSettings);
            Control3.MakeCurrent();
            scene3.Initialize();

            Control1.KeyDown += Control1_KeyDown;

            Keyboard.AddPreviewKeyDownHandler(this, Keyboard_PreviewKeyDown);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // The order here is important as the lifetime of the context Control2 uses is tied to the
            // lifetime of Control1, so we can't dispose the context from Control1 before we dispose Control2.
            Control2.Dispose();
            Control1.Dispose();
            Control3.Dispose();
        }

        private void Keyboard_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Debug.WriteLine($"Preview key down: {e.Key}");
        }

        private void Control1_KeyDown(object sender, KeyEventArgs e)
        {
            Debug.WriteLine(e.Key);

            if (e.Key == Key.A)
            {
                Control1.Dispose();
            }
        }

        private void Control1_OnRender(TimeSpan delta)
        {
            scene1.Render();
        }

        private void Control2_OnRender(TimeSpan delta)
        {
            scene2.Render();
        }

        private void Control3_OnRender(TimeSpan delta)
        {
            scene3.Render(0.0f);
        }
    }
}
