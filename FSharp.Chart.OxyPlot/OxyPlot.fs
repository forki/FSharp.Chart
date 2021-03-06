﻿module FSharp.Chart.OxyPlot

open System
open System.Drawing

open OxyPlot

open FSharp.Chart

module Color =
    let from (x : System.Drawing.Color) = OxyColor.FromArgb(x.A, x.R, x.G, x.B)

module AxisPosition =
    let from =
        function
        | AxisPosition.Bottom -> Axes.AxisPosition.Bottom
        | AxisPosition.Left   -> Axes.AxisPosition.Left
        | AxisPosition.Top    -> Axes.AxisPosition.Top
        | AxisPosition.Right  -> Axes.AxisPosition.Right

module Axis =
    let from (x : Axis) : Axes.Axis =
        let axis =
            match x.AxisType with
            | Categorical -> Axes.CategoryAxis() :> Axes.Axis
            | DateTime    -> Axes.DateTimeAxis() :> Axes.Axis
            | Linear      -> Axes.LinearAxis  () :> Axes.Axis
            | TimeSpan    -> Axes.TimeSpanAxis() :> Axes.Axis

        axis.Title      <- x.Title.Value
        axis.TitleColor <- x.Title.Color  |> Color.from
        axis.TextColor  <- x.TextColor    |> Color.from
        axis.Position   <- x.AxisPosition |> AxisPosition.from

        x.Minimum |> Option.iter (fun x -> axis.Minimum <- x)
        x.Maximum |> Option.iter (fun x -> axis.Maximum <- x)

        axis

module Series =
    let private toFloats =
        function
        | FloatData    xs -> xs
        | DateTimeData xs -> xs |> Array.map Axes.DateTimeAxis.ToDouble
        | TimeSpanData xs -> xs |> Array.map Axes.TimeSpanAxis.ToDouble

    let private toDataPoints =
        function
        | Data1D        ys  -> ys |> toFloats |> Array.mapi (fun i y -> OxyPlot.DataPoint(float i, y))
        | Data1x1D (xs, ys) ->
            let xs = xs |> toFloats
            let ys = ys |> toFloats
            xs |> Array.mapi (fun i x -> OxyPlot.DataPoint(x, ys.[i]))
        | Data2D xys ->
            match xys with
            | FloatFloat       xys -> xys |> Array.map (fun (x, y) -> OxyPlot.DataPoint(                           x,                            y))
            | FloatDateTime    xys -> xys |> Array.map (fun (x, y) -> OxyPlot.DataPoint(                           x, Axes.DateTimeAxis.ToDouble y))
            | FloatTimeSpan    xys -> xys |> Array.map (fun (x, y) -> OxyPlot.DataPoint(                           x, Axes.TimeSpanAxis.ToDouble y))
            | DateTimeFloat    xys -> xys |> Array.map (fun (x, y) -> OxyPlot.DataPoint(Axes.DateTimeAxis.ToDouble x,                            y))
            | DateTimeDateTime xys -> xys |> Array.map (fun (x, y) -> OxyPlot.DataPoint(Axes.DateTimeAxis.ToDouble x, Axes.DateTimeAxis.ToDouble y))
            | DateTimeTimeSpan xys -> xys |> Array.map (fun (x, y) -> OxyPlot.DataPoint(Axes.DateTimeAxis.ToDouble x, Axes.TimeSpanAxis.ToDouble y))
            | TimeSpanFloat    xys -> xys |> Array.map (fun (x, y) -> OxyPlot.DataPoint(Axes.TimeSpanAxis.ToDouble x,                            y))
            | TimeSpanDateTime xys -> xys |> Array.map (fun (x, y) -> OxyPlot.DataPoint(Axes.TimeSpanAxis.ToDouble x, Axes.DateTimeAxis.ToDouble y))
            | TimeSpanTimeSpan xys -> xys |> Array.map (fun (x, y) -> OxyPlot.DataPoint(Axes.TimeSpanAxis.ToDouble x, Axes.TimeSpanAxis.ToDouble y))

    let private toErrorItems =
        function
        | TupleOfArrays (xs, ys) ->
            let xs = xs |> toFloats
            let ys = ys |> toFloats
            xs |> Array.mapi (fun i x -> OxyPlot.Series.ErrorColumnItem(x, ys.[i]))
        | ArrayOfTuples xys ->
            match xys with
            | FloatFloat       xys -> xys |> Array.map (fun (x, y) -> OxyPlot.Series.ErrorColumnItem(                           x,                            y))
            | FloatDateTime    xys -> xys |> Array.map (fun (x, y) -> OxyPlot.Series.ErrorColumnItem(                           x, Axes.DateTimeAxis.ToDouble y))
            | FloatTimeSpan    xys -> xys |> Array.map (fun (x, y) -> OxyPlot.Series.ErrorColumnItem(                           x, Axes.TimeSpanAxis.ToDouble y))
            | DateTimeFloat    xys -> xys |> Array.map (fun (x, y) -> OxyPlot.Series.ErrorColumnItem(Axes.DateTimeAxis.ToDouble x,                            y))
            | DateTimeDateTime xys -> xys |> Array.map (fun (x, y) -> OxyPlot.Series.ErrorColumnItem(Axes.DateTimeAxis.ToDouble x, Axes.DateTimeAxis.ToDouble y))
            | DateTimeTimeSpan xys -> xys |> Array.map (fun (x, y) -> OxyPlot.Series.ErrorColumnItem(Axes.DateTimeAxis.ToDouble x, Axes.TimeSpanAxis.ToDouble y))
            | TimeSpanFloat    xys -> xys |> Array.map (fun (x, y) -> OxyPlot.Series.ErrorColumnItem(Axes.TimeSpanAxis.ToDouble x,                            y))
            | TimeSpanDateTime xys -> xys |> Array.map (fun (x, y) -> OxyPlot.Series.ErrorColumnItem(Axes.TimeSpanAxis.ToDouble x, Axes.DateTimeAxis.ToDouble y))
            | TimeSpanTimeSpan xys -> xys |> Array.map (fun (x, y) -> OxyPlot.Series.ErrorColumnItem(Axes.TimeSpanAxis.ToDouble x, Axes.TimeSpanAxis.ToDouble y))

    let private toBoxPlotItems (xs : BoxPlotItem[]) =
        xs
        |> Array.mapi
            (
                fun i x ->
                    OxyPlot.Series.BoxPlotItem
                        (
                            float (i + 1),

                            x.LowerWhisker,
                            x.BoxBottom,
                            x.Median,
                            x.BoxTop,
                            x.UpperWhisker,

                            Mean     = x.Mean,
                            Outliers = x.Outliers
                        )
            )

    let private bar width color xs =
        let xs = xs |> Array.map (fun value -> OxyPlot.Series.BarItem(value))
        let s = OxyPlot.Series.BarSeries(FillColor = color, BarWidth = width, ItemsSource = xs)
        s :> OxyPlot.Series.XYAxisSeries

    let private boxplot color xs =
        OxyPlot.Series.BoxPlotSeries(Fill = color, ItemsSource = xs)
        :> OxyPlot.Series.XYAxisSeries

    let private column width color xs =
        let xs = xs |> Array.map (fun value -> OxyPlot.Series.ColumnItem(value))
        let s = OxyPlot.Series.ColumnSeries(FillColor = color, ColumnWidth = width, ItemsSource = xs)
