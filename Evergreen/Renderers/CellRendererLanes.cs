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
		public int laneWidth { get; set; } = 16;
		public int dotWidth { get; set; } = 10;
		public List<Reference> labels { get; set; }

		private delegate double DirectionFunc(double i);

		private int num_visible_lanes
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
			return (num_visible_lanes * laneWidth) + LabelRenderer.Width(widget, FontDesc, labels);
		}

		protected override void OnGetPreferredWidth(Gtk.Widget widget, out int minimum_width, out int natural_width)
		{
			base.OnGetPreferredWidth(widget, out minimum_width, out natural_width);

			var w = TotalWidth(widget);

			if (w > minimum_width)
			{
				minimum_width = w;
			}
		}

		private void draw_arrow(Cairo.Context context, Gdk.Rectangle area, uint laneidx, bool top)
		{
			var cw = laneWidth;
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

		private void draw_arrows(Cairo.Context context,
		                         Gdk.Rectangle area)
		{
			uint to = 0;

			foreach (var lane in Commit.GetLanes())
			{
				var color = lane.Color;
				context.SetSourceRGB(color.R, color.G, color.B);

				if (lane.Tag == LaneTag.START)
				{
					draw_arrow(context, area, to, true);
				}
				else if (lane.Tag == LaneTag.END)
				{
					draw_arrow(context, area, to, false);
				}

				++to;
			}
		}

		private void draw_paths_real(Cairo.Context context,
		                             Gdk.Rectangle area,
                                     CommitModel commit,
		                             DirectionFunc f,
		                             double yoffset)
		{
			if (commit == null)
			{
				return;
			}

			var to = 0;
			var cw = laneWidth;
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

		private void draw_top_paths(Cairo.Context context,
		                            Gdk.Rectangle area,
		                            DirectionFunc f)
		{
			draw_paths_real(context, area, Commit, f, -1);
		}

		private void draw_bottom_paths(Cairo.Context context,
		                               Gdk.Rectangle area,
		                               DirectionFunc f)
		{
			draw_paths_real(context, area, NextCommit, f, 1);
		}

		private void draw_paths(Cairo.Context context,
		                        Gdk.Rectangle area,
		                        DirectionFunc f)
		{
			context.LineWidth = 2.0;
			context.LineCap = Cairo.LineCap.Round;

			context.Save();

			draw_top_paths(context, area, f);
			draw_bottom_paths(context, area, f);
			draw_arrows(context, area);

			context.Restore();
		}

		private void draw_indicator(Cairo.Context context,
		                            Gdk.Rectangle area,
		                            DirectionFunc f)
		{
			double offset;
			double radius;

			offset = (Commit.MyLane * laneWidth) + ((laneWidth - dotWidth) / 2.0);
			radius = dotWidth / 2.0;

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

		private void draw_labels(Cairo.Context context,
		                         Gtk.Widget    widget,
		                         Gdk.Rectangle area)
		{
			var offset = (int)(num_visible_lanes * laneWidth);

			var rtl = (widget.StyleContext.State & Gtk.StateFlags.DirRtl) != 0;

			if (rtl)
			{
				offset = -offset;
			}

			context.Save();
			context.Translate(offset, 0);
			LabelRenderer.draw(widget, FontDesc, context, labels, area);
			context.Restore();
		}

		private void draw_lane(Cairo.Context context,
		                       Gtk.Widget    widget,
		                       Gdk.Rectangle area)
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

			draw_paths(context, area, f);
			draw_indicator(context, area, f);

			context.Restore();
		}

		protected new void Render(
            Cairo.Context context, Gtk.Widget widget, Gdk.Rectangle area,
            Gdk.Rectangle cell_area, Gtk.CellRendererState flags)
		{
			var ncell_area = cell_area;
			var narea = area;

			var rtl = (widget.StyleContext.State & Gtk.StateFlags.DirRtl) != 0;

			if (Commit != null)
			{
				context.Save();

				Gdk.CairoHelper.Rectangle(context, area);
				context.Clip();

				draw_lane(context, widget, area);
				draw_labels(context, widget, area);

				var tw = TotalWidth(widget);

				if (!rtl)
				{
					narea.X += (int)tw;
					ncell_area.X += (int)tw;
				}
				else
				{
					narea.Width -= (int)tw;
					ncell_area.Width -= (int)tw;
				}

				context.Restore();
			}

			// if (rtl == (Pango..find_base_dir(text, -1) != Pango.Direction.Rtl))
			// {
				Xalign = 1.0f;
			// }

			base.Render(context, widget, narea, ncell_area, flags);
		}

		public Reference get_ref_at_pos(Gtk.Widget widget, int x, int cell_w, out int hot_x)
		{
			var rtl = (widget.StyleContext.State & Gtk.StateFlags.DirRtl) != 0;
			var offset = (int)(labels.Count * laneWidth);

			if (rtl)
			{
				x = cell_w - x;
			}

			return LabelRenderer.GetRefAtPos(widget, FontDesc, labels, x - offset, out hot_x);
		}
	}
}
