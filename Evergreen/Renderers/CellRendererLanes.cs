using System;
using System.Collections.Generic;

using Evergreen.Lib.Git.Models;

using GLib;

using LibGit2Sharp;

namespace Evergreen.Renderers
{
    public class CellRendererLanes : Gtk.CellRendererText
	{
        [Property("commit")]
		public CommitModel Commit { get; set; }
		public CommitModel NextCommit { get; set; }
		public int LaneWidth { get; set; } = 16;
		public int DotWidth { get; set; } = 10;
		public List<Reference> Labels { get; set; }

		private delegate double DirectionFunc(double i);

		private int NumVisibleLanes
		{
			get
			{
				var ret = 0;
				var trailing_hidden = 0;

				foreach (var lane in Commit.GetLanes())
				{
					++ret;

					if ((lane.Tag & LaneTag.HIDDEN) != 0)
					{
						trailing_hidden++;
					}
					else
					{
						trailing_hidden = 0;
					}
				}

				return ret - trailing_hidden;
			}
		}

		private int TotalWidth(Gtk.Widget widget)
		{
			return (NumVisibleLanes * LaneWidth) + LabelRenderer.Width(widget, FontDesc, Labels);
		}

		protected override void OnGetPreferredWidth(Gtk.Widget widget, out int minimumWidth, out int naturalWidth)
		{
			base.OnGetPreferredWidth(widget, out minimumWidth, out naturalWidth);

			var w = TotalWidth(widget);

			if (w > minimumWidth)
			{
				minimumWidth = w;
			}
		}

		private void DrawArrow(Cairo.Context context, Gdk.Rectangle area, uint laneidx, bool top)
		{
			var cw = LaneWidth;
			var xpos = area.X + (laneidx * cw) + (cw / 2.0);
			var df = (top ? -1 : 1) * 0.25 * area.Height;
			var ypos = area.Y + (area.Height / 2.0) + df;
			var q = cw / 4.0;

			context.MoveTo(xpos - q, ypos + (top ? q : -q));
			context.LineTo(xpos, ypos);
			context.LineTo(xpos + q, ypos + (top ? q : -q));
			context.Stroke();

			context.MoveTo(xpos, ypos);
			context.LineTo(xpos, ypos - df);
			context.Stroke();
		}

		private void DrawArrows(Cairo.Context context,
		                         Gdk.Rectangle area)
		{
			uint to = 0;

			foreach (var lane in Commit.GetLanes())
			{
				var color = lane.Color;
				context.SetSourceRGB(color.R, color.G, color.B);

				if (lane.Tag == LaneTag.START)
				{
					DrawArrow(context, area, to, true);
				}
				else if (lane.Tag == LaneTag.END)
				{
					DrawArrow(context, area, to, false);
				}

				++to;
			}
		}

		private void DrawPathsReal(
            Cairo.Context context, Gdk.Rectangle area,
             CommitModel commit, DirectionFunc f, double yoffset)
		{
			if (commit == null)
			{
				return;
			}

			var to = 0;
			var cw = LaneWidth;
			var ch = area.Height / 2.0;

			foreach (var lane in commit.GetLanes())
			{
				if ((lane.Tag & LaneTag.HIDDEN) != 0)
				{
					++to;
					continue;
				}

				var color = lane.Color;
				context.SetSourceRGB(color.R, color.G, color.B);

				foreach (var from in lane.From)
				{
					var x1 = area.X + f((from * cw) + (cw / 2.0));
					var x2 = area.X + f((to * cw) + (cw / 2.0));
					var y1 = area.Y + (yoffset * ch);
					var y2 = area.Y + ((yoffset + 1) * ch);
					var y3 = area.Y + ((yoffset + 2) * ch);

					context.MoveTo(x1, y1);
					context.CurveTo(x1, y2, x2, y2, x2, y3);
					context.Stroke();
				}

				++to;
			}
		}

