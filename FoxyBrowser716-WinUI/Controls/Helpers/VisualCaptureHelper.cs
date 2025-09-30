//TODO: get this working for WebViews somehow, ran out of time today to do it

using Windows.Graphics.Effects;

namespace FoxyBrowser716_WinUI.Controls.Helpers;

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Windows.Foundation;

public class VisualCaptureHelper
{
    private readonly Compositor _compositor;
    private readonly CompositionVisualSurface _visualSurface;
    private readonly CompositionSurfaceBrush _surfaceBrush;
    private readonly SpriteVisual _destinationVisual;
    
    public VisualCaptureHelper(FrameworkElement sourceElement, FrameworkElement destinationElement)
    {
        var sourceVisual = ElementCompositionPreview.GetElementVisual(sourceElement);
        _compositor = sourceVisual.Compositor;
        
        _visualSurface = _compositor.CreateVisualSurface();
        _visualSurface.SourceVisual = sourceVisual;
        
        _surfaceBrush = _compositor.CreateSurfaceBrush(_visualSurface);
        _surfaceBrush.Stretch = CompositionStretch.UniformToFill;
        
        _destinationVisual = _compositor.CreateSpriteVisual();
        _destinationVisual.Brush = _surfaceBrush;
        
        ElementCompositionPreview.SetElementChildVisual(destinationElement, _destinationVisual);
        
        UpdateSizes(sourceElement, destinationElement);
        
        sourceElement.SizeChanged += (s, e) => UpdateSizes(sourceElement, destinationElement);
        destinationElement.SizeChanged += (s, e) => UpdateSizes(sourceElement, destinationElement);
    }
    
    private void UpdateSizes(UIElement sourceElement, UIElement destinationElement)
    {
        var sourceSize = new Size(
            sourceElement.ActualSize.X,
            sourceElement.ActualSize.Y
        );
        
        var destSize = new Size(
            destinationElement.ActualSize.X,
            destinationElement.ActualSize.Y
        );
        
        _visualSurface.SourceSize = new Vector2(
            (float)sourceSize.Width,
            (float)sourceSize.Height
        );
        
        _destinationVisual.Size = new Vector2(
            (float)destSize.Width,
            (float)destSize.Height
        );
    }
    
    private void SetBlur(float blurAmount)
    {
        var blurEffect = new GaussianBlurEffect
        {
            Name = "Blur",
            BlurAmount = blurAmount,
            Source = new CompositionEffectSourceParameter("source")
        };
        
        var effectFactory = _compositor.CreateEffectFactory(blurEffect);
        var effectBrush = effectFactory.CreateBrush();
        effectBrush.SetSourceParameter("source", _surfaceBrush);
        
        _destinationVisual.Brush = effectBrush;
    }
    
    public void Dispose()
    {
        _surfaceBrush?.Dispose();
        _visualSurface?.Dispose();
        _destinationVisual?.Dispose();
    }
}

public partial class GaussianBlurEffect : IGraphicsEffect
{
    public string Name { get; set; }
    public float BlurAmount { get; set; }
    public IGraphicsEffectSource Source { get; set; }
}


public interface IGraphicsEffectSource
{
}

public class CompositionEffectSourceParameter : IGraphicsEffectSource
{
    public string Name { get; }
    
    public CompositionEffectSourceParameter(string name)
    {
        Name = name;
    }
}