using AudioConverter.Core.Remix;
using AudioConverter.Infrastructure.Remix;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class RemixFilterBuilderTests
{
    [TestMethod]
    public void BuildGraph_UsesHighQualityTempoThenConvolutionReverb()
    {
        RemixEffect[] rack = [new TempoPitchEffect(0.85), new ReverbEffect(0.28, 2.4)];

        var graph = RemixFilterBuilder.BuildGraph(rack, 44_100, 120, preview: true, reverbInputIndexes: [1]);

        StringAssert.Contains(graph.Graph, "asetrate=37485,aresample=44100:filter_size=64:phase_shift=10");
        StringAssert.Contains(graph.Graph, "[1:a]aresample=44100[ir0]");
        StringAssert.Contains(graph.Graph, "asplit=2[dry0][wetin0]");
        StringAssert.Contains(graph.Graph, "afir=dry=1:wet=1");
        StringAssert.Contains(graph.Graph, "amix=inputs=2:weights='0.72 0.28':normalize=0");
        StringAssert.Contains(graph.Graph, "alimiter=limit=0.84:level=false");
        Assert.AreEqual("remixout", graph.OutputLabel);
        Assert.IsFalse(graph.Graph.Contains("aecho", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Build_CreatesBassEqAndVolumeFilters()
    {
        RemixEffect[] rack =
        [
            new BassEffect(8, 90),
            new EqualizerEffect(2, -1, 3),
            new VolumeEffect(-2),
        ];

        var filter = RemixFilterBuilder.Build(rack, 48_000, 100, preview: false);

        Assert.AreEqual(
            "bass=g=8:f=90:w=0.5,equalizer=f=100:width_type=q:w=1:g=2,equalizer=f=1000:width_type=q:w=1:g=-1,equalizer=f=10000:width_type=q:w=1:g=3,volume=-2dB",
            filter);
    }

    [TestMethod]
    public void Build_CalculatesFadeOutFromRemixedDuration()
    {
        RemixEffect[] rack = [new TempoPitchEffect(0.8), new FadeOutEffect(5)];

        var filter = RemixFilterBuilder.Build(rack, 44_100, 120, preview: false);

        StringAssert.EndsWith(filter, "afade=t=out:st=145:d=5");
    }

    [TestMethod]
    public void Build_UsesMeasuredLoudnessForFinalExport()
    {
        RemixEffect[] rack = [new LoudnessNormalizeEffect(-14)];
        var measurement = new LoudnessMeasurements(-20, -18, 5, -1, 1.5);

        var filter = RemixFilterBuilder.Build(rack, 44_100, 120, preview: false, measurement);

        StringAssert.Contains(filter, "measured_I=-20");
        StringAssert.Contains(filter, "offset=1.5");
        StringAssert.Contains(filter, "linear=true");
    }

    [TestMethod]
    public void Build_CreatesMasteringFilters()
    {
        RemixEffect[] rack =
        [
            new HighPassEffect(35),
            new LowPassEffect(18_000),
            new CompressorEffect(-18, 3, 2),
            new StereoWidthEffect(1.2),
            new SoftLimiterEffect(-1),
        ];

        var filter = RemixFilterBuilder.Build(rack, 48_000, 100, preview: false);

        StringAssert.Contains(filter, "highpass=f=35");
        StringAssert.Contains(filter, "lowpass=f=18000");
        StringAssert.Contains(filter, "acompressor=threshold=0.126:ratio=3:attack=20:release=250:makeup=1.259");
        StringAssert.Contains(filter, "stereotools=mlev=1:slev=1.2");
        StringAssert.Contains(filter, "alimiter=limit=0.891:level=false");
    }

    [TestMethod]
    public void Build_FallsBackToSinglePassForInfiniteSilenceMeasurements()
    {
        RemixEffect[] rack = [new LoudnessNormalizeEffect(-14)];
        var measurement = new LoudnessMeasurements(double.NegativeInfinity, double.NegativeInfinity, 0, -70, double.PositiveInfinity);

        var filter = RemixFilterBuilder.Build(rack, 44_100, 10, preview: false, measurement);

        Assert.AreEqual("loudnorm=I=-14:TP=-1.5:LRA=11", filter);
    }

    [TestMethod]
    public void Build_CreatesExtremeDistortionAndBitCrusherFilters()
    {
        RemixEffect[] rack = [new DistortionEffect(30, 1, 4), new BitCrusherEffect(3, 16, 1), new SoftLimiterEffect(-0.1)];

        var filter = RemixFilterBuilder.Build(rack, 44_100, 120, preview: true);

        StringAssert.Contains(filter, "aformat=channel_layouts=stereo,volume=30dB,asoftclip=type=hard:threshold=1:output=1:oversample=4");
        StringAssert.Contains(filter, "acrusher=bits=3:samples=16:mix=1:mode=lin:aa=0");
        StringAssert.EndsWith(filter, "alimiter=limit=0.989:level=false");
    }
}
