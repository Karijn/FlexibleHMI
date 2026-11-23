namespace FunctionGenerator;

/// <summary>Waveform shapes and noise distributions supported by <see cref="SignalGenerator"/>.</summary>
public enum SignalWaveform
{
    /// <summary>A rising ramp that wraps at the end of each cycle.</summary>
    Sawtooth,
    /// <summary>A sine wave.</summary>
    Sinus,
    /// <summary>A cosine wave.</summary>
    Cosinus,
    /// <summary>A square wave with a positive first half-cycle and negative second half-cycle.</summary>
    Block,
    /// <summary>A triangle wave that rises and falls once per cycle.</summary>
    Triangle,
    /// <summary>Uniform white noise in the range from -1 to 1.</summary>
    WhiteNoise,
    /// <summary>Gaussian white noise with standard-normal samples.</summary>
    GaussianNoise,
    /// <summary>Pink noise produced by a multi-row filtered random sequence.</summary>
    PinkNoise,
    /// <summary>Brown noise produced by a smoothed random sequence.</summary>
    BrownNoise
}

/// <summary>Produces scaled periodic and noise waveform samples.</summary>
public static class SignalGenerator
{
    /// <summary>Determines whether the selected waveform is one of the noise waveforms.</summary>
    /// <param name="waveform">Waveform to inspect.</param>
    /// <returns><see langword="true"/> for white, Gaussian, pink, or brown noise; otherwise <see langword="false"/>.</returns>
    public static bool IsNoise(SignalWaveform waveform) => waveform is
        SignalWaveform.WhiteNoise or
        SignalWaveform.GaussianNoise or
        SignalWaveform.PinkNoise or
        SignalWaveform.BrownNoise;

    /// <summary>Calculates a waveform sample for a phase and applies amplitude and offset.</summary>
    /// <param name="waveform">Waveform shape or noise distribution.</param>
    /// <param name="phase">Phase in cycles; periodic waveforms wrap any finite value into the interval [0, 1).</param>
    /// <param name="amplitude">Scale applied to the normalized waveform sample.</param>
    /// <param name="offset">Value added after amplitude scaling.</param>
    /// <param name="noiseGenerator">Stateful source required when <paramref name="waveform"/> is a noise waveform.</param>
    /// <returns>The generated sample after scaling and offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The phase, amplitude, or offset is not finite, or the waveform is unsupported.</exception>
    /// <exception cref="ArgumentNullException">A noise waveform was selected without a noise generator.</exception>
    /// <exception cref="OverflowException">The scaled sample is not finite.</exception>
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

/// <summary>Maintains the random state used to produce noise waveform samples.</summary>
public sealed class SignalNoiseGenerator
{
    private const int PinkNoiseRows = 16;
    private readonly Random _random = new();
    private readonly double[] _pinkRows = new double[PinkNoiseRows];
    private uint _pinkCounter;
    private double _brownValue;
    private bool _hasGaussianSample;
    private double _gaussianSample;

    /// <summary>Produces the next sample for a supported noise waveform.</summary>
    /// <param name="waveform">White, Gaussian, pink, or brown noise.</param>
    /// <returns>The next sample for the requested distribution.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="waveform"/> is not a supported noise waveform.</exception>
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
