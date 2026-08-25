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
}
