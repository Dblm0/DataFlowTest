using OxyPlot;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace DataFlowTest
{
    static class OxyPlotExporter
    {
        public static void ToPNG(string fileName, string plotTitle, params IEnumerable<double>[] dataCollection)
        {
            var model = new PlotModel { Title = plotTitle };
            foreach (var data in dataCollection)
            {
                var line = new LineSeries { };
                model.Series.Add(line);
                int i = 0;
                line.Points.AddRange(data.Select(x => new DataPoint(i++, x)));
            }

            PngExporter.Export(model, fileName, 2 * 600, 2 * 400);
        }
        public static void ToPNG(string fileName, string plotTitle, MeasurmentsData measData)
        {
            int resX = 1280;
            int resY = 1024;

            var delaysModel = new PlotModel { Title = plotTitle };
            int i;
            foreach (var data in measData.Delays.Values)
            {
                var line = new LineSeries { };
                delaysModel.Series.Add(line);
                i = 0;
                line.Points.AddRange(data.Select(x => new DataPoint(i++, x)));
            }

            var seqModel = new PlotModel { Title = "Sequence" };
            var indexedSeq = measData.Sequence.Select((x, idx) => (idx, x));

            foreach (var group in indexedSeq.GroupBy(x => x.x))
            {
                var seqSeries = new LinearBarSeries { };
                seqSeries.Points.AddRange(group.Select(x => new DataPoint(x.idx, 1)));
                seqModel.Series.Add(seqSeries);
            }

            using var delaysPngStream = new MemoryStream();
            using var seqPngStream = new MemoryStream();

            PngExporter.Export(delaysModel, delaysPngStream, resX, resY / 2);
            PngExporter.Export(seqModel, seqPngStream, resX, resY / 2);

            using var b1 = new Bitmap(Image.FromStream(delaysPngStream));
            using var b2 = new Bitmap(Image.FromStream(seqPngStream));
            using var result = new Bitmap(resX, resY);
            using var g = Graphics.FromImage(result);
            g.DrawImage(b1, 0, 0);
            g.DrawImage(b2, 0, resY / 2);
            result.Save(fileName);
        }
    }
}