//        s.Items.AddRange (xs |> Array.map (fun d -> OxyPlot.Series.ColumnItem(d)))
        s :> OxyPlot.Series.XYAxisSeries

    let private errorColumn width color (xs : OxyPlot.Series.ErrorColumnItem[]) =
        let s = OxyPlot.Series.ErrorColumnSeries(FillColor = color, ColumnWidth = width, ItemsSource = xs)
//        s.Items.AddRange (xs |> Array.map (fun d -> OxyPlot.Series.ErrorColumnItem(d)))
        s :> OxyPlot.Series.XYAxisSeries

    let private scatter color (xs : DataPoint[]) =
        OxyPlot.Series.ScatterSeries(MarkerFill = color, ItemsSource = xs, DataFieldX = "X", DataFieldY = "Y")
        :> OxyPlot.Series.XYAxisSeries

    let convert color x =
        match x with
        | Bar         (data, width) -> bar         width color (toFloats       data     )
        | BoxPlot      data         -> boxplot           color (toBoxPlotItems data     )
        | Column      (data, width) -> column      width color (toFloats       data     )
        | ErrorColumn (data, width) -> errorColumn width color (toErrorItems   data.Data)
        | Scatter      data         -> scatter           color (toDataPoints   data.Data)

    let from (xAxes : Axes.Axis[]) (yAxes : Axes.Axis[]) (x : Series) =

        let ensureAxisKeys baseKey (axes : Axes.Axis[]) =
            axes |> Array.iteri (fun n axis -> if axis.Key = null then axis.Key <- sprintf "%s.%d" baseKey n)

        ensureAxisKeys "X" xAxes
        ensureAxisKeys "Y" yAxes

        let series = x.SeriesData |> convert (Color.from x.Color)
        if x.XAxisIndex >= 0 then series.XAxisKey <- xAxes.[x.XAxisIndex].Key
        if x.YAxisIndex >= 0 then series.YAxisKey <- yAxes.[x.YAxisIndex].Key

        series

module PlotModel =
    let private setPositionTiers (xs : Axes.Axis[]) =

        let filterByPosition (p : Axes.AxisPosition) (xs : Axes.Axis[]) =
            xs |> Array.filter (fun x -> x.Position = p)

        let setPositionTier (xs : Axes.Axis[]) =
            xs |> Array.iteri (fun n x -> x.PositionTier <- n)

        xs |> filterByPosition Axes.AxisPosition.Top    |> setPositionTier
        xs |> filterByPosition Axes.AxisPosition.Left   |> setPositionTier
        xs |> filterByPosition Axes.AxisPosition.Bottom |> setPositionTier
        xs |> filterByPosition Axes.AxisPosition.Right  |> setPositionTier

        xs

    let from (chart : Chart) =

        let plotModel =
            PlotModel
                (
                    Title = chart.Title.Value,
                    Subtitle = chart.Subtitle.Value,
                    Background = Color.from chart.Background,
                    PlotAreaBackground = Color.from chart.PlotAreaBackground
                )

        let xAxes = chart.XAxes |> Array.map Axis.from |> setPositionTiers
        xAxes |> Array.iter plotModel.Axes.Add

        let yAxes = chart.YAxes |> Array.map Axis.from |> setPositionTiers
        yAxes |> Array.iter plotModel.Axes.Add

        chart.Series |> Array.iter (Series.from xAxes yAxes >> plotModel.Series.Add)

        plotModel
