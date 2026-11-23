namespace WpfPropertyGrid.Utilities;
/// <summary>
/// Provides helpers for configuring WPF binding trace output.
/// </summary>
public static class WpfTracing
{
    /// <summary>
    /// Enables or disables WPF binding trace output.
    /// </summary>
    public static void Enable(SourceLevels levels = SourceLevels.Warning, TraceListener? listener = null)
    {
        listener ??= new DefaultTraceListener();
        PresentationTraceSources.Refresh();
        foreach (var property in typeof(PresentationTraceSources).GetProperties(BindingFlags.Static | BindingFlags.Public))
        {
            if (property.Name == "FreezableSource")
                continue;

            if (typeof(TraceSource).IsAssignableFrom(property.PropertyType))
            {
                if (property.GetValue(null, null) is TraceSource source)
                {
                    source.Listeners.Add(listener);
                    source.Switch.Level = levels;
                }
            }
        }
    }
}
