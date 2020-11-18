using OxyPlot;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    }
}