		private void DrawTopPaths(Cairo.Context context, Gdk.Rectangle area, DirectionFunc f)
		{
			DrawPathsReal(context, area, Commit, f, -1);
		}

		private void DrawBottomPaths(Cairo.Context context, Gdk.Rectangle area, DirectionFunc f)
		{
			DrawPathsReal(context, area, NextCommit, f, 1);
		}

		private void DrawPaths(Cairo.Context context, Gdk.Rectangle area, DirectionFunc f)
		{
			context.LineWidth = 2.0;
			context.LineCap = Cairo.LineCap.Round;

			context.Save();

			DrawTopPaths(context, area, f);
			DrawBottomPaths(context, area, f);
			DrawArrows(context, area);

			context.Restore();
		}

		private void DrawIndicator(Cairo.Context context, Gdk.Rectangle area, DirectionFunc f)
		{
			double offset;
			double radius;

			offset = (Commit.MyLane * LaneWidth) + ((LaneWidth - DotWidth) / 2.0);
			radius = DotWidth / 2.0;

			context.LineWidth = 0.0;

			context.Arc(area.X + f(offset + radius),
			            area.Y + (area.Height / 2.0),
			            radius,
			            0,
			            2 * Math.PI);

			context.SetSourceRGB(0, 0, 0);
			context.StrokePreserve();

			if (Commit.Lane != null)
			{
				var color = Commit.Lane.Color;
				context.SetSourceRGB(color.R, color.G, color.B);
			}

			context.Fill();
		}

		private void DrawLabels(Cairo.Context context, Gtk.Widget widget, Gdk.Rectangle area)
		{
			var offset = NumVisibleLanes * LaneWidth;

			var rtl = (widget.StyleContext.State & Gtk.StateFlags.DirRtl) != 0;

			if (rtl)
			{
				offset = -offset;
			}

			context.Save();
			context.Translate(offset, 0);
			LabelRenderer.Draw(widget, FontDesc, context, Labels, area);
			context.Restore();
		}

		private void DrawLane(Cairo.Context context, Gtk.Widget widget, Gdk.Rectangle area)
		{
			DirectionFunc f;

			var rtl = (widget.StyleContext.State & Gtk.StateFlags.DirRtl) != 0;

			context.Save();

			if (rtl)
			{
				context.Translate(area.Width, 0);
				f = (a) => -a;
			}
			else
			{
				f = (a) => a;
			}

			DrawPaths(context, area, f);
			DrawIndicator(context, area, f);

			context.Restore();
		}

		protected new void Render(
            Cairo.Context context, Gtk.Widget widget, Gdk.Rectangle area,
            Gdk.Rectangle cellArea, Gtk.CellRendererState flags)
		{
			var ncell_area = cellArea;
			var narea = area;

			var rtl = (widget.StyleContext.State & Gtk.StateFlags.DirRtl) != 0;

			if (Commit != null)
			{
				context.Save();

				Gdk.CairoHelper.Rectangle(context, area);
				context.Clip();

				DrawLane(context, widget, area);
				DrawLabels(context, widget, area);

				var tw = TotalWidth(widget);

				if (!rtl)
				{
					narea.X += tw;
					ncell_area.X += tw;
				}
				else
				{
					narea.Width -= tw;
					ncell_area.Width -= tw;
				}

				context.Restore();
			}

			// if (rtl == (Pango..find_base_dir(text, -1) != Pango.Direction.Rtl))
			// {
				Xalign = 1.0f;
			// }

			base.Render(context, widget, narea, ncell_area, flags);
		}

		public Reference GetRefAtPos(Gtk.Widget widget, int x, int cellW, out int hotX)
		{
			var rtl = (widget.StyleContext.State & Gtk.StateFlags.DirRtl) != 0;
			var offset = Labels.Count * LaneWidth;

			if (rtl)
			{
				x = cellW - x;
			}

			return LabelRenderer.GetRefAtPos(widget, FontDesc, Labels, x - offset, out hotX);
		}
	}
}
