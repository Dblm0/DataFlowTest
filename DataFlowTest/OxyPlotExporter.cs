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
            var seqSeries = new LineSeries { };
            i = 0;
            seqSeries.Points.AddRange(measData.Sequence.Select(x => new DataPoint(i++, x)));
            seqModel.Series.Add(seqSeries);

            using var delaysPngStream = new MemoryStream();
            using var seqPngStream = new MemoryStream();

            PngExporter.Export(delaysModel, delaysPngStream, 600, 400);
            PngExporter.Export(seqModel, seqPngStream, 600, 400);

            using var b1 = new Bitmap(Image.FromStream(delaysPngStream));
            using var b2 = new Bitmap(Image.FromStream(seqPngStream));
            using var result = new Bitmap(600, 800);
            using var g = Graphics.FromImage(result);
            g.DrawImage(b1, 0, 0);
            g.DrawImage(b2, 0, 400);
            result.Save(fileName);
        }
    }
}
