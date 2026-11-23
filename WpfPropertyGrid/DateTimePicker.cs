namespace WpfPropertyGrid;
/// <summary>
/// Provides a date-and-time picker control for editing a DateTime value.
/// </summary>
public class DateTimePicker : DatePicker
{
    /// <summary>
    /// Identifies the dependency property for the selected date and time.
    /// </summary>
    public static readonly DependencyProperty SelectedDateTimeProperty =
        DependencyProperty.Register("SelectedDateTime", typeof(DateTime?), typeof(DateTimePicker),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, SelectedTimesChanged));
    /// <summary>
    /// Identifies the dependency property for the selectable time interval.
    /// </summary>
    public static readonly DependencyProperty TimeIntervalProperty =
        DependencyProperty.Register("TimeInterval", typeof(TimeSpan), typeof(DateTimePicker),
        new FrameworkPropertyMetadata(new TimeSpan(0, 15, 0), FrameworkPropertyMetadataOptions.AffectsRender, TimesChanged));
    /// <summary>
    /// Identifies the dependency property for the earliest selectable time.
    /// </summary>
    public static readonly DependencyProperty StartTimeProperty =
        DependencyProperty.Register("StartTime", typeof(TimeSpan), typeof(DateTimePicker),
        new FrameworkPropertyMetadata(new TimeSpan(0, 0, 0), FrameworkPropertyMetadataOptions.AffectsRender, TimesChanged));
    /// <summary>
    /// Identifies the dependency property for the latest selectable time.
    /// </summary>
    public static readonly DependencyProperty EndTimeProperty =
        DependencyProperty.Register("EndTime", typeof(TimeSpan), typeof(DateTimePicker),
        new FrameworkPropertyMetadata(new TimeSpan(1, 0, 0, 0), FrameworkPropertyMetadataOptions.AffectsRender, TimesChanged));

    private static void SelectedTimesChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
    {
        var dtp = (DateTimePicker)source;
        dtp.SelectClosestMatch();
    }

    private static void TimesChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
    {
        var dtp = (DateTimePicker)source;
        var tsi = dtp.TimeInterval;
        if (tsi <= TimeSpan.Zero)
        {
            tsi = new TimeSpan(0, 15, 0);
        }

        dtp._timeControl.Items.Clear();
        var ts = dtp.StartTime;
        do
        {
            dtp._timeControl.Items.Add(ts);
            ts += tsi;
        }
        while (ts < dtp.EndTime);
    }

    private readonly ListBox _timeControl;
    private System.Windows.Controls.Calendar? _calendar;
    private Popup? _popup;
    private TextBox? _textbox;
    private bool _initialSetup;
    private bool _handleEvents = true;
    /// <summary>
    /// Initializes a new instance of DateTimePicker.
    /// </summary>
    public DateTimePicker()
    {
        _timeControl = new ListBox();
        _timeControl.SelectionChanged += OnTimeControlSelectionChanged;
        _timeControl.ItemStringFormat = "{0:hh\\:mm}";
    }
    /// <summary>
    /// Gets or sets the selected time.
    /// </summary>
    public TimeSpan? SelectedTime
    {
        get
        {
            if (!SelectedDateTime.HasValue)
                return null;

            return SelectedDateTime.Value.TimeOfDay;
        }
        set
        {
            if (SelectedDateTime.HasValue)
            {
                SelectedDateTime = SelectedDateTime.Value.Date + value;
                return;
            }
            SelectedDateTime = DateTime.Now.Date + value;
        }
    }
    /// <summary>
    /// Gets or sets the selected date time.
    /// </summary>
    public DateTime? SelectedDateTime { get => (DateTime?)GetValue(SelectedDateTimeProperty); set => SetValue(SelectedDateTimeProperty, value); }
    /// <summary>
    /// Gets or sets the start time.
    /// </summary>
    public TimeSpan StartTime { get => (TimeSpan)GetValue(StartTimeProperty); set => SetValue(StartTimeProperty, value); }
    /// <summary>
    /// Gets or sets the end time.
    /// </summary>
    public TimeSpan EndTime { get => (TimeSpan)GetValue(EndTimeProperty); set => SetValue(EndTimeProperty, value); }
    /// <summary>
    /// Gets or sets the time interval.
    /// </summary>
    public TimeSpan TimeInterval { get => (TimeSpan)GetValue(TimeIntervalProperty); set => SetValue(TimeIntervalProperty, value); }

    /// <summary>
    /// Selects the available time value closest to the current date-and-time value.
    /// </summary>
    protected virtual void SelectClosestMatch()
    {
        if (_timeControl.Items.Count == 0 || !SelectedTime.HasValue)
        {
            _timeControl.SelectedIndex = -1;
            if (_timeControl.Items.Count > 0)
            {
                _timeControl.ScrollIntoView(0);
            }
            return;
        }

        TimeSpan? prev = null;
        foreach (var ts in _timeControl.Items.OfType<TimeSpan>())
        {
            if (ts > SelectedTime.Value)
            {
                if (prev.HasValue)
                {
                    _handleEvents = false;
                    _timeControl.SelectedItem = prev.Value;
                    _handleEvents = true;
                }
                else
                {
                    _timeControl.SelectedIndex = 0;
                }

                _timeControl.ScrollIntoView(_timeControl.SelectedItem);
                return;
            }
            prev = ts;
        }

        _timeControl.SelectedIndex = _timeControl.Items.Count - 1;
        _timeControl.ScrollIntoView(_timeControl.SelectedItem);
    }

    /// <summary>
    /// Updates the text box to reflect the selected date and time.
    /// </summary>
    protected virtual void UpdateTextbox()
    {
        if (SelectedTime.HasValue && SelectedDate.HasValue)
        {
            var newText = SelectedDateFormat switch
            {
                DatePickerFormat.Long => SelectedDate.Value.ToString(System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern),
                _ => SelectedDate.Value.ToString(System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern),
            };

            newText += " " + SelectedTime.Value;
            if (_textbox != null)
            {
                _textbox.Text = newText;
            }
        }
    }

    /// <summary>
    /// Updates the selected date and time after the time control selection changes.
    /// </summary>
    protected virtual void OnTimeControlSelectionChanged(object? sender, SelectionChangedEventArgs? e)
    {
        if (!_handleEvents)
            return;

        if (_timeControl.SelectedIndex >= 0)
        {
            SelectedTime = (TimeSpan)_timeControl.SelectedItem;
        }
        else
        {
            SelectedTime = null;
        }

        UpdateTextbox();
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!e.Handled)
        {
            switch (e.Key)
            {
                case Key.Space:
                case Key.Enter:
                    if (_popup != null)
                    {
                        _popup.IsOpen = false;
                    }
                    OnTimeControlSelectionChanged(null, null);
                    break;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnSelectedDateChanged(SelectionChangedEventArgs e)
    {
        base.OnSelectedDateChanged(e);
        if (SelectedDate.HasValue && SelectedDate.Value.TimeOfDay != TimeSpan.Zero)
        {
            SelectedTime = SelectedDate.Value.TimeOfDay;
        }
    }

    /// <inheritdoc/>
    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        TimesChanged(this, new DependencyPropertyChangedEventArgs());
    }

    /// <inheritdoc/>
    protected override void OnCalendarClosed(RoutedEventArgs e)
    {
        base.OnCalendarClosed(e);
        OnTimeControlSelectionChanged(null, null);
    }

    /// <inheritdoc/>
    protected override void OnCalendarOpened(RoutedEventArgs e)
    {
        base.OnCalendarOpened(e);
        if (SelectedDate.HasValue && (SelectedDate.Value.Year == 1 || SelectedDate.Value.Year == 9999))
        {
            DisplayDate = DateTime.Now;
        }

        if (_timeControl != null && _calendar != null)
        {
            // TODO: unhardcode
            _timeControl.Height = _calendar.RenderSize.Height - 6;
        }

        SelectedTimesChanged(this, new DependencyPropertyChangedEventArgs());
        OnTimeControlSelectionChanged(null, null);
    }
    /// <inheritdoc/>
    public override void OnApplyTemplate()
    {
        _textbox = GetTemplateChild("PART_TextBox") as TextBox;
        if (_textbox != null)
        {
            _textbox.TextChanged += (s, e) =>
            {
                if (!_initialSetup)
                {
                    UpdateTextbox();
                    _initialSetup = true;
                }
            };
        }

        base.OnApplyTemplate();
        _popup = GetTemplateChild("PART_Popup") as Popup;
        if (_popup != null)
        {
            _calendar = (System.Windows.Controls.Calendar)_popup.Child;
            _popup.Child = null;
            var grid = new Grid();
            _popup.Child = grid;

            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            _timeControl.VerticalAlignment = VerticalAlignment.Top;
            // TODO: unhardcode
            _timeControl.Margin = new Thickness(3);
            Grid.SetColumn(_timeControl, 1);

            grid.Children.Add(_calendar);
            grid.Children.Add(_timeControl);
        }
    }
}
