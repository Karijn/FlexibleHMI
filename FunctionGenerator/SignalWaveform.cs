namespace FunctionGenerator;

public enum SignalWaveform
{
    Sawtooth,
    Sinus,
    Cosinus,
    Block,
    Triangle,
    WhiteNoise,
    GaussianNoise,
    PinkNoise,
    BrownNoise
}

public static class SignalGenerator
{
    public static bool IsNoise(SignalWaveform waveform) => waveform is
        SignalWaveform.WhiteNoise or
        SignalWaveform.GaussianNoise or
        SignalWaveform.PinkNoise or
        SignalWaveform.BrownNoise;

    public static double Generate(
        SignalWaveform waveform,
        double phase,
        double amplitude = 1,
        double offset = 0,
        SignalNoiseGenerator? noiseGenerator = null)
    {
        if (!double.IsFinite(phase))
        {
            throw new ArgumentOutOfRangeException(nameof(phase), "Phase must be finite.");
        }

        if (!double.IsFinite(amplitude))
        {
            throw new ArgumentOutOfRangeException(nameof(amplitude), "Amplitude must be finite.");
        }

        if (!double.IsFinite(offset))
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be finite.");
        }

        phase %= 1;
        if (phase < 0)
        {
            phase += 1;
        }

        var normalizedValue = waveform switch
        {
            SignalWaveform.Sawtooth => 2 * phase - 1,
            SignalWaveform.Sinus => Math.Sin(2 * Math.PI * phase),
            SignalWaveform.Cosinus => Math.Cos(2 * Math.PI * phase),
            SignalWaveform.Block => phase < 0.5 ? 1 : -1,
            SignalWaveform.Triangle => 1 - 4 * Math.Abs(phase - 0.5),
            SignalWaveform.WhiteNoise or SignalWaveform.GaussianNoise or SignalWaveform.PinkNoise or SignalWaveform.BrownNoise =>
                (noiseGenerator ?? throw new ArgumentNullException(nameof(noiseGenerator), "Noise waveforms require a noise generator."))
                    .Generate(waveform),
            _ => throw new ArgumentOutOfRangeException(nameof(waveform), waveform, "Unsupported waveform.")
        };

        var value = offset + amplitude * normalizedValue;
        if (!double.IsFinite(value))
        {
            throw new OverflowException("The waveform settings produced a non-finite value.");
        }

        return value;
    }
}

public sealed class SignalNoiseGenerator
{
    private const int PinkNoiseRows = 16;
    private readonly Random _random = new();
    private readonly double[] _pinkRows = new double[PinkNoiseRows];
    private uint _pinkCounter;
    private double _brownValue;
    private bool _hasGaussianSample;
    private double _gaussianSample;

    public double Generate(SignalWaveform waveform) => waveform switch
    {
        SignalWaveform.WhiteNoise => NextUniform(),
        SignalWaveform.GaussianNoise => NextGaussian(),
        SignalWaveform.PinkNoise => NextPink(),
        SignalWaveform.BrownNoise => NextBrown(),
        _ => throw new ArgumentOutOfRangeException(nameof(waveform), waveform, "The selected waveform is not a noise function.")
    };

    private double NextUniform() => 2 * _random.NextDouble() - 1;

    private double NextGaussian()
    {
        if (_hasGaussianSample)
        {
            _hasGaussianSample = false;
            return _gaussianSample;
        }

        var first = Math.Max(_random.NextDouble(), double.Epsilon);
        var second = _random.NextDouble();
        var magnitude = Math.Sqrt(-2 * Math.Log(first));
        var angle = 2 * Math.PI * second;
        _gaussianSample = magnitude * Math.Sin(angle);
        _hasGaussianSample = true;
        return magnitude * Math.Cos(angle);
    }

    private double NextPink()
    {
        var row = 0;
        var counter = ++_pinkCounter;
        while ((counter & 1) == 0 && row < PinkNoiseRows - 1)
        {
            row++;
            counter >>= 1;
        }

        _pinkRows[row] = NextUniform();
        var sum = _pinkRows.Sum();
        var normalized = sum / Math.Sqrt(PinkNoiseRows);
        return Math.Clamp(normalized, -1, 1);
    }

    private double NextBrown()
    {
        const double smoothing = 0.02;
        _brownValue = (1 - smoothing) * _brownValue + smoothing * NextUniform();
        return Math.Clamp(_brownValue * Math.Sqrt((2 - smoothing) / smoothing * 3), -1, 1);
    }
}
